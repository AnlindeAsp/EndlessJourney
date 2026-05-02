using EndlessJourney.Player;

namespace EndlessJourney.Interfaces
{
    /// <summary>
    /// Optional receiver for buff-type spell effects.
    /// </summary>
    public interface ISpellBuffReceiver2D
    {
        bool ApplySpellBuff(string buffId, float duration, SpellData2D sourceSpell, object source);
    }
}
