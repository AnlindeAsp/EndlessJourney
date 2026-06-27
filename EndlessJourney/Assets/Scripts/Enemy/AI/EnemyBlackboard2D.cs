using System;
using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Shared runtime memory for enemy perception and decision logic.
    /// Keeps target/perception data in one place for FSM or future behavior trees.
    /// </summary>
    public class EnemyBlackboard2D : MonoBehaviour
    {
        public enum BrainState
        {
            Patrol,
            Chase,
            Attack,
            HitStun,
            Return
        }

        [Header("Target Memory (Read-Only At Runtime)")]
        [SerializeField] private Transform currentTarget;
        [SerializeField] private bool hasDetectedTarget;
        [SerializeField] private bool canSeeTarget;
        [SerializeField] private float distanceToTarget;
        [SerializeField] private Vector2 lastKnownTargetPosition;
        [SerializeField] private float lastSeenTime = -999f;

        [Header("Brain State (Read-Only At Runtime)")]
        [SerializeField] private BrainState currentState = BrainState.Patrol;

        public Transform CurrentTarget => currentTarget;
        public bool HasTarget => currentTarget != null;
        public bool HasDetectedTarget => hasDetectedTarget;
        public bool CanSeeTarget => canSeeTarget;
        public float DistanceToTarget => distanceToTarget;
        public Vector2 LastKnownTargetPosition => lastKnownTargetPosition;
        public float LastSeenTime => lastSeenTime;
        public BrainState CurrentState => currentState;

        public event Action<BrainState> OnStateChanged;

        /// <summary>
        /// Writes a perception snapshot into blackboard memory.
        /// </summary>
        public void SetPerception(Transform target, bool visible, float distance, Vector2 knownPosition)
        {
            if (target != null)
            {
                currentTarget = target;
            }

            canSeeTarget = visible && currentTarget != null;
            distanceToTarget = Mathf.Max(0f, distance);
            lastKnownTargetPosition = knownPosition;

            if (canSeeTarget)
            {
                hasDetectedTarget = true;
                lastSeenTime = Time.time;
            }
        }

        /// <summary>
        /// Keeps target memory but marks current line of sight as lost.
        /// </summary>
        public void MarkTargetOutOfSight(bool keepCurrentTarget)
        {
            canSeeTarget = false;

            if (keepCurrentTarget && currentTarget != null)
            {
                RefreshTrackedTarget(transform.position);
                return;
            }

            currentTarget = null;
            hasDetectedTarget = false;
            distanceToTarget = 0f;
        }

        /// <summary>
        /// Refreshes live target distance/position while chasing a remembered target.
        /// </summary>
        public void RefreshTrackedTarget(Vector2 observerPosition)
        {
            if (currentTarget == null)
            {
                hasDetectedTarget = false;
                canSeeTarget = false;
                distanceToTarget = 0f;
                return;
            }

            Vector2 currentPosition = currentTarget.position;
            distanceToTarget = Vector2.Distance(observerPosition, currentPosition);
            lastKnownTargetPosition = currentPosition;
        }

        /// <summary>
        /// Clears current target and detection memory, but keeps last-known position/timestamp for inspection.
        /// </summary>
        public void ClearTarget()
        {
            currentTarget = null;
            hasDetectedTarget = false;
            canSeeTarget = false;
            distanceToTarget = 0f;
        }

        /// <summary>
        /// True while target is considered recently seen.
        /// </summary>
        public bool HasRecentSight(float memoryDuration)
        {
            return Time.time - lastSeenTime <= Mathf.Max(0f, memoryDuration);
        }

        /// <summary>
        /// Sets current decision state and emits change event when needed.
        /// </summary>
        public void SetState(BrainState state)
        {
            if (currentState == state)
            {
                return;
            }

            currentState = state;
            OnStateChanged?.Invoke(currentState);
        }
    }
}
