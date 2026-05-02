using System;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Dual-pool mana module for player entities.
    /// - Mana: normal cast resource
    /// - PotentialMana: hidden overload reserve
    /// Includes spend/restore rules, natural regen, and mana-out health penalty.
    /// </summary>
    public class PlayerMana2D : MonoBehaviour
    {
        [Header("Mana Pools")]
        [SerializeField, Min(1f)] private float maxMana = 100f;
        [SerializeField, Min(1f)] private float maxPotentialMana = 100f;
        [SerializeField, Min(0f)] private float startingMana = 100f;
        [SerializeField, Min(0f)] private float startingPotentialMana = 100f;

        [Header("Overload")]
        [Tooltip("When true, mana spend can overflow into PotentialMana.")]
        [SerializeField] private bool potentialManaAllow = false;
        [Tooltip("When true in overload mode, one last cast can be forced even if mana is insufficient, creating negative mana debt.")]
        [SerializeField] private bool forlornCast = false;

        [Header("Natural Regeneration")]
        [Tooltip("Normal state: PotentialMana contributes this percentage per second.")]
        [SerializeField, Min(0f)] private float normalPotentialRegenPercentPerSecond = 0.10f;
        [Tooltip("Normal state: Mana contributes this percentage per second.")]
        [SerializeField, Min(0f)] private float normalManaRegenPercentPerSecond = 0.05f;
        [Tooltip("Exhausting state: regen is reduced to PotentialMana * this percentage per second.")]
        [SerializeField, Min(0f)] private float exhaustingPotentialRegenPercentPerSecond = 0.05f;
        [SerializeField, Min(0f)] private float regenMultiplier = 1f;

        [Header("Mana-Out Damage")]
        [SerializeField] private bool applyDamageWhenManaOut = true;
        [SerializeField, Min(0f)] private float manaOutDamagePerSecond = 8f;
        [SerializeField] private PlayerHealth2D health;

        private float _currentMana;
        private float _currentPotentialMana;
        private bool _externalNaturalRegenBlocked;

        /// <summary>Current normal mana value.</summary>
        public float CurrentMana => _currentMana;

        /// <summary>Maximum normal mana.</summary>
        public float MaxMana => maxMana;

        /// <summary>Current potential mana value.</summary>
        public float CurrentPotentialMana => _currentPotentialMana;

        /// <summary>Maximum potential mana.</summary>
        public float MaxPotentialMana => maxPotentialMana;

        /// <summary>Total mana across both pools.</summary>
        public float NetMana => _currentMana + _currentPotentialMana;

        /// <summary>Normalized normal mana in range [0, 1].</summary>
        public float ManaNormalized => maxMana > 0f ? _currentMana / maxMana : 0f;

        /// <summary>Normalized potential mana in range [0, 1].</summary>
        public float PotentialManaNormalized => maxPotentialMana > 0f ? _currentPotentialMana / maxPotentialMana : 0f;

        /// <summary>
        /// True when PotentialMana is not full.
        /// </summary>
        public bool ManaExhausting => _currentPotentialMana < maxPotentialMana - 0.001f;

        /// <summary>
        /// True when PotentialMana is fully depleted.
        /// </summary>
        public bool ManaOut => _currentPotentialMana <= 0.001f;

        /// <summary>
        /// Whether overload spending into PotentialMana is allowed.
        /// </summary>
        public bool PotentialManaAllow
        {
            get => potentialManaAllow;
            set => potentialManaAllow = value;
        }

        /// <summary>
        /// Whether forced last-cast behavior is enabled in overload mode.
        /// </summary>
        public bool ForlornCast
        {
            get => forlornCast;
            set => forlornCast = value;
        }

        /// <summary>
        /// True when normal mana is below zero (mana debt).
        /// </summary>
        public bool HasManaDebt => _currentMana < 0f;

        /// <summary>
        /// External multiplier for natural mana regen.
        /// </summary>
        public float RegenMultiplier
        {
            get => regenMultiplier;
            set => regenMultiplier = Mathf.Max(0f, value);
        }

        /// <summary>
        /// True when external systems are temporarily blocking natural regeneration.
        /// </summary>
        public bool IsNaturalRegenBlockedExternally => _externalNaturalRegenBlocked;

        /// <summary>
        /// Raised whenever normal mana changes. Args: current, max.
        /// </summary>
        public event Action<float, float> OnManaChanged;

        /// <summary>
        /// Raised whenever potential mana changes. Args: current, max.
        /// </summary>
        public event Action<float, float> OnPotentialManaChanged;

        /// <summary>
        /// Raised whenever either pool changes. Args: manaCurrent, manaMax, potentialCurrent, potentialMax.
        /// </summary>
        public event Action<float, float, float, float> OnManaStateChanged;

        /// <summary>
        /// Raised when mana is spent. Arg: spent amount.
        /// </summary>
        public event Action<float> OnManaSpent;

        /// <summary>
        /// Raised when mana is restored. Arg: restored amount.
        /// </summary>
        public event Action<float> OnManaRestored;

        /// <summary>
        /// Raised when ManaOut state changes. Arg: current ManaOut state.
        /// </summary>
        public event Action<bool> OnManaOutChanged;

        /// <summary>
        /// Temporarily enables/disables natural mana regeneration from external systems.
        /// </summary>
        public void SetNaturalRegenBlocked(bool blocked)
        {
            _externalNaturalRegenBlocked = blocked;
        }

        /// <summary>
        /// Initializes pool values and auto-finds PlayerHealth2D for mana-out damage.
        /// </summary>
        private void Awake()
        {
            if (health == null)
            {
                health = GetComponent<PlayerHealth2D>();
            }

            InitializeMana();
        }

        /// <summary>
        /// Runs natural regeneration and mana-out damage every frame.
        /// </summary>
        private void Update()
        {
            ApplyNaturalRegen(Time.deltaTime);
            ApplyManaOutDamage(Time.deltaTime);
        }

        /// <summary>
        /// Returns true if the requested cost can be paid with current rules.
        /// - PotentialManaAllow = false: only CurrentMana can be used.
        /// - PotentialManaAllow = true: CurrentMana + CurrentPotentialMana can be used.
        /// - ForlornCast in overload mode: allows one final forced cast before debt exists.
        /// </summary>
        /// <param name="cost">Requested mana cost.</param>
        public bool HasEnoughMana(float cost)
        {
            if (cost <= 0f)
            {
                return true;
            }

            if (!potentialManaAllow)
            {
                return _currentMana >= cost;
            }

            if (NetMana >= cost)
            {
                return true;
            }

            // Forlorn cast is only valid before entering debt.
            if (forlornCast && _currentMana >= 0f)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to spend mana. Returns true when payment succeeds.
        /// Spend order: normal Mana first, then PotentialMana (if overload is allowed).
        /// In forlorn-cast mode, unresolved remainder becomes negative normal mana.
        /// </summary>
        /// <param name="cost">Mana cost to spend.</param>
        public bool TrySpendMana(float cost)
        {
            if (cost <= 0f)
            {
                return true;
            }

            if (!HasEnoughMana(cost))
            {
                return false;
            }

            bool wasManaOut = ManaOut;
            float remaining = cost;

            // Negative mana is debt and cannot be spent as available resource.
            float availableMana = Mathf.Max(0f, _currentMana);
            float fromMana = Mathf.Min(availableMana, remaining);
            _currentMana -= fromMana;
            remaining -= fromMana;

            if (remaining > 0f && potentialManaAllow)
            {
                float availablePotential = Mathf.Max(0f, _currentPotentialMana);
                float fromPotential = Mathf.Min(availablePotential, remaining);
                _currentPotentialMana -= fromPotential;
                remaining -= fromPotential;
            }

            // Force the last cast by converting missing remainder into mana debt.
            if (remaining > 0f && potentialManaAllow && forlornCast && _currentMana >= 0f)
            {
                _currentMana -= remaining;
                remaining = 0f;
            }

            float spent = cost - remaining;
            if (spent <= 0f)
            {
                return false;
            }

            OnManaSpent?.Invoke(spent);
            RaiseManaEvents();
            NotifyManaOutIfChanged(wasManaOut);
            return true;
        }

        /// <summary>
        /// Restores mana with priority:
        /// 1) PotentialMana until full
        /// 2) Mana
        /// </summary>
        /// <param name="amount">Restore amount.</param>
        public bool RestoreMana(float amount)
        {
            if (amount <= 0f)
            {
                return false;
            }

            bool wasManaOut = ManaOut;
            float appliedRestore = AddRecoveryWithPriority(amount);

            if (appliedRestore <= 0f)
            {
                return false;
            }

            OnManaRestored?.Invoke(appliedRestore);
            RaiseManaEvents();
            NotifyManaOutIfChanged(wasManaOut);
            return true;
        }

        /// <summary>
        /// Sets normal mana directly (clamped to [0, maxMana]).
        /// </summary>
        /// <param name="value">New normal mana value.</param>
        public void SetMana(float value)
        {
            bool wasManaOut = ManaOut;
            _currentMana = Mathf.Clamp(value, 0f, maxMana);
            RaiseManaEvents();
            NotifyManaOutIfChanged(wasManaOut);
        }

        /// <summary>
        /// Sets potential mana directly (clamped to [0, maxPotentialMana]).
        /// </summary>
        /// <param name="value">New potential mana value.</param>
        public void SetPotentialMana(float value)
        {
            bool wasManaOut = ManaOut;
            _currentPotentialMana = Mathf.Clamp(value, 0f, maxPotentialMana);
            RaiseManaEvents();
            NotifyManaOutIfChanged(wasManaOut);
        }

        /// <summary>
        /// Sets both mana pools directly.
        /// </summary>
        /// <param name="manaValue">New normal mana value.</param>
        /// <param name="potentialManaValue">New potential mana value.</param>
        public void SetManaState(float manaValue, float potentialManaValue)
        {
            bool wasManaOut = ManaOut;
            _currentMana = Mathf.Clamp(manaValue, 0f, maxMana);
            _currentPotentialMana = Mathf.Clamp(potentialManaValue, 0f, maxPotentialMana);
            RaiseManaEvents();
            NotifyManaOutIfChanged(wasManaOut);
        }

        /// <summary>
        /// Initializes both mana pools from starting values and emits initial events.
        /// </summary>
        private void InitializeMana()
        {
            _currentMana = Mathf.Clamp(startingMana, 0f, maxMana);
            _currentPotentialMana = Mathf.Clamp(startingPotentialMana, 0f, maxPotentialMana);
            RaiseManaEvents();
            NotifyManaOutIfChanged(false, forceNotify: true);
        }

        /// <summary>
        /// Applies per-frame natural regeneration based on current mana state.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        private void ApplyNaturalRegen(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            if (_externalNaturalRegenBlocked)
            {
                return;
            }

            // Negative mana (debt) state never receives natural mana regeneration.
            if (_currentMana < 0f)
            {
                return;
            }

            float regenPerSecond;
            if (ManaExhausting)
            {
                regenPerSecond = _currentPotentialMana * exhaustingPotentialRegenPercentPerSecond;
            }
            else
            {
                regenPerSecond = (_currentPotentialMana * normalPotentialRegenPercentPerSecond) +
                                 (_currentMana * normalManaRegenPercentPerSecond);
            }

            if (regenPerSecond <= 0f)
            {
                return;
            }

            regenPerSecond *= regenMultiplier;
            if (regenPerSecond <= 0f)
            {
                return;
            }

            bool wasManaOut = ManaOut;
            float restored = AddRecoveryWithPriority(regenPerSecond * deltaTime);
            if (restored > 0f)
            {
                OnManaRestored?.Invoke(restored);
                RaiseManaEvents();
                NotifyManaOutIfChanged(wasManaOut);
            }
        }

        /// <summary>
        /// Applies periodic health damage while mana-out state is active.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        private void ApplyManaOutDamage(float deltaTime)
        {
            if (!applyDamageWhenManaOut || !ManaOut || health == null || deltaTime <= 0f)
            {
                return;
            }

            // Mana-out drain is non-harm health loss: it should not trigger hit invincibility.
            health.ApplyNonHarmHealthLoss(manaOutDamagePerSecond * deltaTime);
        }

        /// <summary>
        /// Adds recovery amount with priority: PotentialMana first, then Mana.
        /// </summary>
        /// <param name="amount">Recovery amount to distribute.</param>
        /// <returns>Actual restored amount.</returns>
        private float AddRecoveryWithPriority(float amount)
        {
            if (amount <= 0f)
            {
                return 0f;
            }

            float restored = 0f;
            float remaining = amount;

            if (_currentPotentialMana < maxPotentialMana)
            {
                float beforePotential = _currentPotentialMana;
                _currentPotentialMana = Mathf.Min(maxPotentialMana, _currentPotentialMana + remaining);
                float toPotential = _currentPotentialMana - beforePotential;
                restored += toPotential;
                remaining -= toPotential;
            }

            if (remaining > 0f && _currentMana < maxMana)
            {
                float beforeMana = _currentMana;
                _currentMana = Mathf.Min(maxMana, _currentMana + remaining);
                float toMana = _currentMana - beforeMana;
                restored += toMana;
            }

            return restored;
        }

        /// <summary>
        /// Emits all mana state change events.
        /// </summary>
        private void RaiseManaEvents()
        {
            OnManaChanged?.Invoke(_currentMana, maxMana);
            OnPotentialManaChanged?.Invoke(_currentPotentialMana, maxPotentialMana);
            OnManaStateChanged?.Invoke(_currentMana, maxMana, _currentPotentialMana, maxPotentialMana);
        }

        /// <summary>
        /// Emits mana-out change event when state toggles (or when forced).
        /// </summary>
        /// <param name="previousState">Previous mana-out state.</param>
        /// <param name="forceNotify">When true, emits regardless of state change.</param>
        private void NotifyManaOutIfChanged(bool previousState, bool forceNotify = false)
        {
            bool currentState = ManaOut;
            if (forceNotify || currentState != previousState)
            {
                OnManaOutChanged?.Invoke(currentState);
            }
        }

        /// <summary>
        /// Clamps inspector values to valid ranges.
        /// </summary>
        private void OnValidate()
        {
            maxMana = Mathf.Max(1f, maxMana);
            maxPotentialMana = Mathf.Max(1f, maxPotentialMana);
            startingMana = Mathf.Clamp(startingMana, 0f, maxMana);
            startingPotentialMana = Mathf.Clamp(startingPotentialMana, 0f, maxPotentialMana);
            normalPotentialRegenPercentPerSecond = Mathf.Max(0f, normalPotentialRegenPercentPerSecond);
            normalManaRegenPercentPerSecond = Mathf.Max(0f, normalManaRegenPercentPerSecond);
            exhaustingPotentialRegenPercentPerSecond = Mathf.Max(0f, exhaustingPotentialRegenPercentPerSecond);
            regenMultiplier = Mathf.Max(0f, regenMultiplier);
            manaOutDamagePerSecond = Mathf.Max(0f, manaOutDamagePerSecond);
        }
    }
}
