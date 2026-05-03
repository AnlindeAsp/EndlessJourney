using EndlessJourney.UI;
using UnityEngine;

namespace EndlessJourney.Interaction
{
    /// <summary>
    /// Save-point interaction that opens the saving library / storage canvas.
    /// Trigger registration and prompt behavior come from TriggerInteractable2D.
    /// </summary>
    public class OpenSavingLibrary2D : TriggerInteractable2D
    {
        [Header("Canvas Manager")]
        [SerializeField] private GameCanvasManager2D canvasManager;
        [SerializeField] private bool closeWhenPlayerLeaves = true;

        [Header("Debug")]
        [SerializeField] private bool logStateChanges;

        private void OnEnable()
        {
            if (canvasManager != null)
            {
                canvasManager.OnStateChanged += HandleCanvasStateChanged;
            }
        }

        protected override void OnDisable()
        {
            if (canvasManager != null)
            {
                canvasManager.OnStateChanged -= HandleCanvasStateChanged;
            }

            base.OnDisable();
        }

        protected override void OnTriggerExit2D(Collider2D other)
        {
            base.OnTriggerExit2D(other);

            if (closeWhenPlayerLeaves
                && canvasManager != null
                && canvasManager.CurrentState == GameCanvasState2D.SavingLibrary
                && !HasInsideInteractors)
            {
                canvasManager.CloseSavingLibrary();
                RefreshPromptDisplay();
            }
        }

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor)
                && canvasManager != null
                && canvasManager.CurrentState == GameCanvasState2D.Gameplay;
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            if (canvasManager.TryOpenSavingLibrary(interactor))
            {
                HidePromptDisplay();

                if (logStateChanges)
                {
                    Debug.Log("Saving library open requested.", this);
                }
            }
        }

        private void HandleCanvasStateChanged(GameCanvasState2D previousState, GameCanvasState2D currentState)
        {
            if (currentState == GameCanvasState2D.Gameplay)
            {
                RefreshPromptDisplay();
                return;
            }

            HidePromptDisplay();
        }
    }
}
