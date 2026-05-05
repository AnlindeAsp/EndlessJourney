using System;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Armor durability module for the player.
    /// Armor reduces harm while it has any durability left, then loses durability by the absorbed amount.
    /// The final active hit always gets the full reduction effect even if durability is lower than the absorbed amount.
    /// </summary>
    public class PlayerArmor2D : MonoBehaviour
    {
        [Header("Armor")]
        [SerializeField, Min(1f)] private float maxDurability = 1000f;
        [SerializeField] private bool startAtMaxDurability = true;
        [SerializeField, Min(0f)] private float startingDurability = 1000f;
        [SerializeField, Range(0f, 1f)] private float damageReductionEfficiency = 0.6f;

        [Header("Behavior")]
        [SerializeField] private bool armorEnabled = true;

        private float _currentDurability;
        private bool _wasBroken;

        public float CurrentDurability => _currentDurability;
        public float MaxDurability => maxDurability;
        public float DamageReductionEfficiency => damageReductionEfficiency;
        public bool ArmorEnabled => armorEnabled;
        public bool IsBroken => !armorEnabled || _currentDurability <= 0f;
        public float DurabilityNormalized => maxDurability > 0f ? Mathf.Clamp01(_currentDurability / maxDurability) : 0f;

        public event Action<float, float, bool> OnArmorChanged;
        public event Action<float, float, float> OnArmorAbsorbed;
        public event Action OnArmorBroken;
        public event Action OnArmorRepaired;

        private void Awake()
        {
            InitializeArmor();
        }

        /// <summary>
        /// Applies armor reduction to incoming harm and returns the final damage dealt to health.
        /// </summary>
        public float ApplyToIncomingHarm(float incomingDamage)
        {
            if (incomingDamage <= 0f)
            {
                return 0f;
            }

            if (IsBroken || damageReductionEfficiency <= 0f)
            {
                return incomingDamage;
            }

            float absorbedDamage = incomingDamage * damageReductionEfficiency;
            float finalDamage = Mathf.Max(0f, incomingDamage - absorbedDamage);

            SetDurabilityInternal(_currentDurability - absorbedDamage, true);
            OnArmorAbsorbed?.Invoke(incomingDamage, absorbedDamage, finalDamage);

            return finalDamage;
        }

        public void Repair(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            SetDurabilityInternal(_currentDurability + amount, true);
        }

        public void RestoreFullDurability()
        {
            SetDurabilityInternal(maxDurability, true);
        }

        public void SetDurability(float value)
        {
            SetDurabilityInternal(value, true);
        }

        public void SetArmorEnabled(bool enabled)
        {
            if (armorEnabled == enabled)
            {
                return;
            }

            armorEnabled = enabled;
            NotifyArmorChanged(true);
        }

        private void InitializeArmor()
        {
            float initialDurability = startAtMaxDurability ? maxDurability : startingDurability;
            _currentDurability = Mathf.Clamp(initialDurability, 0f, maxDurability);
            _wasBroken = IsBroken;
            NotifyArmorChanged(false);
        }

        private void SetDurabilityInternal(float value, bool notify)
        {
            bool wasBroken = IsBroken;
            _currentDurability = Mathf.Clamp(value, 0f, maxDurability);
            bool isBroken = IsBroken;

            if (notify)
            {
                NotifyArmorChanged(false);
            }

            if (!wasBroken && isBroken)
            {
                OnArmorBroken?.Invoke();
            }
            else if (wasBroken && !isBroken)
            {
                OnArmorRepaired?.Invoke();
            }

            _wasBroken = isBroken;
        }

        private void NotifyArmorChanged(bool forceBrokenEventCheck)
        {
            bool isBroken = IsBroken;
            OnArmorChanged?.Invoke(_currentDurability, maxDurability, isBroken);

            if (!forceBrokenEventCheck || _wasBroken == isBroken)
            {
                _wasBroken = isBroken;
                return;
            }

            if (isBroken)
            {
                OnArmorBroken?.Invoke();
            }
            else
            {
                OnArmorRepaired?.Invoke();
            }

            _wasBroken = isBroken;
        }

        private void OnValidate()
        {
            maxDurability = Mathf.Max(1f, maxDurability);
            startingDurability = Mathf.Clamp(startingDurability, 0f, maxDurability);
            damageReductionEfficiency = Mathf.Clamp01(damageReductionEfficiency);
        }
    }
}
