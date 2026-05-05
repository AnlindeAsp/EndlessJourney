using EndlessJourney.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessJourney.UI
{
    /// <summary>
    /// UGUI display for player armor durability.
    /// - Reads PlayerArmor2D
    /// - Updates fill bar by durability percentage
    /// - Displays armor value near bar end
    /// </summary>
    public class ArmorDisplayer : MonoBehaviour
    {
        public enum ValueTextMode
        {
            CurrentOnly,
            CurrentAndMax,
            Percent,
            BrokenState
        }

        [Header("Data Source")]
        [SerializeField] private PlayerArmor2D armorSource;
        [SerializeField] private bool autoFindInParents = true;

        [Header("Bar")]
        [SerializeField] private Image armorFillImage;
        [SerializeField] private bool autoConfigureFillImage = true;
        [SerializeField] private Color normalArmorColor = new Color(0.72f, 0.78f, 0.82f, 1f);
        [SerializeField] private Color lowArmorColor = new Color(0.48f, 0.52f, 0.56f, 1f);
        [SerializeField] private Color brokenArmorColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        [SerializeField, Range(0f, 1f)] private float lowArmorThreshold = 0.25f;

        [Header("Value Label")]
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private ValueTextMode valueTextMode = ValueTextMode.CurrentAndMax;
        [SerializeField, Range(0, 2)] private int valueDecimals = 0;
        [SerializeField] private RectTransform barRect;
        [SerializeField] private RectTransform valueLabelRect;
        [SerializeField] private Vector2 valueLabelOffset = new Vector2(0f, 14f);
        [SerializeField] private bool clampValueLabelInsideBar = true;

        private void Awake()
        {
            TryResolveSource();
            EnsureFillImageSetup();
            ApplyBaseVisualState();
        }

        private void OnEnable()
        {
            TryResolveSource();
            EnsureFillImageSetup();
            Subscribe();
            RefreshFromSource();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnValidate()
        {
            valueDecimals = Mathf.Clamp(valueDecimals, 0, 2);
            lowArmorThreshold = Mathf.Clamp01(lowArmorThreshold);
            EnsureFillImageSetup();
            ApplyBaseVisualState();
        }

        private void TryResolveSource()
        {
            if (armorSource != null || !autoFindInParents)
            {
                return;
            }

            armorSource = GetComponentInParent<PlayerArmor2D>();
        }

        private void Subscribe()
        {
            if (armorSource == null)
            {
                return;
            }

            armorSource.OnArmorChanged += OnArmorChanged;
        }

        private void Unsubscribe()
        {
            if (armorSource == null)
            {
                return;
            }

            armorSource.OnArmorChanged -= OnArmorChanged;
        }

        private void OnArmorChanged(float currentDurability, float maxDurability, bool broken)
        {
            UpdateDisplay(currentDurability, maxDurability, broken);
        }

        private void RefreshFromSource()
        {
            if (armorSource == null)
            {
                return;
            }

            UpdateDisplay(armorSource.CurrentDurability, armorSource.MaxDurability, armorSource.IsBroken);
        }

        private void UpdateDisplay(float currentDurability, float maxDurability, bool broken)
        {
            float armorPercent = maxDurability > 0f ? Mathf.Clamp01(currentDurability / maxDurability) : 0f;

            if (armorFillImage != null)
            {
                armorFillImage.fillAmount = armorPercent;
                armorFillImage.color = ResolveArmorColor(armorPercent, broken);
            }

            if (valueText != null)
            {
                valueText.text = BuildValueText(currentDurability, maxDurability, broken);
            }

            UpdateValueLabelPosition(armorPercent);
        }

        private Color ResolveArmorColor(float armorPercent, bool broken)
        {
            if (broken)
            {
                return brokenArmorColor;
            }

            return armorPercent <= lowArmorThreshold ? lowArmorColor : normalArmorColor;
        }

        private string BuildValueText(float currentDurability, float maxDurability, bool broken)
        {
            string format = "F" + valueDecimals;

            switch (valueTextMode)
            {
                case ValueTextMode.CurrentOnly:
                    return currentDurability.ToString(format);
                case ValueTextMode.Percent:
                    float percent = maxDurability > 0f ? (currentDurability / maxDurability) * 100f : 0f;
                    return percent.ToString(format) + "%";
                case ValueTextMode.BrokenState:
                    return broken ? "Broken" : currentDurability.ToString(format);
                default:
                    return currentDurability.ToString(format);
            }
        }

        private void UpdateValueLabelPosition(float armorPercent)
        {
            if (barRect == null || valueLabelRect == null)
            {
                return;
            }

            Vector3 worldLeft = barRect.TransformPoint(new Vector3(barRect.rect.xMin, 0f, 0f));
            Vector3 worldRight = barRect.TransformPoint(new Vector3(barRect.rect.xMax, 0f, 0f));
            Vector3 worldEnd = Vector3.Lerp(worldLeft, worldRight, armorPercent);

            float minX = Mathf.Min(worldLeft.x, worldRight.x);
            float maxX = Mathf.Max(worldLeft.x, worldRight.x);

            float targetX = worldEnd.x + valueLabelOffset.x;
            if (clampValueLabelInsideBar)
            {
                targetX = Mathf.Clamp(targetX, minX, maxX);
            }

            float targetY = worldEnd.y + valueLabelOffset.y;
            valueLabelRect.position = new Vector3(targetX, targetY, valueLabelRect.position.z);
        }

        private void ApplyBaseVisualState()
        {
            if (armorFillImage != null)
            {
                armorFillImage.color = normalArmorColor;
            }
        }

        private void EnsureFillImageSetup()
        {
            if (!autoConfigureFillImage || armorFillImage == null)
            {
                return;
            }

            armorFillImage.type = Image.Type.Filled;
            armorFillImage.fillMethod = Image.FillMethod.Horizontal;
            armorFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            armorFillImage.fillClockwise = true;
        }
    }
}
