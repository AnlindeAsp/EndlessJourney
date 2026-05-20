using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessJourney.UI
{
    /// <summary>
    /// One selectable inscription row in the forge inscription page.
    /// </summary>
    public class WeaponInscriptionChoiceRow2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private GameObject engravedIndicator;

        [Header("Visual State")]
        [SerializeField] private CanvasGroup rowCanvasGroup;
        [SerializeField, Range(0f, 1f)] private float selectedAlpha = 1f;
        [SerializeField, Range(0f, 1f)] private float unselectedAlpha = 0.6f;
        [SerializeField] private Color normalNameColor = Color.white;
        [SerializeField] private Color engravedNameColor = new Color(1f, 0.78f, 0.18f, 1f);

        private string _inscriptionId = string.Empty;

        private void OnDisable()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
            }
        }

        public void Bind(WeaponInscriptionChoiceViewData2D item, Action<string> onSelected)
        {
            if (item.InscriptionData == null)
            {
                return;
            }

            _inscriptionId = item.InscriptionData.InscriptionId;
            SetText(nameText, item.InscriptionData.InscriptionName);
            SetText(effectText, item.InscriptionData.EffectType.ToString());
            SetIndicator(selectedIndicator, item.Selected);
            SetIndicator(engravedIndicator, item.EngravedOnSelectedWeapon);
            ApplyVisualState(item);

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(() => onSelected?.Invoke(_inscriptionId));
            }
        }

        private void ApplyVisualState(WeaponInscriptionChoiceViewData2D item)
        {
            if (rowCanvasGroup != null)
            {
                rowCanvasGroup.alpha = item.Selected ? selectedAlpha : unselectedAlpha;
            }

            if (nameText != null)
            {
                nameText.color = item.EngravedOnSelectedWeapon ? engravedNameColor : normalNameColor;
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
