using System;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Minimal enemy hit receiver.
    /// Handles health, death state, and hit events.
    /// </summary>
    public class EnemyHittable : HittableBase
    {
        [Header("Health")]
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField] private bool startAtMaxHealth = true;
        [SerializeField, Min(0f)] private float startingHealth = 100f;
        
        [Header("Runtime (Read-Only)")]
        [SerializeField, Min(0f)] private float currentHealth;
        [SerializeField] private bool isDead;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public float HealthNormalized => maxHealth > 0f ? currentHealth / maxHealth : 0f;

        public event Action<float> OnDamaged;
        public event Action OnDied;

        private void Awake()
        {
            float initial = startAtMaxHealth ? maxHealth : startingHealth;
            currentHealth = Mathf.Clamp(initial, 0f, maxHealth);
            isDead = currentHealth <= 0f;
        }

        public void SetHealth(float value)
        {
            currentHealth = Mathf.Clamp(value, 0f, maxHealth);
            isDead = currentHealth <= 0f;
        }

        protected override HitResult ProcessHit(HitContext context)
        {
            if (isDead)
            {
                return HitResult.Blocked("Target already dead.");
            }

            float damage = Mathf.Max(0f, context.Damage);
            if (damage <= 0f)
            {
                return HitResult.Blocked("Damage <= 0.");
            }

            // Damage applying logic
            float previous = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            float applied = previous - currentHealth;
            if (applied <= 0f)
            {
                return HitResult.Blocked("No effective damage.");
            }

            OnDamaged?.Invoke(applied);

            bool killed = currentHealth <= 0f;
            if (killed && !isDead)
            {
                isDead = true;
                OnDied?.Invoke();
            }

            return HitResult.Applied(applied, killed);
        }

        private void OnValidate()
        {
            maxHealth = Mathf.Max(1f, maxHealth);
            startingHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
        }
    }
}
