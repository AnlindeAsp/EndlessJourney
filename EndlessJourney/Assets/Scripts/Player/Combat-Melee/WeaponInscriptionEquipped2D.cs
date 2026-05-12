using System;
using System.Collections.Generic;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Stores one inscription id for each weapon id and resolves them through WeaponInscriptionLibrary2D.
    /// </summary>
    public class WeaponInscriptionEquipped2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponInscriptionLibrary2D inscriptionLibrary;
        [SerializeField] private WeaponEquipped2D weaponEquipped;

        [Header("Weapon Inscriptions")]
        [SerializeField] private WeaponInscriptionStateEntry2D[] weaponInscriptions = Array.Empty<WeaponInscriptionStateEntry2D>();
        [SerializeField] private bool requireInscriptionKnownForEquip = true;

        [Header("Record")]
        [SerializeField] private bool loadFromRecordOnAwake = true;
        [SerializeField] private bool saveToRecordOnChange = true;
        [SerializeField] private string recordFileName = "record.json";
        [SerializeField] private bool prettyPrintRecordJson = true;

        private readonly Dictionary<string, string> _inscriptionByWeaponId = new Dictionary<string, string>(16);
        private string _recordPath;

        public string EquippedInscriptionId => GetInscriptionIdForWeapon(GetCurrentWeaponId());

        public event Action<string> OnEquippedInscriptionChanged;
        public event Action<string, string> OnWeaponInscriptionChanged;

        private void Reset()
        {
            weaponEquipped = GetComponent<WeaponEquipped2D>();
        }

        private void Awake()
        {
            _recordPath = PlayerRecordStore2D.GetRecordPath(recordFileName);
            RebuildRuntimeIndexFromSerializedEntries();
            TryLoadEquippedStateFromRecord();
        }

        private void OnEnable()
        {
            if (weaponEquipped != null)
            {
                weaponEquipped.OnEquippedWeaponChanged += HandleEquippedWeaponChanged;
            }
        }

        private void OnDisable()
        {
            if (weaponEquipped != null)
            {
                weaponEquipped.OnEquippedWeaponChanged -= HandleEquippedWeaponChanged;
            }
        }

        /// <summary>
        /// Equips an inscription on the currently equipped weapon.
        /// </summary>
        public bool EquipInscription(string inscriptionId)
        {
            return EquipInscriptionForWeapon(GetCurrentWeaponId(), inscriptionId);
        }

        public bool EquipInscriptionForWeapon(string weaponId, string inscriptionId)
        {
            string normalizedId = string.IsNullOrWhiteSpace(inscriptionId) ? string.Empty : inscriptionId.Trim();
            string normalizedWeaponId = NormalizeId(weaponId);
            if (string.IsNullOrEmpty(normalizedWeaponId))
            {
                Debug.LogWarning("WeaponInscriptionEquipped2D cannot equip an inscription without a weapon id.", this);
                return false;
            }

            if (!string.IsNullOrEmpty(normalizedId) && requireInscriptionKnownForEquip)
            {
                if (inscriptionLibrary == null)
                {
                    Debug.LogError("WeaponInscriptionEquipped2D requires WeaponInscriptionLibrary2D for known-inscription validation, but none is assigned.", this);
                    return false;
                }

                if (!inscriptionLibrary.HasInscription(normalizedId))
                {
                    Debug.LogWarning($"WeaponInscriptionEquipped2D cannot equip unknown inscription id '{normalizedId}'.", this);
                    return false;
                }
            }

            string currentId = GetInscriptionIdForWeapon(normalizedWeaponId);
            if (currentId == normalizedId)
            {
                return true;
            }

            if (string.IsNullOrEmpty(normalizedId))
            {
                _inscriptionByWeaponId.Remove(normalizedWeaponId);
            }
            else
            {
                _inscriptionByWeaponId[normalizedWeaponId] = normalizedId;
            }

            SyncSerializedEntriesFromRuntimeIndex();
            SaveEquippedStateToRecord();
            NotifyInscriptionChanged(normalizedWeaponId, normalizedId);
            return true;
        }

        public void ClearInscription()
        {
            EquipInscription(string.Empty);
        }

        public void ClearInscriptionForWeapon(string weaponId)
        {
            EquipInscriptionForWeapon(weaponId, string.Empty);
        }

        public WeaponInscriptionData GetEquippedInscriptionData()
        {
            return GetInscriptionDataForWeapon(GetCurrentWeaponId());
        }

        public WeaponInscriptionData GetInscriptionDataForWeapon(string weaponId)
        {
            if (inscriptionLibrary == null)
            {
                return null;
            }

            string inscriptionId = GetInscriptionIdForWeapon(weaponId);
            return string.IsNullOrWhiteSpace(inscriptionId) ? null : inscriptionLibrary.GetInscriptionData(inscriptionId);
        }

        public string GetInscriptionIdForWeapon(string weaponId)
        {
            string normalizedWeaponId = NormalizeId(weaponId);
            if (string.IsNullOrEmpty(normalizedWeaponId))
            {
                return string.Empty;
            }

            return _inscriptionByWeaponId.TryGetValue(normalizedWeaponId, out string inscriptionId)
                ? inscriptionId ?? string.Empty
                : string.Empty;
        }

        private void TryLoadEquippedStateFromRecord()
        {
            if (!loadFromRecordOnAwake)
            {
                return;
            }

            if (!PlayerRecordStore2D.TryLoad(_recordPath, out PlayerRecordData2D recordData) || recordData == null)
            {
                return;
            }

            _inscriptionByWeaponId.Clear();
            if (recordData.weaponInscriptionIds != null)
            {
                for (int i = 0; i < recordData.weaponInscriptionIds.Length; i++)
                {
                    WeaponInscriptionStateEntry2D entry = recordData.weaponInscriptionIds[i];
                    string weaponId = NormalizeId(entry.weaponId);
                    string inscriptionId = NormalizeId(entry.inscriptionId);
                    if (string.IsNullOrEmpty(weaponId) || string.IsNullOrEmpty(inscriptionId))
                    {
                        continue;
                    }

                    if (!CanUseInscriptionId(inscriptionId, logFailure: true))
                    {
                        continue;
                    }

                    _inscriptionByWeaponId[weaponId] = inscriptionId;
                }
            }

            TryMigrateLegacyEquippedInscription(recordData);
            SyncSerializedEntriesFromRuntimeIndex();
        }

        private void TryMigrateLegacyEquippedInscription(PlayerRecordData2D recordData)
        {
            if (recordData == null || _inscriptionByWeaponId.Count > 0)
            {
                return;
            }

            string legacyInscriptionId = NormalizeId(recordData.equippedWeaponInscriptionId);
            if (string.IsNullOrEmpty(legacyInscriptionId))
            {
                return;
            }

            if (!CanUseInscriptionId(legacyInscriptionId, logFailure: true))
            {
                return;
            }

            string weaponId = NormalizeId(recordData.equippedWeaponId);
            if (string.IsNullOrEmpty(weaponId))
            {
                weaponId = GetCurrentWeaponId();
            }

            if (string.IsNullOrEmpty(weaponId))
            {
                Debug.LogWarning($"WeaponInscriptionEquipped2D found legacy inscription id '{legacyInscriptionId}', but no equipped weapon id is available for migration.", this);
                return;
            }

            _inscriptionByWeaponId[weaponId] = legacyInscriptionId;
        }

        private void SaveEquippedStateToRecord()
        {
            if (!saveToRecordOnChange)
            {
                return;
            }

            PlayerRecordData2D recordData;
            if (!PlayerRecordStore2D.TryLoad(_recordPath, out recordData) || recordData == null)
            {
                recordData = new PlayerRecordData2D();
            }

            recordData.weaponInscriptionIds = BuildRecordEntries();
            recordData.equippedWeaponInscriptionId = string.Empty;
            PlayerRecordStore2D.Save(_recordPath, recordData, prettyPrintRecordJson);
        }

        private WeaponInscriptionStateEntry2D[] BuildRecordEntries()
        {
            if (_inscriptionByWeaponId.Count == 0)
            {
                return Array.Empty<WeaponInscriptionStateEntry2D>();
            }

            WeaponInscriptionStateEntry2D[] entries = new WeaponInscriptionStateEntry2D[_inscriptionByWeaponId.Count];
            int index = 0;
            foreach (KeyValuePair<string, string> pair in _inscriptionByWeaponId)
            {
                entries[index++] = new WeaponInscriptionStateEntry2D
                {
                    weaponId = pair.Key,
                    inscriptionId = pair.Value
                };
            }

            return entries;
        }

        private bool CanUseInscriptionId(string inscriptionId, bool logFailure)
        {
            if (string.IsNullOrWhiteSpace(inscriptionId) || !requireInscriptionKnownForEquip)
            {
                return true;
            }

            if (inscriptionLibrary == null)
            {
                if (logFailure)
                {
                    Debug.LogError("WeaponInscriptionEquipped2D requires WeaponInscriptionLibrary2D for known-inscription validation, but none is assigned.", this);
                }

                return false;
            }

            if (inscriptionLibrary.HasInscription(inscriptionId))
            {
                return true;
            }

            if (logFailure)
            {
                Debug.LogWarning($"WeaponInscriptionEquipped2D cannot use unknown inscription id '{inscriptionId}'.", this);
            }

            return false;
        }

        private void RebuildRuntimeIndexFromSerializedEntries()
        {
            _inscriptionByWeaponId.Clear();
            if (weaponInscriptions == null)
            {
                return;
            }

            for (int i = 0; i < weaponInscriptions.Length; i++)
            {
                WeaponInscriptionStateEntry2D entry = weaponInscriptions[i];
                string weaponId = NormalizeId(entry.weaponId);
                string inscriptionId = NormalizeId(entry.inscriptionId);
                if (string.IsNullOrEmpty(weaponId) || string.IsNullOrEmpty(inscriptionId))
                {
                    continue;
                }

                _inscriptionByWeaponId[weaponId] = inscriptionId;
            }
        }

        private void SyncSerializedEntriesFromRuntimeIndex()
        {
            weaponInscriptions = BuildRecordEntries();
        }

        private string GetCurrentWeaponId()
        {
            return weaponEquipped != null ? weaponEquipped.EquippedWeaponId : string.Empty;
        }

        private void HandleEquippedWeaponChanged(string weaponId)
        {
            OnEquippedInscriptionChanged?.Invoke(GetInscriptionIdForWeapon(weaponId));
        }

        private void NotifyInscriptionChanged(string weaponId, string inscriptionId)
        {
            OnWeaponInscriptionChanged?.Invoke(weaponId, inscriptionId);
            if (string.Equals(NormalizeId(weaponId), GetCurrentWeaponId(), StringComparison.Ordinal))
            {
                OnEquippedInscriptionChanged?.Invoke(inscriptionId);
            }
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }

        private void OnValidate()
        {
            if (weaponInscriptions == null)
            {
                weaponInscriptions = Array.Empty<WeaponInscriptionStateEntry2D>();
                return;
            }

            for (int i = 0; i < weaponInscriptions.Length; i++)
            {
                WeaponInscriptionStateEntry2D entry = weaponInscriptions[i];
                entry.weaponId = NormalizeId(entry.weaponId);
                entry.inscriptionId = NormalizeId(entry.inscriptionId);
                weaponInscriptions[i] = entry;
            }
        }
    }
}
