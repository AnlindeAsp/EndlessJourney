using UnityEngine;
using EndlessJourney.Player;

namespace EndlessJourney.Cameras
{
    /// <summary>
    /// Lightweight but practical 2D follow camera for platformer prototypes.
    /// </summary>
    public class SimpleCameraFollow2D : MonoBehaviour
    {
        [Header("Follow Target")]
        [SerializeField] private Transform target;
        [SerializeField] private bool autoFindPlayerOnStart = true;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
        [SerializeField] private bool snapOnStart = false;

        [Header("Follow Smoothing")]
        [SerializeField, Min(0f)] private float smoothTimeX = 0.10f;
        [SerializeField, Min(0f)] private float smoothTimeY = 0.16f;

        [Header("Look Ahead")]
        [SerializeField] private bool enableLookAhead = true;
        [SerializeField, Min(0f)] private float lookAheadDistance = 1.6f;
        [SerializeField, Min(0.01f)] private float lookAheadSmoothTime = 0.16f;
        [SerializeField, Min(0f)] private float lookAheadResetSpeed = 6f;
        [SerializeField, Min(0f)] private float lookAheadMoveThreshold = 0.02f;

        [Header("Dead Zone")]
        [SerializeField] private bool enableDeadZone = true;
        [SerializeField, Min(0f)] private float deadZoneWidth = 1.2f;
        [SerializeField, Min(0f)] private float deadZoneHeight = 0.7f;

        [Header("Bounds (Optional)")]
        [SerializeField] private bool clampToBounds = false;
        [SerializeField] private Vector2 minBounds = new Vector2(-999f, -999f);
        [SerializeField] private Vector2 maxBounds = new Vector2(999f, 999f);

        [Header("Debug")]
        [SerializeField] private bool drawDeadZoneGizmo = true;

        private float _velocityX;
        private float _velocityY;
        private float _lookAheadX;
        private float _lookAheadVelocity;
        private Vector3 _lastTargetPosition;

        private void Awake()
        {
            TryAutoFindTarget();

            if (target != null)
            {
                if (snapOnStart)
                {
                    Vector3 initial = target.position + offset;
                    transform.position = new Vector3(initial.x, initial.y, initial.z);
                }

                _lastTargetPosition = target.position;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                TryAutoFindTarget();
                if (target == null)
                {
                    return;
                }
            }

            Vector3 targetPosition = target.position;
            Vector2 targetVelocity = ResolveTargetVelocity(targetPosition);
            UpdateLookAhead(targetVelocity.x, Time.deltaTime);

            Vector3 desiredPosition = targetPosition + offset + new Vector3(_lookAheadX, 0f, 0f);

            if (enableDeadZone)
            {
                desiredPosition = ApplyDeadZone(desiredPosition, transform.position);
            }

            float newX = Mathf.SmoothDamp(transform.position.x, desiredPosition.x, ref _velocityX, smoothTimeX);
            float newY = Mathf.SmoothDamp(transform.position.y, desiredPosition.y, ref _velocityY, smoothTimeY);
            float newZ = desiredPosition.z;

            if (clampToBounds)
            {
                newX = Mathf.Clamp(newX, minBounds.x, maxBounds.x);
                newY = Mathf.Clamp(newY, minBounds.y, maxBounds.y);
            }

            transform.position = new Vector3(newX, newY, newZ);
            _lastTargetPosition = targetPosition;
        }

        private void TryAutoFindTarget()
        {
            if (target != null || !autoFindPlayerOnStart)
            {
                return;
            }

            PlayerCore2D playerCore = FindAnyObjectByType<PlayerCore2D>();
            if (playerCore != null)
            {
                target = playerCore.transform;
                _lastTargetPosition = target.position;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            if (target != null)
            {
                _lastTargetPosition = target.position;
            }
        }

        private Vector2 ResolveTargetVelocity(Vector3 targetPosition)
        {
            PlayerCore2D core = target != null ? target.GetComponent<PlayerCore2D>() : null;
            if (core != null && core.Body != null)
            {
                return core.Body.linearVelocity;
            }

            if (Time.deltaTime <= 0f)
            {
                return Vector2.zero;
            }

            Vector3 delta = targetPosition - _lastTargetPosition;
            return new Vector2(delta.x / Time.deltaTime, delta.y / Time.deltaTime);
        }

        private void UpdateLookAhead(float horizontalVelocity, float deltaTime)
        {
            if (!enableLookAhead || lookAheadDistance <= 0f)
            {
                _lookAheadX = 0f;
                return;
            }

            float desiredLookAhead = 0f;
            if (Mathf.Abs(horizontalVelocity) >= lookAheadMoveThreshold)
            {
                desiredLookAhead = Mathf.Sign(horizontalVelocity) * lookAheadDistance;
            }
            else
            {
                desiredLookAhead = Mathf.MoveTowards(_lookAheadX, 0f, lookAheadResetSpeed * deltaTime);
            }

            _lookAheadX = Mathf.SmoothDamp(_lookAheadX, desiredLookAhead, ref _lookAheadVelocity, lookAheadSmoothTime);
        }

        private Vector3 ApplyDeadZone(Vector3 desiredPosition, Vector3 currentPosition)
        {
            Vector3 adjusted = desiredPosition;
            float halfWidth = Mathf.Max(0f, deadZoneWidth) * 0.5f;
            float halfHeight = Mathf.Max(0f, deadZoneHeight) * 0.5f;

            float deltaX = desiredPosition.x - currentPosition.x;
            if (Mathf.Abs(deltaX) <= halfWidth)
            {
                adjusted.x = currentPosition.x;
            }
            else
            {
                adjusted.x = desiredPosition.x - Mathf.Sign(deltaX) * halfWidth;
            }

            float deltaY = desiredPosition.y - currentPosition.y;
            if (Mathf.Abs(deltaY) <= halfHeight)
            {
                adjusted.y = currentPosition.y;
            }
            else
            {
                adjusted.y = desiredPosition.y - Mathf.Sign(deltaY) * halfHeight;
            }

            return adjusted;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDeadZoneGizmo || !enableDeadZone)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.84f, 0.2f, 0.9f);
            Vector3 center = Application.isPlaying ? transform.position : transform.position;
            Gizmos.DrawWireCube(center, new Vector3(deadZoneWidth, deadZoneHeight, 0f));

            if (clampToBounds)
            {
                Gizmos.color = new Color(0.25f, 0.85f, 0.95f, 0.8f);
                Vector2 size = maxBounds - minBounds;
                Vector3 boundsCenter = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0f);
                Gizmos.DrawWireCube(boundsCenter, new Vector3(size.x, size.y, 0f));
            }
        }
    }
}
