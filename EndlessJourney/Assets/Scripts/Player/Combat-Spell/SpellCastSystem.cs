using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Spell runtime executor with a two-phase cast flow:
    /// - Singing phase (channel): gradual mana spend + optional interrupt by accumulated harm.
    /// - Cast lock phase (stiffness): blocks all action starts for a short duration.
    /// </summary>
    [RequireComponent(typeof(PlayerMana2D))]
    public class SpellCastSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCore2D core;
        [SerializeField] private PlayerMana2D mana;
        [SerializeField] private PlayerHealth2D health;
        [SerializeField] private PlayerProjectileLauncher2D projectileLauncher;

        [Header("Cast Effect")]
        [Tooltip("Optional spawn point for cast effects. If null, uses player position + spell offset.")]
        [SerializeField] private Transform castPoint;

        [Header("Singing Unlocks")]
        [Tooltip("When false, singing locks player movement/action; when true, movement input can continue during singing.")]
        [SerializeField] private bool allowSingingWhileMovingUnlocked = false;
        [Tooltip("When false, singing can start and continue only while grounded.")]
        [SerializeField] private bool allowSingingWhileAirborneUnlocked = false;

        public bool IsCasting => _castPhase != CastPhase.Idle;
        public bool IsSinging => _castPhase == CastPhase.Singing;
        public bool IsInCastStiffness => _castPhase == CastPhase.CastLock;
        public bool IsSingingMovementUnlocked => allowSingingWhileMovingUnlocked;
        public SpellData2D CurrentCastingSpell => _castingSpell;

        public float CastProgressNormalized
        {
            get
            {
                if (_castPhase == CastPhase.Idle || _castingSpell == null)
                {
                    return 0f;
                }

                if (_phaseDuration <= 0f)
                {
                    return 1f;
                }

                return 1f - Mathf.Clamp01(_phaseTimer / _phaseDuration);
            }
        }

        public event Action<float> OnSpellCast;
        public event Action<float> OnCastStarted;
        public event Action<float> OnCooldownStarted;
        public event Action<float> OnCastFailedInsufficientMana;
        public event Action<float> OnCastFailedCooldown;
        public event Action OnCastFailedNoSpellData;
        public event Action<SpellData2D, float, float> OnCastInterrupted;
        public event Action<string, float, SpellData2D> OnBuffEffectRequested;

        private readonly Dictionary<string, float> _cooldownBySpellId = new Dictionary<string, float>(16);
        private readonly List<string> _cooldownKeysScratch = new List<string>(16);

        private enum CastPhase
        {
            Idle = 0,
            Singing = 1,
            CastLock = 2
        }

        private CastPhase _castPhase = CastPhase.Idle;
        private float _phaseTimer;
        private float _phaseDuration;
        private SpellData2D _castingSpell;

        private float _remainingSingingManaCost;
        private float _accumulatedChannelDamage;
        private float _channelDamageInterruptThreshold;
        private bool _subscribedToHealthHarmDamage;
        private bool _castLockZeroGravityApplied;

        private void Reset()
        {
            core = GetComponent<PlayerCore2D>();
            mana = GetComponent<PlayerMana2D>();
            health = GetComponent<PlayerHealth2D>();
            projectileLauncher = GetComponent<PlayerProjectileLauncher2D>();
        }

        private void Awake()
        {
            if (core == null) core = GetComponent<PlayerCore2D>();
            if (mana == null) mana = GetComponent<PlayerMana2D>();
            if (health == null) health = GetComponent<PlayerHealth2D>();
            if (projectileLauncher == null) projectileLauncher = GetComponent<PlayerProjectileLauncher2D>();

            if (core == null || mana == null)
            {
                Debug.LogError("SpellCastSystem is missing references. Please assign PlayerCore2D and PlayerMana2D.");
                enabled = false;
            }
        }

        private void OnDisable()
        {
            ForceClearCastState();
        }

        private void Update()
        {
            TickCooldown(Time.deltaTime);
            TickCastPhase(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (_castPhase == CastPhase.CastLock && core != null && core.Body != null)
            {
                core.Body.linearVelocity = Vector2.zero;
            }
        }

        public bool TryRequestCast(SpellData2D spellData)
        {
            if (core == null || mana == null)
            {
                return false;
            }

            if (IsCasting || core.IsActionLocked)
            {
                return false;
            }

            if (spellData == null || string.IsNullOrWhiteSpace(spellData.SpellId))
            {
                OnCastFailedNoSpellData?.Invoke();
                return false;
            }

            if (!spellData.AllowCastWhileMovementLocked && core.IsMovementLocked)
            {
                return false;
            }

            float cooldownRemaining = GetCooldownRemaining(spellData.SpellId);
            if (cooldownRemaining > 0f)
            {
                OnCastFailedCooldown?.Invoke(cooldownRemaining);
                return false;
            }

            if (!mana.HasEnoughMana(spellData.ManaCost))
            {
                OnCastFailedInsufficientMana?.Invoke(spellData.ManaCost);
                return false;
            }

            float singingTime = Mathf.Max(0f, spellData.SingingTime);
            if (singingTime > 0f)
            {
                if (!CanStartSinging())
                {
                    return false;
                }

                BeginSingingPhase(spellData, singingTime);
                return true;
            }

            if (!mana.TrySpendMana(spellData.ManaCost))
            {
                OnCastFailedInsufficientMana?.Invoke(spellData.ManaCost);
                return false;
            }

            ResolveCast(spellData);
            BeginCastLockPhase(spellData);
            return true;
        }

        public float GetCooldownRemaining(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
            {
                return 0f;
            }

            return _cooldownBySpellId.TryGetValue(spellId, out float cooldown) ? Mathf.Max(0f, cooldown) : 0f;
        }

        public void UnlockSingingWhileMoving()
        {
            allowSingingWhileMovingUnlocked = true;
        }

        public void UnlockSingingWhileAirborne()
        {
            allowSingingWhileAirborneUnlocked = true;
        }

        private bool CanStartSinging()
        {
            if (!allowSingingWhileAirborneUnlocked && !core.IsGrounded)
            {
                return false;
            }

            return true;
        }

        private void BeginSingingPhase(SpellData2D spellData, float singingTime)
        {
            _castPhase = CastPhase.Singing;
            _castingSpell = spellData;
            _phaseDuration = Mathf.Max(0.001f, singingTime);
            _phaseTimer = _phaseDuration;

            _remainingSingingManaCost = Mathf.Max(0f, spellData.ManaCost);
            _accumulatedChannelDamage = 0f;
            _channelDamageInterruptThreshold = Mathf.Max(0f, singingTime * 3f);

            core.SetActionLocked(true);

            if (!allowSingingWhileMovingUnlocked)
            {
                Vector2 velocity = core.Body.linearVelocity;
                velocity.x = 0f;
                core.Body.linearVelocity = velocity;
            }

            mana.SetNaturalRegenBlocked(true);
            SubscribeHealthHarmDamage();
            OnCastStarted?.Invoke(singingTime);
        }

        private void TickCastPhase(float deltaTime)
        {
            switch (_castPhase)
            {
                case CastPhase.Singing:
                    TickSingingPhase(deltaTime);
                    break;
                case CastPhase.CastLock:
                    TickCastLockPhase(deltaTime);
                    break;
            }
        }

        private void TickSingingPhase(float deltaTime)
        {
            if (_castingSpell == null)
            {
                InterruptCurrentSinging();
                return;
            }

            if (!allowSingingWhileAirborneUnlocked && !core.IsGrounded)
            {
                InterruptCurrentSinging();
                return;
            }

            if (deltaTime > 0f && _remainingSingingManaCost > 0f && _phaseDuration > 0f)
            {
                float manaPerSecond = _castingSpell.ManaCost / _phaseDuration;
                float frameCost = Mathf.Min(_remainingSingingManaCost, manaPerSecond * deltaTime);

                if (frameCost > 0f)
                {
                    if (!mana.TrySpendMana(frameCost))
                    {
                        OnCastFailedInsufficientMana?.Invoke(frameCost);
                        InterruptCurrentSinging();
                        return;
                    }

                    _remainingSingingManaCost -= frameCost;
                }
            }

            _phaseTimer -= deltaTime;
            if (_phaseTimer > 0f)
            {
                return;
            }

            if (_remainingSingingManaCost > 0f)
            {
                float remaining = _remainingSingingManaCost;
                if (!mana.TrySpendMana(remaining))
                {
                    OnCastFailedInsufficientMana?.Invoke(remaining);
                    InterruptCurrentSinging();
                    return;
                }

                _remainingSingingManaCost = 0f;
            }

            CompleteSingingCast();
        }

        private void CompleteSingingCast()
        {
            SpellData2D spellData = _castingSpell;
            EndSingingPhaseState();

            if (spellData == null)
            {
                ForceClearCastState();
                return;
            }

            ResolveCast(spellData);
            BeginCastLockPhase(spellData);
        }

        private void BeginCastLockPhase(SpellData2D spellData)
        {
            if (spellData == null)
            {
                ForceClearCastState();
                return;
            }

            float castTime = Mathf.Max(0f, spellData.CastTime);
            if (castTime <= 0f)
            {
                ForceClearCastState();
                return;
            }

            _castPhase = CastPhase.CastLock;
            _castingSpell = spellData;
            _phaseDuration = castTime;
            _phaseTimer = castTime;
            core.SetActionLocked(true);
            core.SetGravityMultiplier(0f);
            _castLockZeroGravityApplied = true;
            core.Body.linearVelocity = Vector2.zero;
        }

        private void TickCastLockPhase(float deltaTime)
        {
            _phaseTimer -= deltaTime;
            if (_phaseTimer > 0f)
            {
                return;
            }

            EndCastLockPhase();
        }

        private void EndCastLockPhase()
        {
            if (_castPhase != CastPhase.CastLock)
            {
                return;
            }

            _castPhase = CastPhase.Idle;
            _phaseTimer = 0f;
            _phaseDuration = 0f;
            _castingSpell = null;
            core.SetActionLocked(false);
            RestoreGravityAfterCastLockIfNeeded();
        }

        private void InterruptCurrentSinging()
        {
            if (_castPhase != CastPhase.Singing)
            {
                return;
            }

            SpellData2D interruptedSpell = _castingSpell;
            float damageTaken = _accumulatedChannelDamage;
            float threshold = _channelDamageInterruptThreshold;

            EndSingingPhaseState();
            _castPhase = CastPhase.Idle;
            _phaseTimer = 0f;
            _phaseDuration = 0f;
            _castingSpell = null;
            core.SetActionLocked(false);

            OnCastInterrupted?.Invoke(interruptedSpell, damageTaken, threshold);
        }

        private void EndSingingPhaseState()
        {
            UnsubscribeHealthHarmDamage();
            mana.SetNaturalRegenBlocked(false);
        }

        private void ForceClearCastState()
        {
            UnsubscribeHealthHarmDamage();
            if (mana != null)
            {
                mana.SetNaturalRegenBlocked(false);
            }

            if (core != null)
            {
                core.SetActionLocked(false);
                RestoreGravityAfterCastLockIfNeeded();
            }

            _castPhase = CastPhase.Idle;
            _phaseTimer = 0f;
            _phaseDuration = 0f;
            _castingSpell = null;
            _remainingSingingManaCost = 0f;
            _accumulatedChannelDamage = 0f;
            _channelDamageInterruptThreshold = 0f;
        }

        private void RestoreGravityAfterCastLockIfNeeded()
        {
            if (!_castLockZeroGravityApplied || core == null)
            {
                return;
            }

            core.RestoreDefaultGravity();
            _castLockZeroGravityApplied = false;
        }

        private void SubscribeHealthHarmDamage()
        {
            if (_subscribedToHealthHarmDamage || health == null)
            {
                return;
            }

            health.OnHarmDamaged += OnCasterHarmDamaged;
            _subscribedToHealthHarmDamage = true;
        }

        private void UnsubscribeHealthHarmDamage()
        {
            if (!_subscribedToHealthHarmDamage || health == null)
            {
                return;
            }

            health.OnHarmDamaged -= OnCasterHarmDamaged;
            _subscribedToHealthHarmDamage = false;
        }

        private void OnCasterHarmDamaged(float damage, GameObject source)
        {
            if (_castPhase != CastPhase.Singing || damage <= 0f)
            {
                return;
            }

            _accumulatedChannelDamage += damage;
            if (_accumulatedChannelDamage < _channelDamageInterruptThreshold)
            {
                return;
            }

            InterruptCurrentSinging();
        }

        private void TickCooldown(float deltaTime)
        {
            if (_cooldownBySpellId.Count == 0)
            {
                return;
            }

            _cooldownKeysScratch.Clear();
            foreach (string spellId in _cooldownBySpellId.Keys)
            {
                _cooldownKeysScratch.Add(spellId);
            }

            for (int i = 0; i < _cooldownKeysScratch.Count; i++)
            {
                string spellId = _cooldownKeysScratch[i];
                float next = Mathf.Max(0f, _cooldownBySpellId[spellId] - deltaTime);
                if (next <= 0f)
                {
                    _cooldownBySpellId.Remove(spellId);
                }
                else
                {
                    _cooldownBySpellId[spellId] = next;
                }
            }
        }

        private void ResolveCast(SpellData2D spellData)
        {
            if (spellData == null)
            {
                OnCastFailedNoSpellData?.Invoke();
                return;
            }

            ExecuteSpellEffect(spellData);
            SpawnCastEffect(spellData);
            StartCooldown(spellData);
            OnSpellCast?.Invoke(spellData.ManaCost);
        }

        private void ExecuteSpellEffect(SpellData2D spellData)
        {
            if (spellData == null || spellData.EffectData == null)
            {
                return;
            }

            SpellEffectContext2D context = new SpellEffectContext2D(
                this,
                spellData,
                gameObject,
                core,
                health,
                projectileLauncher);

            spellData.EffectData.Execute(context);
        }

        private void StartCooldown(SpellData2D spellData)
        {
            if (spellData == null || string.IsNullOrWhiteSpace(spellData.SpellId))
            {
                return;
            }

            float cooldown = Mathf.Max(0f, spellData.CastCooldown);
            if (cooldown <= 0f)
            {
                _cooldownBySpellId.Remove(spellData.SpellId);
                return;
            }

            _cooldownBySpellId[spellData.SpellId] = cooldown;
            OnCooldownStarted?.Invoke(cooldown);
        }

        private void SpawnCastEffect(SpellData2D spellData)
        {
            if (spellData == null)
            {
                return;
            }

            Vector3 origin = GetCastPosition(spellData);
            GameObject effectPrefab = spellData.CastEffectPrefab;
            if (effectPrefab == null)
            {
                Vector3 dir = new Vector3(core.FacingDirection, 0f, 0f);
                Debug.DrawRay(origin, dir * 0.8f, Color.cyan, 0.25f);
                return;
            }

            Quaternion rotation = core.FacingDirection >= 0 ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            GameObject instance = Instantiate(effectPrefab, origin, rotation);

            if (spellData.CastEffectLifetime > 0f)
            {
                Destroy(instance, spellData.CastEffectLifetime);
            }
        }

        private Vector3 GetCastPosition(SpellData2D spellData)
        {
            if (castPoint != null)
            {
                return castPoint.position;
            }

            Vector3 offset = spellData != null ? spellData.DefaultCastOffset : Vector3.zero;
            Vector3 directionalOffset = new Vector3(offset.x * core.FacingDirection, offset.y, offset.z);
            return transform.position + directionalOffset;
        }

        public void NotifyBuffEffectRequested(string buffId, float duration, SpellData2D sourceSpell)
        {
            OnBuffEffectRequested?.Invoke(buffId, duration, sourceSpell);
        }
    }
}
