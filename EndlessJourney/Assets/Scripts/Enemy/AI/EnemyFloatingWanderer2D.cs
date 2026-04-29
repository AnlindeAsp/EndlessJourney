using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Passive floating enemy behavior:
    /// - Randomly moves in 8 directions
    /// - Bounces when touching movement bounds
    /// - Bounces when detecting obstacle/ground ahead
    /// </summary>
    [RequireComponent(typeof(EnemyCore2D))]
    public class EnemyFloatingWanderer2D : EnemyBase2D
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 2.2f;
        [SerializeField, Min(0.01f)] private float minDirectionChangeInterval = 0.45f;
        [SerializeField, Min(0.01f)] private float maxDirectionChangeInterval = 1.15f;

        [Header("Movement Bounds")]
        [SerializeField] private bool useMovementBounds = true;
        [SerializeField] private Vector2 boundsHalfSize = new Vector2(3.5f, 2f);

        [Header("Obstacle Check")]
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField, Min(0.01f)] private float obstacleCheckDistance = 0.25f;
        [SerializeField, Min(0f)] private float obstacleCheckRadius = 0.12f;

        [Header("Debug")]
        [SerializeField] private bool drawDebug;
        [SerializeField] private Color boundsColor = new Color(0.25f, 0.9f, 1f, 0.8f);
        [SerializeField] private Color obstacleRayColor = new Color(1f, 0.75f, 0.2f, 0.9f);

        private static readonly Vector2[] EightDirections =
        {
            new Vector2(1f, 0f),
            new Vector2(-1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, -1f),
            new Vector2(1f, 1f).normalized,
            new Vector2(1f, -1f).normalized,
            new Vector2(-1f, 1f).normalized,
            new Vector2(-1f, -1f).normalized
        };

        private Vector2 _originPosition;
        private Vector2 _moveDirection;
        private float _directionTimer;

        protected override void Awake()
        {
            base.Awake();

            if (body == null)
            {
                return;
            }

            _originPosition = body.position;
            PickRandomDirection(resetTimer: true);
        }

        private void OnEnable()
        {
            if (body == null)
            {
                return;
            }

            _originPosition = body.position;
            PickRandomDirection(resetTimer: true);
        }

        protected override void TickEnemyPhysics(float deltaTime)
        {
            if (body == null)
            {
                return;
            }

            TickDirectionTimer(deltaTime);
            BounceByBoundsIfNeeded();
            BounceByObstacleIfNeeded(deltaTime);
            ApplyVelocity();
            UpdateFacingByDirection();
        }

        private void TickDirectionTimer(float deltaTime)
        {
            _directionTimer -= deltaTime;
            if (_directionTimer <= 0f)
            {
                PickRandomDirection(resetTimer: true);
            }
        }

        private void PickRandomDirection(bool resetTimer)
        {
            int index = Random.Range(0, EightDirections.Length);
            _moveDirection = EightDirections[index];

            if (resetTimer)
            {
                float minInterval = Mathf.Max(0.01f, minDirectionChangeInterval);
                float maxInterval = Mathf.Max(minInterval, maxDirectionChangeInterval);
                _directionTimer = Random.Range(minInterval, maxInterval);
            }
        }

        private void BounceByBoundsIfNeeded()
        {
            if (!useMovementBounds)
            {
                return;
            }

            Vector2 pos = body.position;
            Vector2 delta = pos - _originPosition;
            bool flipped = false;

            if (delta.x > boundsHalfSize.x && _moveDirection.x > 0f)
            {
                _moveDirection.x = -_moveDirection.x;
                flipped = true;
            }
            else if (delta.x < -boundsHalfSize.x && _moveDirection.x < 0f)
            {
                _moveDirection.x = -_moveDirection.x;
                flipped = true;
            }

            if (delta.y > boundsHalfSize.y && _moveDirection.y > 0f)
            {
                _moveDirection.y = -_moveDirection.y;
                flipped = true;
            }
            else if (delta.y < -boundsHalfSize.y && _moveDirection.y < 0f)
            {
                _moveDirection.y = -_moveDirection.y;
                flipped = true;
            }

            if (!flipped)
            {
                return;
            }

            _moveDirection = NormalizeOrFallback(_moveDirection);
            _directionTimer = Mathf.Min(_directionTimer, 0.2f);

            // Clamp inside bounds to avoid sitting outside and jittering.
            Vector2 clamped = new Vector2(
                Mathf.Clamp(pos.x, _originPosition.x - boundsHalfSize.x, _originPosition.x + boundsHalfSize.x),
                Mathf.Clamp(pos.y, _originPosition.y - boundsHalfSize.y, _originPosition.y + boundsHalfSize.y));
            body.position = clamped;
        }

        private void BounceByObstacleIfNeeded(float deltaTime)
        {
            if (_moveDirection.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Vector2 origin = body.position;
            float distance = Mathf.Max(0.01f, obstacleCheckDistance + moveSpeed * deltaTime);
            RaycastHit2D hit = FindFirstObstacleHit(origin, distance);

            if (drawDebug)
            {
                Debug.DrawRay(origin, _moveDirection * distance, obstacleRayColor, 0f, false);
            }

            if (hit.collider == null)
            {
                return;
            }

            Vector2 reflected = Vector2.Reflect(_moveDirection, hit.normal);
            _moveDirection = NormalizeOrFallback(reflected);
            _directionTimer = Mathf.Min(_directionTimer, 0.25f);
        }

        private RaycastHit2D FindFirstObstacleHit(Vector2 origin, float distance)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, obstacleCheckRadius, _moveDirection, distance, obstacleLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit2D candidate = hits[i];
                if (candidate.collider == null)
                {
                    continue;
                }

                if (body != null && candidate.rigidbody == body)
                {
                    continue;
                }

                Transform t = candidate.collider.transform;
                if (t == transform || t.IsChildOf(transform))
                {
                    continue;
                }

                return candidate;
            }

            return default;
        }

        private void ApplyVelocity()
        {
            Vector2 velocity = _moveDirection * moveSpeed;
            body.linearVelocity = velocity;
        }

        private void UpdateFacingByDirection()
        {
            if (core == null)
            {
                return;
            }

            if (Mathf.Abs(_moveDirection.x) > 0.01f)
            {
                core.FaceDirection(_moveDirection.x >= 0f ? 1 : -1);
            }
        }

        private static Vector2 NormalizeOrFallback(Vector2 input)
        {
            if (input.sqrMagnitude <= 0.0001f)
            {
                return Vector2.right;
            }

            return input.normalized;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebug || !useMovementBounds)
            {
                return;
            }

            Gizmos.color = boundsColor;
            Vector3 center = Application.isPlaying ? (Vector3)_originPosition : transform.position;
            Vector3 size = new Vector3(boundsHalfSize.x * 2f, boundsHalfSize.y * 2f, 0f);
            Gizmos.DrawWireCube(center, size);
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            minDirectionChangeInterval = Mathf.Max(0.01f, minDirectionChangeInterval);
            maxDirectionChangeInterval = Mathf.Max(minDirectionChangeInterval, maxDirectionChangeInterval);
            boundsHalfSize.x = Mathf.Max(0.05f, boundsHalfSize.x);
            boundsHalfSize.y = Mathf.Max(0.05f, boundsHalfSize.y);
            obstacleCheckDistance = Mathf.Max(0.01f, obstacleCheckDistance);
            obstacleCheckRadius = Mathf.Max(0f, obstacleCheckRadius);
        }
    }
}
