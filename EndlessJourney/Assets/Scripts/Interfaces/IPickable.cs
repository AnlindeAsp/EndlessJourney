using UnityEngine;

namespace EndlessJourney.Interfaces
{
    /// <summary>
    /// Contract for world pickup objects.
    /// </summary>
    public interface IPickable
    {
        /// <summary>
        /// Returns true if this object can currently be picked by the given picker.
        /// </summary>
        bool CanBePickedBy(GameObject picker);

        /// <summary>
        /// Attempts to apply pickup effect to the picker.
        /// Returns true when pickup effect is applied successfully.
        /// </summary>
        bool TryPick(GameObject picker);
    }
}
