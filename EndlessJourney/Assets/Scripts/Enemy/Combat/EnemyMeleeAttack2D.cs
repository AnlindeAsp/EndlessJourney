using System.Collections;
using System.Collections.Generic;
using EndlessJourney.Interfaces;
using EndlessJourney.Player;
using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Active melee attack module for enemies.
    /// The brain decides when to start; this module owns windup/active/recovery/cooldown
    /// and applies harm through a dedicated hitbox collider.
    /// </summary>
    [RequireComponent(typeof(EnemyCore2D))]
    public class EnemyMeleeAttack2D : MonoBehaviour
    {
        private enum AttackState
        {
            Ready,
            Windup,
            Active,
            Recovery,
            Cooldown
        }

        private struct TargetCacheEntry
        {
            public IPlayerHarmful Harmful;
            public PlayerHealth2D Health;
            public bool IsResolved;
        }

        [Header("References (Assign Manually)")]
        [SerializeField] private EnemyCore2D core;
        [Tooltip("Child trigger collider that represents the active weapon range. Keep its GameObject active; this script can enable/disable the Collider2D.")]
        [SerializeField] private Collider2D hitboxCollider;

        [Header("Attack Detection")]
        [SerializeField, Min(0f)] private float attackDetectionRange = 1.35f;
        [Tooltip("If enabled, target must be in range before windup starts. Once windup starts, the attack will continue even if target leaves range.")]
        [SerializeField] private bool requireTargetInRangeToStart = true;

        [Header("Attack Timing")]
        [SerializeField, Min(0f)] private float windupDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float activeDuration = 0.12f;
        [SerializeField, Min(0f)] private float recoveryDuration = 0.25f;
        [SerializeField, Min(0f)] private float cooldownDuration = 0.65f;

        [Header("Damage")]
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [Tooltip("Check targetLayers against the collider actually touched. This avoids hitting through parent rigidbodies on unrelated child colliders.")]
        [SerializeField] private bool filterByTouchedColliderLayer = true;
        [SerializeField] private bool includeTriggerTargets = true;

        [Header("Attack Facing")]
        [SerializeField] private bool faceTargetOnStart = true;
        [Tooltip("When enabled, enemy keeps the facing direction chosen at attack start until recovery ends.")]
        [SerializeField] private bool lockFacingDuringAttack = true;
        [SerializeField] private bool stopMovementDuringAttackSequence = true;

        [Header("Hitbox")]
        [SerializeField] private bool enableHitboxOnlyDuringActiveFrames = true;

        [Header("Runtime (Read-Only)")]
        [SerializeField] private AttackState currentState = AttackState.Ready;

        [Header("Debug")]
        [SerializeField] private bool logAttackState = false;
        [SerializeField] private bool logHit = false;
        [SerializeField] private bool drawAttackDetectionRange = true;

        private readonly HashSet<int> _hitTargetIds = new HashSet<int>();
        private readonly Dictionary<int, TargetCacheEntry> _targetCache = new Dictionary<int, TargetCacheEntry>(8);
        private readonly Collider2D[] _overlapResults = new Collider2D[16];
        private Coroutine _attackRoutine;
        private Transform _attackTarget;
        private int _lockedFacingDirection = 1;

        public float AttackDetectionRange => attackDetectionRange;
        public bool IsReady => currentState == AttackState.Ready;
        public bool IsActive => currentState == AttackState.Active;
        public bool IsCoolingDown => currentState == AttackState.Cooldown;
        public bool IsInAttackSequence =>
            currentState == AttackState.Windup ||
            currentState == AttackState.Active ||
            currentState == AttackState.Recovery;

        private void Reset()
        {
            core = GetComponent<EnemyCore2D>();
        }

        private void Awake()
        {
            if (core == null)
            {
                Debug.LogError("EnemyMeleeAttack2D requires EnemyCore2D assigned in Inspector.", this);
                enabled = false;
                return;
            }

            if (hitboxCollider == null)
            {
                Debug.LogError("EnemyMeleeAttack2D requires a hitbox Collider2D assigned in Inspector.", this);
                enabled = false;
                return;
            }

            if (!hitboxCollider.isTrigger)
            {
                Debug.LogWarning("EnemyMeleeAttack2D hitboxCollider is recommended to be trigger.", this);
            }

            SetHitboxActive(false);
        }

        private void OnDisable()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            _attackTarget = null;
            _hitTargetIds.Clear();
            _targetCache.Clear();
            currentState = AttackState.Ready;
            SetHitboxActive(false);
        }

        public bool CanStartAttack(Transform target)
        {
            if (!enabled || target == null || currentState != AttackState.Ready)
            {
                return false;
            }

            if (core == null || core.IsDead || core.IsStunned)
            {
                return false;
            }

            if (requireTargetInRangeToStart && !IsTargetInAttackDetectionRange(target))
            {
                return false;
            }

            return true;
        }

        public bool IsTargetInAttackDetectionRange(Transform target)
        {
            if (target == null)
            {
                return false;
            }

            float range = Mathf.Max(0f, attackDetectionRange);
            float sqrDistance = ((Vector2)target.position - (Vector2)transform.position).sqrMagnitude;
            return sqrDistance <= range * range;
        }

        public bool TryStartAttack(Transform target)
        {
            if (!CanStartAttack(target))
            {
                return false;
            }

            _attackTarget = target;
            if (faceTargetOnStart && _attackTarget != null)
            {
                core.FaceTowardX(_attackTarget.position.x);
            }

            _lockedFacingDirection = core.FacingDirection;
            _attackRoutine = StartCoroutine(AttackRoutine());
            return true;
        }

        private IEnumerator AttackRoutine()
        {
            _hitTargetIds.Clear();

            yield return TickTimedState(AttackState.Windup, windupDuration, false);
            yield return TickTimedState(AttackState.Active, activeDuration, true);
            yield return TickTimedState(AttackState.Recovery, recoveryDuration, false);
            yield return TickTimedState(AttackState.Cooldown, cooldownDuration, false);

            SetState(AttackState.Ready);
            _attackTarget = null;
            _attackRoutine = null;
        }

        private IEnumerator TickTimedState(AttackState state, float duration, bool hitboxActive)
        {
            SetState(state);
            SetHitboxActive(hitboxActive);

            float timer = Mathf.Max(0f, duration);
            while (timer > 0f)
            {
                TickAttackFrame(hitboxActive);
                timer -= Time.deltaTime;
                yield return null;
            }

            if (hitboxActive)
            {
                TickActiveHitScan();
            }
        }

        private void TickAttackFrame(bool hitboxActive)
        {
            if (core == null)
            {
                return;
            }

            if (core.IsDead || core.IsStunned)
            {
                CancelAttack();
                return;
            }

            if (stopMovementDuringAttackSequence && IsInAttackSequence)
            {
                core.StopMovement();
            }

            if (lockFacingDuringAttack)
            {
                core.FaceDirection(_lockedFacingDirection);
            }
            else if (_attackTarget != null)
            {
                core.FaceTowardX(_attackTarget.position.x);
            }

            if (hitboxActive)
            {
                TickActiveHitScan();
            }
        }

        private void TickActiveHitScan()
        {
            if (hitboxCollider == null || damage <= 0f)
            {
                return;
            }

            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = targetLayers,
                useTriggers = includeTriggerTargets
            };

            int count = hitboxCollider.Overlap(filter, _overlapResults);
            for (int i = 0; i < count; i++)
            {
                Collider2D other = _overlapResults[i];
                if (other == null || IsSelfCollider(other))
                {
                    continue;
                }

                if (filterByTouchedColliderLayer && !IsInTargetLayer(other.gameObject.layer))
                {
                    continue;
                }

                if (!TryResolveTargetRoot(other, out GameObject targetRoot))
                {
                    continue;
                }

                if (!filterByTouchedColliderLayer && !IsInTargetLayer(targetRoot.layer))
                {
                    continue;
                }

                int targetId = targetRoot.GetInstanceID();
                if (_hitTargetIds.Contains(targetId))
                {
                    continue;
                }

                if (!TryApplyDamage(targetRoot))
                {
                    continue;
                }

                _hitTargetIds.Add(targetId);
                if (logHit)
                {
                    Debug.Log($"Enemy melee hit -> {targetRoot.name}, damage={damage:0.##}", this);
                }
            }
        }

        private bool TryApplyDamage(GameObject targetRoot)
        {
            TargetCacheEntry cache = GetOrResolveTargetCache(targetRoot);

            if (cache.Harmful != null)
            {
                return cache.Harmful.CanReceiveHarm() && cache.Harmful.ReceiveHarm(damage, gameObject);
            }

            return cache.Health != null && cache.Health.ReceiveHarm(damage, gameObject);
        }

        private TargetCacheEntry GetOrResolveTargetCache(GameObject targetRoot)
        {
            int id = targetRoot.GetInstanceID();
            if (_targetCache.TryGetValue(id, out TargetCacheEntry cached))
            {
                bool harmfulAlive = IsComponentAlive(cached.Harmful as Component);
                bool healthAlive = IsComponentAlive(cached.Health);
                if (cached.IsResolved && (harmfulAlive || healthAlive || (cached.Harmful == null && cached.Health == null)))
                {
                    if (!harmfulAlive) cached.Harmful = null;
                    if (!healthAlive) cached.Health = null;
                    _targetCache[id] = cached;
                    return cached;
                }
            }

            TargetCacheEntry resolved = new TargetCacheEntry
            {
                Harmful = targetRoot.GetComponent<IPlayerHarmful>(),
                Health = targetRoot.GetComponent<PlayerHealth2D>(),
                IsResolved = true
            };

            if (resolved.Harmful == null)
            {
                resolved.Harmful = targetRoot.GetComponentInParent<IPlayerHarmful>();
            }

            if (resolved.Health == null)
            {
                resolved.Health = targetRoot.GetComponentInParent<PlayerHealth2D>();
            }

            _targetCache[id] = resolved;
            return resolved;
        }

        private void CancelAttack()
        {
            if (_attackRoutine != null)
            {
                StopCoroutine(_attackRoutine);
                _attackRoutine = null;
            }

            _attackTarget = null;
            _hitTargetIds.Clear();
            SetHitboxActive(false);
            SetState(AttackState.Ready);
        }

        private void SetState(AttackState state)
        {
            if (currentState == state)
            {
                return;
            }

            currentState = state;
            if (logAttackState)
            {
                Debug.Log($"Enemy melee attack -> {state}", this);
            }
        }

        private void SetHitboxActive(bool active)
        {
            if (!enableHitboxOnlyDuringActiveFrames || hitboxCollider == null)
            {
                return;
            }

            if (hitboxCollider.enabled != active)
            {
                hitboxCollider.enabled = active;
            }
        }

        private bool IsInTargetLayer(int layer)
        {
            return (targetLayers.value & (1 << layer)) != 0;
        }

        private bool IsSelfCollider(Collider2D colliderRef)
        {
            if (colliderRef == null)
            {
                return true;
            }

            if (core != null && core.Body != null && colliderRef.attachedRigidbody == core.Body)
            {
                return true;
            }

            Transform t = colliderRef.transform;
            return t == transform || t.IsChildOf(transform);
        }

        private static bool TryResolveTargetRoot(Collider2D touchedCollider, out GameObject targetRoot)
        {
            if (touchedCollider == null)
            {
                targetRoot = null;
                return false;
            }

            targetRoot = touchedCollider.attachedRigidbody != null
                ? touchedCollider.attachedRigidbody.gameObject
                : touchedCollider.gameObject;

            return targetRoot != null;
        }

        private static bool IsComponentAlive(Component component)
        {
            return component != null && component.gameObject != null;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawAttackDetectionRange)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.45f, 0.15f, 0.65f);
            Gizmos.DrawWireSphere(transform.position, attackDetectionRange);
        }

        private void OnValidate()
        {
            attackDetectionRange = Mathf.Max(0f, attackDetectionRange);
            windupDuration = Mathf.Max(0f, windupDuration);
            activeDuration = Mathf.Max(0.01f, activeDuration);
            recoveryDuration = Mathf.Max(0f, recoveryDuration);
            cooldownDuration = Mathf.Max(0f, cooldownDuration);
            damage = Mathf.Max(0f, damage);
        }
    }
}
