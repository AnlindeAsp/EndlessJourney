using System;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Stores the current equipped weapon id and resolves it through WeaponLibrary2D.
    /// </summary>
    public class WeaponEquipped2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponLibrary2D weaponLibrary;

        [Header("Equipped Weapon")]
        [SerializeField] private string equippedWeaponId = string.Empty;
        [SerializeField] private bool requireWeaponKnownForEquip = true;
        [SerializeField] private bool requireWeaponUnlockedForEquip = true;

        [Header("Record")]
        [SerializeField] private bool loadFromRecordOnAwake = true;
        [SerializeField] private bool saveToRecordOnChange = true;
        [SerializeField] private string recordFileName = "record.json";
        [SerializeField] private bool prettyPrintRecordJson = true;

        private string _recordPath;

        public string EquippedWeaponId => equippedWeaponId ?? string.Empty;

        public event Action<string> OnEquippedWeaponChanged;

        private void Awake()
        {
            _recordPath = PlayerRecordStore2D.GetRecordPath(recordFileName);
            TryLoadEquippedStateFromRecord();
        }

        public bool EquipWeapon(string weaponId)
        {
            return EquipWeapon(weaponId, false);
        }

        public bool EquipWeapon(string weaponId, bool ignoreKnownCheck)
        {
            string normalizedId = string.IsNullOrWhiteSpace(weaponId) ? string.Empty : weaponId.Trim();
            if (string.IsNullOrEmpty(normalizedId))
            {
                Debug.LogWarning("WeaponEquipped2D cannot equip an empty weapon id. Player should always keep one weapon equipped.", this);
                return false;
            }

            if (!ignoreKnownCheck && requireWeaponKnownForEquip && !string.IsNullOrEmpty(normalizedId))
            {
                if (weaponLibrary == null)
                {
                    Debug.LogError("WeaponEquipped2D requires WeaponLibrary2D for known-weapon validation, but none is assigned.", this);
                }
                else if (!weaponLibrary.HasWeapon(normalizedId))
                {
                    return false;
                }
            }

            if (!ignoreKnownCheck && requireWeaponUnlockedForEquip && !string.IsNullOrEmpty(normalizedId))
            {
                if (weaponLibrary == null)
                {
                    Debug.LogError("WeaponEquipped2D requires WeaponLibrary2D for unlock validation, but none is assigned.", this);
                }
                else if (!weaponLibrary.IsUnlocked(normalizedId))
                {
                    return false;
                }
            }

            if (equippedWeaponId == normalizedId)
            {
                return true;
            }

            equippedWeaponId = normalizedId;
            SaveEquippedStateToRecord();
            OnEquippedWeaponChanged?.Invoke(equippedWeaponId);
            return true;
        }

        public WeaponData GetEquippedWeaponData()
        {
            if (weaponLibrary == null || string.IsNullOrWhiteSpace(equippedWeaponId))
            {
                return null;
            }

            return weaponLibrary.GetWeaponData(equippedWeaponId);
        }

        public bool IsEquippedWeaponUnlocked()
        {
            if (weaponLibrary == null || string.IsNullOrWhiteSpace(equippedWeaponId))
            {
                return false;
            }

            return weaponLibrary.IsUnlocked(equippedWeaponId);
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

            equippedWeaponId = recordData.equippedWeaponId ?? string.Empty;
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

            recordData.equippedWeaponId = equippedWeaponId ?? string.Empty;
            PlayerRecordStore2D.Save(_recordPath, recordData, prettyPrintRecordJson);
        }

        private void OnValidate()
        {
            equippedWeaponId = string.IsNullOrWhiteSpace(equippedWeaponId) ? string.Empty : equippedWeaponId.Trim();
        }
    }
}
