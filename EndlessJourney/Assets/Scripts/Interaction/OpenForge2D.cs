using EndlessJourney.Player;
using EndlessJourney.UI;
using UnityEngine;

namespace EndlessJourney.Interaction
{
    /// <summary>
    /// Forge interaction that opens the weapon inscription canvas.
    /// Trigger registration and prompt behavior come from TriggerInteractable2D.
    /// </summary>
    public class OpenForge2D : TriggerInteractable2D
    {
        [Header("Canvas Manager")]
        [SerializeField] private GameCanvasManager2D canvasManager;
        [SerializeField] private bool closeWhenPlayerLeaves = true;

        [Header("Armor")]
        [SerializeField] private bool repairArmorOnOpen = true;
        [SerializeField] private PlayerArmorEquipmentSystem2D armorEquipmentSystem;

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
                && canvasManager.CurrentState == GameCanvasState2D.Forge
                && !HasInsideInteractors)
            {
                canvasManager.CloseForge();
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

            if (canvasManager.TryOpenForge(interactor))
            {
                HidePromptDisplay();
                RepairArmorIfConfigured();

                if (logStateChanges)
                {
                    Debug.Log("Forge open requested.", this);
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

        private void RepairArmorIfConfigured()
        {
            if (!repairArmorOnOpen || armorEquipmentSystem == null)
            {
                return;
            }

            armorEquipmentSystem.RepairEquippedArmorFull();
        }
    }
}
