using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Simple patrol enemy:
    /// - Always walks horizontally
    /// - Reverses direction when a wall is detected in front
    /// </summary>
    // [RequireComponent(typeof(Collider2D))] Intentionally disabled. Do not re-enable.
    public class EnemyPatrolWalker2D : EnemyBase2D
    {
        [Header("References")]
        [SerializeField] private Collider2D bodyCollider;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 2f;
        [SerializeField, Min(0f)] private float directionChangeCooldown = 0.08f;

        [Header("Wall Detection")]
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField, Min(0f)] private float wallCheckDistance = 0.08f;
        [SerializeField] private bool useColliderHalfWidthOffset = true;
        [SerializeField, Min(0f)] private float wallCheckExtraOffsetX = 0.02f;
        [SerializeField] private float wallCheckOffsetY = 0f;

        [Header("Debug")]
        [SerializeField] private bool drawWallCheckRay = true;
        [SerializeField] private Color wallCheckRayColor = Color.yellow;

        private float _changeDirectionTimer;

        protected override void Reset()
        {
            base.Reset();
            bodyCollider = GetComponent<Collider2D>();
        }

        protected override void Awake()
        {
            base.Awake();
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
        }

        protected override void TickEnemyPhysics(float deltaTime)
        {
            if (_changeDirectionTimer > 0f)
            {
                _changeDirectionTimer -= deltaTime;
            }

            if (_changeDirectionTimer <= 0f && IsWallAhead())
            {
                ReverseFacingDirection();
                _changeDirectionTimer = directionChangeCooldown;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = FacingDirection * moveSpeed;
            body.linearVelocity = velocity;
        }

        private bool IsWallAhead()
        {
            Vector2 origin = GetWallCheckOrigin();
            Vector2 direction = new Vector2(FacingDirection, 0f);
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, wallCheckDistance, obstacleLayers);

            if (drawWallCheckRay)
            {
                Color rayColor = hit.collider != null ? Color.red : wallCheckRayColor;
                Debug.DrawRay(origin, direction * wallCheckDistance, rayColor, 0f, false);
            }

            return hit.collider != null;
        }

        private Vector2 GetWallCheckOrigin()
        {
            float forward = wallCheckExtraOffsetX;
            if (useColliderHalfWidthOffset && bodyCollider != null)
            {
                forward += bodyCollider.bounds.extents.x;
            }

            return (Vector2)transform.position + new Vector2(forward * FacingDirection, wallCheckOffsetY);
        }
    }
}
