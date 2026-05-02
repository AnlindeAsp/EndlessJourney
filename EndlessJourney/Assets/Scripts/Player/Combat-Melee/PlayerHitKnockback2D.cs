using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Applies horizontal-only knockback to player when harm damage is received.
    /// </summary>
    public class PlayerHitKnockback2D : MonoBehaviour
    {
        [Header("References (Assign Manually)")]
        [SerializeField] private PlayerCore2D core;
        [SerializeField] private PlayerHealth2D health;

        [Header("Horizontal Knockback")]
        [SerializeField] private bool enableKnockback = true;
        [SerializeField, Min(0f)] private float baseKnockbackX = 8f;
        [SerializeField, Min(0f)] private float damageToKnockbackXRatio = 0f;
        [SerializeField, Min(0f)] private float maxKnockbackX = 16f;

        [Header("Debug")]
        [SerializeField] private bool logKnockback;

        private void Awake()
        {
            if (core == null || health == null)
            {
                Debug.LogError("PlayerHitKnockback2D missing references. Assign PlayerCore2D and PlayerHealth2D.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnHarmDamaged += HandleHarmDamaged;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnHarmDamaged -= HandleHarmDamaged;
            }
        }

        private void HandleHarmDamaged(float damage, GameObject source)
        {
            if (!enableKnockback || core == null || core.Body == null || health == null || health.IsDead)
            {
                return;
            }

            float directionX = ResolveHorizontalDirection(source);
            float knockbackX = baseKnockbackX + Mathf.Max(0f, damage) * damageToKnockbackXRatio;
            knockbackX = Mathf.Clamp(knockbackX, 0f, Mathf.Max(0f, maxKnockbackX));

            Vector2 velocity = core.Body.linearVelocity;
            velocity.x = directionX * knockbackX;
            core.Body.linearVelocity = velocity;

            if (logKnockback)
            {
                Debug.Log($"Player hit knockback applied: vx={velocity.x:0.##}, source={(source != null ? source.name : "null")}", this);
            }
        }

        private float ResolveHorizontalDirection(GameObject source)
        {
            if (source == null)
            {
                return core.FacingDirection >= 0 ? -1f : 1f;
            }

            return transform.position.x >= source.transform.position.x ? 1f : -1f;
        }

        private void OnValidate()
        {
            baseKnockbackX = Mathf.Max(0f, baseKnockbackX);
            damageToKnockbackXRatio = Mathf.Max(0f, damageToKnockbackXRatio);
            maxKnockbackX = Mathf.Max(0f, maxKnockbackX);
        }
    }
}
