using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Centralized ground detection so all movement abilities
    /// can trust the same grounded state.
    /// Keep this focused and deterministic: one place decides "grounded".
    /// </summary>
    public class GroundCheck2D : MonoBehaviour
    {
        [Header("Ground Check")]
        [SerializeField] private Transform checkPoint;
        [SerializeField, Min(0.01f)] private float checkRadius = 0.2f;
        [SerializeField] private LayerMask groundLayers;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            if (checkPoint == null)
            {
                checkPoint = transform;
            }
        }

        /// <summary>
        /// Updates the current grounded state.
        /// </summary>
        public void Refresh()
        {
            // OverlapCircle is simple and reliable for platformer feet checks.
            Vector2 point = checkPoint != null ? checkPoint.position : transform.position;
            IsGrounded = Physics2D.OverlapCircle(point, checkRadius, groundLayers);
        }

        private void OnDrawGizmosSelected()
        {
            Transform pointTransform = checkPoint != null ? checkPoint : transform;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(pointTransform.position, checkRadius);
        }
    }
}
