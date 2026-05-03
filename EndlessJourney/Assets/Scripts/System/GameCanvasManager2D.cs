using System;
using EndlessJourney.Player;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessJourney.UI
{
    public enum GameCanvasState2D
    {
        Gameplay,
        PauseMenu,
        SavingLibrary,
        Inventory,
        Map,
        Settings
    }

    /// <summary>
    /// Routes global canvas input and owns the current high-level UI state.
    /// World interaction selection stays in PlayerInteractor2D.
    /// </summary>
    public class GameCanvasManager2D : MonoBehaviour
    {
        [Header("References (Assign Manually)")]
        [SerializeField] private PauseMenuController2D pauseMenuController;
        [SerializeField] private GameObject savingLibraryRoot;
        [Tooltip("Optional fallback cores to lock for non-pause gameplay canvases.")]
        [SerializeField] private PlayerCore2D[] playerCoresToLock;

        [Header("Input Routing")]
        [SerializeField] private bool routeEscape = true;
        [SerializeField] private bool routeSavingLibraryCloseKey = true;
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private Key savingLibraryCloseKey = Key.R;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        [SerializeField] private KeyCode legacySavingLibraryCloseKey = KeyCode.R;
#endif

        [Header("Pause Menu")]
        [SerializeField] private bool disablePauseControllerEscapeInput = true;

        [Header("Saving Library")]
        [SerializeField] private bool hideSavingLibraryOnAwake = true;
        [SerializeField] private bool lockMovementWhileSavingLibraryOpen = true;
        [SerializeField] private bool lockActionsWhileSavingLibraryOpen = true;

        [Header("Cursor")]
        [SerializeField] private bool showCursorForGameplayCanvases = true;
        [SerializeField] private bool restoreCursorWhenGameplayCanvasCloses = true;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges;

        private GameCanvasState2D _currentState = GameCanvasState2D.Gameplay;
        private PlayerCore2D _activeCanvasPlayerCore;
        private bool _cursorStateCaptured;
        private bool _previousCursorVisible;
        private CursorLockMode _previousCursorLockMode;
        private int _stateOpenedFrame = -1;

        public GameCanvasState2D CurrentState => _currentState;
        public bool IsGameplay => _currentState == GameCanvasState2D.Gameplay;

        public event Action<GameCanvasState2D, GameCanvasState2D> OnStateChanged;

        private void Awake()
        {
            if (disablePauseControllerEscapeInput && pauseMenuController != null)
            {
                pauseMenuController.SetEscapeToggleEnabled(false);
            }

            if (savingLibraryRoot != null && hideSavingLibraryOnAwake)
            {
                savingLibraryRoot.SetActive(false);
            }
        }

        private void Update()
        {
            if (routeEscape && WasEscapePressedThisFrame())
            {
                HandleEscapePressed();
                return;
            }

            if (routeSavingLibraryCloseKey
                && _currentState == GameCanvasState2D.SavingLibrary
                && Time.frameCount > _stateOpenedFrame
                && WasSavingLibraryClosePressedThisFrame())
            {
                CloseSavingLibrary();
            }
        }

        private void OnDisable()
        {
            CloseCurrentCanvas();
        }

        public bool TryOpenSavingLibrary(GameObject interactor)
        {
            if (_currentState != GameCanvasState2D.Gameplay)
            {
                return false;
            }

            if (savingLibraryRoot == null)
            {
                Debug.LogError("GameCanvasManager2D cannot open SavingLibrary because savingLibraryRoot is not assigned.", this);
                return false;
            }

            _activeCanvasPlayerCore = ResolvePlayerCore(interactor);
            savingLibraryRoot.SetActive(true);
            SetGameplayCanvasPlayerLocked(true);
            ApplyGameplayCanvasCursorState();
            SetState(GameCanvasState2D.SavingLibrary);
            return true;
        }

        public void CloseSavingLibrary()
        {
            if (_currentState != GameCanvasState2D.SavingLibrary)
            {
                return;
            }

            if (savingLibraryRoot != null)
            {
                savingLibraryRoot.SetActive(false);
            }

            SetGameplayCanvasPlayerLocked(false);
            RestoreGameplayCanvasCursorState();
            _activeCanvasPlayerCore = null;
            SetState(GameCanvasState2D.Gameplay);
        }

        public void OpenPauseMenu()
        {
            if (_currentState != GameCanvasState2D.Gameplay || pauseMenuController == null)
            {
                return;
            }

            pauseMenuController.Pause();
            SetState(GameCanvasState2D.PauseMenu);
        }

        public void ClosePauseMenu()
        {
            if (_currentState != GameCanvasState2D.PauseMenu)
            {
                return;
            }

            if (pauseMenuController != null)
            {
                pauseMenuController.Resume();
            }

            SetState(GameCanvasState2D.Gameplay);
        }

        public void CloseCurrentCanvas()
        {
            switch (_currentState)
            {
                case GameCanvasState2D.PauseMenu:
                    ClosePauseMenu();
                    break;
                case GameCanvasState2D.SavingLibrary:
                    CloseSavingLibrary();
                    break;
                default:
                    break;
            }
        }

        private void HandleEscapePressed()
        {
            switch (_currentState)
            {
                case GameCanvasState2D.Gameplay:
                    OpenPauseMenu();
                    break;
                case GameCanvasState2D.PauseMenu:
                    ClosePauseMenu();
                    break;
                case GameCanvasState2D.SavingLibrary:
                    CloseSavingLibrary();
                    break;
                default:
                    CloseCurrentCanvas();
                    break;
            }
        }

        private void SetState(GameCanvasState2D state)
        {
            if (_currentState == state)
            {
                return;
            }

            GameCanvasState2D previousState = _currentState;
            _currentState = state;
            _stateOpenedFrame = Time.frameCount;
            OnStateChanged?.Invoke(previousState, _currentState);

            if (logStateChanges)
            {
                Debug.Log($"Canvas state changed to {_currentState}.", this);
            }
        }

        private void SetGameplayCanvasPlayerLocked(bool locked)
        {
            SetCoreLocked(_activeCanvasPlayerCore, locked);

            if (playerCoresToLock == null)
            {
                return;
            }

            for (int i = 0; i < playerCoresToLock.Length; i++)
            {
                SetCoreLocked(playerCoresToLock[i], locked);
            }
        }

        private void SetCoreLocked(PlayerCore2D core, bool locked)
        {
            if (core == null)
            {
                return;
            }

            if (lockMovementWhileSavingLibraryOpen)
            {
                core.SetMovementLocked(locked);
            }

            if (lockActionsWhileSavingLibraryOpen)
            {
                core.SetActionLocked(locked);
            }
        }

        private void ApplyGameplayCanvasCursorState()
        {
            if (!showCursorForGameplayCanvases || _cursorStateCaptured)
            {
                return;
            }

            _previousCursorVisible = Cursor.visible;
            _previousCursorLockMode = Cursor.lockState;
            _cursorStateCaptured = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void RestoreGameplayCanvasCursorState()
        {
            if (!restoreCursorWhenGameplayCanvasCloses || !_cursorStateCaptured)
            {
                _cursorStateCaptured = false;
                return;
            }

            Cursor.visible = _previousCursorVisible;
            Cursor.lockState = _previousCursorLockMode;
            _cursorStateCaptured = false;
        }

        private static PlayerCore2D ResolvePlayerCore(GameObject interactor)
        {
            if (interactor == null)
            {
                return null;
            }

            PlayerCore2D core = interactor.GetComponent<PlayerCore2D>();
            if (core != null)
            {
                return core;
            }

            return interactor.GetComponentInParent<PlayerCore2D>();
        }

        private bool WasEscapePressedThisFrame()
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

        private bool WasSavingLibraryClosePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[savingLibraryCloseKey].wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(legacySavingLibraryCloseKey);
#else
            return false;
#endif
        }
    }
}
