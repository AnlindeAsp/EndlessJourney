using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Handles enemy hitstun and knockback reaction from successful hits.
    /// </summary>
    [RequireComponent(typeof(EnemyCore2D))]
    [RequireComponent(typeof(EnemyHittable))]
    public class EnemyHitReaction2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyCore2D core;
        [SerializeField] private EnemyHittable hittable;

        [Header("Hit Stun")]
        [SerializeField] private bool enableHitReaction = true;
        [SerializeField, Min(0f)] private float baseHitStunDuration = 0.08f;
        [SerializeField, Min(0f)] private float damageToHitStun = 0.015f;
        [SerializeField, Min(0f)] private float maxHitStunDuration = 0.28f;
        [SerializeField, Min(0f)] private float stunHorizontalDamping = 18f;

        [Header("Knockback")]
        [SerializeField, Min(0f)] private float baseKnockbackX = 4.5f;
        [SerializeField, Min(0f)] private float damageToKnockbackXRatio = 0.08f;
        [SerializeField] private bool enableVerticalKnockback = false;
        [SerializeField, Min(0f)] private float baseKnockbackY = 2f;
        [SerializeField, Min(0f)] private float damageToKnockbackYRatio = 0.02f;
        [SerializeField, Min(0f)] private float knockResistance = 1f;

        [Header("Debug")]
        [SerializeField] private bool logHitReaction = false;

        private float _hitStunTimer;

        /// <summary>
        /// True while enemy is in hitstun.
        /// </summary>
        public bool IsInHitStun => _hitStunTimer > 0f;

        /// <summary>
        /// Remaining hitstun time in seconds.
        /// </summary>
        public float HitStunRemaining => Mathf.Max(0f, _hitStunTimer);

        private void Reset()
        {
            core = GetComponent<EnemyCore2D>();
            hittable = GetComponent<EnemyHittable>();
        }

        private void Awake()
        {
            if (core == null) core = GetComponent<EnemyCore2D>();
            if (hittable == null) hittable = GetComponent<EnemyHittable>();
        }

        private void OnEnable()
        {
            if (hittable != null)
            {
                hittable.OnHitApplied += HandleHitApplied;
            }
        }

        private void OnDisable()
        {
            if (hittable != null)
            {
                hittable.OnHitApplied -= HandleHitApplied;
            }

            _hitStunTimer = 0f;
            core?.SetStunned(false);
        }

        private void Update()
        {
            if (_hitStunTimer > 0f)
            {
                _hitStunTimer = Mathf.Max(0f, _hitStunTimer - Time.deltaTime);
                if (_hitStunTimer <= 0f)
                {
                    core?.SetStunned(false);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsInHitStun || stunHorizontalDamping <= 0f || core == null || core.Body == null)
            {
                return;
            }

            Vector2 velocity = core.Body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, stunHorizontalDamping * Time.fixedDeltaTime);
            core.Body.linearVelocity = velocity;
        }

        private void HandleHitApplied(HitContext context, HitResult result)
        {
            if (!enableHitReaction || !result.WasApplied || result.KilledTarget)
            {
                return;
            }

            if (core == null || core.Body == null || core.IsDead)
            {
                return;
            }

            ApplyHitStun(context.Damage);
            ApplyKnockback(context);
        }

        private void ApplyHitStun(float damage)
        {
            float stunDuration = baseHitStunDuration + Mathf.Max(0f, damage) * damageToHitStun;
            stunDuration = Mathf.Clamp(stunDuration, 0f, maxHitStunDuration);

            if (stunDuration > _hitStunTimer)
            {
                _hitStunTimer = stunDuration;
            }

            if (_hitStunTimer > 0f)
            {
                core?.SetStunned(true);
            }
        }

        private void ApplyKnockback(HitContext context)
        {
            float knockbackScale = 1f / (1f + Mathf.Max(0f, knockResistance));
            float knockbackX = (baseKnockbackX + Mathf.Max(0f, context.Damage) * damageToKnockbackXRatio) * knockbackScale;
            float knockbackY = (baseKnockbackY + Mathf.Max(0f, context.Damage) * damageToKnockbackYRatio) * knockbackScale;

            float dirX = ResolveHorizontalKnockbackDirection(context);
            float dirY = ResolveVerticalKnockbackDirection(context);

            Vector2 velocity = core.Body.linearVelocity;
            velocity.x = dirX * knockbackX;
            if (enableVerticalKnockback)
            {
                velocity.y = dirY * knockbackY;
            }
            core.Body.linearVelocity = velocity;

            if (logHitReaction)
            {
                Debug.Log($"Enemy hit reaction: stun={_hitStunTimer:0.###}, kb=({velocity.x:0.##}, {velocity.y:0.##})");
            }
        }

        private float ResolveHorizontalKnockbackDirection(HitContext context)
        {
            if (Mathf.Abs(context.HitDirection.x) > 0.01f)
            {
                return Mathf.Sign(context.HitDirection.x);
            }

            if (context.Source != null)
            {
                return transform.position.x >= context.Source.transform.position.x ? 1f : -1f;
            }

            return core != null ? core.FacingDirection : 1f;
        }

        private float ResolveVerticalKnockbackDirection(HitContext context)
        {
            if (Mathf.Abs(context.HitDirection.y) > 0.01f)
            {
                return Mathf.Sign(context.HitDirection.y);
            }

            if (context.Source != null)
            {
                return transform.position.y >= context.Source.transform.position.y ? 1f : -1f;
            }

            return 1f;
        }

        private void OnValidate()
        {
            baseHitStunDuration = Mathf.Max(0f, baseHitStunDuration);
            damageToHitStun = Mathf.Max(0f, damageToHitStun);
            maxHitStunDuration = Mathf.Max(0f, maxHitStunDuration);
            stunHorizontalDamping = Mathf.Max(0f, stunHorizontalDamping);
            baseKnockbackX = Mathf.Max(0f, baseKnockbackX);
            damageToKnockbackXRatio = Mathf.Max(0f, damageToKnockbackXRatio);
            baseKnockbackY = Mathf.Max(0f, baseKnockbackY);
            damageToKnockbackYRatio = Mathf.Max(0f, damageToKnockbackYRatio);
            knockResistance = Mathf.Max(0f, knockResistance);
        }
    }
}
