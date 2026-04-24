using UnityEngine;

namespace EndlessJourney.Interfaces
{
    /// <summary>
    /// Minimal combat damage receiver contract.
    /// Any hittable target can implement this to receive melee/spell damage.
    /// </summary>
    public interface IDamageable2D
    {
        /// <summary>
        /// Applies incoming damage.
        /// </summary>
        /// <param name="amount">Damage amount to apply.</param>
        /// <param name="source">Who dealt the damage.</param>
        /// <returns>True when damage was accepted and applied.</returns>
        bool ReceiveDamage(float amount, GameObject source);
    }
}
