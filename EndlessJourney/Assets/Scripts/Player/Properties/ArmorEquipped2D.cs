using System;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Stores the currently equipped armor id and resolves it through ArmorLibrary2D.
    /// Armor changing is intended to be called by forge or higher equipment points.
    /// </summary>
    public class ArmorEquipped2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ArmorLibrary2D armorLibrary;

        [Header("Equipped Armor")]
        [SerializeField] private string equippedArmorId = string.Empty;
        [SerializeField] private bool allowEmptyArmor;
        [SerializeField] private bool requireArmorKnownForEquip = true;
        [SerializeField] private bool requireArmorUnlockedForEquip = true;

        [Header("Record")]
        [SerializeField] private bool loadFromRecordOnAwake = true;
        [SerializeField] private bool saveToRecordOnChange = true;
        [SerializeField] private string recordFileName = "record.json";
        [SerializeField] private bool prettyPrintRecordJson = true;

        private string _recordPath;

        public string EquippedArmorId => equippedArmorId ?? string.Empty;

        public event Action<string> OnEquippedArmorChanged;

        private void Awake()
        {
            _recordPath = PlayerRecordStore2D.GetRecordPath(recordFileName);
            TryLoadEquippedStateFromRecord();
        }

        public bool EquipArmor(string armorId)
        {
            return EquipArmor(armorId, false);
        }

        public bool EquipArmor(string armorId, bool ignoreKnownCheck)
        {
            string normalizedId = NormalizeId(armorId);
            if (string.IsNullOrEmpty(normalizedId) && !allowEmptyArmor)
            {
                Debug.LogWarning("ArmorEquipped2D cannot equip an empty armor id unless allowEmptyArmor is enabled.", this);
                return false;
            }

            if (!ignoreKnownCheck && !string.IsNullOrEmpty(normalizedId) && requireArmorKnownForEquip)
            {
                if (armorLibrary == null)
                {
                    Debug.LogError("ArmorEquipped2D requires ArmorLibrary2D for known-armor validation, but none is assigned.", this);
                    return false;
                }

                if (!armorLibrary.HasArmor(normalizedId))
                {
                    Debug.LogWarning($"ArmorEquipped2D cannot equip unknown armor id '{normalizedId}'.", this);
                    return false;
                }
            }

            if (!ignoreKnownCheck && !string.IsNullOrEmpty(normalizedId) && requireArmorUnlockedForEquip)
            {
                if (armorLibrary == null)
                {
                    Debug.LogError("ArmorEquipped2D requires ArmorLibrary2D for unlock validation, but none is assigned.", this);
                    return false;
                }

                if (!armorLibrary.IsUnlocked(normalizedId))
                {
                    return false;
                }
            }

            if (equippedArmorId == normalizedId)
            {
                return true;
            }

            equippedArmorId = normalizedId;
            SaveEquippedStateToRecord();
            OnEquippedArmorChanged?.Invoke(equippedArmorId);
            return true;
        }

        public ArmorData GetEquippedArmorData()
        {
            if (armorLibrary == null || string.IsNullOrWhiteSpace(equippedArmorId))
            {
                return null;
            }

            return armorLibrary.GetArmorData(equippedArmorId);
        }

        public bool IsEquippedArmorUnlocked()
        {
            if (armorLibrary == null || string.IsNullOrWhiteSpace(equippedArmorId))
            {
                return false;
            }

            return armorLibrary.IsUnlocked(equippedArmorId);
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

            string loadedArmorId = NormalizeId(recordData.equippedArmorId);
            if (string.IsNullOrEmpty(loadedArmorId))
            {
                return;
            }

            if (!CanUseArmorId(loadedArmorId, logFailure: true))
            {
                return;
            }

            equippedArmorId = loadedArmorId;
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

            recordData.equippedArmorId = equippedArmorId ?? string.Empty;
            PlayerRecordStore2D.Save(_recordPath, recordData, prettyPrintRecordJson);
        }

        private bool CanUseArmorId(string armorId, bool logFailure)
        {
            if (string.IsNullOrWhiteSpace(armorId))
            {
                return allowEmptyArmor;
            }

            if (requireArmorKnownForEquip)
            {
                if (armorLibrary == null)
                {
                    if (logFailure)
                    {
                        Debug.LogError("ArmorEquipped2D requires ArmorLibrary2D for known-armor validation, but none is assigned.", this);
                    }

                    return false;
                }

                if (!armorLibrary.HasArmor(armorId))
                {
                    if (logFailure)
                    {
                        Debug.LogWarning($"ArmorEquipped2D cannot use unknown armor id '{armorId}'.", this);
                    }

                    return false;
                }
            }

            if (!requireArmorUnlockedForEquip)
            {
                return true;
            }

            if (armorLibrary == null)
            {
                if (logFailure)
                {
                    Debug.LogError("ArmorEquipped2D requires ArmorLibrary2D for unlock validation, but none is assigned.", this);
                }

                return false;
            }

            return armorLibrary.IsUnlocked(armorId);
        }

        private static string NormalizeId(string id)
        {
            return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
        }

        private void OnValidate()
        {
            equippedArmorId = NormalizeId(equippedArmorId);
        }
    }
}
