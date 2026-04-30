using EndlessJourney.Player;

namespace EndlessJourney.Items
{
    /// <summary>
    /// Pickup that unlocks double-jump ability.
    /// </summary>
    public class AllowDoubleJumpPickup2D : AbilityPickupItem2D
    {
        protected override bool ApplyPickup(PlayerAbilityCore2D abilityCore)
        {
            if (abilityCore == null || abilityCore.AllowDoubleJumpEnabled)
            {
                return false;
            }

            abilityCore.AllowDoubleJump();
            return true;
        }
    }
}
