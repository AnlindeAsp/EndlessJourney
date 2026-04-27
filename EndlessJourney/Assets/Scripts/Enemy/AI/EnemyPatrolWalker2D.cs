using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Simple patrol enemy:
    /// - Always walks horizontally
    /// - Reverses direction when a wall is detected in front
    /// - Optionally reverses direction when there is no ground ahead (ledge)
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

        [Header("Ledge Detection")]
        [SerializeField] private bool turnBackAtLedge = true;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private bool useColliderBottomForLedgeCheck = true;
        [SerializeField, Min(0f)] private float ledgeCheckExtraOffsetX = 0.02f;
        [SerializeField] private float ledgeCheckStartOffsetY = 0.05f;
        [SerializeField, Min(0.01f)] private float ledgeCheckDistance = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool drawWallCheckRay = true;
        [SerializeField] private Color wallCheckRayColor = Color.yellow;
        [SerializeField] private bool drawLedgeCheckRay = true;
        [SerializeField] private Color ledgeCheckRayColor = Color.cyan;

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

            if (_changeDirectionTimer <= 0f && (IsWallAhead() || IsLedgeAhead()))
            {
                ReverseFacingDirection();
                _changeDirectionTimer = directionChangeCooldown;
            }

            core.SetHorizontalVelocity(FacingDirection * moveSpeed);
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

        private bool IsLedgeAhead()
        {
            if (!turnBackAtLedge)
            {
                return false;
            }

            Vector2 origin = GetLedgeCheckOrigin();
            RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, ledgeCheckDistance, groundLayers);
            bool noGroundAhead = hit.collider == null;

            if (drawLedgeCheckRay)
            {
                Color rayColor = noGroundAhead ? Color.red : ledgeCheckRayColor;
                Debug.DrawRay(origin, Vector2.down * ledgeCheckDistance, rayColor, 0f, false);
            }

            return noGroundAhead;
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

        private Vector2 GetLedgeCheckOrigin()
        {
            float forward = ledgeCheckExtraOffsetX;
            if (useColliderHalfWidthOffset && bodyCollider != null)
            {
                forward += bodyCollider.bounds.extents.x;
            }

            float baseY = transform.position.y;
            if (useColliderBottomForLedgeCheck && bodyCollider != null)
            {
                baseY = bodyCollider.bounds.min.y;
            }

            return new Vector2(
                transform.position.x + forward * FacingDirection,
                baseY + ledgeCheckStartOffsetY
            );
        }

        private void OnValidate()
        {
            ledgeCheckDistance = Mathf.Max(0.01f, ledgeCheckDistance);
            wallCheckDistance = Mathf.Max(0f, wallCheckDistance);
            wallCheckExtraOffsetX = Mathf.Max(0f, wallCheckExtraOffsetX);
            ledgeCheckExtraOffsetX = Mathf.Max(0f, ledgeCheckExtraOffsetX);
        }
    }
}
