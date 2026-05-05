using System;
using System.Collections.Generic;
using EndlessJourney.Combat;
using EndlessJourney.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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

        [Header("Keyboard Navigation")]
        [SerializeField] private bool enableKeyboardNavigation = true;
        [SerializeField] private bool wrapSelection = true;

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

        private void Update()
        {
            if (!enableKeyboardNavigation)
            {
                return;
            }

            if (WasSelectPreviousPressedThisFrame())
            {
                SelectPreviousWeapon();
                return;
            }

            if (WasSelectNextPressedThisFrame())
            {
                SelectNextWeapon();
                return;
            }

            if (WasEquipPressedThisFrame())
            {
                EquipSelectedWeapon();
            }
        }

        public void SelectWeapon(string weaponId)
        {
            selectedWeaponId = string.IsNullOrWhiteSpace(weaponId) ? string.Empty : weaponId.Trim();
            RefreshPage();
        }

        public void SelectPreviousWeapon()
        {
            MoveSelection(-1);
        }

        public void SelectNextWeapon()
        {
            MoveSelection(1);
        }

        public void EquipSelectedWeapon()
        {
            if (weaponEquipped == null || string.IsNullOrWhiteSpace(selectedWeaponId))
            {
                return;
            }

            weaponEquipped.EquipWeapon(selectedWeaponId);
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
                    EquipSelectedWeapon);
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

        private void MoveSelection(int direction)
        {
            if (weaponLibrary == null || weaponLibrary.WeaponCount <= 0)
            {
                return;
            }

            BuildViewItems();
            if (_viewItems.Count == 0)
            {
                return;
            }

            int currentIndex = FindSelectedViewIndex();
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = currentIndex + Math.Sign(direction);
            if (wrapSelection)
            {
                if (nextIndex < 0)
                {
                    nextIndex = _viewItems.Count - 1;
                }
                else if (nextIndex >= _viewItems.Count)
                {
                    nextIndex = 0;
                }
            }
            else
            {
                nextIndex = Mathf.Clamp(nextIndex, 0, _viewItems.Count - 1);
            }

            WeaponPageItemViewData2D nextItem = _viewItems[nextIndex];
            if (nextItem.WeaponData == null)
            {
                return;
            }

            selectedWeaponId = nextItem.WeaponData.WeaponId;
            RefreshPage();
        }

        private int FindSelectedViewIndex()
        {
            string currentId = SelectedWeaponId;
            for (int i = 0; i < _viewItems.Count; i++)
            {
                WeaponPageItemViewData2D item = _viewItems[i];
                if (item.WeaponData != null && item.WeaponData.WeaponId == currentId)
                {
                    return i;
                }
            }

            return -1;
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

        private bool WasSelectPreviousPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
#else
            return false;
#endif
        }

        private bool WasSelectNextPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
#else
            return false;
#endif
        }

        private bool WasEquipPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }
    }
}
