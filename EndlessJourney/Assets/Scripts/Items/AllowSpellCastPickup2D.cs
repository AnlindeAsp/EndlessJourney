using EndlessJourney.Player;

namespace EndlessJourney.Items
{
    /// <summary>
    /// Pickup that unlocks spell-cast ability.
    /// </summary>
    public class AllowSpellCastPickup2D : AbilityPickupItem2D
    {
        protected override bool ApplyPickup(PlayerAbilityCore2D abilityCore)
        {
            if (abilityCore == null || abilityCore.AllowSpellCastEnabled)
            {
                return false;
            }

            abilityCore.AllowSpellCast();
            return true;
        }
    }
}
