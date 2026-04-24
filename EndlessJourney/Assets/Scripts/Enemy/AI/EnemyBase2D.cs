using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Shared base class for enemy runtime behavior.
    /// Provides common references, facing direction, and death-aware gating.
    /// </summary>
    [RequireComponent(typeof(EnemyCore2D))]
    public abstract class EnemyBase2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected EnemyCore2D core;
        protected Rigidbody2D body => core != null ? core.Body : null;

        /// <summary>Current horizontal facing direction (-1 or +1).</summary>
        public int FacingDirection => core != null ? core.FacingDirection : 1;
        /// <summary>Read-only rigidbody access for enemy brains.</summary>
        public Rigidbody2D Body => core != null ? core.Body : null;

        /// <summary>True when this enemy is dead and should stop acting.</summary>
        protected bool IsDead => core != null && core.IsDead;

        protected virtual void Reset()
        {
            core = GetComponent<EnemyCore2D>();
        }

        protected virtual void Awake()
        {
            if (core == null) core = GetComponent<EnemyCore2D>();
            if (core == null)
            {
                Debug.LogError("EnemyBase2D is missing EnemyCore2D.");
                enabled = false;
                return;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (IsDead)
            {
                StopHorizontalMovement();
                return;
            }

            TickEnemyPhysics(Time.fixedDeltaTime);
        }

        /// <summary>
        /// Child classes implement movement/physics behavior here.
        /// </summary>
        protected abstract void TickEnemyPhysics(float deltaTime);

        /// <summary>
        /// Turns enemy to the opposite horizontal direction.
        /// </summary>
        protected void ReverseFacingDirection()
        {
            if (core == null)
            {
                return;
            }

            core.FaceDirection(-core.FacingDirection);
        }

        /// <summary>
        /// Sets facing direction and flips visual root.
        /// </summary>
        protected void SetFacingDirection(int direction)
        {
            if (core == null)
            {
                return;
            }

            core.FaceDirection(direction);
        }

        /// <summary>
        /// Stops only horizontal velocity.
        /// </summary>
        protected void StopHorizontalMovement()
        {
            core?.StopMovement();
        }

        /// <summary>
        /// Public facing helper for external brain scripts.
        /// </summary>
        public void FaceDirection(int direction)
        {
            core?.FaceDirection(direction);
        }

        /// <summary>
        /// Faces toward a target world X position.
        /// </summary>
        public void FaceTowardX(float targetWorldX)
        {
            core?.FaceTowardX(targetWorldX);
        }

        /// <summary>
        /// Sets horizontal velocity while preserving current vertical velocity.
        /// </summary>
        public void SetHorizontalVelocity(float velocityX)
        {
            core?.SetHorizontalVelocity(velocityX);
        }
    }
}
