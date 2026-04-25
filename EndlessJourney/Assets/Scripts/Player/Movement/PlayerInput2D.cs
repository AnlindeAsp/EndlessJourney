using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessJourney.Player
{
    /// <summary>
    /// Small input reader for platformer controls.
    /// It keeps input collection separate from movement/physics logic.
    /// 
    /// Why this exists:
    /// - Movement scripts read clean, frame-stable values from here.
    /// - Input mappings can change without touching physics code.
    /// - We can support both Input System and Legacy Input Manager.
    /// </summary>
    public class PlayerInput2D : MonoBehaviour
    {
        /// <summary>
        /// Horizontal movement intent in range [-1, 1].
        /// -1 = left, +1 = right, 0 = no horizontal intent.
        /// </summary>
        public float MoveX { get; private set; }

        /// <summary>
        /// True only on the frame jump was pressed.
        /// Used for buffered jump windows and single-trigger jump actions.
        /// </summary>
        public bool JumpPressedThisFrame { get; private set; }

        /// <summary>
        /// True while jump button is continuously held.
        /// Used for variable jump height (low-jump when released early).
        /// </summary>
        public bool JumpHeld { get; private set; }

        /// <summary>
        /// True only on the frame dash was pressed.
        /// Prevents repeated dash starts while the button is held down.
        /// </summary>
        public bool DashPressedThisFrame { get; private set; }

        /// <summary>
        /// True only on the frame melee attack was pressed.
        /// Default binding in the new Input System is mouse left button.
        /// </summary>
        public bool AttackPressedThisFrame { get; private set; }

        /// <summary>
        /// Vertical attack intent in range [-1, 1].
        /// +1 = up intent, -1 = down intent.
        /// This is separated from jump so combat systems can resolve directional attacks.
        /// </summary>
        public float VerticalIntent { get; private set; }

        /// <summary>
        /// True only on the frame cast key was pressed.
        /// Used by spell systems for single-trigger casting actions.
        /// </summary>
        public bool CastPressedThisFrame { get; private set; }

#if ENABLE_LEGACY_INPUT_MANAGER
        [Header("Legacy Input Fallback")]
        [Tooltip("Used only when the new Input System has no active keyboard/gamepad/mouse.")]
        // Legacy axis/button names from Project Settings > Input Manager.
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private string jumpButton = "Jump";
        [SerializeField] private string dashButton = "Fire3";
        [SerializeField] private string attackButton = "Fire1";
        [SerializeField] private string castButton = "Fire2";
#endif

        private void Update()
        {
            // Input is read in Update (not FixedUpdate) so press timing is not lost
            // between physics ticks.
            bool didReadInput = false;

#if ENABLE_INPUT_SYSTEM
            didReadInput = ReadWithNewInputSystem();
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!didReadInput)
            {
                ReadWithLegacyInputManager();
                didReadInput = true;
            }
#endif

            if (!didReadInput)
            {
                // Explicitly reset all values when no input backend is available.
                MoveX = 0f;
                VerticalIntent = 0f;
                JumpPressedThisFrame = false;
                JumpHeld = false;
                DashPressedThisFrame = false;
                AttackPressedThisFrame = false;
                CastPressedThisFrame = false;
            }
        }

#if ENABLE_INPUT_SYSTEM
        private bool ReadWithNewInputSystem()
        {
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            Mouse mouse = Mouse.current;

            if (keyboard == null && gamepad == null && mouse == null)
            {
                // Returning false allows fallback to legacy input (if enabled).
                return false;
            }

            float move = 0f;

            if (keyboard != null)
            {
                // Keyboard is treated as digital input, accumulating left/right intent.
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move += 1f;
            }

            if (gamepad != null)
            {
                // Left stick adds analog contribution. Final value is clamped to [-1, 1].
                move += gamepad.leftStick.ReadValue().x;
            }

            MoveX = Mathf.Clamp(move, -1f, 1f);

            float verticalIntent = 0f;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) verticalIntent += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) verticalIntent -= 1f;
            }

            if (gamepad != null)
            {
                verticalIntent += gamepad.leftStick.ReadValue().y;
            }

            VerticalIntent = Mathf.Clamp(verticalIntent, -1f, 1f);

            // "PressedThisFrame" is an edge trigger for actions that should happen once.
            bool keyboardJumpPressed = keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
            bool gamepadJumpPressed = gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;

            // "Held" is used for continuous state checks (example: variable jump height).
            bool keyboardJumpHeld = keyboard != null && keyboard.spaceKey.isPressed;
            bool gamepadJumpHeld = gamepad != null && gamepad.buttonSouth.isPressed;

            // Dash uses edge trigger so one press = one dash start attempt.
            bool mouseDashPressed = mouse != null && mouse.rightButton.wasPressedThisFrame;
            bool gamepadDashPressed = gamepad != null && gamepad.buttonEast.wasPressedThisFrame;

            // Melee attack uses left mouse button by default, with gamepad fallback.
            bool mouseAttackPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
            bool gamepadAttackPressed = gamepad != null && gamepad.buttonWest.wasPressedThisFrame;

            // Spell cast key: C on keyboard, right shoulder on gamepad by default.
            bool keyboardCastPressed = keyboard != null && keyboard.cKey.wasPressedThisFrame;
            bool gamepadCastPressed = gamepad != null && gamepad.rightShoulder.wasPressedThisFrame;

            JumpPressedThisFrame = keyboardJumpPressed || gamepadJumpPressed;
            JumpHeld = keyboardJumpHeld || gamepadJumpHeld;
            DashPressedThisFrame = mouseDashPressed || gamepadDashPressed;
            AttackPressedThisFrame = mouseAttackPressed || gamepadAttackPressed;
            CastPressedThisFrame = keyboardCastPressed || gamepadCastPressed;

            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        private void ReadWithLegacyInputManager()
        {
            // Legacy API mirrors the same semantic outputs:
            // - axis for MoveX
            // - axis for VerticalIntent
            // - button down for one-frame triggers
            // - button for held state
            MoveX = Input.GetAxisRaw(horizontalAxis);
            VerticalIntent = Input.GetAxisRaw(verticalAxis);
            JumpPressedThisFrame = Input.GetButtonDown(jumpButton);
            JumpHeld = Input.GetButton(jumpButton);
            DashPressedThisFrame = Input.GetButtonDown(dashButton);
            AttackPressedThisFrame = Input.GetButtonDown(attackButton);
            CastPressedThisFrame = Input.GetButtonDown(castButton);
        }
#endif
    }
}
