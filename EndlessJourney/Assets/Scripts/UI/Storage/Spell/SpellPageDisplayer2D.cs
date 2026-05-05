using System;
using System.Collections.Generic;
using EndlessJourney.Player;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EndlessJourney.UI
{
    public readonly struct SpellPageSpellViewData2D
    {
        public SpellPageSpellViewData2D(
            SpellData2D spellData,
            bool unlocked,
            bool writtenAnywhere,
            bool writtenOnCurrentPage,
            bool selected,
            int signedOffsetFromSelected)
        {
            SpellData = spellData;
            Unlocked = unlocked;
            WrittenAnywhere = writtenAnywhere;
            WrittenOnCurrentPage = writtenOnCurrentPage;
            Selected = selected;
            SignedOffsetFromSelected = signedOffsetFromSelected;
        }

        public SpellData2D SpellData { get; }
        public bool Unlocked { get; }
        public bool WrittenAnywhere { get; }
        public bool WrittenOnCurrentPage { get; }
        public bool Selected { get; }
        public int SignedOffsetFromSelected { get; }
    }

    public readonly struct SpellPageSlotViewData2D
    {
        public SpellPageSlotViewData2D(int pageIndex, bool available, string writtenSpellId, SpellData2D writtenSpell, bool selected)
        {
            SlotIndex = pageIndex;
            Available = available;
            EquippedSpellId = writtenSpellId ?? string.Empty;
            EquippedSpell = writtenSpell;
            Selected = selected;
        }

        public int SlotIndex { get; }
        public bool Available { get; }
        public string EquippedSpellId { get; }
        public SpellData2D EquippedSpell { get; }
        public bool Selected { get; }
    }

    /// <summary>
    /// Renders the storage spell page as a spell book. Logic and record changes stay in SpellPageController2D.
    /// </summary>
    public class SpellPageDisplayer2D : MonoBehaviour
    {
        [Header("Book Pages")]
        [FormerlySerializedAs("slotParent")]
        [SerializeField] private Transform pageButtonParent;
        [FormerlySerializedAs("slotButtonPrefab")]
        [SerializeField] private SpellPageSlotButton2D pageButtonPrefab;

        [Header("Spell Name List")]
        [FormerlySerializedAs("spellCardParent")]
        [SerializeField] private Transform spellNameParent;
        [FormerlySerializedAs("spellCardPrefab")]
        [SerializeField] private SpellPageSpellNameRow2D spellNameRowPrefab;
        [FormerlySerializedAs("visibleSideCardCount")]
        [SerializeField, Min(0)] private int visibleSideNameCount = 4;
        [SerializeField] private bool keepSelectedNameRowCentered = true;
        [SerializeField] private bool resetNameParentAnchoredPosition = true;
        [SerializeField] private GameObject emptySpellStateRoot;

        [Header("Current Page")]
        [FormerlySerializedAs("selectedSlotText")]
        [SerializeField] private TMP_Text currentPageText;
        [SerializeField] private TMP_Text writtenSpellText;

        [Header("Spell Data")]
        [SerializeField] private TMP_Text spellNameText;
        [SerializeField] private TMP_Text spellIdText;
        [SerializeField] private TMP_Text spellKindText;
        [SerializeField] private TMP_Text spellCastText;

        [Header("Spell Description")]
        [SerializeField] private TMP_Text spellDescriptionText;
        [SerializeField] private TMP_Text spellSupplementaryNoteText;
        [SerializeField] private CanvasGroup descriptionCanvasGroup;
        [SerializeField, Range(0f, 1f)] private float previewBlinkMinAlpha = 0.45f;
        [SerializeField, Range(0f, 1f)] private float previewBlinkMaxAlpha = 1f;
        [SerializeField, Min(0.01f)] private float previewBlinkSpeed = 1.6f;

        [Header("Actions")]
        [FormerlySerializedAs("equipButton")]
        [SerializeField] private Button writeButton;
        [FormerlySerializedAs("clearSlotButton")]
        [SerializeField] private Button eraseButton;
        [FormerlySerializedAs("equipButtonText")]
        [SerializeField] private TMP_Text writeButtonText;
        [FormerlySerializedAs("clearSlotButtonText")]
        [SerializeField] private TMP_Text eraseButtonText;

        private readonly List<SpellPageSlotButton2D> _spawnedPageButtons = new List<SpellPageSlotButton2D>(5);
        private readonly List<SpellPageSpellNameRow2D> _spawnedNameRows = new List<SpellPageSpellNameRow2D>(16);
        private bool _previewBlinkActive;

        private void Update()
        {
            if (descriptionCanvasGroup == null)
            {
                return;
            }

            if (!_previewBlinkActive)
            {
                descriptionCanvasGroup.alpha = 1f;
                return;
            }

            float t = (Mathf.Sin(Time.unscaledTime * previewBlinkSpeed) + 1f) * 0.5f;
            descriptionCanvasGroup.alpha = Mathf.Lerp(previewBlinkMinAlpha, previewBlinkMaxAlpha, t);
        }

        private void OnDisable()
        {
            _previewBlinkActive = false;
            if (descriptionCanvasGroup != null)
            {
                descriptionCanvasGroup.alpha = 1f;
            }

            ClearActionButtons();
        }

        public void Render(
            IReadOnlyList<SpellPageSpellViewData2D> spells,
            IReadOnlyList<SpellPageSlotViewData2D> pages,
            SpellData2D displayedSpell,
            SpellData2D writtenSpell,
            SpellData2D previewSpell,
            int selectedPageIndex,
            bool previewingUnwrittenSpell,
            Action<string> onSelectSpell,
            Action<int> onSelectPage,
            Action onWriteSelected,
            Action onEraseCurrentPage)
        {
            _previewBlinkActive = previewingUnwrittenSpell;

            RenderPageButtons(pages, onSelectPage);
            RenderSpellNameList(spells, onSelectSpell);
            RenderCurrentPage(selectedPageIndex, writtenSpell, previewSpell, previewingUnwrittenSpell);
            RenderSpellDetails(displayedSpell, previewingUnwrittenSpell);
            BindActionButtons(displayedSpell, writtenSpell, pages, selectedPageIndex, onWriteSelected, onEraseCurrentPage);
        }

        private void RenderPageButtons(IReadOnlyList<SpellPageSlotViewData2D> pages, Action<int> onSelectPage)
        {
            ClearPageButtons();

            if (pages == null || pageButtonParent == null || pageButtonPrefab == null)
            {
                return;
            }

            for (int i = 0; i < pages.Count; i++)
            {
                SpellPageSlotButton2D pageButton = Instantiate(pageButtonPrefab, pageButtonParent);
                pageButton.Bind(pages[i], onSelectPage);
                _spawnedPageButtons.Add(pageButton);
            }
        }

        private void RenderSpellNameList(IReadOnlyList<SpellPageSpellViewData2D> spells, Action<string> onSelectSpell)
        {
            ClearNameRows();
            ResetNameParentPositionIfNeeded();

            bool hasSpells = spells != null && spells.Count > 0;
            if (emptySpellStateRoot != null)
            {
                emptySpellStateRoot.SetActive(!hasSpells);
            }

            if (!hasSpells || spellNameParent == null || spellNameRowPrefab == null)
            {
                return;
            }

            if (keepSelectedNameRowCentered)
            {
                RenderCenteredSpellNameWindow(spells, onSelectSpell);
                return;
            }

            List<SpellPageSpellViewData2D> visibleSpells = new List<SpellPageSpellViewData2D>(spells.Count);
            for (int i = 0; i < spells.Count; i++)
            {
                SpellPageSpellViewData2D spell = spells[i];
                if (Mathf.Abs(spell.SignedOffsetFromSelected) <= visibleSideNameCount)
                {
                    visibleSpells.Add(spell);
                }
            }

            visibleSpells.Sort((a, b) => a.SignedOffsetFromSelected.CompareTo(b.SignedOffsetFromSelected));

            for (int i = 0; i < visibleSpells.Count; i++)
            {
                SpellPageSpellNameRow2D row = Instantiate(spellNameRowPrefab, spellNameParent);
                row.Bind(visibleSpells[i], onSelectSpell);
                _spawnedNameRows.Add(row);
            }
        }

        private void RenderCenteredSpellNameWindow(IReadOnlyList<SpellPageSpellViewData2D> spells, Action<string> onSelectSpell)
        {
            int sideCount = Mathf.Max(0, visibleSideNameCount);
            for (int offset = -sideCount; offset <= sideCount; offset++)
            {
                SpellPageSpellNameRow2D row = Instantiate(spellNameRowPrefab, spellNameParent);
                if (TryFindSpellByOffset(spells, offset, out SpellPageSpellViewData2D item))
                {
                    row.Bind(item, onSelectSpell);
                }
                else
                {
                    row.BindSpacer();
                }

                _spawnedNameRows.Add(row);
            }
        }

        private void ResetNameParentPositionIfNeeded()
        {
            if (!resetNameParentAnchoredPosition || spellNameParent == null)
            {
                return;
            }

            if (spellNameParent is RectTransform rectTransform)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        private void RenderCurrentPage(int selectedPageIndex, SpellData2D writtenSpell, SpellData2D previewSpell, bool previewingUnwrittenSpell)
        {
            SetText(currentPageText, $"Page {selectedPageIndex + 1}");

            if (writtenSpell != null)
            {
                SetText(writtenSpellText, previewingUnwrittenSpell
                    ? $"Written: {writtenSpell.DisplayName}\nPreviewing: {previewSpell.DisplayName}"
                    : $"Written: {writtenSpell.DisplayName}");
                return;
            }

            SetText(writtenSpellText, previewSpell != null ? "Blank page - previewing" : "Blank page");
        }

        private void RenderSpellDetails(SpellData2D spell, bool previewingUnwrittenSpell)
        {
            if (spell == null)
            {
                SetText(spellNameText, "No spell selected");
                SetText(spellIdText, string.Empty);
                SetText(spellKindText, string.Empty);
                SetText(spellCastText, string.Empty);
                SetText(spellDescriptionText, string.Empty);
                SetText(spellSupplementaryNoteText, string.Empty);
                return;
            }

            SetText(spellNameText, previewingUnwrittenSpell ? $"{spell.DisplayName} (Preview)" : spell.DisplayName);
            SetText(spellIdText, spell.SpellId);
            SetText(spellKindText, spell.Kind.ToString());
            SetText(spellCastText, $"Mana {spell.ManaCost:0.##}\nSing {spell.SingingTime:0.##}s\nCast {spell.CastTime:0.##}s\nCooldown {spell.CastCooldown:0.##}s");
            SetText(spellDescriptionText, spell.Description);
            SetText(spellSupplementaryNoteText, spell.SupplementaryNote);
        }

        private void BindActionButtons(
            SpellData2D displayedSpell,
            SpellData2D writtenSpell,
            IReadOnlyList<SpellPageSlotViewData2D> pages,
            int selectedPageIndex,
            Action onWriteSelected,
            Action onEraseCurrentPage)
        {
            ClearActionButtons();

            bool pageAvailable = TryFindPage(pages, selectedPageIndex, out SpellPageSlotViewData2D page) && page.Available;
            bool hasPreviewSpell = displayedSpell != null;
            bool hasWrittenSpell = writtenSpell != null;
            bool displayedSpellAlreadyWritten = displayedSpell != null
                && writtenSpell != null
                && string.Equals(displayedSpell.SpellId, writtenSpell.SpellId, StringComparison.Ordinal);

            if (writeButton != null)
            {
                writeButton.interactable = pageAvailable && hasPreviewSpell && !displayedSpellAlreadyWritten;
                writeButton.onClick.AddListener(() => onWriteSelected?.Invoke());
            }

            if (eraseButton != null)
            {
                eraseButton.interactable = pageAvailable && hasWrittenSpell;
                eraseButton.onClick.AddListener(() => onEraseCurrentPage?.Invoke());
            }

            SetText(writeButtonText, displayedSpellAlreadyWritten ? "Written" : "Write");
            SetText(eraseButtonText, "Erase");
        }

        private void ClearPageButtons()
        {
            for (int i = 0; i < _spawnedPageButtons.Count; i++)
            {
                SpellPageSlotButton2D pageButton = _spawnedPageButtons[i];
                if (pageButton != null)
                {
                    Destroy(pageButton.gameObject);
                }
            }

            _spawnedPageButtons.Clear();
        }

        private void ClearNameRows()
        {
            for (int i = 0; i < _spawnedNameRows.Count; i++)
            {
                SpellPageSpellNameRow2D row = _spawnedNameRows[i];
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _spawnedNameRows.Clear();
        }

        private void ClearActionButtons()
        {
            if (writeButton != null)
            {
                writeButton.onClick.RemoveAllListeners();
            }

            if (eraseButton != null)
            {
                eraseButton.onClick.RemoveAllListeners();
            }
        }

        private static bool TryFindPage(IReadOnlyList<SpellPageSlotViewData2D> pages, int pageIndex, out SpellPageSlotViewData2D page)
        {
            if (pages != null)
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    SpellPageSlotViewData2D candidate = pages[i];
                    if (candidate.SlotIndex == pageIndex)
                    {
                        page = candidate;
                        return true;
                    }
                }
            }

            page = default;
            return false;
        }

        private static bool TryFindSpellByOffset(IReadOnlyList<SpellPageSpellViewData2D> spells, int offset, out SpellPageSpellViewData2D spell)
        {
            if (spells != null)
            {
                for (int i = 0; i < spells.Count; i++)
                {
                    SpellPageSpellViewData2D candidate = spells[i];
                    if (candidate.SignedOffsetFromSelected == offset)
                    {
                        spell = candidate;
                        return true;
                    }
                }
            }

            spell = default;
            return false;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }
    }
}
