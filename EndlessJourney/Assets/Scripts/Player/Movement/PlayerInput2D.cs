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
        public enum MouseButtonBinding
        {
            Left = 0,
            Right = 1,
            Middle = 2
        }

        [Header("Input System Rebindable Keys")]
        [SerializeField] private Key moveLeftKey = Key.A;
        [SerializeField] private Key moveRightKey = Key.D;
        [SerializeField] private Key moveUpKey = Key.W;
        [SerializeField] private Key moveDownKey = Key.S;
        [SerializeField] private Key jumpKey = Key.Space;
        [SerializeField] private Key spellSlot1Key = Key.Digit1;
        [SerializeField] private Key spellSlot2Key = Key.Digit2;
        [SerializeField] private Key spellSlot3Key = Key.Digit3;
        [SerializeField] private Key spellSlot4Key = Key.Digit4;
        [SerializeField] private Key spellSlot5Key = Key.Digit5;
        [SerializeField] private MouseButtonBinding attackMouseButton = MouseButtonBinding.Left;
        [SerializeField] private MouseButtonBinding dashMouseButton = MouseButtonBinding.Right;
        [SerializeField] private bool loadSavedBindingsOnAwake = true;
        [SerializeField] private bool saveBindingsOnRuntimeRebind = true;

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

        public bool SpellSlot1PressedThisFrame { get; private set; }
        public bool SpellSlot2PressedThisFrame { get; private set; }
        public bool SpellSlot3PressedThisFrame { get; private set; }
        public bool SpellSlot4PressedThisFrame { get; private set; }
        public bool SpellSlot5PressedThisFrame { get; private set; }

        public Key MoveLeftKey => moveLeftKey;
        public Key MoveRightKey => moveRightKey;
        public Key MoveUpKey => moveUpKey;
        public Key MoveDownKey => moveDownKey;
        public Key JumpKey => jumpKey;
        public Key SpellSlot1Key => spellSlot1Key;
        public Key SpellSlot2Key => spellSlot2Key;
        public Key SpellSlot3Key => spellSlot3Key;
        public Key SpellSlot4Key => spellSlot4Key;
        public Key SpellSlot5Key => spellSlot5Key;
        public MouseButtonBinding AttackMouseButton => attackMouseButton;
        public MouseButtonBinding DashMouseButton => dashMouseButton;

        private const string PrefMoveLeft = "input.moveLeftKey";
        private const string PrefMoveRight = "input.moveRightKey";
        private const string PrefMoveUp = "input.moveUpKey";
        private const string PrefMoveDown = "input.moveDownKey";
        private const string PrefJump = "input.jumpKey";
        private const string PrefSpellSlot1 = "input.spellSlot1Key";
        private const string PrefSpellSlot2 = "input.spellSlot2Key";
        private const string PrefSpellSlot3 = "input.spellSlot3Key";
        private const string PrefSpellSlot4 = "input.spellSlot4Key";
        private const string PrefSpellSlot5 = "input.spellSlot5Key";
        private const string PrefAttackMouse = "input.attackMouseButton";
        private const string PrefDashMouse = "input.dashMouseButton";

        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            if (loadSavedBindingsOnAwake)
            {
                LoadBindingsFromPrefs();
            }
#endif
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        [Header("Legacy Input Fallback")]
        [Tooltip("Used only when the new Input System has no active keyboard/gamepad/mouse.")]
        // Legacy axis/button names from Project Settings > Input Manager.
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private string jumpButton = "Jump";
        [SerializeField] private string dashButton = "Fire3";
        [SerializeField] private string attackButton = "Fire1";
        [SerializeField] private KeyCode spellSlot1KeyLegacy = KeyCode.Alpha1;
        [SerializeField] private KeyCode spellSlot2KeyLegacy = KeyCode.Alpha2;
        [SerializeField] private KeyCode spellSlot3KeyLegacy = KeyCode.Alpha3;
        [SerializeField] private KeyCode spellSlot4KeyLegacy = KeyCode.Alpha4;
        [SerializeField] private KeyCode spellSlot5KeyLegacy = KeyCode.Alpha5;
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
                SpellSlot1PressedThisFrame = false;
                SpellSlot2PressedThisFrame = false;
                SpellSlot3PressedThisFrame = false;
                SpellSlot4PressedThisFrame = false;
                SpellSlot5PressedThisFrame = false;
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
                if (keyboard[moveLeftKey].isPressed || keyboard.leftArrowKey.isPressed) move -= 1f;
                if (keyboard[moveRightKey].isPressed || keyboard.rightArrowKey.isPressed) move += 1f;
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
                if (keyboard[moveUpKey].isPressed || keyboard.upArrowKey.isPressed) verticalIntent += 1f;
                if (keyboard[moveDownKey].isPressed || keyboard.downArrowKey.isPressed) verticalIntent -= 1f;
            }

            if (gamepad != null)
            {
                verticalIntent += gamepad.leftStick.ReadValue().y;
            }

            VerticalIntent = Mathf.Clamp(verticalIntent, -1f, 1f);

            // "PressedThisFrame" is an edge trigger for actions that should happen once.
            bool keyboardJumpPressed = keyboard != null && keyboard[jumpKey].wasPressedThisFrame;
            bool gamepadJumpPressed = gamepad != null && gamepad.buttonSouth.wasPressedThisFrame;

            // "Held" is used for continuous state checks (example: variable jump height).
            bool keyboardJumpHeld = keyboard != null && keyboard[jumpKey].isPressed;
            bool gamepadJumpHeld = gamepad != null && gamepad.buttonSouth.isPressed;

            // Dash uses edge trigger so one press = one dash start attempt.
            bool mouseDashPressed = IsMousePressedThisFrame(mouse, dashMouseButton);
            bool gamepadDashPressed = gamepad != null && gamepad.buttonEast.wasPressedThisFrame;

            // Melee attack uses left mouse button by default, with gamepad fallback.
            bool mouseAttackPressed = IsMousePressedThisFrame(mouse, attackMouseButton);
            bool gamepadAttackPressed = gamepad != null && gamepad.buttonWest.wasPressedThisFrame;

            bool keyboardSlot1Pressed = keyboard != null && keyboard[spellSlot1Key].wasPressedThisFrame;
            bool keyboardSlot2Pressed = keyboard != null && keyboard[spellSlot2Key].wasPressedThisFrame;
            bool keyboardSlot3Pressed = keyboard != null && keyboard[spellSlot3Key].wasPressedThisFrame;
            bool keyboardSlot4Pressed = keyboard != null && keyboard[spellSlot4Key].wasPressedThisFrame;
            bool keyboardSlot5Pressed = keyboard != null && keyboard[spellSlot5Key].wasPressedThisFrame;

            JumpPressedThisFrame = keyboardJumpPressed || gamepadJumpPressed;
            JumpHeld = keyboardJumpHeld || gamepadJumpHeld;
            DashPressedThisFrame = mouseDashPressed || gamepadDashPressed;
            AttackPressedThisFrame = mouseAttackPressed || gamepadAttackPressed;
            SpellSlot1PressedThisFrame = keyboardSlot1Pressed;
            SpellSlot2PressedThisFrame = keyboardSlot2Pressed;
            SpellSlot3PressedThisFrame = keyboardSlot3Pressed;
            SpellSlot4PressedThisFrame = keyboardSlot4Pressed;
            SpellSlot5PressedThisFrame = keyboardSlot5Pressed;

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
            SpellSlot1PressedThisFrame = Input.GetKeyDown(spellSlot1KeyLegacy);
            SpellSlot2PressedThisFrame = Input.GetKeyDown(spellSlot2KeyLegacy);
            SpellSlot3PressedThisFrame = Input.GetKeyDown(spellSlot3KeyLegacy);
            SpellSlot4PressedThisFrame = Input.GetKeyDown(spellSlot4KeyLegacy);
            SpellSlot5PressedThisFrame = Input.GetKeyDown(spellSlot5KeyLegacy);
        }
#endif

#if ENABLE_INPUT_SYSTEM
        private static bool IsMousePressedThisFrame(Mouse mouse, MouseButtonBinding button)
        {
            if (mouse == null)
            {
                return false;
            }

            switch (button)
            {
                case MouseButtonBinding.Left:
                    return mouse.leftButton.wasPressedThisFrame;
                case MouseButtonBinding.Right:
                    return mouse.rightButton.wasPressedThisFrame;
                case MouseButtonBinding.Middle:
                    return mouse.middleButton.wasPressedThisFrame;
                default:
                    return false;
            }
        }

        public bool SetKeyboardBinding(string actionId, Key key)
        {
            switch (actionId)
            {
                case "move_left":
                    moveLeftKey = key;
                    SaveBindingsIfNeeded();
                    return true;
                case "move_right":
                    moveRightKey = key;
                    SaveBindingsIfNeeded();
                    return true;
                case "move_up":
                    moveUpKey = key;
                    SaveBindingsIfNeeded();
                    return true;
                case "move_down":
                    moveDownKey = key;
                    SaveBindingsIfNeeded();
                    return true;
                case "jump":
                    jumpKey = key;
                    SaveBindingsIfNeeded();
                    return true;
                case "spell_slot_1":
                    spellSlot1Key = key;
                    SaveBindingsIfNeeded();
                    return true;
                case "spell_slot_2":
                    spellSlot2Key = key;
                    SaveBindingsIfNeeded();
                    return true;
                case "spell_slot_3":
                    spellSlot3Key = key;
                    SaveBindingsIfNeeded();
                    return true;
                case "spell_slot_4":
                    spellSlot4Key = key;
                    SaveBindingsIfNeeded();
                    return true;
                case "spell_slot_5":
                    spellSlot5Key = key;
                    SaveBindingsIfNeeded();
                    return true;
                default:
                    return false;
            }
        }

        public bool SetMouseBinding(string actionId, MouseButtonBinding button)
        {
            switch (actionId)
            {
                case "attack":
                    attackMouseButton = button;
                    SaveBindingsIfNeeded();
                    return true;
                case "dash":
                    dashMouseButton = button;
                    SaveBindingsIfNeeded();
                    return true;
                default:
                    return false;
            }
        }

        public void SaveBindingsToPrefs()
        {
            PlayerPrefs.SetInt(PrefMoveLeft, (int)moveLeftKey);
            PlayerPrefs.SetInt(PrefMoveRight, (int)moveRightKey);
            PlayerPrefs.SetInt(PrefMoveUp, (int)moveUpKey);
            PlayerPrefs.SetInt(PrefMoveDown, (int)moveDownKey);
            PlayerPrefs.SetInt(PrefJump, (int)jumpKey);
            PlayerPrefs.SetInt(PrefSpellSlot1, (int)spellSlot1Key);
            PlayerPrefs.SetInt(PrefSpellSlot2, (int)spellSlot2Key);
            PlayerPrefs.SetInt(PrefSpellSlot3, (int)spellSlot3Key);
            PlayerPrefs.SetInt(PrefSpellSlot4, (int)spellSlot4Key);
            PlayerPrefs.SetInt(PrefSpellSlot5, (int)spellSlot5Key);
            PlayerPrefs.SetInt(PrefAttackMouse, (int)attackMouseButton);
            PlayerPrefs.SetInt(PrefDashMouse, (int)dashMouseButton);
            PlayerPrefs.Save();
        }

        public void LoadBindingsFromPrefs()
        {
            if (PlayerPrefs.HasKey(PrefMoveLeft)) moveLeftKey = (Key)PlayerPrefs.GetInt(PrefMoveLeft);
            if (PlayerPrefs.HasKey(PrefMoveRight)) moveRightKey = (Key)PlayerPrefs.GetInt(PrefMoveRight);
            if (PlayerPrefs.HasKey(PrefMoveUp)) moveUpKey = (Key)PlayerPrefs.GetInt(PrefMoveUp);
            if (PlayerPrefs.HasKey(PrefMoveDown)) moveDownKey = (Key)PlayerPrefs.GetInt(PrefMoveDown);
            if (PlayerPrefs.HasKey(PrefJump)) jumpKey = (Key)PlayerPrefs.GetInt(PrefJump);
            if (PlayerPrefs.HasKey(PrefSpellSlot1)) spellSlot1Key = (Key)PlayerPrefs.GetInt(PrefSpellSlot1);
            if (PlayerPrefs.HasKey(PrefSpellSlot2)) spellSlot2Key = (Key)PlayerPrefs.GetInt(PrefSpellSlot2);
            if (PlayerPrefs.HasKey(PrefSpellSlot3)) spellSlot3Key = (Key)PlayerPrefs.GetInt(PrefSpellSlot3);
            if (PlayerPrefs.HasKey(PrefSpellSlot4)) spellSlot4Key = (Key)PlayerPrefs.GetInt(PrefSpellSlot4);
            if (PlayerPrefs.HasKey(PrefSpellSlot5)) spellSlot5Key = (Key)PlayerPrefs.GetInt(PrefSpellSlot5);
            if (PlayerPrefs.HasKey(PrefAttackMouse)) attackMouseButton = (MouseButtonBinding)PlayerPrefs.GetInt(PrefAttackMouse);
            if (PlayerPrefs.HasKey(PrefDashMouse)) dashMouseButton = (MouseButtonBinding)PlayerPrefs.GetInt(PrefDashMouse);
        }

        public void ResetBindingsToDefault()
        {
            moveLeftKey = Key.A;
            moveRightKey = Key.D;
            moveUpKey = Key.W;
            moveDownKey = Key.S;
            jumpKey = Key.Space;
            spellSlot1Key = Key.Digit1;
            spellSlot2Key = Key.Digit2;
            spellSlot3Key = Key.Digit3;
            spellSlot4Key = Key.Digit4;
            spellSlot5Key = Key.Digit5;
            attackMouseButton = MouseButtonBinding.Left;
            dashMouseButton = MouseButtonBinding.Right;
            SaveBindingsIfNeeded();
        }

        private void SaveBindingsIfNeeded()
        {
            if (!saveBindingsOnRuntimeRebind)
            {
                return;
            }

            SaveBindingsToPrefs();
        }
#endif

        public bool WasSpellSlotPressedThisFrame(int slotIndex)
        {
            switch (slotIndex)
            {
                case 0:
                    return SpellSlot1PressedThisFrame;
                case 1:
                    return SpellSlot2PressedThisFrame;
                case 2:
                    return SpellSlot3PressedThisFrame;
                case 3:
                    return SpellSlot4PressedThisFrame;
                case 4:
                    return SpellSlot5PressedThisFrame;
                default:
                    return false;
            }
        }
    }
}
