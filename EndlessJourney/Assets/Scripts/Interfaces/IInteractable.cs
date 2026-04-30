using UnityEngine;

namespace EndlessJourney.Interfaces
{
    /// <summary>
    /// Contract for non-combat world interactions (dialogue/read/use/etc.).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Priority when multiple interactables are available at once.
        /// Higher value wins.
        /// </summary>
        int InteractionPriority { get; }

        /// <summary>
        /// Returns true when this object can currently be interacted with.
        /// </summary>
        bool CanInteract(GameObject interactor);

        /// <summary>
        /// Executes interaction behavior.
        /// </summary>
        void Interact(GameObject interactor);

        /// <summary>
        /// Returns UI prompt text for current interaction state.
        /// </summary>
        string GetInteractionPrompt(GameObject interactor);
    }
}
