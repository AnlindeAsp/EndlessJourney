using EndlessJourney.Interfaces;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Buff spell effect. Actual buff behavior is handled by external receivers.
    /// </summary>
    [CreateAssetMenu(fileName = "BuffEffect_", menuName = "EndlessJourney/Scriptable/Spell/Effects/Buff Effect 2D")]
    public class BuffSpellEffectData2D : SpellEffectData2D
    {
        [SerializeField] private string buffId = "buff_default";
        [SerializeField, Min(0f)] private float duration = 5f;
        [SerializeField] private bool logIfNoReceiver = true;

        public override bool Execute(SpellEffectContext2D context)
        {
            if (context.CastSystem != null)
            {
                context.CastSystem.NotifyBuffEffectRequested(buffId, duration, context.SpellData);
            }

            if (context.Caster == null || string.IsNullOrWhiteSpace(buffId) || duration <= 0f)
            {
                return false;
            }

            ISpellBuffReceiver2D receiver = context.Caster.GetComponent(typeof(ISpellBuffReceiver2D)) as ISpellBuffReceiver2D;
            if (receiver == null)
            {
                receiver = context.Caster.GetComponentInChildren(typeof(ISpellBuffReceiver2D)) as ISpellBuffReceiver2D;
            }

            if (receiver == null)
            {
                if (logIfNoReceiver)
                {
                    Debug.Log($"No ISpellBuffReceiver2D found on caster for buff '{buffId}'.");
                }

                return false;
            }

            return receiver.ApplySpellBuff(buffId, duration, context.SpellData, context.CastSystem);
        }
    }
}
