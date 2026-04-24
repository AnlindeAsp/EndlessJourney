using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Simple perception sensor:
    /// - Finds targets in radius
    /// - Filters by FOV and line of sight
    /// - Writes results into EnemyBlackboard2D
    /// </summary>
    [RequireComponent(typeof(EnemyBlackboard2D))]
    public class EnemyPerception2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyBlackboard2D blackboard;
        [SerializeField] private EnemyCore2D core;
        [Tooltip("Optional eye point. If null, uses transform + eyeOffset.")]
        [SerializeField] private Transform eyePoint;

        [Header("Detection")]
        [SerializeField, Min(0.1f)] private float detectionRadius = 6f;
        [SerializeField, Range(1f, 360f)] private float fieldOfView = 140f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private LayerMask obstacleLayers = ~0;
        [SerializeField, Min(0.01f)] private float scanInterval = 0.1f;
        [SerializeField] private Vector2 eyeOffset = new Vector2(0f, 0.35f);

        [Header("Debug")]
        [SerializeField] private bool drawSightRay = false;
        [SerializeField] private Color sightRayColor = Color.cyan;

        private float _scanTimer;

        private void Reset()
        {
            blackboard = GetComponent<EnemyBlackboard2D>();
            core = GetComponent<EnemyCore2D>();
        }

        private void Awake()
        {
            if (blackboard == null) blackboard = GetComponent<EnemyBlackboard2D>();
            if (core == null) core = GetComponent<EnemyCore2D>();
        }

        private void Update()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f)
            {
                return;
            }

            _scanTimer = scanInterval;
            ScanTargets();
        }

        private void ScanTargets()
        {
            Vector2 eye = GetEyePosition();
            Collider2D[] candidates = Physics2D.OverlapCircleAll(eye, detectionRadius, targetLayers);

            Transform bestTarget = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < candidates.Length; i++)
            {
                Collider2D col = candidates[i];
                if (col == null)
                {
                    continue;
                }

                Transform target = col.attachedRigidbody != null ? col.attachedRigidbody.transform : col.transform;
                if (target == transform || target.IsChildOf(transform) || transform.IsChildOf(target))
                {
                    continue;
                }

                Vector2 targetPoint = col.bounds.center;
                Vector2 toTarget = targetPoint - eye;
                float distance = toTarget.magnitude;
                if (distance <= 0.001f || distance > detectionRadius)
                {
                    continue;
                }

                if (!IsWithinFov(toTarget))
                {
                    continue;
                }

                if (!HasLineOfSight(eye, targetPoint, distance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = target;
                }
            }

            if (bestTarget == null)
            {
                blackboard.ClearTarget();
                return;
            }

            Vector2 knownPosition = bestTarget.position;
            blackboard.SetPerception(bestTarget, true, bestDistance, knownPosition);
        }

        private bool IsWithinFov(Vector2 toTarget)
        {
            if (fieldOfView >= 359f || core == null)
            {
                return true;
            }

            Vector2 forward = new Vector2(core.FacingDirection, 0f);
            float angle = Vector2.Angle(forward, toTarget.normalized);
            return angle <= fieldOfView * 0.5f;
        }

        private bool HasLineOfSight(Vector2 eye, Vector2 targetPoint, float distance)
        {
            Vector2 dir = (targetPoint - eye).normalized;
            RaycastHit2D block = Physics2D.Raycast(eye, dir, distance, obstacleLayers);

            if (drawSightRay)
            {
                Color color = block.collider == null ? sightRayColor : Color.red;
                Debug.DrawRay(eye, dir * distance, color, scanInterval, false);
            }

            return block.collider == null;
        }

        private Vector2 GetEyePosition()
        {
            if (eyePoint != null)
            {
                return eyePoint.position;
            }

            return (Vector2)transform.position + eyeOffset;
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 eye = GetEyePosition();
            Gizmos.color = new Color(0.2f, 0.9f, 0.9f, 0.7f);
            Gizmos.DrawWireSphere(eye, detectionRadius);
        }
    }
}
