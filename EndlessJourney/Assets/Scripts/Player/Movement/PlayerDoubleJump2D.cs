using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Double-jump ability module.
    /// Keeps extra-jump rules isolated from base movement logic.
    /// </summary>
    [RequireComponent(typeof(PlayerCore2D))]
    [RequireComponent(typeof(PlayerMovement2D))]
    public class PlayerDoubleJump2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCore2D core;
        [SerializeField] private PlayerMovement2D movement;

        [Header("Double Jump")]
        [SerializeField] private bool enableDoubleJump = true;
        [SerializeField, Min(0)] private int extraJumps = 1;

        private int _remainingExtraJumps;

        private void Reset()
        {
            core = GetComponent<PlayerCore2D>();
            movement = GetComponent<PlayerMovement2D>();
        }

        private void Awake()
        {
            if (core == null) core = GetComponent<PlayerCore2D>();
            if (movement == null) movement = GetComponent<PlayerMovement2D>();

            if (core == null || movement == null)
            {
                Debug.LogError("PlayerDoubleJump2D is missing references. Please assign PlayerCore2D and PlayerMovement2D.");
                enabled = false;
                return;
            }

            _remainingExtraJumps = extraJumps;
        }

        private void Update()
        {
            if (!enableDoubleJump)
            {
                return;
            }

            // Landing restores all extra jumps.
            if (core.JustLanded)
            {
                _remainingExtraJumps = extraJumps;
            }

            if (core.IsMovementLocked)
            {
                return;
            }

            if (!core.Input.JumpPressedThisFrame)
            {
                return;
            }

            // If base jump can still be used, let PlayerMovement2D handle this press.
            if (movement.CanUseGroundOrCoyoteJump)
            {
                return;
            }

            // If base movement already consumed a jump this frame, do not fire again.
            if (movement.DidJumpThisFrame)
            {
                return;
            }

            if (_remainingExtraJumps <= 0)
            {
                return;
            }

            _remainingExtraJumps--;
            movement.PerformAbilityJump();
        }
    }
}
