using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Shared directional-attack resolver.
    /// Converts input + player state into one attack direction snapshot.
    /// </summary>
    [RequireComponent(typeof(PlayerCore2D))]
    public class PlayerAttackDirectionResolver2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCore2D core;

        [Header("Direction Rules")]
        [SerializeField] private bool downAttackRequiresAirborne = true;
        [SerializeField, Range(0.01f, 1f)] private float verticalIntentThreshold = 0.35f;

        private void Reset()
        {
            core = GetComponent<PlayerCore2D>();
        }

        private void Awake()
        {
            if (core == null)
            {
                core = GetComponent<PlayerCore2D>();
            }
        }

        /// <summary>
        /// Resolves direction for a new attack action.
        /// </summary>
        public AttackDirection2D ResolveDirectionForNewAttack()
        {
            if (core == null || core.Input == null)
            {
                return AttackDirection2D.Forward;
            }

            float verticalIntent = core.Input.VerticalIntent;

            if (verticalIntent >= verticalIntentThreshold)
            {
                return AttackDirection2D.Up;
            }

            if (verticalIntent <= -verticalIntentThreshold)
            {
                if (!downAttackRequiresAirborne || !core.IsGrounded)
                {
                    return AttackDirection2D.Down;
                }
            }

            return AttackDirection2D.Forward;
        }

        private void OnValidate()
        {
            verticalIntentThreshold = Mathf.Clamp(verticalIntentThreshold, 0.01f, 1f);
        }
    }
}
