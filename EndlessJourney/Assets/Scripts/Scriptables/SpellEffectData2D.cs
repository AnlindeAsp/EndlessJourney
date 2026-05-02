using UnityEngine;

namespace EndlessJourney.Player
{
    public struct SpellEffectContext2D
    {
        public SpellCastSystem CastSystem { get; }
        public SpellData2D SpellData { get; }
        public GameObject Caster { get; }
        public PlayerCore2D Core { get; }
        public PlayerHealth2D Health { get; }
        public PlayerProjectileLauncher2D ProjectileLauncher { get; }

        public SpellEffectContext2D(
            SpellCastSystem castSystem,
            SpellData2D spellData,
            GameObject caster,
            PlayerCore2D core,
            PlayerHealth2D health,
            PlayerProjectileLauncher2D projectileLauncher)
        {
            CastSystem = castSystem;
            SpellData = spellData;
            Caster = caster;
            Core = core;
            Health = health;
            ProjectileLauncher = projectileLauncher;
        }
    }

    /// <summary>
    /// Base class for spell effect data assets.
    /// </summary>
    public abstract class SpellEffectData2D : ScriptableObject
    {
        public abstract bool Execute(SpellEffectContext2D context);
    }
}
