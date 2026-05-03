using EndlessJourney.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessJourney.UI
{
    /// <summary>
    /// In-game pause menu controller.
    /// Handles pause toggle input and menu visibility.
    /// </summary>
    public class PauseMenuController2D : MonoBehaviour
    {
        [Header("References (Assign Manually)")]
        [SerializeField] private GameObject pauseMenuRoot;
        [SerializeField] private GameObject pauseMainPanel;
        [SerializeField] private GameObject settingsPanel;
        [Tooltip("Optional: lock these player cores while paused.")]
        [SerializeField] private PlayerCore2D[] playerCoresToLock;

        [Header("Pause Control")]
        [SerializeField] private bool toggleByEscape = true;
        [SerializeField] private bool startPaused = false;
        [SerializeField] private bool pauseAudioListener = false;

        [Header("Cursor")]
        [SerializeField] private bool showCursorWhenPaused = true;
        [SerializeField] private bool lockCursorWhenResumed = true;

        private bool _isPaused;
        private float _timeScaleBeforePause = 1f;

        public bool IsPaused => _isPaused;

        public void SetEscapeToggleEnabled(bool enabled)
        {
            toggleByEscape = enabled;
        }

        private void Awake()
        {
            if (pauseMenuRoot != null)
            {
                pauseMenuRoot.SetActive(false);
            }

            if (pauseMainPanel != null)
            {
                pauseMainPanel.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            if (startPaused)
            {
                Pause();
            }
        }

        private void Update()
        {
            if (!toggleByEscape)
            {
                return;
            }

            if (WasPausePressedThisFrame())
            {
                TogglePause();
            }
        }

        private void OnDisable()
        {
            // Safety: prevent getting stuck paused when object is disabled.
            if (_isPaused)
            {
                Resume();
            }
        }

        public void TogglePause()
        {
            if (_isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            if (_isPaused)
            {
                return;
            }

            _isPaused = true;
            _timeScaleBeforePause = Mathf.Max(0.0001f, Time.timeScale);
            Time.timeScale = 0f;

            if (pauseAudioListener)
            {
                AudioListener.pause = true;
            }

            SetPlayerMovementLock(true);
            SetMenuVisible(true);
            ShowPauseMainPanel();
            ApplyPausedCursorState();
        }

        public void Resume()
        {
            if (!_isPaused)
            {
                return;
            }

            _isPaused = false;
            Time.timeScale = _timeScaleBeforePause;
            AudioListener.pause = false;

            SetPlayerMovementLock(false);
            SetMenuVisible(false);
            ApplyResumedCursorState();
        }

        /// <summary>
        /// Convenience method for UI Button OnClick bindings.
        /// </summary>
        public void ResumeFromUIButton()
        {
            Resume();
        }

        public void OpenSettingsPanel()
        {
            if (pauseMainPanel != null)
            {
                pauseMainPanel.SetActive(false);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        public void BackToPauseMainPanel()
        {
            ShowPauseMainPanel();
        }

        private void SetMenuVisible(bool visible)
        {
            if (pauseMenuRoot != null)
            {
                pauseMenuRoot.SetActive(visible);
            }
        }

        private void ShowPauseMainPanel()
        {
            if (pauseMainPanel != null)
            {
                pauseMainPanel.SetActive(true);
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void SetPlayerMovementLock(bool locked)
        {
            if (playerCoresToLock == null)
            {
                return;
            }

            for (int i = 0; i < playerCoresToLock.Length; i++)
            {
                PlayerCore2D core = playerCoresToLock[i];
                if (core != null)
                {
                    core.SetMovementLocked(locked);
                }
            }
        }

        private void ApplyPausedCursorState()
        {
            if (!showCursorWhenPaused)
            {
                return;
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void ApplyResumedCursorState()
        {
            if (!lockCursorWhenResumed)
            {
                return;
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private bool WasPausePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            bool keyboardPressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            bool gamepadPressed = gamepad != null && gamepad.startButton.wasPressedThisFrame;
            if (keyboardPressed || gamepadPressed)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }
    }
}
