using EndlessJourney.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessJourney.UI
{
    /// <summary>
    /// UGUI display for player health.
    /// - Reads PlayerHealth2D
    /// - Updates fill bar by percentage
    /// - Displays health text near bar end
    /// </summary>
    public class HealthDisplayer : MonoBehaviour
    {
        /// <summary>
        /// Formatting mode for health value text.
        /// </summary>
        public enum ValueTextMode
        {
            CurrentOnly,
            CurrentAndMax,
            Percent
        }

        [Header("Data Source")]
        [SerializeField] private PlayerHealth2D healthSource;
        [SerializeField] private bool autoFindInParents = true;

        [Header("Bar")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private bool autoConfigureFillImage = true;
        [SerializeField] private Color normalHealthColor = new Color(0.92f, 0.20f, 0.25f, 1f);
        [SerializeField] private Color lowHealthColor = new Color(0.68f, 0.08f, 0.10f, 1f);
        [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.25f;

        [Header("Value Label")]
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private ValueTextMode valueTextMode = ValueTextMode.CurrentAndMax;
        [SerializeField, Range(0, 2)] private int valueDecimals = 0;
        [SerializeField] private RectTransform barRect;
        [SerializeField] private RectTransform valueLabelRect;
        [SerializeField] private Vector2 valueLabelOffset = new Vector2(0f, 14f);
        [SerializeField] private bool clampValueLabelInsideBar = true;

        /// <summary>
        /// Initial setup for source and visual defaults.
        /// </summary>
        private void Awake()
        {
            TryResolveSource();
            EnsureFillImageSetup();
            ApplyBaseVisualState();
        }

        /// <summary>
        /// Subscribes to health updates and refreshes UI.
        /// </summary>
        private void OnEnable()
        {
            TryResolveSource();
            EnsureFillImageSetup();
            Subscribe();
            RefreshFromSource();
        }

        /// <summary>
        /// Unsubscribes from health updates.
        /// </summary>
        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Clamps inspector values and reapplies visuals in edit mode.
        /// </summary>
        private void OnValidate()
        {
            valueDecimals = Mathf.Clamp(valueDecimals, 0, 2);
            lowHealthThreshold = Mathf.Clamp01(lowHealthThreshold);
            EnsureFillImageSetup();
            ApplyBaseVisualState();
        }

        /// <summary>
        /// Auto-finds PlayerHealth2D in parent hierarchy.
        /// </summary>
        private void TryResolveSource()
        {
            if (healthSource != null || !autoFindInParents)
            {
                return;
            }

            healthSource = GetComponentInParent<PlayerHealth2D>();
        }

        /// <summary>
        /// Subscribes to health change event.
        /// </summary>
        private void Subscribe()
        {
            if (healthSource == null)
            {
                return;
            }

            healthSource.OnHealthChanged += OnHealthChanged;
        }

        /// <summary>
        /// Unsubscribes from health change event.
        /// </summary>
        private void Unsubscribe()
        {
            if (healthSource == null)
            {
                return;
            }

            healthSource.OnHealthChanged -= OnHealthChanged;
        }

        /// <summary>
        /// Event callback for health changes.
        /// </summary>
        private void OnHealthChanged(float currentHealth, float maxHealth)
        {
            UpdateDisplay(currentHealth, maxHealth);
        }

        /// <summary>
        /// Refreshes UI immediately from current health state.
        /// </summary>
        private void RefreshFromSource()
        {
            if (healthSource == null)
            {
                return;
            }

            UpdateDisplay(healthSource.CurrentHealth, healthSource.MaxHealth);
        }

        /// <summary>
        /// Updates health fill, text content, text position, and bar color.
        /// </summary>
        private void UpdateDisplay(float currentHealth, float maxHealth)
        {
            float healthPercent = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = healthPercent;
                healthFillImage.color = healthPercent <= lowHealthThreshold ? lowHealthColor : normalHealthColor;
            }

            if (valueText != null)
            {
                valueText.text = BuildValueText(currentHealth, maxHealth);
            }

            UpdateValueLabelPosition(healthPercent);
        }

        /// <summary>
        /// Builds formatted health text according to selected mode.
        /// </summary>
        private string BuildValueText(float currentHealth, float maxHealth)
        {
            string format = "F" + valueDecimals;

            switch (valueTextMode)
            {
                case ValueTextMode.CurrentOnly:
                    return currentHealth.ToString(format);
                case ValueTextMode.Percent:
                    float percent = maxHealth > 0f ? (currentHealth / maxHealth) * 100f : 0f;
                    return percent.ToString(format) + "%";
                default:
                    // return $"{currentHealth.ToString(format)}/{maxHealth.ToString(format)}";
                    return $"{currentHealth.ToString(format)}";
            }
        }

        /// <summary>
        /// Moves value label to the end of health fill amount.
        /// </summary>
        private void UpdateValueLabelPosition(float healthPercent)
        {
            if (barRect == null || valueLabelRect == null)
            {
                return;
            }

            Vector3 worldLeft = barRect.TransformPoint(new Vector3(barRect.rect.xMin, 0f, 0f));
            Vector3 worldRight = barRect.TransformPoint(new Vector3(barRect.rect.xMax, 0f, 0f));
            Vector3 worldEnd = Vector3.Lerp(worldLeft, worldRight, healthPercent);

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
        /// Applies initial visual values for edit/runtime consistency.
        /// </summary>
        private void ApplyBaseVisualState()
        {
            if (healthFillImage != null)
            {
                healthFillImage.color = normalHealthColor;
            }
        }

        /// <summary>
        /// Ensures health bar image is configured for percentage fill rendering.
        /// </summary>
        private void EnsureFillImageSetup()
        {
            if (!autoConfigureFillImage || healthFillImage == null)
            {
                return;
            }

            healthFillImage.type = Image.Type.Filled;
            healthFillImage.fillMethod = Image.FillMethod.Horizontal;
            healthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            healthFillImage.fillClockwise = true;
        }
    }
}
