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
        [SerializeField] private bool dualWieldingModeEnabled;
        [SerializeField] private bool requireWeaponKnownForEquip = true;
        [SerializeField] private bool requireWeaponUnlockedForEquip = true;

        [Header("Record")]
        [SerializeField] private bool loadFromRecordOnAwake = true;
        [SerializeField] private bool saveToRecordOnChange = true;
        [SerializeField] private string recordFileName = "record.json";
        [SerializeField] private bool prettyPrintRecordJson = true;

        private string _recordPath;

        public string EquippedWeaponId => equippedWeaponId ?? string.Empty;
        public bool DualWieldingModeEnabled => dualWieldingModeEnabled && CanUseDualWieldingForEquippedWeapon();

        public event Action<string> OnEquippedWeaponChanged;
        public event Action<bool> OnDualWieldingModeChanged;

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

            bool previousDualWieldingMode = dualWieldingModeEnabled;
            bool shouldDisableDualWielding = !CanUseDualWieldingForWeaponId(normalizedId);

            if (equippedWeaponId == normalizedId)
            {
                if (shouldDisableDualWielding && dualWieldingModeEnabled)
                {
                    dualWieldingModeEnabled = false;
                    SaveEquippedStateToRecord();
                    OnDualWieldingModeChanged?.Invoke(dualWieldingModeEnabled);
                }

                return true;
            }

            equippedWeaponId = normalizedId;
            if (shouldDisableDualWielding)
            {
                dualWieldingModeEnabled = false;
            }

            SaveEquippedStateToRecord();
            OnEquippedWeaponChanged?.Invoke(equippedWeaponId);
            if (previousDualWieldingMode != dualWieldingModeEnabled)
            {
                OnDualWieldingModeChanged?.Invoke(dualWieldingModeEnabled);
            }

            return true;
        }

        public bool SetDualWieldingMode(bool enabled)
        {
            bool normalizedEnabled = enabled && CanUseDualWieldingForEquippedWeapon();
            if (dualWieldingModeEnabled == normalizedEnabled)
            {
                return true;
            }

            dualWieldingModeEnabled = normalizedEnabled;
            SaveEquippedStateToRecord();
            OnDualWieldingModeChanged?.Invoke(dualWieldingModeEnabled);
            return true;
        }

        public bool ToggleDualWieldingMode()
        {
            return SetDualWieldingMode(!DualWieldingModeEnabled);
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

        public bool CanUseDualWieldingForEquippedWeapon()
        {
            return CanUseDualWieldingForWeaponId(equippedWeaponId);
        }

        public bool CanUseDualWieldingForWeaponId(string weaponId)
        {
            if (weaponLibrary == null || string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            WeaponData weaponData = weaponLibrary.GetWeaponData(weaponId);
            return weaponData != null && weaponData.DualWieldable;
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
            dualWieldingModeEnabled = recordData.equippedWeaponDualWielding;
            if (dualWieldingModeEnabled && !CanUseDualWieldingForEquippedWeapon())
            {
                dualWieldingModeEnabled = false;
            }
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
            recordData.equippedWeaponDualWielding = dualWieldingModeEnabled;
            PlayerRecordStore2D.Save(_recordPath, recordData, prettyPrintRecordJson);
        }

        private void OnValidate()
        {
            equippedWeaponId = string.IsNullOrWhiteSpace(equippedWeaponId) ? string.Empty : equippedWeaponId.Trim();
        }
    }
}
