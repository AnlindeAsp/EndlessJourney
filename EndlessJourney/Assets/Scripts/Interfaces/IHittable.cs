using EndlessJourney.Combat;

namespace EndlessJourney.Interfaces
{
    /// <summary>
    /// Contract for any object that can be hit by combat actions.
    /// </summary>
    public interface IHittable
    {
        /// <summary>
        /// Quick pre-check before applying hit logic.
        /// </summary>
        bool CanBeHit(HitContext context);

        /// <summary>
        /// Handles one hit attempt and returns the result.
        /// </summary>
        HitResult ReceiveHit(HitContext context);
    }
}
