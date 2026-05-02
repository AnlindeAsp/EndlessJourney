using System;
using System.Collections.Generic;
using EndlessJourney.Combat;
using EndlessJourney.Interfaces;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Player projectile combat core.
    /// Owns only hit detection/damage/lifetime. Movement is handled by a separate module.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerProjectile2D : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Owner of this projectile. Used to ignore self-hit.")]
        [SerializeField] private GameObject owner;
        [Tooltip("Trigger collider used for hit detection.")]
        [SerializeField] private Collider2D hitTrigger;

        [Header("Damage")]
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField] private HitType hitType = HitType.Projectile;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private bool includeTriggerTargets = true;
        [SerializeField] private bool preventRepeatHitOnSameTarget = true;

        [Header("Lifetime")]
        [SerializeField, Min(0.01f)] private float lifeTime = 3f;

        [Header("Collision Consume")]
        [Tooltip("Destroy projectile after a successful damage hit.")]
        [SerializeField] private bool destroyOnHit = true;
        [Tooltip("Destroy projectile when colliding a valid target layer even if hit was blocked.")]
        [SerializeField] private bool destroyOnTargetCollision = false;
        [Tooltip("How many successful hits this projectile can apply. <=0 means unlimited.")]
        [SerializeField] private int maxSuccessfulHits = 1;

        [Header("Debug")]
        [SerializeField] private bool logHitDebug;

        private readonly HashSet<int> _hitTargetIds = new HashSet<int>();
        private int _successfulHitCount;
        private float _lifeTimer;

        public event Action<GameObject, float> OnProjectileHitApplied;
        public event Action OnProjectileExpired;

        public GameObject Owner => owner;
        public float Damage => damage;
        public float LifeTime => lifeTime;

        private void Reset()
        {
            hitTrigger = GetComponent<Collider2D>();
            if (hitTrigger != null)
            {
                hitTrigger.isTrigger = true;
            }
        }

        private void Awake()
        {
            if (hitTrigger == null)
            {
                hitTrigger = GetComponent<Collider2D>();
            }

            if (hitTrigger == null)
            {
                Debug.LogError("PlayerProjectile2D requires a trigger collider.", this);
                enabled = false;
                return;
            }

            if (!hitTrigger.isTrigger)
            {
                hitTrigger.isTrigger = true;
            }

            _lifeTimer = lifeTime;
        }

        /// <summary>
        /// Initializes projectile runtime owner and optional stat overrides after instantiate.
        /// Movement is intentionally handled by separate modules.
        /// </summary>
        public void InitializeProjectile(GameObject projectileOwner, bool overrideDamage = false, float damageValue = 0f, bool overrideLifeTime = false, float lifeTimeValue = 0f)
        {
            owner = projectileOwner;

            if (overrideDamage)
            {
                damage = Mathf.Max(0f, damageValue);
            }

            if (overrideLifeTime)
            {
                lifeTime = Mathf.Max(0.01f, lifeTimeValue);
            }

            _successfulHitCount = 0;
            _hitTargetIds.Clear();
            _lifeTimer = lifeTime;
        }

        private void Update()
        {
            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer > 0f)
            {
                return;
            }

            OnProjectileExpired?.Invoke();
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !enabled)
            {
                return;
            }

            if (!includeTriggerTargets && other.isTrigger)
            {
                return;
            }

            if (!IsInTargetLayer(other.gameObject.layer))
            {
                return;
            }

            if (IsSelfOrOwnerCollider(other))
            {
                return;
            }

            if (!TryResolveTargetRoot(other, out GameObject targetRoot))
            {
                return;
            }

            if (preventRepeatHitOnSameTarget && _hitTargetIds.Contains(targetRoot.GetInstanceID()))
            {
                return;
            }

            bool applied = TryApplyDamage(other, targetRoot, out float damageApplied);
            if (applied)
            {
                _successfulHitCount++;
                _hitTargetIds.Add(targetRoot.GetInstanceID());
                OnProjectileHitApplied?.Invoke(targetRoot, damageApplied);

                if (logHitDebug)
                {
                    Debug.Log($"Projectile hit [{targetRoot.name}] damage={damageApplied:0.##}", this);
                }

                if (destroyOnHit || ShouldDestroyBySuccessfulHitLimit())
                {
                    Destroy(gameObject);
                }

                return;
            }

            if (destroyOnTargetCollision)
            {
                Destroy(gameObject);
            }
        }

        private bool ShouldDestroyBySuccessfulHitLimit()
        {
            return maxSuccessfulHits > 0 && _successfulHitCount >= maxSuccessfulHits;
        }

        private bool TryApplyDamage(Collider2D hitCollider, GameObject targetRoot, out float damageApplied)
        {
            damageApplied = 0f;
            if (damage <= 0f)
            {
                return false;
            }

            Vector2 hitDirection = ResolveHitDirection(targetRoot);
            Vector2 hitPoint = hitCollider.ClosestPoint(transform.position);

            IHittable hittable = targetRoot.GetComponent(typeof(IHittable)) as IHittable;
            if (hittable == null)
            {
                hittable = targetRoot.GetComponentInParent(typeof(IHittable)) as IHittable;
            }

            if (hittable != null)
            {
                HitContext context = new HitContext(
                    owner != null ? owner : gameObject,
                    hitTrigger,
                    hitPoint,
                    hitDirection,
                    damage,
                    hitType
                );

                HitResult result = hittable.ReceiveHit(context);
                if (result.WasApplied)
                {
                    damageApplied = result.DamageApplied > 0f ? result.DamageApplied : damage;
                    return true;
                }

                return false;
            }

            IDamageable2D damageable = targetRoot.GetComponent(typeof(IDamageable2D)) as IDamageable2D;
            if (damageable == null)
            {
                damageable = targetRoot.GetComponentInParent(typeof(IDamageable2D)) as IDamageable2D;
            }

            if (damageable != null && damageable.ReceiveDamage(damage, owner != null ? owner : gameObject))
            {
                damageApplied = damage;
                return true;
            }

            return false;
        }

        private Vector2 ResolveHitDirection(GameObject targetRoot)
        {
            if (targetRoot == null)
            {
                return Vector2.right;
            }

            Vector2 dir = (Vector2)(targetRoot.transform.position - transform.position);
            if (dir.sqrMagnitude <= 0.0001f)
            {
                return Vector2.right;
            }

            return dir.normalized;
        }

        private bool IsSelfOrOwnerCollider(Collider2D other)
        {
            if (other == hitTrigger)
            {
                return true;
            }

            if (owner == null)
            {
                return other.transform.IsChildOf(transform);
            }

            Transform t = other.transform;
            return t == owner.transform || t.IsChildOf(owner.transform) || t.IsChildOf(transform);
        }

        private bool IsInTargetLayer(int layer)
        {
            return (targetLayers.value & (1 << layer)) != 0;
        }

        private static bool TryResolveTargetRoot(Collider2D hitCollider, out GameObject targetRoot)
        {
            if (hitCollider == null)
            {
                targetRoot = null;
                return false;
            }

            targetRoot = hitCollider.attachedRigidbody != null
                ? hitCollider.attachedRigidbody.gameObject
                : hitCollider.gameObject;

            return targetRoot != null;
        }

        private void OnValidate()
        {
            lifeTime = Mathf.Max(0.01f, lifeTime);
            damage = Mathf.Max(0f, damage);
        }
    }
}
