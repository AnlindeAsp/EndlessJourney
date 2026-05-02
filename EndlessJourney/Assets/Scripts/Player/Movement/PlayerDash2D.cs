using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Dash ability module.
    /// Reads shared state from PlayerCore2D and owns only dash-specific logic.
    /// </summary>
    [RequireComponent(typeof(PlayerCore2D))]
    public class PlayerDash2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCore2D core;

        [Header("Dash")]
        [SerializeField] private bool enableDash = true;
        [SerializeField, Min(0f)] private float dashSpeed = 18f;
        [SerializeField, Min(0f)] private float dashDuration = 0.14f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.25f;
        [SerializeField, Range(0f, 1f)] private float dashGravityMultiplier = 0f;
        [SerializeField, Range(0f, 1f)] private float postDashSpeedMultiplier = 0.5f;

        [Header("Air Dash Rule")]
        [Tooltip("If true, only one dash is allowed while airborne until landing.")]
        [SerializeField] private bool oneDashPerAirborne = true;

        [Header("Velocity Handling")]
        [Tooltip("If false, vertical velocity is set to 0 when dash starts.")]
        [SerializeField] private bool preserveVerticalVelocity = false;

        private bool _isDashing;
        private bool _airDashAvailable = true;
        private int _dashDirection = 1;
        private float _dashTimer;
        private float _cooldownTimer;

        public bool IsDashing => _isDashing;

        private void Reset()
        {
            core = GetComponent<PlayerCore2D>();
        }

        private void Awake()
        {
            if (core == null) core = GetComponent<PlayerCore2D>();

            if (core == null)
            {
                Debug.LogError("PlayerDash2D is missing PlayerCore2D reference.");
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (core == null)
            {
                return;
            }

            if (_isDashing)
            {
                _isDashing = false;
                core.SetMovementLocked(false);
                core.RestoreDefaultGravity();
            }
        }

        private void Update()
        {
            if (!enableDash)
            {
                return;
            }

            if (core.IsGrounded)
            {
                _airDashAvailable = true;
            }

            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.deltaTime;
            }

            if (_isDashing)
            {
                _dashTimer -= Time.deltaTime;
                if (_dashTimer <= 0f)
                {
                    EndDash();
                }

                return;
            }

            if (core.IsActionLocked)
            {
                return;
            }

            if (core.Input.DashPressedThisFrame)
            {
                TryStartDash();
            }
        }

        private void FixedUpdate()
        {
            if (!_isDashing)
            {
                return;
            }

            float y = preserveVerticalVelocity ? core.Body.linearVelocity.y : 0f;
            core.Body.linearVelocity = new Vector2(_dashDirection * dashSpeed, y);
        }

        private void TryStartDash()
        {
            if (_isDashing || _cooldownTimer > 0f)
            {
                return;
            }

            if (core.IsActionLocked || core.IsMovementLocked)
            {
                return;
            }

            if (oneDashPerAirborne && !core.IsGrounded && !_airDashAvailable)
            {
                return;
            }

            StartDash();
        }

        private void StartDash()
        {
            _isDashing = true;
            _dashTimer = dashDuration;
            _cooldownTimer = dashCooldown;
            _dashDirection = core.FacingDirection >= 0 ? 1 : -1;

            if (oneDashPerAirborne && !core.IsGrounded)
            {
                _airDashAvailable = false;
            }

            core.SetMovementLocked(true);
            core.SetGravityMultiplier(dashGravityMultiplier);

            float y = preserveVerticalVelocity ? core.Body.linearVelocity.y : 0f;
            core.Body.linearVelocity = new Vector2(_dashDirection * dashSpeed, y);
        }

        private void EndDash()
        {
            _isDashing = false;
            core.SetMovementLocked(false);
            core.RestoreDefaultGravity();

            core.Body.linearVelocity = new Vector2(
                _dashDirection * dashSpeed * postDashSpeedMultiplier,
                core.Body.linearVelocity.y
            );
        }

        /// <summary>
        /// Restores dash availability immediately.
        /// Used by mechanics such as successful down-attack hit rewards.
        /// </summary>
        public void ResetDashAvailability()
        {
            _airDashAvailable = true;
            _cooldownTimer = 0f;
        }
    }
}
