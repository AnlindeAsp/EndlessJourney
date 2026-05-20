using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessJourney.UI
{
    /// <summary>
    /// One selectable weapon row in the forge inscription page.
    /// </summary>
    public class WeaponInscriptionWeaponRow2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text inscriptionText;
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private GameObject equippedIndicator;
        [SerializeField] private GameObject engravedIndicator;
        [SerializeField] private GameObject lockedIndicator;

        [Header("Visual State")]
        [SerializeField] private CanvasGroup rowCanvasGroup;
        [SerializeField, Range(0f, 1f)] private float selectedAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float unselectedAlpha = 0.6f;
        [SerializeField, Range(0f, 1f)] private float lockedAlpha = 0.35f;
        [SerializeField] private Color normalNameColor = Color.white;
        [SerializeField] private Color engravedNameColor = new Color(1f, 0.78f, 0.18f, 1f);
        [SerializeField] private Color lockedNameColor = new Color(0.55f, 0.55f, 0.55f, 1f);

        private string _weaponId = string.Empty;

        private void OnDisable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
            }
        }

        public void Bind(WeaponInscriptionWeaponViewData2D item, Action<string> onSelected)
        {
            if (item.WeaponData == null)
            {
                return;
            }

            _weaponId = item.WeaponData.WeaponId;
            SetText(nameText, item.WeaponData.WeaponName);
            SetText(inscriptionText, item.InscriptionData != null ? item.InscriptionData.InscriptionName : string.Empty);
            SetIcon(item.WeaponData.Icon);
            SetIndicator(selectedIndicator, item.Selected);
            SetIndicator(equippedIndicator, item.Equipped);
            SetIndicator(engravedIndicator, item.HasInscription);
            SetIndicator(lockedIndicator, !item.Unlocked);
            ApplyVisualState(item);

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.interactable = item.Unlocked;
                selectButton.onClick.AddListener(() => onSelected?.Invoke(_weaponId));
            }
        }

        private void ApplyVisualState(WeaponInscriptionWeaponViewData2D item)
        {
            if (rowCanvasGroup != null)
            {
                rowCanvasGroup.alpha = !item.Unlocked ? lockedAlpha : item.Selected ? selectedAlpha : unselectedAlpha;
            }

            if (nameText == null)
            {
                return;
            }

            if (!item.Unlocked)
            {
                nameText.color = lockedNameColor;
            }
            else if (item.HasInscription)
            {
                nameText.color = engravedNameColor;
            }
            else
            {
                nameText.color = normalNameColor;
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
