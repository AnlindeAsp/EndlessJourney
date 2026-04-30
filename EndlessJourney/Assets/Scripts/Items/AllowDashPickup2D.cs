using EndlessJourney.Player;

namespace EndlessJourney.Items
{
    /// <summary>
    /// Pickup that unlocks dash ability.
    /// </summary>
    public class AllowDashPickup2D : AbilityPickupItem2D
    {
        protected override bool ApplyPickup(PlayerAbilityCore2D abilityCore)
        {
            if (abilityCore == null || abilityCore.AllowDashEnabled)
            {
                return false;
            }

            abilityCore.AllowDash();
            return true;
        }
    }
}
