using System;
using System.Collections.Generic;
using EndlessJourney.Combat;
using EndlessJourney.Player;
using UnityEngine;

namespace EndlessJourney.UI
{
    /// <summary>
    /// Handles player operations on the storage weapon page.
    /// </summary>
    public class WeaponPageController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponLibrary2D weaponLibrary;
        [SerializeField] private WeaponEquipped2D weaponEquipped;
        [SerializeField] private WeaponPageDisplayer2D displayer;

        [Header("Selection")]
        [SerializeField] private string selectedWeaponId = string.Empty;

        private readonly List<WeaponPageItemViewData2D> _viewItems = new List<WeaponPageItemViewData2D>(16);

        public string SelectedWeaponId => selectedWeaponId ?? string.Empty;

        private void OnEnable()
        {
            SubscribeToStateEvents();
            RefreshPage();
        }

        private void OnDisable()
        {
            UnsubscribeFromStateEvents();
        }

        public void SelectWeapon(string weaponId)
        {
            selectedWeaponId = string.IsNullOrWhiteSpace(weaponId) ? string.Empty : weaponId.Trim();
            RefreshPage();
        }

        public void EquipSelectedWeapon()
        {
            if (weaponEquipped == null || string.IsNullOrWhiteSpace(selectedWeaponId))
            {
                return;
            }

            if (weaponEquipped.EquipWeapon(selectedWeaponId))
            {
                RefreshPage();
            }
        }

        public void UnequipCurrentWeapon()
        {
            if (weaponEquipped == null)
            {
                return;
            }

            weaponEquipped.UnequipWeapon();
            RefreshPage();
        }

        public void RefreshPage()
        {
            BuildViewItems();

            if (displayer != null)
            {
                displayer.Render(
                    _viewItems,
                    GetSelectedWeapon(),
                    SelectedWeaponId,
                    weaponEquipped != null ? weaponEquipped.EquippedWeaponId : string.Empty,
                    SelectWeapon,
                    EquipSelectedWeapon,
                    UnequipCurrentWeapon);
            }
        }

        private void BuildViewItems()
        {
            _viewItems.Clear();

            if (weaponLibrary == null)
            {
                selectedWeaponId = string.Empty;
                return;
            }

            EnsureSelection();
            string equippedWeaponId = weaponEquipped != null ? weaponEquipped.EquippedWeaponId : string.Empty;

            for (int i = 0; i < weaponLibrary.WeaponCount; i++)
            {
                WeaponData weaponData = weaponLibrary.GetWeaponAt(i);
                if (weaponData == null || string.IsNullOrWhiteSpace(weaponData.WeaponId))
                {
                    continue;
                }

                string weaponId = weaponData.WeaponId;
                bool unlocked = weaponLibrary.IsUnlocked(weaponId);
                bool equipped = string.Equals(equippedWeaponId, weaponId, StringComparison.Ordinal);
                bool selected = string.Equals(SelectedWeaponId, weaponId, StringComparison.Ordinal);

                _viewItems.Add(new WeaponPageItemViewData2D(weaponData, unlocked, equipped, selected));
            }
        }

        private void EnsureSelection()
        {
            if (weaponLibrary == null)
            {
                selectedWeaponId = string.Empty;
                return;
            }

            if (!string.IsNullOrWhiteSpace(selectedWeaponId) && weaponLibrary.HasWeapon(selectedWeaponId))
            {
                return;
            }

            string equippedWeaponId = weaponEquipped != null ? weaponEquipped.EquippedWeaponId : string.Empty;
            if (!string.IsNullOrWhiteSpace(equippedWeaponId) && weaponLibrary.HasWeapon(equippedWeaponId))
            {
                selectedWeaponId = equippedWeaponId;
                return;
            }

            selectedWeaponId = string.Empty;
            for (int i = 0; i < weaponLibrary.WeaponCount; i++)
            {
                WeaponData weaponData = weaponLibrary.GetWeaponAt(i);
                if (weaponData == null || string.IsNullOrWhiteSpace(weaponData.WeaponId))
                {
                    continue;
                }

                selectedWeaponId = weaponData.WeaponId;
                return;
            }
        }

        private WeaponData GetSelectedWeapon()
        {
            if (weaponLibrary == null || string.IsNullOrWhiteSpace(selectedWeaponId))
            {
                return null;
            }

            return weaponLibrary.GetWeaponData(selectedWeaponId);
        }

        private void SubscribeToStateEvents()
        {
            if (weaponLibrary != null)
            {
                weaponLibrary.OnWeaponUnlockStateChanged += HandleWeaponUnlockStateChanged;
            }

            if (weaponEquipped != null)
            {
                weaponEquipped.OnEquippedWeaponChanged += HandleEquippedWeaponChanged;
            }
        }

        private void UnsubscribeFromStateEvents()
        {
            if (weaponLibrary != null)
            {
                weaponLibrary.OnWeaponUnlockStateChanged -= HandleWeaponUnlockStateChanged;
            }

            if (weaponEquipped != null)
            {
                weaponEquipped.OnEquippedWeaponChanged -= HandleEquippedWeaponChanged;
            }
        }

        private void HandleWeaponUnlockStateChanged(string weaponId, bool unlocked)
        {
            RefreshPage();
        }

        private void HandleEquippedWeaponChanged(string weaponId)
        {
            RefreshPage();
        }

        private void OnValidate()
        {
            selectedWeaponId = string.IsNullOrWhiteSpace(selectedWeaponId) ? string.Empty : selectedWeaponId.Trim();
        }
    }
}
