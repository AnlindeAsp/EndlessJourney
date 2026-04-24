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
            Return
        }

        [Header("Target Memory (Read-Only At Runtime)")]
        [SerializeField] private Transform currentTarget;
        [SerializeField] private bool canSeeTarget;
        [SerializeField] private float distanceToTarget;
        [SerializeField] private Vector2 lastKnownTargetPosition;
        [SerializeField] private float lastSeenTime = -999f;

        [Header("Brain State (Read-Only At Runtime)")]
        [SerializeField] private BrainState currentState = BrainState.Patrol;

        public Transform CurrentTarget => currentTarget;
        public bool HasTarget => currentTarget != null;
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
            currentTarget = target;
            canSeeTarget = visible;
            distanceToTarget = Mathf.Max(0f, distance);
            lastKnownTargetPosition = knownPosition;

            if (visible)
            {
                lastSeenTime = Time.time;
            }
        }

        /// <summary>
        /// Clears current target but keeps last-known position and timestamp.
        /// </summary>
        public void ClearTarget()
        {
            currentTarget = null;
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
