using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Shared player context for 2D abilities.
    /// Centralizes references and shared state so each ability script
    /// (movement, dash, spell, etc.) can stay focused and modular.
    /// </summary>
    [DefaultExecutionOrder(-300)]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput2D))]
    public class PlayerCore2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private PlayerInput2D inputReader;
        [SerializeField] private GroundCheck2D groundCheck;
        [SerializeField] private SpellCastSystem spellCastSystem;

        private float _defaultGravityScale;
        private bool _isGrounded;
        private bool _wasGrounded;
        private bool _justLanded;
        private int _facingDirection = 1;
        private bool _movementLocked;

        /// <summary>Cached Rigidbody2D used by movement abilities.</summary>
        public Rigidbody2D Body => body;

        /// <summary>Input provider used by all ability modules.</summary>
        public PlayerInput2D Input => inputReader;

        /// <summary>Ground checker used for grounded state updates.</summary>
        public GroundCheck2D GroundCheck => groundCheck;

        /// <summary>Baseline gravity scale captured at startup.</summary>
        public float DefaultGravityScale => _defaultGravityScale;

        /// <summary>Current grounded state for this frame.</summary>
        public bool IsGrounded => _isGrounded;

        /// <summary>Grounded state from previous frame.</summary>
        public bool WasGrounded => _wasGrounded;

        /// <summary>True only on frames when landing just happened.</summary>
        public bool JustLanded => _justLanded;

        /// <summary>Facing direction, -1 for left and +1 for right.</summary>
        public int FacingDirection => _facingDirection;

        /// <summary>When true, movement abilities should pause their own control.</summary>
        public bool IsMovementLocked => _movementLocked;

        /// <summary>Optional spell system reference for higher-level coordination.</summary>
        public SpellCastSystem SpellCast => spellCastSystem;

        /// <summary>
        /// Auto-wires references when component is first added.
        /// </summary>
        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            inputReader = GetComponent<PlayerInput2D>();
            groundCheck = GetComponentInChildren<GroundCheck2D>();
            spellCastSystem = GetComponent<SpellCastSystem>();
        }

        /// <summary>
        /// Validates core references and captures default gravity baseline.
        /// </summary>
        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (inputReader == null) inputReader = GetComponent<PlayerInput2D>();
            if (groundCheck == null) groundCheck = GetComponentInChildren<GroundCheck2D>();
            if (spellCastSystem == null) spellCastSystem = GetComponent<SpellCastSystem>();

            if (body == null || inputReader == null || groundCheck == null)
            {
                Debug.LogError("PlayerCore2D is missing references. Please assign Rigidbody2D, PlayerInput2D, and GroundCheck2D.");
                enabled = false;
                return;
            }

            _defaultGravityScale = body.gravityScale;
        }

        /// <summary>
        /// Refreshes shared state every frame.
        /// </summary>
        private void Update()
        {
            RefreshGroundedState();
            RefreshFacingDirection();
        }

        /// <summary>
        /// Updates grounded flags and computes one-frame landing transition.
        /// </summary>
        private void RefreshGroundedState()
        {
            groundCheck.Refresh();
            _isGrounded = groundCheck.IsGrounded;
            _justLanded = _isGrounded && !_wasGrounded;
            _wasGrounded = _isGrounded;
        }

        /// <summary>
        /// Updates facing direction from horizontal input intent.
        /// </summary>
        private void RefreshFacingDirection()
        {
            if (Mathf.Abs(inputReader.MoveX) > 0.01f)
            {
                _facingDirection = inputReader.MoveX > 0f ? 1 : -1;
            }
        }

        /// <summary>
        /// Lets an ability take temporary ownership of movement.
        /// </summary>
        /// <param name="locked">True to lock movement, false to release lock.</param>
        public void SetMovementLocked(bool locked)
        {
            _movementLocked = locked;
        }

        /// <summary>
        /// Applies gravity relative to baseline Rigidbody2D gravity.
        /// </summary>
        /// <param name="multiplier">Gravity multiplier against baseline gravity scale.</param>
        public void SetGravityMultiplier(float multiplier)
        {
            body.gravityScale = _defaultGravityScale * multiplier;
        }

        /// <summary>
        /// Restores baseline Rigidbody2D gravity.
        /// </summary>
        public void RestoreDefaultGravity()
        {
            body.gravityScale = _defaultGravityScale;
        }
    }
}
