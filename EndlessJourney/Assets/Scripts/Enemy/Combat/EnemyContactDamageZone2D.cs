using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Dedicated trigger zone for enemy contact damage.
    /// Attach this to a child object with a trigger collider
    /// (typically slightly larger than enemy body collider).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyContactDamageZone2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyContactAttack2D attackModule;
        [SerializeField] private Collider2D zoneCollider;

        [Header("Auto Find")]
        [Tooltip("Auto locate EnemyContactAttack2D on parent if reference is missing.")]
        [SerializeField] private bool autoFindAttackModuleOnParent = true;

        private void Reset()
        {
            zoneCollider = GetComponent<Collider2D>();
            attackModule = GetComponentInParent<EnemyContactAttack2D>();
        }

        private void Awake()
        {
            if (zoneCollider == null)
            {
                zoneCollider = GetComponent<Collider2D>();
            }

            if (attackModule == null && autoFindAttackModuleOnParent)
            {
                attackModule = GetComponentInParent<EnemyContactAttack2D>();
            }

            if (zoneCollider != null && !zoneCollider.isTrigger)
            {
                Debug.LogWarning("EnemyContactDamageZone2D expects trigger collider. Please set isTrigger = true.");
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (attackModule == null)
            {
                return;
            }

            attackModule.TryHitTargetFromZone(other, zoneCollider);
        }
    }
}
