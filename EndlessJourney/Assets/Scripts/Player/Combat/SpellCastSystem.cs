using System;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Minimal spell casting module.
    /// - Reads cast input from PlayerInput2D (via PlayerCore2D)
    /// - Supports cast time + cooldown
    /// - Spends mana on successful cast resolve
    /// - Plays a simple cast effect
    /// </summary>
    //[RequireComponent(typeof(PlayerCore2D))] Purposely removed, do not add back
    [RequireComponent(typeof(PlayerMana2D))]
    public class SpellCastSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCore2D core;
        [SerializeField] private PlayerMana2D mana;

        [Header("Cast Rule")]
        [SerializeField, Min(0f)] private float manaCostPerCast = 30f;
        [SerializeField, Min(0f)] private float castTime = 0.2f;
        [SerializeField, Min(0f)] private float castCooldown = 0.35f;
        [SerializeField] private bool allowCastWhileMovementLocked = true;

        [Header("Unlock")]
        [Tooltip("Only learned spells can be cast.")]
        [SerializeField] private bool isSpellLearned = true;

        [Header("Cast Effect")]
        [Tooltip("Optional spawn point for cast effects. If null, uses player position.")]
        [SerializeField] private Transform castPoint;
        [SerializeField] private GameObject castEffectPrefab;
        [SerializeField, Min(0f)] private float castEffectLifetime = 1.5f;
        [SerializeField] private Vector3 defaultCastOffset = new Vector3(0.8f, 0.2f, 0f);

        /// <summary>
        /// Whether the spell is currently in cast-time phase.
        /// </summary>
        public bool IsCasting => _isCasting;

        /// <summary>
        /// Cast progress in range [0, 1].
        /// </summary>
        public float CastProgressNormalized => castTime <= 0f ? (_isCasting ? 1f : 0f) : 1f - Mathf.Clamp01(_castTimer / castTime);

        /// <summary>
        /// Remaining cooldown seconds. 0 means ready.
        /// </summary>
        public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);

        /// <summary>
        /// Whether the spell is currently blocked by cooldown.
        /// </summary>
        public bool IsOnCooldown => CooldownRemaining > 0f;

        /// <summary>
        /// Whether this spell has been learned/unlocked.
        /// </summary>
        public bool IsSpellLearned
        {
            get => isSpellLearned;
            set => isSpellLearned = value;
        }

        /// <summary>
        /// Fired when a spell successfully resolves and pays mana.
        /// Payload: mana cost spent.
        /// </summary>
        public event Action<float> OnSpellCast;

        /// <summary>
        /// Fired when cast-time starts.
        /// Payload: cast duration.
        /// </summary>
        public event Action<float> OnCastStarted;

        /// <summary>
        /// Fired when cooldown starts.
        /// Payload: cooldown duration.
        /// </summary>
        public event Action<float> OnCooldownStarted;

        /// <summary>
        /// Fired when cast fails because mana is insufficient.
        /// Payload: required mana cost.
        /// </summary>
        public event Action<float> OnCastFailedInsufficientMana;

        /// <summary>
        /// Fired when cast fails because cooldown is still active.
        /// Payload: remaining cooldown seconds.
        /// </summary>
        public event Action<float> OnCastFailedCooldown;

        /// <summary>
        /// Fired when cast fails because spell is not learned.
        /// </summary>
        public event Action OnCastFailedNotLearned;

        private bool _isCasting;
        private float _castTimer;
        private float _cooldownTimer;

        /// <summary>
        /// Auto-wires references when component is first added.
        /// </summary>
        private void Reset()
        {
            core = GetComponent<PlayerCore2D>();
            mana = GetComponent<PlayerMana2D>();
        }

        /// <summary>
        /// Validates required references on startup.
        /// </summary>
        private void Awake()
        {
            if (core == null) core = GetComponent<PlayerCore2D>();
            if (mana == null) mana = GetComponent<PlayerMana2D>();

            if (core == null || mana == null)
            {
                Debug.LogError("SpellCastSystem is missing references. Please assign PlayerCore2D and PlayerMana2D.");
                enabled = false;
            }
        }

        /// <summary>
        /// Per-frame driver for cooldown tick, cast-time tick, and cast input handling.
        /// </summary>
        private void Update()
        {
            TickCooldown(Time.deltaTime);
            TickCasting(Time.deltaTime);

            // Ignore new cast input while a cast is already in progress.
            if (_isCasting)
            {
                return;
            }

            if (!core.Input.CastPressedThisFrame)
            {
                return;
            }

            if (!isSpellLearned)
            {
                OnCastFailedNotLearned?.Invoke();
                return;
            }

            if (!allowCastWhileMovementLocked && core.IsMovementLocked)
            {
                return;
            }

            TryStartCast();
        }

        /// <summary>
        /// Decreases cooldown timer towards zero.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        private void TickCooldown(float deltaTime)
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer = Mathf.Max(0f, _cooldownTimer - deltaTime);
            }
        }

        /// <summary>
        /// Advances cast-time timer and resolves cast on completion.
        /// </summary>
        /// <param name="deltaTime">Frame delta time.</param>
        private void TickCasting(float deltaTime)
        {
            if (!_isCasting)
            {
                return;
            }

            _castTimer -= deltaTime;
            if (_castTimer <= 0f)
            {
                _isCasting = false;
                ResolveCast();
            }
        }

        /// <summary>
        /// Attempts to enter casting state.
        /// Performs cooldown and mana pre-checks before cast-time starts.
        /// </summary>
        private void TryStartCast()
        {
            if (_cooldownTimer > 0f)
            {
                OnCastFailedCooldown?.Invoke(_cooldownTimer);
                return;
            }

            if (!mana.HasEnoughMana(manaCostPerCast))
            {
                OnCastFailedInsufficientMana?.Invoke(manaCostPerCast);
                return;
            }

            if (castTime <= 0f)
            {
                ResolveCast();
                return;
            }

            _isCasting = true;
            _castTimer = castTime;
            OnCastStarted?.Invoke(castTime);
        }

        /// <summary>
        /// Finalizes casting: spends mana, spawns effect, starts cooldown, emits success event.
        /// </summary>
        private void ResolveCast()
        {
            // Mana is paid when cast resolves, so external changes can still cancel it.
            if (!mana.TrySpendMana(manaCostPerCast))
            {
                OnCastFailedInsufficientMana?.Invoke(manaCostPerCast);
                return;
            }
            Debug.Log("Spell cast successfully! Mana spent: " + manaCostPerCast);
            SpawnCastEffect();
            StartCooldown();
            OnSpellCast?.Invoke(manaCostPerCast);
        }

        /// <summary>
        /// Starts cooldown timer after a successful cast.
        /// </summary>
        private void StartCooldown()
        {
            _cooldownTimer = castCooldown;
            if (_cooldownTimer > 0f)
            {
                OnCooldownStarted?.Invoke(_cooldownTimer);
            }
        }

        /// <summary>
        /// Spawns cast VFX prefab or draws a short debug ray when no prefab is assigned.
        /// </summary>
        private void SpawnCastEffect()
        {
            if (castEffectPrefab == null)
            {
                Vector3 origin = GetCastPosition();
                Vector3 dir = new Vector3(core.FacingDirection, 0f, 0f);
                Debug.DrawRay(origin, dir * 0.8f, Color.cyan, 0.25f);
                return;
            }

            Quaternion rotation = core.FacingDirection >= 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            GameObject instance = Instantiate(castEffectPrefab, GetCastPosition(), rotation);

            if (castEffectLifetime > 0f)
            {
                Destroy(instance, castEffectLifetime);
            }
        }

        /// <summary>
        /// Calculates world-space spawn position for the cast effect.
        /// </summary>
        private Vector3 GetCastPosition()
        {
            if (castPoint != null)
            {
                return castPoint.position;
            }

            Vector3 directionalOffset = new Vector3(defaultCastOffset.x * core.FacingDirection, defaultCastOffset.y, defaultCastOffset.z);
            return transform.position + directionalOffset;
        }
    }
}
