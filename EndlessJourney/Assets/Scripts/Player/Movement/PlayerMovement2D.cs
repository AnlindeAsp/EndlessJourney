using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Base 2D movement ability.
    /// Handles run + jump stack and reads shared state from PlayerCore2D.
    /// </summary>
    [RequireComponent(typeof(PlayerCore2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCore2D core;

        [Header("Horizontal Movement")]
        [SerializeField, Min(0f)] private float maxRunSpeed = 8f;
        [SerializeField, Min(0f)] private float groundAcceleration = 70f;
        [SerializeField, Min(0f)] private float groundDeceleration = 80f;
        [SerializeField, Min(0f)] private float airAcceleration = 45f;
        [SerializeField, Min(0f)] private float airDeceleration = 45f;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpVelocity = 14f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        [Header("Jump Feel")]
        [SerializeField, Min(1f)] private float fallMultiplier = 1.8f;
        [SerializeField, Min(1f)] private float lowJumpMultiplier = 2.2f;
        [SerializeField] private bool enableApexHang = true;
        [SerializeField, Min(0f)] private float apexHangVelocityThreshold = 1.25f;
        [SerializeField, Min(0f)] private float apexHangTime = 0.08f;
        [SerializeField, Range(0.05f, 1f)] private float apexHangGravityMultiplier = 0.45f;
        [SerializeField, Min(0.1f)] private float maxFallSpeed = 18f;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private float _apexHangTimer;
        private bool _didJumpThisFrame;

        /// <summary>
        /// True on frames where this movement module already fired a jump.
        /// Other abilities (double jump, etc.) can use it to avoid double-triggering.
        /// </summary>
        public bool DidJumpThisFrame => _didJumpThisFrame;

        /// <summary>
        /// Whether base jump (ground/coyote) is currently available.
        /// </summary>
        public bool CanUseGroundOrCoyoteJump => core.IsGrounded || _coyoteTimer > 0f;

        private void Reset()
        {
            core = GetComponent<PlayerCore2D>();
        }

        private void Awake()
        {
            if (core == null) core = GetComponent<PlayerCore2D>();

            if (core == null)
            {
                Debug.LogError("PlayerMovement2D is missing PlayerCore2D reference.");
                enabled = false;
            }
        }

        private void Update()
        {
            _didJumpThisFrame = false;
            UpdateTimers();

            if (core.JustLanded)
            {
                OnLanded();
            }

            TryHandleJump();
        }

        private void FixedUpdate()
        {
            if (core.IsMovementLocked)
            {
                return;
            }

            ApplyHorizontalMovement();
            ApplyJumpGravityScale();
            ClampFallingSpeed();
        }

        private void UpdateTimers()
        {
            float dt = Time.deltaTime;

            // Coyote timer: small grace time after stepping off a platform.
            _coyoteTimer = core.IsGrounded ? coyoteTime : _coyoteTimer - dt;

            // While locked, drop buffered jump input to avoid surprise jump after dash.
            if (core.IsMovementLocked)
            {
                _jumpBufferTimer = 0f;
                return;
            }

            // Jump buffer: remember jump press briefly for forgiving timing.
            _jumpBufferTimer = core.Input.JumpPressedThisFrame ? jumpBufferTime : _jumpBufferTimer - dt;
        }

        private void TryHandleJump()
        {
            if (core.IsMovementLocked)
            {
                return;
            }

            if (_jumpBufferTimer <= 0f)
            {
                return;
            }

            bool canGroundJump = core.IsGrounded || _coyoteTimer > 0f;
            if (canGroundJump)
            {
                PerformJump();
            }
        }

        private void PerformJump()
        {
            _didJumpThisFrame = true;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _apexHangTimer = 0f;

            Vector2 velocity = core.Body.linearVelocity;
            velocity.y = jumpVelocity;
            core.Body.linearVelocity = velocity;
        }

        private void ApplyHorizontalMovement()
        {
            float targetSpeed = core.Input.MoveX * maxRunSpeed;
            bool hasMovementInput = Mathf.Abs(targetSpeed) > 0.01f;

            float accel = hasMovementInput
                ? (core.IsGrounded ? groundAcceleration : airAcceleration)
                : (core.IsGrounded ? groundDeceleration : airDeceleration);

            Vector2 velocity = core.Body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accel * Time.fixedDeltaTime);
            core.Body.linearVelocity = velocity;
        }

        private void ApplyJumpGravityScale()
        {
            float verticalVelocity = core.Body.linearVelocity.y;

            if (core.IsGrounded && verticalVelocity <= 0.01f)
            {
                core.RestoreDefaultGravity();
                _apexHangTimer = 0f;
                return;
            }

            float gravityMultiplier = 1f;

            if (verticalVelocity < -0.01f)
            {
                gravityMultiplier = fallMultiplier;
                _apexHangTimer = 0f;
            }
            else if (verticalVelocity > 0.01f)
            {
                if (!core.Input.JumpHeld)
                {
                    gravityMultiplier = lowJumpMultiplier;
                    _apexHangTimer = 0f;
                }
                else if (enableApexHang &&
                         Mathf.Abs(verticalVelocity) <= apexHangVelocityThreshold &&
                         _apexHangTimer < apexHangTime)
                {
                    gravityMultiplier = apexHangGravityMultiplier;
                    _apexHangTimer += Time.fixedDeltaTime;
                }
                else
                {
                    _apexHangTimer = 0f;
                }
            }

            core.SetGravityMultiplier(gravityMultiplier);
        }

        private void ClampFallingSpeed()
        {
            Vector2 velocity = core.Body.linearVelocity;
            if (velocity.y >= -maxFallSpeed)
            {
                return;
            }

            velocity.y = -maxFallSpeed;
            core.Body.linearVelocity = velocity;
        }

        private void OnLanded()
        {
            _apexHangTimer = 0f;
        }

        /// <summary>
        /// Lets another ability (example: double jump) trigger a jump impulse
        /// while reusing the same jump velocity and apex logic.
        /// </summary>
        public void PerformAbilityJump()
        {
            if (core.IsMovementLocked)
            {
                return;
            }

            _didJumpThisFrame = true;
            _jumpBufferTimer = 0f;
            _coyoteTimer = 0f;
            _apexHangTimer = 0f;

            Vector2 velocity = core.Body.linearVelocity;
            velocity.y = jumpVelocity;
            core.Body.linearVelocity = velocity;
        }
    }
}
