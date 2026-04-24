using EndlessJourney.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessJourney.UI
{
    /// <summary>
    /// UGUI display for dual-pool mana (Mana + PotentialMana).
    /// - Potential bar is rendered under normal mana bar.
    /// - Value label can follow the end of the normal mana fill.
    /// - Optional overload blink when PotentialManaAllow is active.
    /// </summary>
    public class ManaDisplay : MonoBehaviour
    {
        /// <summary>
        /// Formatting mode for value text.
        /// </summary>
        public enum ValueTextMode
        {
            CurrentOnly,
            CurrentAndMax,
            BothPools
        }

        [Header("Data Source")]
        [SerializeField] private PlayerMana2D manaSource;
        [SerializeField] private bool autoFindInParents = true;

        [Header("Bar Images (Fill)")]
        [SerializeField] private Image normalManaFillImage;
        [SerializeField] private Image potentialManaFillImage;
        [SerializeField] private bool enforcePotentialUnderNormal = true;
        [Tooltip("Auto-set bar images to Image.Type=Filled for correct percentage display.")]
        [SerializeField] private bool autoConfigureFillImages = true;

        [Header("Colors")]
        [SerializeField] private Color normalManaColor = new Color(0.52f, 0.83f, 1f, 1f);
        [SerializeField] private Color potentialManaColor = new Color(0.14f, 0.28f, 0.56f, 1f);

        [Header("Overload Blink")]
        [Tooltip("Blink slightly while PotentialManaAllow is active.")]
        [SerializeField] private bool enableOverloadBlink = true;
        [SerializeField, Min(0f)] private float blinkCyclesPerSecond = 0.8f;
        [SerializeField, Range(0f, 1f)] private float blinkMinAlpha = 0.78f;
        [SerializeField, Range(0f, 1f)] private float blinkMaxAlpha = 1f;
        [Tooltip("When true, both bars blink. Otherwise only normal mana bar blinks.")]
        [SerializeField] private bool blinkBothBars = true;

        [Header("Value Label")]
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private ValueTextMode valueTextMode = ValueTextMode.CurrentAndMax;
        [SerializeField, Range(0, 2)] private int valueDecimals = 0;
        [Tooltip("When true, CurrentOnly/CurrentAndMax switches to PotentialMana in exhausted state.")]
        [SerializeField] private bool showPotentialValueWhenExhausted = true;
        [Tooltip("When true, value text turns potential color once PotentialMana is exposed.")]
        [SerializeField] private bool usePotentialStateTextColor = true;
        [SerializeField] private Color normalValueTextColor = new Color(0.52f, 0.83f, 1f, 1f);
        [SerializeField] private Color potentialValueTextColor = new Color(0.14f, 0.28f, 0.56f, 1f);
        [Tooltip("When true, negative normal mana (mana debt) is shown in a dedicated color.")]
        [SerializeField] private bool useDebtTextColor = true;
        [SerializeField] private Color debtValueTextColor = Color.black;
        [SerializeField] private RectTransform barRect;
        [SerializeField] private RectTransform valueLabelRect;
        [SerializeField] private Vector2 valueLabelOffset = new Vector2(0f, 14f);
        [SerializeField] private bool clampValueLabelInsideBar = true;

        /// <summary>
        /// Initial setup of source and visual defaults.
        /// </summary>
        private void Awake()
        {
            TryResolveSource();
            EnsureFillImageSetup();
            ApplyBarColors();
            ApplyLayerOrder();
        }

        /// <summary>
        /// Subscribes to mana events and refreshes display when enabled.
        /// </summary>
        private void OnEnable()
        {
            TryResolveSource();
            EnsureFillImageSetup();
            Subscribe();
            RefreshFromSource();
        }

        /// <summary>
        /// Unsubscribes from mana events when disabled.
        /// </summary>
        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Clamps inspector values and reapplies visual defaults in edit mode.
        /// </summary>
        private void OnValidate()
        {
            valueDecimals = Mathf.Clamp(valueDecimals, 0, 2);
            blinkCyclesPerSecond = Mathf.Max(0f, blinkCyclesPerSecond);
            blinkMinAlpha = Mathf.Clamp01(blinkMinAlpha);
            blinkMaxAlpha = Mathf.Clamp01(blinkMaxAlpha);
            if (blinkMinAlpha > blinkMaxAlpha)
            {
                float temp = blinkMinAlpha;
                blinkMinAlpha = blinkMaxAlpha;
                blinkMaxAlpha = temp;
            }

            EnsureFillImageSetup();
            ApplyBarColors();
            ApplyLayerOrder();
        }

        /// <summary>
        /// Auto-finds PlayerMana2D in parent hierarchy if enabled.
        /// </summary>
        private void TryResolveSource()
        {
            if (manaSource != null || !autoFindInParents)
            {
                return;
            }

            manaSource = GetComponentInParent<PlayerMana2D>();
        }

        /// <summary>
        /// Subscribes to mana state change event.
        /// </summary>
        private void Subscribe()
        {
            if (manaSource == null)
            {
                return;
            }

            manaSource.OnManaStateChanged += OnManaStateChanged;
        }

        /// <summary>
        /// Unsubscribes from mana state change event.
        /// </summary>
        private void Unsubscribe()
        {
            if (manaSource == null)
            {
                return;
            }

            manaSource.OnManaStateChanged -= OnManaStateChanged;
        }

        /// <summary>
        /// Event callback that updates the display from latest mana values.
        /// </summary>
        private void OnManaStateChanged(float currentMana, float maxMana, float currentPotential, float maxPotential)
        {
            UpdateDisplay(currentMana, maxMana, currentPotential, maxPotential);
        }

        /// <summary>
        /// Refreshes UI immediately from current mana source state.
        /// </summary>
        private void RefreshFromSource()
        {
            if (manaSource == null)
            {
                return;
            }

            UpdateDisplay(
                manaSource.CurrentMana,
                manaSource.MaxMana,
                manaSource.CurrentPotentialMana,
                manaSource.MaxPotentialMana
            );
        }

        /// <summary>
        /// Per-frame visual update for overload blink effect.
        /// </summary>
        private void Update()
        {
            UpdateBlinkVisuals();
        }

        /// <summary>
        /// Updates fill amounts, value text, and value label position.
        /// </summary>
        private void UpdateDisplay(float currentMana, float maxMana, float currentPotential, float maxPotential)
        {
            float normalPercent = maxMana > 0f ? Mathf.Clamp01(currentMana / maxMana) : 0f;
            float potentialPercent = maxPotential > 0f ? Mathf.Clamp01(currentPotential / maxPotential) : 0f;
            bool isExhausted = IsExhausted(currentPotential, maxPotential);
            bool isManaDebt = currentMana < 0f;

            if (potentialManaFillImage != null)
            {
                SetImagePercent(potentialManaFillImage, potentialPercent);
            }

            if (normalManaFillImage != null)
            {
                SetImagePercent(normalManaFillImage, normalPercent);
            }

            if (valueText != null)
            {
                valueText.text = BuildValueText(currentMana, maxMana, currentPotential, maxPotential, isExhausted, isManaDebt);
                valueText.color = ResolveValueTextColor(currentPotential, maxPotential, isManaDebt);
            }

            float labelPercent = (showPotentialValueWhenExhausted && isExhausted && valueTextMode != ValueTextMode.BothPools && !isManaDebt)
                ? potentialPercent
                : normalPercent;
            UpdateValueLabelPosition(labelPercent);
        }

        /// <summary>
        /// Moves the value label to the end of the normal mana fill.
        /// </summary>
        private void UpdateValueLabelPosition(float normalPercent)
        {
            if (barRect == null || valueLabelRect == null)
            {
                return;
            }

            Vector3 worldLeft = barRect.TransformPoint(new Vector3(barRect.rect.xMin, 0f, 0f));
            Vector3 worldRight = barRect.TransformPoint(new Vector3(barRect.rect.xMax, 0f, 0f));
            Vector3 worldEnd = Vector3.Lerp(worldLeft, worldRight, normalPercent);

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

        /// <summary>
        /// Builds formatted value string according to selected text mode.
        /// </summary>
        private string BuildValueText(float currentMana, float maxMana, float currentPotential, float maxPotential, bool isExhausted, bool isManaDebt)
        {
            string format = "F" + valueDecimals;
            bool usePotentialValue = showPotentialValueWhenExhausted && isExhausted && !isManaDebt;

            switch (valueTextMode)
            {
                case ValueTextMode.CurrentOnly:
                    return (usePotentialValue ? currentPotential : currentMana).ToString(format);
                case ValueTextMode.BothPools:
                    return $"M {currentMana.ToString(format)}/{maxMana.ToString(format)}  P {currentPotential.ToString(format)}/{maxPotential.ToString(format)}";
                default:
                    return usePotentialValue
                        ? $"{currentPotential.ToString(format)}"// /{maxPotential.ToString(format)}"
                        : $"{currentMana.ToString(format)}";// /{maxMana.ToString(format)}";
            }
        }

        /// <summary>
        /// Returns true when display should be considered in exhausted state.
        /// </summary>
        private bool IsExhausted(float currentPotential, float maxPotential)
        {
            return currentPotential < maxPotential - 0.001f;
        }

        /// <summary>
        /// Chooses text color based on whether potential mana has been exposed.
        /// </summary>
        private Color ResolveValueTextColor(float currentPotential, float maxPotential, bool isManaDebt)
        {
            if (useDebtTextColor && isManaDebt)
            {
                return debtValueTextColor;
            }

            if (!usePotentialStateTextColor)
            {
                return normalValueTextColor;
            }

            bool inPotentialState = currentPotential < maxPotential - 0.001f;
            return inPotentialState ? potentialValueTextColor : normalValueTextColor;
        }

        /// <summary>
        /// Applies configured base colors to bar images.
        /// </summary>
        private void ApplyBarColors()
        {
            if (normalManaFillImage != null)
            {
                normalManaFillImage.color = normalManaColor;
            }

            if (potentialManaFillImage != null)
            {
                potentialManaFillImage.color = potentialManaColor;
            }
        }

        /// <summary>
        /// Updates overload blink alpha when overload mode is active.
        /// </summary>
        private void UpdateBlinkVisuals()
        {
            if (!enableOverloadBlink || manaSource == null)
            {
                ApplyBarColors();
                return;
            }

            // Blink should start only after entering exhausted state.
            if (!manaSource.ManaExhausting)
            {
                ApplyBarColors();
                return;
            }

            float omega = Mathf.PI * 2f * blinkCyclesPerSecond;
            float t = (Mathf.Sin(Time.unscaledTime * omega) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha, t);

            if (normalManaFillImage != null)
            {
                Color c = normalManaColor;
                c.a = normalManaColor.a * alpha;
                normalManaFillImage.color = c;
            }

            if (potentialManaFillImage != null)
            {
                Color c = potentialManaColor;
                c.a = potentialManaColor.a * (blinkBothBars ? alpha : 1f);
                potentialManaFillImage.color = c;
            }
        }

        /// <summary>
        /// Ensures potential bar is drawn below normal bar when both share a parent.
        /// </summary>
        private void ApplyLayerOrder()
        {
            if (!enforcePotentialUnderNormal || normalManaFillImage == null || potentialManaFillImage == null)
            {
                return;
            }

            if (normalManaFillImage.transform.parent != potentialManaFillImage.transform.parent)
            {
                return;
            }

            int normalIndex = normalManaFillImage.transform.GetSiblingIndex();
            int targetPotentialIndex = Mathf.Max(0, normalIndex - 1);
            potentialManaFillImage.transform.SetSiblingIndex(targetPotentialIndex);
        }

        /// <summary>
        /// Ensures bar images are configured for fillAmount-based percentage rendering.
        /// </summary>
        private void EnsureFillImageSetup()
        {
            if (!autoConfigureFillImages)
            {
                return;
            }

            ConfigureImageAsHorizontalFill(normalManaFillImage);
            ConfigureImageAsHorizontalFill(potentialManaFillImage);
        }

        /// <summary>
        /// Applies a normalized percentage to an image. Assumes filled image configuration.
        /// </summary>
        private void SetImagePercent(Image image, float normalizedValue)
        {
            if (image == null)
            {
                return;
            }

            image.fillAmount = Mathf.Clamp01(normalizedValue);
        }

        /// <summary>
        /// Converts an image to left-to-right horizontal fill mode.
        /// </summary>
        private static void ConfigureImageAsHorizontalFill(Image image)
        {
            if (image == null)
            {
                return;
            }

            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillClockwise = true;
        }
    }
}
