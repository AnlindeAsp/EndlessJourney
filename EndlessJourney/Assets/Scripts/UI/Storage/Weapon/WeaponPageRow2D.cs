using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessJourney.UI
{
    /// <summary>
    /// One selectable row in the storage weapon list.
    /// </summary>
    public class WeaponPageRow2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private GameObject equippedIndicator;
        [SerializeField] private GameObject lockedIndicator;

        [Header("Visual State")]
        [SerializeField] private CanvasGroup rowCanvasGroup;
        [SerializeField, Range(0f, 1f)] private float selectedAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float unselectedAlpha = 0.55f;
        [SerializeField] private Color normalNameColor = Color.white;
        [SerializeField] private Color equippedNameColor = new Color(1f, 0.78f, 0.18f, 1f);
        [SerializeField] private Color lockedNameColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        private string _weaponId = string.Empty;

        private void OnDisable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
            }
        }

        public void Bind(WeaponPageItemViewData2D item, Action<string> onSelected)
        {
            if (item.WeaponData == null)
            {
                return;
            }

            _weaponId = item.WeaponData.WeaponId;

            SetText(nameText, item.WeaponData.WeaponName);
            SetText(stateText, item.Equipped ? "Equipped" : item.Unlocked ? "Unlocked" : "Locked");
            SetIndicator(selectedIndicator, item.Selected);
            SetIndicator(equippedIndicator, item.Equipped);
            SetIndicator(lockedIndicator, !item.Unlocked);
            SetIcon(item.WeaponData.Icon);
            ApplyVisualState(item);

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onSelected?.Invoke(_weaponId));
            }
        }

        private void ApplyVisualState(WeaponPageItemViewData2D item)
        {
            if (rowCanvasGroup != null)
            {
                rowCanvasGroup.alpha = item.Selected ? selectedAlpha : unselectedAlpha;
            }

            if (nameText != null)
            {
                if (item.Equipped)
                {
                    nameText.color = equippedNameColor;
                }
                else if (!item.Unlocked)
                {
                    nameText.color = lockedNameColor;
                }
                else
                {
                    nameText.color = normalNameColor;
                }
            }
        }

        private void SetIcon(Sprite icon)
        {
            if (iconImage == null)
            {
                return;
            }

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
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
