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
        [SerializeField] private bool requireLineOfSight = false;
        [SerializeField] private LayerMask obstacleLayers = 0;
        [SerializeField, Min(0.01f)] private float scanInterval = 0.1f;
        [SerializeField] private Vector2 eyeOffset = new Vector2(0f, 0.35f);
        [Tooltip("When enabled, the enemy keeps chasing the first detected target even after the target leaves detection radius.")]
        [SerializeField] private bool keepTargetAfterDetection = true;

        [Header("Debug")]
        [Tooltip("Optional world object shown only while the enemy currently sees a target.")]
        [SerializeField] private GameObject debugSeenIndicator;
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
            SetDebugSeenIndicator(false);
        }

        private void OnDisable()
        {
            SetDebugSeenIndicator(false);
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
            if (blackboard == null)
            {
                return;
            }

            if (core != null && core.IsDead)
            {
                blackboard.ClearTarget();
                SetDebugSeenIndicator(false);
                return;
            }

            Vector2 eye = GetEyePosition();
            Collider2D[] candidates = Physics2D.OverlapCircleAll(eye, detectionRadius, targetLayers);

            Transform bestTarget = null;
            Vector2 bestTargetPoint = eye;
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

                if (!HasLineOfSight(eye, col, targetPoint, distance))
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = target;
                    bestTargetPoint = targetPoint;
                }
            }

            if (bestTarget == null)
            {
                blackboard.MarkTargetOutOfSight(keepTargetAfterDetection && blackboard.HasTarget);
                SetDebugSeenIndicator(false);
                return;
            }

            blackboard.SetPerception(bestTarget, true, bestDistance, bestTargetPoint);
            SetDebugSeenIndicator(true);
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

        private bool HasLineOfSight(Vector2 eye, Collider2D targetCollider, Vector2 targetPoint, float distance)
        {
            if (!requireLineOfSight || obstacleLayers.value == 0)
            {
                return true;
            }

            Vector2 dir = (targetPoint - eye).normalized;
            RaycastHit2D[] hits = Physics2D.RaycastAll(eye, dir, distance, obstacleLayers);

            bool blocked = false;
            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hitCollider = hits[i].collider;
                if (hitCollider == null || IsSelfOrTargetCollider(hitCollider, targetCollider))
                {
                    continue;
                }

                blocked = true;
                break;
            }

            if (drawSightRay)
            {
                Color color = blocked ? Color.red : sightRayColor;
                Debug.DrawRay(eye, dir * distance, color, scanInterval, false);
            }

            return !blocked;
        }

        private Vector2 GetEyePosition()
        {
            if (eyePoint != null)
            {
                return eyePoint.position;
            }

            return (Vector2)transform.position + eyeOffset;
        }

        private bool IsSelfOrTargetCollider(Collider2D hitCollider, Collider2D targetCollider)
        {
            if (hitCollider == null)
            {
                return true;
            }

            if (targetCollider != null && hitCollider == targetCollider)
            {
                return true;
            }

            Transform hitRoot = hitCollider.attachedRigidbody != null
                ? hitCollider.attachedRigidbody.transform
                : hitCollider.transform;

            if (hitRoot == null)
            {
                return true;
            }

            if (hitRoot == transform || hitRoot.IsChildOf(transform) || transform.IsChildOf(hitRoot))
            {
                return true;
            }

            if (targetCollider == null)
            {
                return false;
            }

            Transform targetRoot = targetCollider.attachedRigidbody != null
                ? targetCollider.attachedRigidbody.transform
                : targetCollider.transform;

            return targetRoot != null && (
                hitRoot == targetRoot ||
                hitRoot.IsChildOf(targetRoot) ||
                targetRoot.IsChildOf(hitRoot));
        }

        private void SetDebugSeenIndicator(bool visible)
        {
            if (debugSeenIndicator != null && debugSeenIndicator.activeSelf != visible)
            {
                debugSeenIndicator.SetActive(visible);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 eye = GetEyePosition();
            bool canSee = blackboard != null && blackboard.CanSeeTarget;
            Gizmos.color = canSee
                ? new Color(0.3f, 1f, 0.25f, 0.8f)
                : new Color(0.2f, 0.9f, 0.9f, 0.7f);
            Gizmos.DrawWireSphere(eye, detectionRadius);
        }
    }
}
