using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Self-heal spell effect.
    /// </summary>
    [CreateAssetMenu(fileName = "HealEffect_", menuName = "EndlessJourney/Scriptable/Spell/Effects/Heal Effect 2D")]
    public class HealSpellEffectData2D : SpellEffectData2D
    {
        [SerializeField, Min(0f)] private float healAmount = 25f;

        public override bool Execute(SpellEffectContext2D context)
        {
            if (context.Health == null || healAmount <= 0f)
            {
                return false;
            }

            return context.Health.Heal(healAmount);
        }
    }
}
