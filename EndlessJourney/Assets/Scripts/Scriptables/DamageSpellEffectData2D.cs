using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Damage spell effect. Current implementation is projectile-oriented.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageEffect_", menuName = "EndlessJourney/Scriptable/Spell/Effects/Damage Effect 2D")]
    public class DamageSpellEffectData2D : SpellEffectData2D
    {
        [Header("Projectile Damage")]
        [SerializeField] private bool useProjectile = true;
        [SerializeField] private PlayerProjectile2D projectilePrefab;
        [SerializeField, Min(0f)] private float projectileDamage = 10f;
        [SerializeField, Min(0.01f)] private float projectileLifeTime = 3f;

        public override bool Execute(SpellEffectContext2D context)
        {
            if (!useProjectile || context.ProjectileLauncher == null || projectilePrefab == null)
            {
                return false;
            }

            Vector3 spawnPosition = context.CastSystem != null
                ? context.CastSystem.GetSpellCastPosition(context.SpellData)
                : context.Caster.transform.position;

            return context.ProjectileLauncher.TryLaunchByFacing(
                projectilePrefab,
                projectileDamage,
                projectileLifeTime,
                spawnPosition);
        }
    }
}
