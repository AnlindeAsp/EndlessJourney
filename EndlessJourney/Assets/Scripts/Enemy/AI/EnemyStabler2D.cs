using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Stationary enemy behavior.
    /// Prevents passive physics push from player while still allowing
    /// hitstun/knockback motion when stunned.
    /// </summary>
    [RequireComponent(typeof(EnemyCore2D))]
    public class EnemyStabler2D : EnemyBase2D
    {
        [Header("Position Lock")]
        [SerializeField] private bool lockPositionX = true;
        [SerializeField] private bool lockRotationZ = true;
        [SerializeField] private bool keepLockedAtAnchor = true;
        [SerializeField] private bool anchorFollowsAfterStun = true;

        private Vector2 _anchorPosition;
        private RigidbodyConstraints2D _originalConstraints;
        private bool _hasCachedConstraints;
        private bool _wasStunned;

        protected override void Awake()
        {
            base.Awake();

            if (body == null)
            {
                return;
            }

            _anchorPosition = transform.position;
            _originalConstraints = body.constraints;
            _hasCachedConstraints = true;
            ApplyConstraints(lockPosition: true);
        }

        private void OnEnable()
        {
            if (body == null)
            {
                return;
            }

            _anchorPosition = transform.position;
            if (!_hasCachedConstraints)
            {
                _originalConstraints = body.constraints;
                _hasCachedConstraints = true;
            }

            _wasStunned = false;
            ApplyConstraints(lockPosition: true);
        }

        private void OnDisable()
        {
            if (body == null || !_hasCachedConstraints)
            {
                return;
            }

            body.constraints = _originalConstraints;
        }

        protected override void FixedUpdate()
        {
            if (core == null || body == null)
            {
                return;
            }

            if (IsDead)
            {
                ApplyConstraints(lockPosition: true);
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                return;
            }

            bool isStunnedNow = core.IsStunned;
            if (isStunnedNow)
            {
                ApplyConstraints(lockPosition: false);
                _wasStunned = true;
                return;
            }

            if (_wasStunned && anchorFollowsAfterStun)
            {
                _anchorPosition = body.position;
            }

            _wasStunned = false;
            TickEnemyPhysics(Time.fixedDeltaTime);
        }

        protected override void TickEnemyPhysics(float deltaTime)
        {
            if (body == null)
            {
                return;
            }

            ApplyConstraints(lockPosition: true);
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            if (keepLockedAtAnchor)
            {
                Vector2 target = body.position;

                if (lockPositionX)
                {
                    target.x = _anchorPosition.x;
                }

                body.MovePosition(target);
            }
        }

        private void ApplyConstraints(bool lockPosition)
        {
            RigidbodyConstraints2D constraints = _originalConstraints;

            if (lockPosition && lockPositionX)
            {
                constraints |= RigidbodyConstraints2D.FreezePositionX;
            }

            if (lockRotationZ)
            {
                constraints |= RigidbodyConstraints2D.FreezeRotation;
            }

            body.constraints = constraints;
        }
    }
}
