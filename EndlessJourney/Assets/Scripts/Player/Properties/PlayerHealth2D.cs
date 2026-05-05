using System;
using EndlessJourney.Interfaces;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Health module for player entities.
    /// Responsibilities:
    /// - Current/Max health state
    /// - Taking damage / healing
    /// - Death check/state
    /// - Out-of-combat natural regeneration
    /// </summary>
    public class PlayerHealth2D : MonoBehaviour, IPlayerHarmful
    {
        [Header("Health")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool startAtMaxHealth = true;
        [SerializeField, Min(0f)] private float startingHealth = 100f;
        [SerializeField] private bool allowHealingWhenDead = false;

        [Header("Armor")]
        [Tooltip("Optional armor module. Assign manually when this health should receive armor reduction.")]
        [SerializeField] private PlayerArmor2D armorSource;
        [SerializeField] private bool applyArmorToHarmDamage = true;

        [Header("Damage Invincibility")]
        [SerializeField] private bool enableHitInvincibility = true;
        [SerializeField, Min(0f)] private float invincibilityDuration = 0.45f;
        [SerializeField] private bool enableInvincibilityFlicker = true;
        [SerializeField, Min(0.01f)] private float flickerInterval = 0.08f;
        [SerializeField, Range(0.05f, 1f)] private float flickerAlphaMultiplier = 0.35f;
        [Tooltip("Optional renderers for flicker. If empty, auto-finds SpriteRenderer(s) in children.")]
        [SerializeField] private SpriteRenderer[] flickerRenderers;

        [Header("Combat State")]
        [Tooltip("When true, taking damage refreshes combat timer.")]
        [SerializeField] private bool autoEnterCombatOnDamage = true;
        [Tooltip("Seconds after last damage before leaving combat.")]
        [SerializeField, Min(0f)] private float combatExitDelay = 5f;

        [Header("Natural Regeneration")]
        [SerializeField] private bool enableNaturalRegen = true;
        [Tooltip("Seconds between each natural regen tick.")]
        [SerializeField, Min(0.01f)] private float regenInterval = 5f;
        [Tooltip("Health restored each regen tick before applying regenMultiplier.")]
        [SerializeField, Min(0f)] private float regenAmount = 1f;
        [Tooltip("No natural regen when health is below this normalized threshold.")]
        [SerializeField, Range(0f, 1f)] private float noRegenBelowNormalizedHealth = 0.5f;
        [SerializeField, Min(0f)] private float regenMultiplier = 1f;

        private float _currentHealth;
        private bool _isDead;
        private float _combatTimer;
        private bool _forcedInCombat;
        private float _regenTimer;
        private float _invincibilityTimer;
        private float _flickerTimer;
        private bool _flickerLowAlpha;
        private Color[] _baseRendererColors;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => _isDead;
        public bool IsInvincible => enableHitInvincibility && _invincibilityTimer > 0f;
        public float HealthNormalized => maxHealth > 0f ? _currentHealth / maxHealth : 0f;
        public bool IsInCombat => _forcedInCombat || _combatTimer > 0f;

        /// <summary>
        /// External multiplier for natural health regen.
        /// </summary>
        public float RegenMultiplier
        {
            get => regenMultiplier;
            set => regenMultiplier = Mathf.Max(0f, value);
        }

        public event Action<float, float> OnHealthChanged;
        public event Action<float> OnDamaged;
        public event Action<float, GameObject> OnHarmDamaged;
        public event Action<float, float, float, GameObject> OnHarmDamageResolved;
        public event Action<float> OnNonHarmHealthLost;
        public event Action<float> OnHealed;
        public event Action OnDied;

        /// <summary>
        /// Last non-null source that successfully harmed the player.
        /// Useful for future hit reactions/aggro/event logic.
        /// </summary>
        public GameObject LastHarmSource { get; private set; }

        private void Awake()
        {
            CacheFlickerRenderers();
            InitializeHealth();
        }

        private void Update()
        {
            TickInvincibility(Time.deltaTime);
            TickCombatState(Time.deltaTime);
            ApplyNaturalRegen(Time.deltaTime);
        }

        /// <summary>
        /// Compatibility API.
        /// Treated as harm damage (triggers invincibility rules).
        /// </summary>
        public bool TakeDamage(float amount)
        {
            return TakeHarmDamage(amount);
        }

        /// <summary>
        /// Harm damage path.
        /// - Respects invincibility
        /// - Can enter combat
        /// - Triggers post-hit invincibility when still alive
        /// </summary>
        public bool TakeHarmDamage(float amount)
        {
            if (_isDead || amount <= 0f || IsInvincible)
            {
                return false;
            }

            float finalDamage = ResolveHarmDamageAfterArmor(amount);
            if (finalDamage <= 0f)
            {
                if (autoEnterCombatOnDamage)
                {
                    EnterCombat();
                }

                StartInvincibility();
                return true;
            }

            bool applied = ApplyHealthLossCore(finalDamage, autoEnterCombatOnDamage, out _);
            if (!applied)
            {
                return false;
            }

            if (!_isDead)
            {
                StartInvincibility();
            }

            return true;
        }

        /// <summary>
        /// Non-harm health loss path (for mana-out, poison, scripted drain, etc.).
        /// - Ignores invincibility
        /// - Does NOT start invincibility
        /// - Optional combat entry (default false)
        /// </summary>
        public bool ApplyNonHarmHealthLoss(float amount, bool enterCombat = false)
        {
            if (_isDead || amount <= 0f)
            {
                return false;
            }

            bool applied = ApplyHealthLossCore(amount, enterCombat, out float appliedAmount);
            if (!applied)
            {
                return false;
            }

            OnNonHarmHealthLost?.Invoke(appliedAmount);
            return true;
        }

        /// <summary>
        /// Interface entry point for external harm systems.
        /// </summary>
        public bool ReceiveHarm(float amount, GameObject source)
        {
            if (!CanReceiveHarm())
            {
                return false;
            }

            bool applied = TakeHarmDamage(amount);
            if (!applied)
            {
                return false;
            }

            LastHarmSource = source;
            OnHarmDamaged?.Invoke(amount, source);
            OnHarmDamageResolved?.Invoke(amount, GetLastResolvedHarmDamage(amount), GetLastResolvedArmorAbsorption(amount), source);

            return true;
        }

        /// <summary>
        /// Returns whether this health module can currently receive harm.
        /// </summary>
        public bool CanReceiveHarm()
        {
            return !_isDead && !IsInvincible;
        }

        /// <summary>
        /// Restores health up to max health.
        /// Returns true if healing changed health.
        /// </summary>
        public bool Heal(float amount)
        {
            if (amount <= 0f)
            {
                return false;
            }

            if (_isDead && !allowHealingWhenDead)
            {
                return false;
            }

            float previous = _currentHealth;
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            float appliedHeal = _currentHealth - previous;

            if (appliedHeal <= 0f)
            {
                return false;
            }

            OnHealed?.Invoke(appliedHeal);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
            return true;
        }

        /// <summary>
        /// Sets health directly (clamped to [0, maxHealth]).
        /// Useful for checkpoints/loading/debug.
        /// </summary>
        public void SetHealth(float value)
        {
            _currentHealth = Mathf.Clamp(value, 0f, maxHealth);
            _isDead = _currentHealth <= 0f;
            if (_isDead)
            {
                StopInvincibility(true);
            }
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>
        /// Revives the player and restores health (full by default).
        /// </summary>
        public void Revive(bool fullHeal = true)
        {
            _isDead = false;
            _currentHealth = fullHeal ? maxHealth : Mathf.Max(1f, _currentHealth);
            StopInvincibility(true);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>
        /// Forces combat state on/off. Useful for boss arenas or scripted phases.
        /// </summary>
        public void SetForcedInCombat(bool inCombat)
        {
            _forcedInCombat = inCombat;
        }

        /// <summary>
        /// Refreshes combat timer.
        /// </summary>
        public void EnterCombat()
        {
            _combatTimer = combatExitDelay;
            _regenTimer = 0f;
        }

        private void InitializeHealth()
        {
            float initial = startAtMaxHealth ? maxHealth : startingHealth;
            _currentHealth = Mathf.Clamp(initial, 0f, maxHealth);
            _isDead = _currentHealth <= 0f;
            _regenTimer = 0f;
            StopInvincibility(true);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        private void StartInvincibility()
        {
            if (!enableHitInvincibility || invincibilityDuration <= 0f)
            {
                return;
            }

            _invincibilityTimer = invincibilityDuration;
            _flickerTimer = 0f;
            _flickerLowAlpha = false;
            ApplyFlickerAlpha(1f);
        }

        private void TickInvincibility(float deltaTime)
        {
            if (_invincibilityTimer <= 0f)
            {
                return;
            }

            _invincibilityTimer = Mathf.Max(0f, _invincibilityTimer - deltaTime);
            if (_invincibilityTimer <= 0f)
            {
                StopInvincibility(true);
                return;
            }

            if (!enableInvincibilityFlicker)
            {
                return;
            }

            _flickerTimer -= deltaTime;
            if (_flickerTimer > 0f)
            {
                return;
            }

            _flickerTimer = Mathf.Max(0.01f, flickerInterval);
            _flickerLowAlpha = !_flickerLowAlpha;
            ApplyFlickerAlpha(_flickerLowAlpha ? flickerAlphaMultiplier : 1f);
        }

        private void StopInvincibility(bool restoreVisual)
        {
            _invincibilityTimer = 0f;
            _flickerTimer = 0f;
            _flickerLowAlpha = false;

            if (restoreVisual)
            {
                ApplyFlickerAlpha(1f);
            }
        }

        private void CacheFlickerRenderers()
        {
            if (flickerRenderers == null || flickerRenderers.Length == 0)
            {
                flickerRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            if (flickerRenderers == null)
            {
                flickerRenderers = Array.Empty<SpriteRenderer>();
            }

            _baseRendererColors = new Color[flickerRenderers.Length];
            for (int i = 0; i < flickerRenderers.Length; i++)
            {
                SpriteRenderer renderer = flickerRenderers[i];
                _baseRendererColors[i] = renderer != null ? renderer.color : Color.white;
            }
        }

        private void ApplyFlickerAlpha(float alphaMultiplier)
        {
            if (flickerRenderers == null || _baseRendererColors == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(alphaMultiplier);
            int count = Mathf.Min(flickerRenderers.Length, _baseRendererColors.Length);
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer renderer = flickerRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Color baseColor = _baseRendererColors[i];
                renderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * clamped);
            }
        }

        private void TickCombatState(float deltaTime)
        {
            if (deltaTime <= 0f || _combatTimer <= 0f)
            {
                return;
            }

            _combatTimer = Mathf.Max(0f, _combatTimer - deltaTime);
        }

        private void ApplyNaturalRegen(float deltaTime)
        {
            if (deltaTime <= 0f || !enableNaturalRegen || _isDead || IsInCombat)
            {
                _regenTimer = 0f;
                return;
            }

            if (HealthNormalized < noRegenBelowNormalizedHealth)
            {
                _regenTimer = 0f;
                return;
            }

            float finalRegenAmount = regenAmount * regenMultiplier;
            if (finalRegenAmount <= 0f)
            {
                return;
            }

            _regenTimer += deltaTime;
            while (_regenTimer >= regenInterval)
            {
                _regenTimer -= regenInterval;
                Heal(finalRegenAmount);

                if (_isDead || IsInCombat || HealthNormalized < noRegenBelowNormalizedHealth)
                {
                    _regenTimer = 0f;
                    break;
                }
            }
        }

        private void Die()
        {
            if (_isDead)
            {
                return;
            }

            _isDead = true;
            OnDied?.Invoke();
        }

        private float _lastIncomingHarmDamage;
        private float _lastFinalHarmDamage;
        private float _lastArmorAbsorbedDamage;

        private float ResolveHarmDamageAfterArmor(float incomingDamage)
        {
            _lastIncomingHarmDamage = incomingDamage;
            _lastFinalHarmDamage = incomingDamage;
            _lastArmorAbsorbedDamage = 0f;

            if (!applyArmorToHarmDamage || armorSource == null)
            {
                return incomingDamage;
            }

            float finalDamage = armorSource.ApplyToIncomingHarm(incomingDamage);
            _lastFinalHarmDamage = Mathf.Max(0f, finalDamage);
            _lastArmorAbsorbedDamage = Mathf.Max(0f, incomingDamage - _lastFinalHarmDamage);
            return _lastFinalHarmDamage;
        }

        private float GetLastResolvedHarmDamage(float fallbackIncomingDamage)
        {
            return Mathf.Approximately(_lastIncomingHarmDamage, fallbackIncomingDamage)
                ? _lastFinalHarmDamage
                : fallbackIncomingDamage;
        }

        private float GetLastResolvedArmorAbsorption(float fallbackIncomingDamage)
        {
            return Mathf.Approximately(_lastIncomingHarmDamage, fallbackIncomingDamage)
                ? _lastArmorAbsorbedDamage
                : 0f;
        }

        /// <summary>
        /// Shared health deduction core used by both harm and non-harm paths.
        /// </summary>
        private bool ApplyHealthLossCore(float amount, bool enterCombat, out float appliedDamage)
        {
            appliedDamage = 0f;

            if (enterCombat)
            {
                EnterCombat();
            }

            float previous = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            appliedDamage = previous - _currentHealth;
            if (appliedDamage <= 0f)
            {
                return false;
            }

            OnDamaged?.Invoke(appliedDamage);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            if (_currentHealth <= 0f)
            {
                Die();
            }

            return true;
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            startingHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
            invincibilityDuration = Mathf.Max(0f, invincibilityDuration);
            flickerInterval = Mathf.Max(0.01f, flickerInterval);
            flickerAlphaMultiplier = Mathf.Clamp(flickerAlphaMultiplier, 0.05f, 1f);
            combatExitDelay = Mathf.Max(0f, combatExitDelay);
            regenInterval = Mathf.Max(0.01f, regenInterval);
            regenAmount = Mathf.Max(0f, regenAmount);
            noRegenBelowNormalizedHealth = Mathf.Clamp01(noRegenBelowNormalizedHealth);
            regenMultiplier = Mathf.Max(0f, regenMultiplier);
        }

        private void OnDisable()
        {
            StopInvincibility(true);
        }
    }
}

