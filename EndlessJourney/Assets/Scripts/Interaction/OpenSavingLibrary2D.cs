using System;
using EndlessJourney.Player;
using UnityEngine;

namespace EndlessJourney.Interaction
{
    /// <summary>
    /// Save-point interaction that opens the saving library / storage canvas.
    /// Trigger registration and prompt behavior come from TriggerInteractable2D.
    /// </summary>
    public class OpenSavingLibrary2D : TriggerInteractable2D
    {
        [Header("Saving Library")]
        [SerializeField] private GameObject savingLibraryRoot;
        [SerializeField] private bool hideLibraryOnAwake = true;
        [SerializeField] private bool toggleOnInteract = true;
        [SerializeField] private bool closeWhenPlayerLeaves = true;
        [SerializeField] private bool closeWhenDisabled = true;

        [Header("Player State")]
        [SerializeField] private bool lockPlayerMovementWhileOpen = true;
        [SerializeField] private bool lockPlayerActionsWhileOpen;

        [Header("Cursor")]
        [SerializeField] private bool showCursorWhileOpen = true;
        [SerializeField] private bool restoreCursorOnClose = true;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges;

        private PlayerCore2D _lockedPlayerCore;
        private bool _isOpen;
        private bool _cursorStateCaptured;
        private bool _previousCursorVisible;
        private CursorLockMode _previousCursorLockMode;

        public bool IsOpen => _isOpen;

        public event Action Opened;
        public event Action Closed;

        protected override void Awake()
        {
            base.Awake();

            if (savingLibraryRoot != null && hideLibraryOnAwake)
            {
                SetLibraryVisible(false);
            }
            else if (savingLibraryRoot != null)
            {
                _isOpen = savingLibraryRoot.activeSelf;
            }
        }

        protected override void OnDisable()
        {
            if (closeWhenDisabled && _isOpen)
            {
                CloseLibrary();
            }

            base.OnDisable();
        }

        protected override void OnTriggerExit2D(Collider2D other)
        {
            base.OnTriggerExit2D(other);

            if (closeWhenPlayerLeaves && _isOpen && !HasInsideInteractors)
            {
                CloseLibrary();
            }
        }

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor) && savingLibraryRoot != null;
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            if (_isOpen && toggleOnInteract)
            {
                CloseLibrary();
                return;
            }

            if (!_isOpen)
            {
                OpenLibrary(interactor);
            }
        }

        public void OpenLibrary(GameObject interactor)
        {
            if (savingLibraryRoot == null || _isOpen)
            {
                return;
            }

            _lockedPlayerCore = ResolvePlayerCore(interactor);
            SetLibraryVisible(true);
            SetPlayerLocked(true);
            ApplyCursorOpenState();
            Opened?.Invoke();
            HidePromptDisplay();

            if (logStateChanges)
            {
                Debug.Log("Saving library opened.", this);
            }
        }

        public void CloseLibrary()
        {
            if (savingLibraryRoot == null || !_isOpen)
            {
                return;
            }

            SetLibraryVisible(false);
            SetPlayerLocked(false);
            RestoreCursorState();
            _lockedPlayerCore = null;
            Closed?.Invoke();
            RefreshPromptDisplay();

            if (logStateChanges)
            {
                Debug.Log("Saving library closed.", this);
            }
        }

        private void SetLibraryVisible(bool visible)
        {
            savingLibraryRoot.SetActive(visible);
            _isOpen = visible;
        }

        private void SetPlayerLocked(bool locked)
        {
            if (_lockedPlayerCore == null)
            {
                return;
            }

            if (lockPlayerMovementWhileOpen)
            {
                _lockedPlayerCore.SetMovementLocked(locked);
            }

            if (lockPlayerActionsWhileOpen)
            {
                _lockedPlayerCore.SetActionLocked(locked);
            }
        }

        private void ApplyCursorOpenState()
        {
            if (!showCursorWhileOpen || _cursorStateCaptured)
            {
                return;
            }

            _previousCursorVisible = Cursor.visible;
            _previousCursorLockMode = Cursor.lockState;
            _cursorStateCaptured = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void RestoreCursorState()
        {
            if (!restoreCursorOnClose || !_cursorStateCaptured)
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
    }
}
