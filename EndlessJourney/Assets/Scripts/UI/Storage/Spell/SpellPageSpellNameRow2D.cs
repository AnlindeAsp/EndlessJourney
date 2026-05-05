using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace EndlessJourney.UI
{
    /// <summary>
    /// One selectable spell name row in the spell book list.
    /// </summary>
    public class SpellPageSpellNameRow2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private GameObject writtenOnCurrentPageIndicator;
        [FormerlySerializedAs("equippedIndicator")]
        [SerializeField] private GameObject writtenAnywhereIndicator;
        [SerializeField] private GameObject lockedIndicator;

        [Header("Visual State")]
        [FormerlySerializedAs("cardCanvasGroup")]
        [SerializeField] private CanvasGroup rowCanvasGroup;
        [SerializeField, Range(0f, 1f)] private float selectedAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float sideAlpha = 0.65f;
        [SerializeField, Range(0f, 1f)] private float farAlpha = 0.35f;
        [SerializeField] private Color normalNameColor = Color.white;
        [SerializeField] private Color selectedNameColor = Color.white;
        [SerializeField] private Color writtenNameColor = new Color(1f, 0.78f, 0.18f, 1f);
        [SerializeField] private Color lockedNameColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        private string _spellId = string.Empty;

        private void OnDisable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
            }
        }

        public void Bind(SpellPageSpellViewData2D item, Action<string> onSelected)
        {
            if (item.SpellData == null)
            {
                return;
            }

            _spellId = item.SpellData.SpellId;
            SetText(nameText, item.SpellData.DisplayName);
            SetIndicator(selectedIndicator, item.Selected);
            SetIndicator(writtenOnCurrentPageIndicator, item.WrittenOnCurrentPage);
            SetIndicator(writtenAnywhereIndicator, item.WrittenAnywhere && !item.WrittenOnCurrentPage);
            SetIndicator(lockedIndicator, !item.Unlocked);
            ApplyVisualState(item);

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onSelected?.Invoke(_spellId));
            }
        }

        public void BindSpacer()
        {
            _spellId = string.Empty;
            SetText(nameText, string.Empty);
            SetIndicator(selectedIndicator, false);
            SetIndicator(writtenOnCurrentPageIndicator, false);
            SetIndicator(writtenAnywhereIndicator, false);
            SetIndicator(lockedIndicator, false);

            if (rowCanvasGroup != null)
            {
                rowCanvasGroup.alpha = 0f;
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.interactable = false;
            }
        }

        private void ApplyVisualState(SpellPageSpellViewData2D item)
        {
            int distance = Mathf.Abs(item.SignedOffsetFromSelected);
            if (rowCanvasGroup != null)
            {
                rowCanvasGroup.alpha = item.Selected ? selectedAlpha : distance <= 1 ? sideAlpha : farAlpha;
            }

            if (nameText == null)
            {
                return;
            }

            if (!item.Unlocked)
            {
                nameText.color = lockedNameColor;
            }
            else if (item.WrittenOnCurrentPage || item.WrittenAnywhere)
            {
                nameText.color = writtenNameColor;
            }
            else if (item.Selected)
            {
                nameText.color = selectedNameColor;
            }
            else
            {
                nameText.color = normalNameColor;
            }
        }

        private static void SetIndicator(GameObject indicator, bool visible)
        {
            if (indicator != null)
            {
                indicator.SetActive(visible);
            }
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
