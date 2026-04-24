using UnityEngine;

namespace EndlessJourney.Interfaces
{
    /// <summary>
    /// Unified contract for systems that can receive player harm.
    /// Keeps all "damage player" behaviors on one interface entry point.
    /// </summary>
    public interface IPlayerHarmful
    {
        /// <summary>
        /// Returns true when this target can currently receive harm.
        /// </summary>
        bool CanReceiveHarm();

        /// <summary>
        /// Applies harm to this target.
        /// </summary>
        /// <param name="amount">Incoming harm amount.</param>
        /// <param name="source">Source object of the harm.</param>
        /// <returns>True when harm was accepted and applied.</returns>
        bool ReceiveHarm(float amount, GameObject source);
    }
}
