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
    public enum WeaponInscriptionPageFocus2D
    {
        WeaponList,
        InscriptionList
    }

    /// <summary>
    /// Handles forge operations for engraving one inscription onto each weapon.
    /// </summary>
    public class WeaponInscriptionPageController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponLibrary2D weaponLibrary;
        [SerializeField] private WeaponEquipped2D weaponEquipped;
        [SerializeField] private WeaponInscriptionLibrary2D inscriptionLibrary;
        [SerializeField] private WeaponInscriptionEquipped2D inscriptionEquipped;
        [SerializeField] private WeaponInscriptionPageDisplayer2D displayer;

        [Header("Selection")]
        [SerializeField] private string selectedWeaponId = string.Empty;
        [SerializeField] private string selectedInscriptionId = string.Empty;
        [SerializeField] private WeaponInscriptionPageFocus2D currentFocus = WeaponInscriptionPageFocus2D.WeaponList;
        [SerializeField] private bool showLockedWeapons;

        [Header("Keyboard Navigation")]
        [SerializeField] private bool enableKeyboardNavigation = true;
        [SerializeField] private bool wrapSelection = true;

        private readonly List<WeaponData> _selectableWeapons = new List<WeaponData>(16);
        private readonly List<WeaponInscriptionData> _selectableInscriptions = new List<WeaponInscriptionData>(16);
        private readonly List<WeaponInscriptionWeaponViewData2D> _weaponViewItems = new List<WeaponInscriptionWeaponViewData2D>(16);
        private readonly List<WeaponInscriptionChoiceViewData2D> _inscriptionViewItems = new List<WeaponInscriptionChoiceViewData2D>(16);

        public string SelectedWeaponId => selectedWeaponId ?? string.Empty;
        public string SelectedInscriptionId => selectedInscriptionId ?? string.Empty;
        public WeaponInscriptionPageFocus2D CurrentFocus => currentFocus;

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

            if (WasFocusWeaponListPressedThisFrame())
            {
                SetFocus(WeaponInscriptionPageFocus2D.WeaponList);
                return;
            }

            if (WasFocusInscriptionListPressedThisFrame())
            {
                SetFocus(WeaponInscriptionPageFocus2D.InscriptionList);
                return;
            }

            if (WasSelectPreviousPressedThisFrame())
            {
                MoveCurrentSelection(-1);
                return;
            }

            if (WasSelectNextPressedThisFrame())
            {
                MoveCurrentSelection(1);
                return;
            }

            if (WasEngravePressedThisFrame())
            {
                EngraveSelectedInscription();
                return;
            }

            if (WasClearPressedThisFrame())
            {
                ClearSelectedWeaponInscription();
            }
        }

        public void SelectWeapon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return;
            }

            string normalizedId = weaponId.Trim();
            if (!IsWeaponSelectable(normalizedId))
            {
                return;
            }

            selectedWeaponId = normalizedId;
            SelectCurrentWeaponInscriptionIfPossible();
            RefreshPage();
        }

        public void SelectInscription(string inscriptionId)
        {
            if (string.IsNullOrWhiteSpace(inscriptionId))
            {
                return;
            }

            string normalizedId = inscriptionId.Trim();
            if (inscriptionLibrary == null || !inscriptionLibrary.HasInscription(normalizedId))
            {
                return;
            }

            selectedInscriptionId = normalizedId;
            RefreshPage();
        }

        public void SetFocus(WeaponInscriptionPageFocus2D focus)
        {
            if (currentFocus == focus)
            {
                return;
            }

            currentFocus = focus;
            RefreshPage();
        }

        public void SelectPrevious()
        {
            MoveCurrentSelection(-1);
        }

        public void SelectNext()
        {
            MoveCurrentSelection(1);
        }

        public void EngraveSelectedInscription()
        {
            if (inscriptionEquipped == null
                || string.IsNullOrWhiteSpace(selectedWeaponId)
                || string.IsNullOrWhiteSpace(selectedInscriptionId)
                || !IsWeaponSelectable(selectedWeaponId)
                || !IsWeaponUnlocked(selectedWeaponId)
                || inscriptionLibrary == null
                || !inscriptionLibrary.HasInscription(selectedInscriptionId))
            {
                return;
            }

            inscriptionEquipped.EquipInscriptionForWeapon(selectedWeaponId, selectedInscriptionId);
        }

        public void ClearSelectedWeaponInscription()
        {
            if (inscriptionEquipped == null || string.IsNullOrWhiteSpace(selectedWeaponId) || !IsWeaponUnlocked(selectedWeaponId))
            {
                return;
            }

            inscriptionEquipped.ClearInscriptionForWeapon(selectedWeaponId);
        }

        public void RefreshPage()
        {
            BuildSelectableWeapons();
            BuildSelectableInscriptions();
            EnsureWeaponSelection();
            EnsureInscriptionSelection();
            BuildWeaponViewItems();
            BuildInscriptionViewItems();

            WeaponData selectedWeapon = GetSelectedWeapon();
            WeaponInscriptionData selectedInscription = GetSelectedInscription();
            WeaponInscriptionData currentWeaponInscription = GetCurrentInscriptionForSelectedWeapon();

            if (displayer != null)
            {
                displayer.Render(
                    _weaponViewItems,
                    _inscriptionViewItems,
                    selectedWeapon,
                    selectedInscription,
                    currentWeaponInscription,
                    currentFocus,
                    SelectWeapon,
                    SelectInscription,
                    EngraveSelectedInscription,
                    ClearSelectedWeaponInscription,
                    () => SetFocus(WeaponInscriptionPageFocus2D.WeaponList),
                    () => SetFocus(WeaponInscriptionPageFocus2D.InscriptionList));
            }
        }

        private void BuildSelectableWeapons()
        {
            _selectableWeapons.Clear();

            if (weaponLibrary == null)
            {
                return;
            }

            for (int i = 0; i < weaponLibrary.WeaponCount; i++)
            {
                WeaponData weaponData = weaponLibrary.GetWeaponAt(i);
                if (weaponData == null || string.IsNullOrWhiteSpace(weaponData.WeaponId))
                {
                    continue;
                }

                bool unlocked = weaponLibrary.IsUnlocked(weaponData.WeaponId);
                if (!showLockedWeapons && !unlocked)
                {
                    continue;
                }

                _selectableWeapons.Add(weaponData);
            }
        }

        private void BuildSelectableInscriptions()
        {
            _selectableInscriptions.Clear();

            if (inscriptionLibrary == null)
            {
                return;
            }

            for (int i = 0; i < inscriptionLibrary.InscriptionCount; i++)
            {
                WeaponInscriptionData inscriptionData = inscriptionLibrary.GetInscriptionAt(i);
                if (inscriptionData == null || string.IsNullOrWhiteSpace(inscriptionData.InscriptionId))
                {
                    continue;
                }

                _selectableInscriptions.Add(inscriptionData);
            }
        }

        private void EnsureWeaponSelection()
        {
            if (_selectableWeapons.Count == 0)
            {
                selectedWeaponId = string.Empty;
                return;
            }

            if (!string.IsNullOrWhiteSpace(selectedWeaponId) && FindSelectableWeaponIndex(selectedWeaponId) >= 0)
            {
                return;
            }

            string equippedWeaponId = weaponEquipped != null ? weaponEquipped.EquippedWeaponId : string.Empty;
            if (!string.IsNullOrWhiteSpace(equippedWeaponId) && FindSelectableWeaponIndex(equippedWeaponId) >= 0)
            {
                selectedWeaponId = equippedWeaponId;
                SelectCurrentWeaponInscriptionIfPossible();
                return;
            }

            selectedWeaponId = _selectableWeapons[0].WeaponId;
            SelectCurrentWeaponInscriptionIfPossible();
        }

        private void EnsureInscriptionSelection()
        {
            if (_selectableInscriptions.Count == 0)
            {
                selectedInscriptionId = string.Empty;
                return;
            }

            if (!string.IsNullOrWhiteSpace(selectedInscriptionId) && FindSelectableInscriptionIndex(selectedInscriptionId) >= 0)
            {
                return;
            }

            SelectCurrentWeaponInscriptionIfPossible();
            if (!string.IsNullOrWhiteSpace(selectedInscriptionId) && FindSelectableInscriptionIndex(selectedInscriptionId) >= 0)
            {
                return;
            }

            selectedInscriptionId = _selectableInscriptions[0].InscriptionId;
        }

        private void SelectCurrentWeaponInscriptionIfPossible()
        {
            if (inscriptionEquipped == null || string.IsNullOrWhiteSpace(selectedWeaponId))
            {
                return;
            }

            string inscriptionId = inscriptionEquipped.GetInscriptionIdForWeapon(selectedWeaponId);
            if (!string.IsNullOrWhiteSpace(inscriptionId))
            {
                selectedInscriptionId = inscriptionId;
            }
        }

        private void BuildWeaponViewItems()
        {
            _weaponViewItems.Clear();

            for (int i = 0; i < _selectableWeapons.Count; i++)
            {
                WeaponData weaponData = _selectableWeapons[i];
                string weaponId = weaponData.WeaponId;
                bool unlocked = weaponLibrary == null || weaponLibrary.IsUnlocked(weaponId);
                bool selected = string.Equals(selectedWeaponId, weaponId, StringComparison.Ordinal);
                bool equipped = weaponEquipped != null && string.Equals(weaponEquipped.EquippedWeaponId, weaponId, StringComparison.Ordinal);
                WeaponInscriptionData inscription = inscriptionEquipped != null ? inscriptionEquipped.GetInscriptionDataForWeapon(weaponId) : null;

                _weaponViewItems.Add(new WeaponInscriptionWeaponViewData2D(weaponData, unlocked, selected, equipped, inscription));
            }
        }

        private void BuildInscriptionViewItems()
        {
            _inscriptionViewItems.Clear();

            string currentInscriptionId = inscriptionEquipped != null
                ? inscriptionEquipped.GetInscriptionIdForWeapon(selectedWeaponId)
                : string.Empty;

            for (int i = 0; i < _selectableInscriptions.Count; i++)
            {
                WeaponInscriptionData inscriptionData = _selectableInscriptions[i];
                string inscriptionId = inscriptionData.InscriptionId;
                bool selected = string.Equals(selectedInscriptionId, inscriptionId, StringComparison.Ordinal);
                bool engravedOnSelectedWeapon = string.Equals(currentInscriptionId, inscriptionId, StringComparison.Ordinal);
                _inscriptionViewItems.Add(new WeaponInscriptionChoiceViewData2D(inscriptionData, selected, engravedOnSelectedWeapon));
            }
        }

        private void MoveCurrentSelection(int direction)
        {
            if (currentFocus == WeaponInscriptionPageFocus2D.WeaponList)
            {
                MoveWeaponSelection(direction);
                return;
            }

            MoveInscriptionSelection(direction);
        }

        private void MoveWeaponSelection(int direction)
        {
            if (_selectableWeapons.Count == 0)
            {
                return;
            }

            int currentIndex = FindSelectableWeaponIndex(selectedWeaponId);
            int nextIndex = MoveIndex(currentIndex, _selectableWeapons.Count, direction);
            selectedWeaponId = _selectableWeapons[nextIndex].WeaponId;
            SelectCurrentWeaponInscriptionIfPossible();
            RefreshPage();
        }

        private void MoveInscriptionSelection(int direction)
        {
            if (_selectableInscriptions.Count == 0)
            {
                return;
            }

            int currentIndex = FindSelectableInscriptionIndex(selectedInscriptionId);
            int nextIndex = MoveIndex(currentIndex, _selectableInscriptions.Count, direction);
            selectedInscriptionId = _selectableInscriptions[nextIndex].InscriptionId;
            RefreshPage();
        }

        private int MoveIndex(int currentIndex, int count, int direction)
        {
            if (count <= 0)
            {
                return 0;
            }

            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = currentIndex + Math.Sign(direction);
            if (wrapSelection)
            {
                if (nextIndex < 0)
                {
                    nextIndex = count - 1;
                }
                else if (nextIndex >= count)
                {
                    nextIndex = 0;
                }
            }
            else
            {
                nextIndex = Mathf.Clamp(nextIndex, 0, count - 1);
            }

            return nextIndex;
        }

        private bool IsWeaponSelectable(string weaponId)
        {
            return FindSelectableWeaponIndex(weaponId) >= 0;
        }

        private bool IsWeaponUnlocked(string weaponId)
        {
            return weaponLibrary == null || weaponLibrary.IsUnlocked(weaponId);
        }

        private int FindSelectableWeaponIndex(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return -1;
            }

            for (int i = 0; i < _selectableWeapons.Count; i++)
            {
                WeaponData weaponData = _selectableWeapons[i];
                if (weaponData != null && weaponData.WeaponId == weaponId)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindSelectableInscriptionIndex(string inscriptionId)
        {
            if (string.IsNullOrWhiteSpace(inscriptionId))
            {
                return -1;
            }

            for (int i = 0; i < _selectableInscriptions.Count; i++)
            {
                WeaponInscriptionData inscriptionData = _selectableInscriptions[i];
                if (inscriptionData != null && inscriptionData.InscriptionId == inscriptionId)
                {
                    return i;
                }
            }

            return -1;
        }

        private WeaponData GetSelectedWeapon()
        {
            if (weaponLibrary == null || string.IsNullOrWhiteSpace(selectedWeaponId))
            {
                return null;
            }

            return weaponLibrary.GetWeaponData(selectedWeaponId);
        }

        private WeaponInscriptionData GetSelectedInscription()
        {
            if (inscriptionLibrary == null || string.IsNullOrWhiteSpace(selectedInscriptionId))
            {
                return null;
            }

            return inscriptionLibrary.GetInscriptionData(selectedInscriptionId);
        }

        private WeaponInscriptionData GetCurrentInscriptionForSelectedWeapon()
        {
            if (inscriptionEquipped == null || string.IsNullOrWhiteSpace(selectedWeaponId))
            {
                return null;
            }

            return inscriptionEquipped.GetInscriptionDataForWeapon(selectedWeaponId);
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

            if (inscriptionEquipped != null)
            {
                inscriptionEquipped.OnWeaponInscriptionChanged += HandleWeaponInscriptionChanged;
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

            if (inscriptionEquipped != null)
            {
                inscriptionEquipped.OnWeaponInscriptionChanged -= HandleWeaponInscriptionChanged;
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

        private void HandleWeaponInscriptionChanged(string weaponId, string inscriptionId)
        {
            RefreshPage();
        }

        private void OnValidate()
        {
            selectedWeaponId = string.IsNullOrWhiteSpace(selectedWeaponId) ? string.Empty : selectedWeaponId.Trim();
            selectedInscriptionId = string.IsNullOrWhiteSpace(selectedInscriptionId) ? string.Empty : selectedInscriptionId.Trim();
        }

        private bool WasFocusWeaponListPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
#else
            return false;
#endif
        }

        private bool WasFocusInscriptionListPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
#else
            return false;
#endif
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

        private bool WasEngravePressedThisFrame()
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

        private bool WasClearPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.backspaceKey.wasPressedThisFrame || keyboard.deleteKey.wasPressedThisFrame))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Delete);
#else
            return false;
#endif
        }
    }
}
