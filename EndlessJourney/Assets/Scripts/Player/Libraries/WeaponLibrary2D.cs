using System;
using System.Collections.Generic;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Holds all weapon definitions and resolves them by stable weapon id.
    /// Runtime state should store ids, not asset references.
    /// </summary>
    public class WeaponLibrary2D : MonoBehaviour
    {
        [Serializable]
        private struct WeaponUnlockEntry
        {
            public string weaponId;
            public bool unlocked;
        }

        [Header("Weapon Definitions")]
        [SerializeField] private WeaponData[] allWeapons = Array.Empty<WeaponData>();

        [Header("Initial Unlock State")]
        [SerializeField] private WeaponUnlockEntry[] initialUnlockedEntries = Array.Empty<WeaponUnlockEntry>();

        [Header("Record")]
        [SerializeField] private bool loadFromRecordOnAwake = true;
        [SerializeField] private bool saveToRecordOnChange = true;
        [SerializeField] private string recordFileName = "record.json";
        [SerializeField] private bool prettyPrintRecordJson = true;

        private readonly Dictionary<string, WeaponData> _weaponById = new Dictionary<string, WeaponData>(32);
        private readonly Dictionary<string, bool> _unlockedById = new Dictionary<string, bool>(32);
        private string _recordPath;

        public int WeaponCount => allWeapons != null ? allWeapons.Length : 0;

        public event Action<string, bool> OnWeaponUnlockStateChanged;

        private void Awake()
        {
            _recordPath = PlayerRecordStore2D.GetRecordPath(recordFileName);
            RebuildWeaponIndex();
            RebuildInitialUnlockState();
            TryLoadUnlockStateFromRecord();
        }

        public bool HasWeapon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            return _weaponById.ContainsKey(weaponId);
        }

        public bool TryGetWeaponData(string weaponId, out WeaponData weaponData)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                weaponData = null;
                return false;
            }

            return _weaponById.TryGetValue(weaponId, out weaponData) && weaponData != null;
        }

        public WeaponData GetWeaponData(string weaponId)
        {
            TryGetWeaponData(weaponId, out WeaponData weaponData);
            return weaponData;
        }

        public WeaponData GetWeaponAt(int index)
        {
            if (allWeapons == null || index < 0 || index >= allWeapons.Length)
            {
                return null;
            }

            return allWeapons[index];
        }

        public bool IsUnlocked(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            return _unlockedById.TryGetValue(weaponId, out bool unlocked) && unlocked;
        }

        public void UnlockWeapon(string weaponId)
        {
            SetWeaponUnlocked(weaponId, true);
        }

        public void SetWeaponUnlocked(string weaponId, bool unlocked)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return;
            }

            string normalizedId = weaponId.Trim();
            bool previous = _unlockedById.TryGetValue(normalizedId, out bool existing) && existing;
            _unlockedById[normalizedId] = unlocked;

            if (previous != unlocked)
            {
                SaveUnlockStateToRecord();
                OnWeaponUnlockStateChanged?.Invoke(normalizedId, unlocked);
            }
        }

        public void RebuildWeaponIndex()
        {
            _weaponById.Clear();

            if (allWeapons == null)
            {
                return;
            }

            for (int i = 0; i < allWeapons.Length; i++)
            {
                WeaponData weaponData = allWeapons[i];
                if (weaponData == null || string.IsNullOrWhiteSpace(weaponData.WeaponId))
                {
                    continue;
                }

                _weaponById[weaponData.WeaponId] = weaponData;
            }
        }

        private void RebuildInitialUnlockState()
        {
            _unlockedById.Clear();

            if (allWeapons != null)
            {
                for (int i = 0; i < allWeapons.Length; i++)
                {
                    WeaponData weaponData = allWeapons[i];
                    if (weaponData == null || string.IsNullOrWhiteSpace(weaponData.WeaponId))
                    {
                        continue;
                    }

                    _unlockedById[weaponData.WeaponId] = false;
                }
            }

            if (initialUnlockedEntries == null)
            {
                return;
            }

            for (int i = 0; i < initialUnlockedEntries.Length; i++)
            {
                WeaponUnlockEntry entry = initialUnlockedEntries[i];
                if (string.IsNullOrWhiteSpace(entry.weaponId))
                {
                    continue;
                }

                _unlockedById[entry.weaponId.Trim()] = entry.unlocked;
            }
        }

        private void TryLoadUnlockStateFromRecord()
        {
            if (!loadFromRecordOnAwake)
            {
                return;
            }

            if (!PlayerRecordStore2D.TryLoad(_recordPath, out PlayerRecordData2D recordData) || recordData == null)
            {
                return;
            }

            WeaponUnlockStateEntry2D[] entries = recordData.unlockedWeaponIds;
            if (entries == null || entries.Length == 0)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                WeaponUnlockStateEntry2D entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry.weaponId))
                {
                    continue;
                }

                _unlockedById[entry.weaponId.Trim()] = entry.unlocked;
            }
        }

        private void SaveUnlockStateToRecord()
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

            recordData.unlockedWeaponIds = PlayerRecordStore2D.BuildWeaponUnlockEntries(_unlockedById);
            PlayerRecordStore2D.Save(_recordPath, recordData, prettyPrintRecordJson);
        }
    }
}
