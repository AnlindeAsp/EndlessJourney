using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Minimal enemy FSM brain.
    /// Current states:
    /// - Patrol: let patrol module drive movement
    /// - Chase: move toward target (or last known position)
    /// - Attack: stop and face target while contact attack handles damage
    /// - Return: go back to spawn X and resume patrol
    /// </summary>
    [RequireComponent(typeof(EnemyCore2D))]
    [RequireComponent(typeof(EnemyBlackboard2D))]
    public class EnemyBrainFSM2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyCore2D core;
        [SerializeField] private EnemyBlackboard2D blackboard;
        [SerializeField] private EnemyPatrolWalker2D patrolModule;
        [SerializeField] private EnemyContactAttack2D contactAttackModule;

        [Header("FSM Tuning")]
        [SerializeField, Min(0f)] private float chaseSpeed = 2.8f;
        [SerializeField, Min(0f)] private float returnSpeed = 2.2f;
        [SerializeField, Min(0f)] private float attackRange = 1.25f;
        [SerializeField, Min(0f)] private float loseSightMemoryDuration = 1.5f;
        [SerializeField, Min(0f)] private float returnArrivalDistance = 0.15f;
        [Tooltip("When enabled, contact damage stays active in all alive states (patrol/chase/attack/return).")]
        [SerializeField] private bool keepContactAttackAlwaysActive = true;

        [Header("Debug")]
        [SerializeField] private bool logStateChange = false;

        private void Reset()
        {
            core = GetComponent<EnemyCore2D>();
            blackboard = GetComponent<EnemyBlackboard2D>();
            patrolModule = GetComponent<EnemyPatrolWalker2D>();
            contactAttackModule = GetComponent<EnemyContactAttack2D>();
        }

        private void Awake()
        {
            if (core == null) core = GetComponent<EnemyCore2D>();
            if (blackboard == null) blackboard = GetComponent<EnemyBlackboard2D>();
            if (patrolModule == null) patrolModule = GetComponent<EnemyPatrolWalker2D>();
            if (contactAttackModule == null) contactAttackModule = GetComponent<EnemyContactAttack2D>();

            if (core == null || blackboard == null)
            {
                Debug.LogError("EnemyBrainFSM2D is missing required references.");
                enabled = false;
                return;
            }

            blackboard.SetState(EnemyBlackboard2D.BrainState.Patrol);
            ApplyStateModules(blackboard.CurrentState);
        }

        private void Update()
        {
            if (core != null && core.IsDead)
            {
                if (patrolModule != null) patrolModule.enabled = false;
                if (contactAttackModule != null) contactAttackModule.enabled = false;
                return;
            }

            EnemyBlackboard2D.BrainState next = DecideNextState();
            if (next != blackboard.CurrentState)
            {
                blackboard.SetState(next);
                ApplyStateModules(next);
                if (logStateChange)
                {
                    Debug.Log($"Enemy FSM -> {next}");
                }
            }
        }

        private void FixedUpdate()
        {
            if (core != null && core.IsDead)
            {
                core.StopMovement();
                return;
            }

            switch (blackboard.CurrentState)
            {
                case EnemyBlackboard2D.BrainState.Chase:
                    TickChase();
                    break;
                case EnemyBlackboard2D.BrainState.Attack:
                    TickAttack();
                    break;
                case EnemyBlackboard2D.BrainState.Return:
                    TickReturn();
                    break;
            }
        }

        private EnemyBlackboard2D.BrainState DecideNextState()
        {
            bool hasTarget = blackboard.HasTarget;
            bool canSee = blackboard.CanSeeTarget;
            bool hasMemory = blackboard.HasRecentSight(loseSightMemoryDuration);
            float distance = blackboard.DistanceToTarget;

            switch (blackboard.CurrentState)
            {
                case EnemyBlackboard2D.BrainState.Patrol:
                    if (canSee || hasMemory)
                    {
                        return EnemyBlackboard2D.BrainState.Chase;
                    }
                    return EnemyBlackboard2D.BrainState.Patrol;

                case EnemyBlackboard2D.BrainState.Chase:
                    if (hasTarget && distance <= attackRange)
                    {
                        return EnemyBlackboard2D.BrainState.Attack;
                    }
                    if (!canSee && !hasMemory)
                    {
                        return EnemyBlackboard2D.BrainState.Return;
                    }
                    return EnemyBlackboard2D.BrainState.Chase;

                case EnemyBlackboard2D.BrainState.Attack:
                    if (!hasTarget)
                    {
                        return EnemyBlackboard2D.BrainState.Return;
                    }
                    if (distance > attackRange * 1.15f)
                    {
                        return canSee || hasMemory
                            ? EnemyBlackboard2D.BrainState.Chase
                            : EnemyBlackboard2D.BrainState.Return;
                    }
                    return EnemyBlackboard2D.BrainState.Attack;

                case EnemyBlackboard2D.BrainState.Return:
                    if (canSee || hasMemory)
                    {
                        return EnemyBlackboard2D.BrainState.Chase;
                    }
                    float xDistance = Mathf.Abs(transform.position.x - core.SpawnPosition.x);
                    if (xDistance <= returnArrivalDistance)
                    {
                        return EnemyBlackboard2D.BrainState.Patrol;
                    }
                    return EnemyBlackboard2D.BrainState.Return;
            }

            return EnemyBlackboard2D.BrainState.Patrol;
        }

        private void ApplyStateModules(EnemyBlackboard2D.BrainState state)
        {
            bool usePatrol = state == EnemyBlackboard2D.BrainState.Patrol;
            if (patrolModule != null)
            {
                patrolModule.enabled = usePatrol;
            }

            bool canAttackByContact = keepContactAttackAlwaysActive ||
                                      state == EnemyBlackboard2D.BrainState.Chase ||
                                      state == EnemyBlackboard2D.BrainState.Attack;
            if (contactAttackModule != null)
            {
                contactAttackModule.enabled = canAttackByContact;
            }
        }

        private void TickChase()
        {
            Vector2 targetPos = blackboard.HasTarget
                ? (Vector2)blackboard.CurrentTarget.position
                : blackboard.LastKnownTargetPosition;

            int direction = targetPos.x >= transform.position.x ? 1 : -1;
            core.FaceDirection(direction);
            core.SetHorizontalVelocity(direction * chaseSpeed);
        }

        private void TickAttack()
        {
            if (blackboard.HasTarget)
            {
                core.FaceTowardX(blackboard.CurrentTarget.position.x);
            }

            core.StopMovement();
        }

        private void TickReturn()
        {
            float deltaX = core.SpawnPosition.x - transform.position.x;
            if (Mathf.Abs(deltaX) <= returnArrivalDistance)
            {
                core.StopMovement();
                return;
            }

            int direction = deltaX >= 0f ? 1 : -1;
            core.FaceDirection(direction);
            core.SetHorizontalVelocity(direction * returnSpeed);
        }
    }
}
