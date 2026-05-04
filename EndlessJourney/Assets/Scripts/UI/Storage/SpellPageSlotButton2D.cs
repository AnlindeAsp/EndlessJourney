using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessJourney.UI
{
    /// <summary>
    /// One selectable spell book page. Each page maps to one SpellBook slot.
    /// </summary>
    public class SpellPageSlotButton2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text slotIndexText;
        [SerializeField] private TMP_Text equippedSpellText;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private GameObject unavailableIndicator;

        [Header("Visual State")]
        [SerializeField] private CanvasGroup slotCanvasGroup;
        [SerializeField, Range(0f, 1f)] private float selectedAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float unselectedAlpha = 0.65f;
        [SerializeField, Range(0f, 1f)] private float unavailableAlpha = 0.35f;

        private int _slotIndex;

        private void OnDisable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
            }
        }

        public void Bind(SpellPageSlotViewData2D item, Action<int> onSelected)
        {
            _slotIndex = item.SlotIndex;
            SetText(slotIndexText, (item.SlotIndex + 1).ToString());
            SetText(equippedSpellText, item.EquippedSpell != null ? item.EquippedSpell.DisplayName : "Blank");
            SetIndicator(selectedIndicator, item.Selected);
            SetIndicator(unavailableIndicator, !item.Available);
            ApplyVisualState(item);

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.interactable = item.Available;
                selectButton.onClick.AddListener(() => onSelected?.Invoke(_slotIndex));
            }
        }

        private void ApplyVisualState(SpellPageSlotViewData2D item)
        {
            if (slotCanvasGroup == null)
            {
                return;
            }

            slotCanvasGroup.alpha = !item.Available ? unavailableAlpha : item.Selected ? selectedAlpha : unselectedAlpha;
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
