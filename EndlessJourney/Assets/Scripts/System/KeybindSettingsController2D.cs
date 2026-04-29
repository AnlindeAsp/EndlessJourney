using EndlessJourney.Player;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessJourney.UI
{
    /// <summary>
    /// Basic keybind settings controller for PlayerInput2D.
    /// Supports runtime rebind, save/load, and reset to defaults.
    /// </summary>
    public class KeybindSettingsController2D : MonoBehaviour
    {
        [System.Serializable]
        private struct BindingLabelEntry
        {
            public string actionId;
            public TMP_Text label;
        }

        public const string ActionMoveLeft = "move_left";
        public const string ActionMoveRight = "move_right";
        public const string ActionMoveUp = "move_up";
        public const string ActionMoveDown = "move_down";
        public const string ActionJump = "jump";
        public const string ActionCast = "cast";
        public const string ActionAttack = "attack";
        public const string ActionDash = "dash";

        [Header("References (Assign Manually)")]
        [SerializeField] private PlayerInput2D playerInput;
        [SerializeField] private TMP_Text rebindStatusLabel;
        [SerializeField] private BindingLabelEntry[] bindingLabels;

        [Header("Behavior")]
        [SerializeField] private bool saveImmediatelyAfterRebind = true;

        private string _pendingActionId;

        public bool IsWaitingForRebind => !string.IsNullOrEmpty(_pendingActionId);

        private void Awake()
        {
            if (playerInput == null)
            {
                Debug.LogError("KeybindSettingsController2D missing PlayerInput2D reference.", this);
                enabled = false;
                return;
            }

            LoadBindings();
        }

        private void OnEnable()
        {
            // Ensure labels always reflect current runtime/persisted bindings
            // whenever the settings panel is opened.
            LoadBindings();
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (IsWaitingForRebind)
            {
                TryCaptureRebindInput();
            }
#endif
        }

        public void BeginRebind(string actionId)
        {
            if (playerInput == null || string.IsNullOrWhiteSpace(actionId))
            {
                return;
            }

            _pendingActionId = actionId;
            if (rebindStatusLabel != null)
            {
                rebindStatusLabel.text = $"Press a key for {GetDisplayName(actionId)}...";
            }
        }

        public void CancelRebind()
        {
            _pendingActionId = null;
            SetStatusIdle();
        }

        public void SaveBindings()
        {
#if ENABLE_INPUT_SYSTEM
            playerInput.SaveBindingsToPrefs();
#endif
            RefreshAllBindingLabels();
        }

        public void LoadBindings()
        {
#if ENABLE_INPUT_SYSTEM
            playerInput.LoadBindingsFromPrefs();
#endif
            RefreshAllBindingLabels();
            SetStatusIdle();
        }

        public void ResetBindingsToDefault()
        {
#if ENABLE_INPUT_SYSTEM
            playerInput.ResetBindingsToDefault();
            playerInput.SaveBindingsToPrefs();
#endif
            RefreshAllBindingLabels();
            SetStatusIdle();
        }

        public void BeginRebindMoveLeft() => BeginRebind(ActionMoveLeft);
        public void BeginRebindMoveRight() => BeginRebind(ActionMoveRight);
        public void BeginRebindMoveUp() => BeginRebind(ActionMoveUp);
        public void BeginRebindMoveDown() => BeginRebind(ActionMoveDown);
        public void BeginRebindJump() => BeginRebind(ActionJump);
        public void BeginRebindCast() => BeginRebind(ActionCast);
        public void BeginRebindAttack() => BeginRebind(ActionAttack);
        public void BeginRebindDash() => BeginRebind(ActionDash);

#if ENABLE_INPUT_SYSTEM
        private void TryCaptureRebindInput()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelRebind();
                return;
            }

            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    TryApplyMouseBinding(PlayerInput2D.MouseButtonBinding.Left);
                    return;
                }

                if (mouse.rightButton.wasPressedThisFrame)
                {
                    TryApplyMouseBinding(PlayerInput2D.MouseButtonBinding.Right);
                    return;
                }

                if (mouse.middleButton.wasPressedThisFrame)
                {
                    TryApplyMouseBinding(PlayerInput2D.MouseButtonBinding.Middle);
                    return;
                }
            }

            if (keyboard == null)
            {
                return;
            }

            for (int i = 0; i < keyboard.allKeys.Count; i++)
            {
                var keyControl = keyboard.allKeys[i];
                if (!keyControl.wasPressedThisFrame)
                {
                    continue;
                }

                if (playerInput.SetKeyboardBinding(_pendingActionId, keyControl.keyCode))
                {
                    OnRebindApplied();
                    return;
                }
            }
        }

        private void TryApplyMouseBinding(PlayerInput2D.MouseButtonBinding mouseButton)
        {
            if (_pendingActionId != ActionAttack && _pendingActionId != ActionDash)
            {
                return;
            }

            if (playerInput.SetMouseBinding(_pendingActionId, mouseButton))
            {
                OnRebindApplied();
            }
        }

        private void OnRebindApplied()
        {
            if (saveImmediatelyAfterRebind)
            {
                playerInput.SaveBindingsToPrefs();
            }

            string actionId = _pendingActionId;
            _pendingActionId = null;

            RefreshBindingLabel(actionId);
            SetStatusIdle();
        }
#endif

        private void SetStatusIdle()
        {
            if (rebindStatusLabel != null)
            {
                rebindStatusLabel.text = "Ready";
            }
        }

        private void RefreshAllBindingLabels()
        {
            if (bindingLabels == null)
            {
                return;
            }

            for (int i = 0; i < bindingLabels.Length; i++)
            {
                RefreshBindingLabel(bindingLabels[i].actionId);
            }
        }

        private void RefreshBindingLabel(string actionId)
        {
            if (bindingLabels == null)
            {
                return;
            }

            for (int i = 0; i < bindingLabels.Length; i++)
            {
                if (bindingLabels[i].label == null || bindingLabels[i].actionId != actionId)
                {
                    continue;
                }

                bindingLabels[i].label.text = GetBindingValueDisplay(actionId);
                return;
            }
        }

        private string GetBindingValueDisplay(string actionId)
        {
            if (playerInput == null)
            {
                return "-";
            }

            switch (actionId)
            {
                case ActionMoveLeft:
                    return playerInput.MoveLeftKey.ToString();
                case ActionMoveRight:
                    return playerInput.MoveRightKey.ToString();
                case ActionMoveUp:
                    return playerInput.MoveUpKey.ToString();
                case ActionMoveDown:
                    return playerInput.MoveDownKey.ToString();
                case ActionJump:
                    return playerInput.JumpKey.ToString();
                case ActionCast:
                    return playerInput.CastKey.ToString();
                case ActionAttack:
                    return playerInput.AttackMouseButton.ToString();
                case ActionDash:
                    return playerInput.DashMouseButton.ToString();
                default:
                    return "-";
            }
        }

        private static string GetDisplayName(string actionId)
        {
            switch (actionId)
            {
                case ActionMoveLeft:
                    return "Move Left";
                case ActionMoveRight:
                    return "Move Right";
                case ActionMoveUp:
                    return "Move Up";
                case ActionMoveDown:
                    return "Move Down";
                case ActionJump:
                    return "Jump";
                case ActionCast:
                    return "Cast";
                case ActionAttack:
                    return "Attack";
                case ActionDash:
                    return "Dash";
                default:
                    return actionId;
            }
        }
    }
}
