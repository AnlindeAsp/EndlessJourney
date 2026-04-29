using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Handles enemy death presentation-only behavior.
    /// On death: disable configured colliders and renderers.
    /// </summary>
    public class EnemyDeathBehaviour2D : MonoBehaviour
    {
        [Header("References (Assign Manually)")]
        [SerializeField] private EnemyHittable hittable;

        [Header("Disable On Death (Assign Manually)")]
        [SerializeField] private Collider2D[] collidersToDisable;
        [SerializeField] private Renderer[] renderersToDisable;

        [Header("Debug")]
        [SerializeField] private bool logDeathAction;

        private bool _deathApplied;

        private void OnEnable()
        {
            if (hittable != null)
            {
                hittable.OnDied += HandleDied;

                if (hittable.IsDead)
                {
                    ApplyDeathEffects();
                }
            }
        }

        private void OnDisable()
        {
            if (hittable != null)
            {
                hittable.OnDied -= HandleDied;
            }
        }

        private void HandleDied()
        {
            ApplyDeathEffects();
        }

        private void ApplyDeathEffects()
        {
            if (_deathApplied)
            {
                return;
            }

            _deathApplied = true;

            if (collidersToDisable != null)
            {
                for (int i = 0; i < collidersToDisable.Length; i++)
                {
                    Collider2D col = collidersToDisable[i];
                    if (col != null)
                    {
                        col.enabled = false;
                    }
                }
            }

            if (renderersToDisable != null)
            {
                for (int i = 0; i < renderersToDisable.Length; i++)
                {
                    Renderer rendererRef = renderersToDisable[i];
                    if (rendererRef != null)
                    {
                        rendererRef.enabled = false;
                    }
                }
            }

            if (logDeathAction)
            {
                Debug.Log($"{name} death behaviour applied: colliders/renderers disabled.", this);
            }
        }
    }
}
