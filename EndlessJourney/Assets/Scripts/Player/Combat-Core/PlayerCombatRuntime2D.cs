using System;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Aggregates the player's final combat runtime state for hit payload creation.
    /// It does not own input, hitbox scanning, weapon equipment, or enemy logic.
    /// </summary>
    public class PlayerCombatRuntime2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCombatCore combatCore;
        [SerializeField] private PlayerWeaponSystem weaponSystem;

        [Header("Melee Hit Defaults")]
        [SerializeField] private DamageType meleeDamageType = DamageType.Physical;
        [SerializeField] private WeaponType fallbackWeaponType = WeaponType.Sword;
        [SerializeField, Min(0f)] private float fallbackWeaponWeight;

        [Header("Debug")]
        [SerializeField] private bool logMissingReferences;

        public DamageType MeleeDamageType => meleeDamageType;
        public float MeleeDamagePerHit => combatCore != null ? Mathf.Max(0f, combatCore.AttackDamagePerHit) : 0f;
        public int MeleeHitCount => combatCore != null ? Mathf.Max(1, combatCore.AttackHitCount) : 1;
        public WeaponType CurrentWeaponType => weaponSystem != null ? weaponSystem.EffectiveWeaponType : fallbackWeaponType;
        public float CurrentWeaponWeight => weaponSystem != null ? weaponSystem.EffectiveWeaponWeight : fallbackWeaponWeight;

        public event Action<HitContext, HitResult, GameObject> OnMeleeHitApplied;

        private void Reset()
        {
            combatCore = GetComponent<PlayerCombatCore>();
            weaponSystem = GetComponent<PlayerWeaponSystem>();
        }

        private void Awake()
        {
            if (logMissingReferences && combatCore == null)
            {
                Debug.LogWarning("PlayerCombatRuntime2D has no PlayerCombatCore reference. Melee hit damage will be 0.", this);
            }

            if (logMissingReferences && weaponSystem == null)
            {
                Debug.LogWarning("PlayerCombatRuntime2D has no PlayerWeaponSystem reference. HitContext will use fallback weapon data.", this);
            }
        }

        public HitContext CreateMeleeHitContext(
            GameObject source,
            Collider2D sourceCollider,
            Vector2 hitPoint,
            Vector2 hitDirection,
            int hitIndex)
        {
            int hitCount = MeleeHitCount;
            return new HitContext(
                source,
                sourceCollider,
                hitPoint,
                hitDirection,
                MeleeDamagePerHit,
                HitType.Melee,
                meleeDamageType,
                CurrentWeaponType,
                CurrentWeaponWeight,
                hitIndex,
                hitCount);
        }

        public void NotifyMeleeHitApplied(HitContext context, HitResult result, GameObject targetRoot)
        {
            if (!result.WasApplied)
            {
                return;
            }

            OnMeleeHitApplied?.Invoke(context, result, targetRoot);
        }

        private void OnValidate()
        {
            fallbackWeaponWeight = Mathf.Max(0f, fallbackWeaponWeight);
        }
    }
}
