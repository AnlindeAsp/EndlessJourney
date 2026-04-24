using System.Collections.Generic;
using EndlessJourney.Player;
using EndlessJourney.Interfaces;
using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Contact damage core for enemies.
    /// Supports both:
    /// - Root callbacks (OnTriggerStay2D / OnCollisionStay2D)
    /// - Child damage-zone forwarding via EnemyContactDamageZone2D
    /// </summary>
    public class EnemyContactAttack2D : MonoBehaviour
    {
        private struct TargetCacheEntry
        {
            public IPlayerHarmful Harmful;
            public PlayerHealth2D Health;
            public bool IsResolved;
        }

        [Header("References")]
        [SerializeField] private EnemyCore2D core;
        [Tooltip("Optional dedicated trigger collider used as contact-damage zone.")]
        [SerializeField] private Collider2D damageZone;

        [Header("Detection")]
        [Tooltip("Use trigger callbacks from this object (root-level collider/trigger).")]
        [SerializeField] private bool useRootTriggerCallbacks = false;
        [Tooltip("Use collision callbacks from this object (solid contact).")]
        [SerializeField] private bool useRootCollisionCallbacks = true;
        [Tooltip("If true, forwarded hit checks must come from the assigned damage zone.")]
        [SerializeField] private bool requireAssignedDamageZoneForForwardedHits = true;

        [Header("Attack")]
        [SerializeField, Min(0f)] private float contactDamage = 10f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.6f;
        [SerializeField] private LayerMask targetLayers = ~0;

        [Header("Debug")]
        [SerializeField] private bool logContactHit = false;

        private float _cooldownTimer;
        private readonly Dictionary<int, TargetCacheEntry> _targetCache = new Dictionary<int, TargetCacheEntry>(16);

        private void Reset()
        {
            core = GetComponent<EnemyCore2D>();
        }

        private void Awake()
        {
            if (core == null) core = GetComponent<EnemyCore2D>();
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);
            }
        }

        private void OnDisable()
        {
            _targetCache.Clear();
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!useRootCollisionCallbacks)
            {
                return;
            }

            TryHitTarget(collision.collider);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!useRootTriggerCallbacks)
            {
                return;
            }

            TryHitTarget(other);
        }

        /// <summary>
        /// Called by child trigger zones to route overlap damage checks
        /// through this shared contact-attack module.
        /// </summary>
        public bool TryHitTargetFromZone(Collider2D other, Collider2D sourceZone)
        {
            if (requireAssignedDamageZoneForForwardedHits && damageZone != null && sourceZone != damageZone)
            {
                return false;
            }

            return TryHitTargetInternal(other);
        }

        private void TryHitTarget(Collider2D other)
        {
            TryHitTargetInternal(other);
        }

        private bool TryHitTargetInternal(Collider2D other)
        {
            if (other == null || _cooldownTimer > 0f)
            {
                return false;
            }

            if (core != null && core.IsDead)
            {
                return false;
            }

            if (IsSelfCollider(other))
            {
                return false;
            }

            if (!TryResolveTargetRoot(other, out GameObject targetRoot))
            {
                return false;
            }

            // Layer filtering should be based on the target root object, not child component objects.
            if (!IsInTargetLayer(targetRoot.layer))
            {
                return false;
            }

            TargetCacheEntry cache = GetOrResolveTargetCache(targetRoot);

            if (cache.Harmful != null)
            {
                if (!cache.Harmful.CanReceiveHarm() || !cache.Harmful.ReceiveHarm(contactDamage, gameObject))
                {
                    return false;
                }

                OnContactHitApplied(targetRoot.name);
                return true;
            }

            if (cache.Health == null)
            {
                return false;
            }

            if (!cache.Health.ReceiveHarm(contactDamage, gameObject))
            {
                return false;
            }

            OnContactHitApplied(targetRoot.name);
            return true;
        }

        private bool IsInTargetLayer(int layer)
        {
            return (targetLayers.value & (1 << layer)) != 0;
        }

        private void OnContactHitApplied(string targetName)
        {
            _cooldownTimer = attackCooldown;
            if (logContactHit)
            {
                Debug.Log($"Enemy contact hit -> {targetName}, damage={contactDamage:0.##}");
            }
        }

        private bool IsSelfCollider(Collider2D collider)
        {
            if (collider == null)
            {
                return true;
            }

            if (core != null && core.Body != null && collider.attachedRigidbody == core.Body)
            {
                return true;
            }

            Transform t = collider.transform;
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

            // Root-first convention: only a lightweight parent fallback is kept.
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

        private static bool IsComponentAlive(Component component)
        {
            return component != null && component.gameObject != null;
        }
    }
}
