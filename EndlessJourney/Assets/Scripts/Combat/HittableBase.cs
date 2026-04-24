using System;
using EndlessJourney.Interfaces;
using UnityEngine;

namespace EndlessJourney.Combat
{
    /// <summary>
    /// Optional base class for hit-receivable entities.
    /// Provides shared gate logic (invulnerability, cooldown, self-hit blocking).
    /// </summary>
    public abstract class HittableBase : MonoBehaviour, IHittable, IDamageable2D
    {
        [Header("Hit Gate")]
        [SerializeField] private bool canReceiveHit = true;
        [SerializeField] private bool invulnerable;
        [SerializeField] private bool blockSelfHits = true;
        [SerializeField, Min(0f)] private float hitCooldown = 0f;

        private float _nextAllowedHitTime;

        /// <summary>
        /// Fired after a hit was successfully applied.
        /// </summary>
        public event Action<HitContext, HitResult> OnHitApplied;

        public virtual bool CanBeHit(HitContext context)
        {
            if (!canReceiveHit || invulnerable || !isActiveAndEnabled)
            {
                return false;
            }

            if (Time.time < _nextAllowedHitTime)
            {
                return false;
            }

            if (!blockSelfHits || context.Source == null)
            {
                return true;
            }

            Transform source = context.Source.transform;
            return !(source == transform || source.IsChildOf(transform) || transform.IsChildOf(source));
        }

        public HitResult ReceiveHit(HitContext context)
        {
            if (!CanBeHit(context))
            {
                return HitResult.Blocked("Hit gate blocked.");
            }

            HitResult result = ProcessHit(context);
            if (!result.WasApplied)
            {
                return result;
            }

            if (hitCooldown > 0f)
            {
                _nextAllowedHitTime = Time.time + hitCooldown;
            }

            OnHitApplied?.Invoke(context, result);
            return result;
        }

        /// <summary>
        /// Backward compatibility bridge for old damage-only pipeline.
        /// </summary>
        public bool ReceiveDamage(float amount, GameObject source)
        {
            HitContext context = new HitContext(
                source,
                null,
                transform.position,
                Vector2.zero,
                Mathf.Max(0f, amount),
                HitType.Melee);

            return ReceiveHit(context).WasApplied;
        }

        /// <summary>
        /// Per-entity hit behavior implementation.
        /// </summary>
        protected abstract HitResult ProcessHit(HitContext context);
    }
}
