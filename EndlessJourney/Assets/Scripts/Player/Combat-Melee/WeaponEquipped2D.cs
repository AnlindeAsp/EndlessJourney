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
            _recordPath = SpellRecordStore2D.GetRecordPath(recordFileName);
            TryLoadEquippedStateFromRecord();
        }

        public bool EquipWeapon(string weaponId)
        {
            return EquipWeapon(weaponId, false);
        }

        public bool EquipWeapon(string weaponId, bool ignoreKnownCheck)
        {
            string normalizedId = string.IsNullOrWhiteSpace(weaponId) ? string.Empty : weaponId.Trim();

            if (!ignoreKnownCheck && requireWeaponKnownForEquip && !string.IsNullOrEmpty(normalizedId))
            {
                if (weaponLibrary != null && !weaponLibrary.HasWeapon(normalizedId))
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

        public void UnequipWeapon()
        {
            EquipWeapon(string.Empty, true);
        }

        public WeaponData GetEquippedWeaponData()
        {
            if (weaponLibrary == null || string.IsNullOrWhiteSpace(equippedWeaponId))
            {
                return null;
            }

            return weaponLibrary.GetWeaponData(equippedWeaponId);
        }

        private void TryLoadEquippedStateFromRecord()
        {
            if (!loadFromRecordOnAwake)
            {
                return;
            }

            if (!SpellRecordStore2D.TryLoad(_recordPath, out SpellRecordData2D recordData) || recordData == null)
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

            SpellRecordData2D recordData;
            if (!SpellRecordStore2D.TryLoad(_recordPath, out recordData) || recordData == null)
            {
                recordData = new SpellRecordData2D();
            }

            recordData.equippedWeaponId = equippedWeaponId ?? string.Empty;
            SpellRecordStore2D.Save(_recordPath, recordData, prettyPrintRecordJson);
        }

        private void OnValidate()
        {
            equippedWeaponId = string.IsNullOrWhiteSpace(equippedWeaponId) ? string.Empty : equippedWeaponId.Trim();
        }
    }
}
