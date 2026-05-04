using System;
using System.Collections.Generic;
using EndlessJourney.Player;
using UnityEngine;
using UnityEngine.Serialization;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessJourney.UI
{
    /// <summary>
    /// Handles the spell book page: page turning, spell preview selection, writing, and erasing.
    /// </summary>
    public class SpellPageController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpellLibrary2D spellLibrary;
        [SerializeField] private SpellBook2D spellBook;
        [SerializeField] private SpellPageDisplayer2D displayer;

        [Header("Book Page")]
        [FormerlySerializedAs("selectedSlotIndex")]
        [SerializeField, Range(0, 4)] private int selectedPageIndex;
        [SerializeField] private string selectedSpellId = string.Empty;
        [SerializeField] private bool showLockedSpells;
        [SerializeField] private bool wrapSpellSelection = true;

        [Header("Input")]
        [SerializeField] private bool enableKeyboardInput = true;
        [SerializeField] private bool enableMouseWheelInput = true;

        private readonly List<SpellData2D> _selectableSpells = new List<SpellData2D>(32);
        private readonly List<SpellPageSpellViewData2D> _spellViewItems = new List<SpellPageSpellViewData2D>(16);
        private readonly List<SpellPageSlotViewData2D> _pageViewItems = new List<SpellPageSlotViewData2D>(5);

        public int SelectedPageIndex => selectedPageIndex;
        public string SelectedSpellId => selectedSpellId ?? string.Empty;

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
            if (!enableKeyboardInput && !enableMouseWheelInput)
            {
                return;
            }

            if (enableKeyboardInput)
            {
                int requestedPage = ReadPageSelectionInput();
                if (requestedPage >= 0)
                {
                    SelectPage(requestedPage);
                    return;
                }

                if (WasPreviousSpellPressedThisFrame())
                {
                    MoveSpellSelection(-1);
                    return;
                }

                if (WasNextSpellPressedThisFrame())
                {
                    MoveSpellSelection(1);
                    return;
                }

                if (WasWritePressedThisFrame())
                {
                    WriteSelectedSpellToCurrentPage();
                    return;
                }
            }

            if (enableMouseWheelInput)
            {
                float scroll = ReadMouseScrollY();
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    MoveSpellSelection(scroll > 0f ? -1 : 1);
                }
            }
        }

        public void SelectPage(int pageIndex)
        {
            if (!IsPageAvailable(pageIndex))
            {
                return;
            }

            selectedPageIndex = Mathf.Clamp(pageIndex, 0, 4);
            SelectWrittenSpellOnCurrentPageIfPossible();
            RefreshPage();
        }

        public void SelectSpell(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
            {
                return;
            }

            BuildSelectableSpells();
            int index = FindSelectableSpellIndex(spellId.Trim());
            if (index < 0)
            {
                return;
            }

            selectedSpellId = _selectableSpells[index].SpellId;
            RefreshPage();
        }

        public void SelectPreviousSpell()
        {
            MoveSpellSelection(-1);
        }

        public void SelectNextSpell()
        {
            MoveSpellSelection(1);
        }

        public void WriteSelectedSpellToCurrentPage()
        {
            if (spellBook == null || string.IsNullOrWhiteSpace(selectedSpellId) || !spellBook.IsSlotAvailable(selectedPageIndex))
            {
                return;
            }

            if (spellBook.EquipSpellToSlot(selectedPageIndex, selectedSpellId))
            {
                RefreshPage();
            }
        }

        public void EraseCurrentPage()
        {
            if (spellBook == null || !spellBook.IsSlotAvailable(selectedPageIndex))
            {
                return;
            }

            spellBook.UnequipSlot(selectedPageIndex);
            RefreshPage();
        }

        public void RefreshPage()
        {
            BuildSelectableSpells();
            EnsureValidPage();
            EnsureSpellSelection();
            BuildSpellViewItems();
            BuildPageViewItems();

            SpellData2D writtenSpell = GetWrittenSpellOnCurrentPage();
            SpellData2D previewSpell = GetSelectedSpell();
            bool previewingUnwrittenSpell = IsPreviewingUnwrittenSpell(writtenSpell, previewSpell);
            SpellData2D displayedSpell = previewingUnwrittenSpell ? previewSpell : writtenSpell;

            if (displayedSpell == null)
            {
                displayedSpell = previewSpell;
                previewingUnwrittenSpell = displayedSpell != null;
            }

            if (displayer != null)
            {
                displayer.Render(
                    _spellViewItems,
                    _pageViewItems,
                    displayedSpell,
                    writtenSpell,
                    previewSpell,
                    selectedPageIndex,
                    previewingUnwrittenSpell,
                    SelectSpell,
                    SelectPage,
                    WriteSelectedSpellToCurrentPage,
                    EraseCurrentPage);
            }
        }

        private void BuildSelectableSpells()
        {
            _selectableSpells.Clear();

            if (spellLibrary == null)
            {
                return;
            }

            for (int i = 0; i < spellLibrary.SpellCount; i++)
            {
                SpellData2D spellData = spellLibrary.GetSpellAt(i);
                if (spellData == null || string.IsNullOrWhiteSpace(spellData.SpellId))
                {
                    continue;
                }

                if (!showLockedSpells && !spellLibrary.IsUnlocked(spellData.SpellId))
                {
                    continue;
                }

                _selectableSpells.Add(spellData);
            }
        }

        private void EnsureValidPage()
        {
            if (IsPageAvailable(selectedPageIndex))
            {
                return;
            }

            int pageCount = spellBook != null ? spellBook.SlotCount : 5;
            for (int i = 0; i < pageCount; i++)
            {
                if (IsPageAvailable(i))
                {
                    selectedPageIndex = i;
                    return;
                }
            }

            selectedPageIndex = 0;
        }

        private void EnsureSpellSelection()
        {
            if (_selectableSpells.Count == 0)
            {
                selectedSpellId = string.Empty;
                return;
            }

            if (!string.IsNullOrWhiteSpace(selectedSpellId) && FindSelectableSpellIndex(selectedSpellId) >= 0)
            {
                return;
            }

            string writtenSpellId = GetWrittenSpellIdOnCurrentPage();
            if (!string.IsNullOrWhiteSpace(writtenSpellId) && FindSelectableSpellIndex(writtenSpellId) >= 0)
            {
                selectedSpellId = writtenSpellId;
                return;
            }

            selectedSpellId = _selectableSpells[0].SpellId;
        }

        private void SelectWrittenSpellOnCurrentPageIfPossible()
        {
            BuildSelectableSpells();
            string writtenSpellId = GetWrittenSpellIdOnCurrentPage();
            if (!string.IsNullOrWhiteSpace(writtenSpellId) && FindSelectableSpellIndex(writtenSpellId) >= 0)
            {
                selectedSpellId = writtenSpellId;
            }
        }

        private void BuildSpellViewItems()
        {
            _spellViewItems.Clear();

            if (_selectableSpells.Count == 0)
            {
                return;
            }

            int selectedIndex = FindSelectableSpellIndex(selectedSpellId);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            string currentPageSpellId = GetWrittenSpellIdOnCurrentPage();
            for (int i = 0; i < _selectableSpells.Count; i++)
            {
                SpellData2D spellData = _selectableSpells[i];
                string spellId = spellData.SpellId;
                bool unlocked = spellLibrary == null || spellLibrary.IsUnlocked(spellId);
                bool writtenAnywhere = spellBook != null && spellBook.IsSpellEquipped(spellId);
                bool writtenOnCurrentPage = !string.IsNullOrWhiteSpace(currentPageSpellId)
                    && string.Equals(currentPageSpellId, spellId, StringComparison.Ordinal);
                bool selected = i == selectedIndex;
                int signedOffset = GetSignedOffset(i, selectedIndex, _selectableSpells.Count);
                _spellViewItems.Add(new SpellPageSpellViewData2D(spellData, unlocked, writtenAnywhere, writtenOnCurrentPage, selected, signedOffset));
            }
        }

        private void BuildPageViewItems()
        {
            _pageViewItems.Clear();

            int pageCount = spellBook != null ? spellBook.SlotCount : 5;
            for (int i = 0; i < pageCount; i++)
            {
                bool available = IsPageAvailable(i);
                string writtenSpellId = spellBook != null ? spellBook.GetEquippedSpellId(i) : string.Empty;
                SpellData2D writtenSpell = spellLibrary != null ? spellLibrary.GetSpellData(writtenSpellId) : null;
                _pageViewItems.Add(new SpellPageSlotViewData2D(i, available, writtenSpellId, writtenSpell, i == selectedPageIndex));
            }
        }

        private void MoveSpellSelection(int direction)
        {
            BuildSelectableSpells();
            if (_selectableSpells.Count == 0)
            {
                return;
            }

            int currentIndex = FindSelectableSpellIndex(selectedSpellId);
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = currentIndex + Math.Sign(direction);
            if (wrapSpellSelection)
            {
                if (nextIndex < 0)
                {
                    nextIndex = _selectableSpells.Count - 1;
                }
                else if (nextIndex >= _selectableSpells.Count)
                {
                    nextIndex = 0;
                }
            }
            else
            {
                nextIndex = Mathf.Clamp(nextIndex, 0, _selectableSpells.Count - 1);
            }

            selectedSpellId = _selectableSpells[nextIndex].SpellId;
            RefreshPage();
        }

        private bool IsPageAvailable(int pageIndex)
        {
            return spellBook == null ? pageIndex >= 0 && pageIndex < 5 : spellBook.IsSlotAvailable(pageIndex);
        }

        private string GetWrittenSpellIdOnCurrentPage()
        {
            return spellBook != null ? spellBook.GetEquippedSpellId(selectedPageIndex) : string.Empty;
        }

        private SpellData2D GetWrittenSpellOnCurrentPage()
        {
            if (spellLibrary == null)
            {
                return null;
            }

            return spellLibrary.GetSpellData(GetWrittenSpellIdOnCurrentPage());
        }

        private SpellData2D GetSelectedSpell()
        {
            if (spellLibrary == null || string.IsNullOrWhiteSpace(selectedSpellId))
            {
                return null;
            }

            return spellLibrary.GetSpellData(selectedSpellId);
        }

        private bool IsPreviewingUnwrittenSpell(SpellData2D writtenSpell, SpellData2D previewSpell)
        {
            if (previewSpell == null)
            {
                return false;
            }

            if (writtenSpell == null)
            {
                return true;
            }

            return !string.Equals(writtenSpell.SpellId, previewSpell.SpellId, StringComparison.Ordinal);
        }

        private int FindSelectableSpellIndex(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
            {
                return -1;
            }

            for (int i = 0; i < _selectableSpells.Count; i++)
            {
                SpellData2D spellData = _selectableSpells[i];
                if (spellData != null && spellData.SpellId == spellId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void SubscribeToStateEvents()
        {
            if (spellLibrary != null)
            {
                spellLibrary.OnSpellUnlockStateChanged += HandleSpellUnlockStateChanged;
            }

            if (spellBook != null)
            {
                spellBook.OnSlotChanged += HandleSlotChanged;
            }
        }

        private void UnsubscribeFromStateEvents()
        {
            if (spellLibrary != null)
            {
                spellLibrary.OnSpellUnlockStateChanged -= HandleSpellUnlockStateChanged;
            }

            if (spellBook != null)
            {
                spellBook.OnSlotChanged -= HandleSlotChanged;
            }
        }

        private void HandleSpellUnlockStateChanged(string spellId, bool unlocked)
        {
            RefreshPage();
        }

        private void HandleSlotChanged(int pageIndex, string spellId)
        {
            RefreshPage();
        }

        private static int GetSignedOffset(int itemIndex, int selectedIndex, int count)
        {
            int direct = itemIndex - selectedIndex;
            if (count <= 0)
            {
                return direct;
            }

            int wrappedLeft = direct - count;
            int wrappedRight = direct + count;
            int best = direct;

            if (Mathf.Abs(wrappedLeft) < Mathf.Abs(best))
            {
                best = wrappedLeft;
            }

            if (Mathf.Abs(wrappedRight) < Mathf.Abs(best))
            {
                best = wrappedRight;
            }

            return best;
        }

        private int ReadPageSelectionInput()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return 0;
                if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return 1;
                if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return 2;
                if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) return 3;
                if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) return 4;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 2;
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) return 3;
            if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) return 4;
#endif

            return -1;
        }

        private bool WasPreviousSpellPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.aKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.A);
#else
            return false;
#endif
        }

        private bool WasNextSpellPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.dKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.D);
#else
            return false;
#endif
        }

        private bool WasWritePressedThisFrame()
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

        private float ReadMouseScrollY()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.scroll.ReadValue().y;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mouseScrollDelta.y;
#else
            return 0f;
#endif
        }

        private void OnValidate()
        {
            selectedPageIndex = Mathf.Clamp(selectedPageIndex, 0, 4);
            selectedSpellId = string.IsNullOrWhiteSpace(selectedSpellId) ? string.Empty : selectedSpellId.Trim();
        }
    }
}
