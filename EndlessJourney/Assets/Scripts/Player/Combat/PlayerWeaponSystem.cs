using System;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Weapon runtime bridge for player combat.
    /// On equip, reads WeaponData and writes computed combat stats into PlayerCombatCore.
    /// </summary>
    [RequireComponent(typeof(PlayerCombatCore))]
    public class PlayerWeaponSystem : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private WeaponData equippedWeapon;
        [SerializeField] private PlayerCombatCore combatCore;

        public WeaponData EquippedWeapon => equippedWeapon;

        public event Action<WeaponData> OnWeaponEquipped;

        private void Reset()
        {
            combatCore = GetComponent<PlayerCombatCore>();
        }

        private void Awake()
        {
            if (combatCore == null)
            {
                combatCore = GetComponent<PlayerCombatCore>();
            }
        }

        /// <summary>
        /// Equips a new weapon data asset.
        /// </summary>
        public void EquipWeapon(WeaponData weapon)
        {
            equippedWeapon = weapon;
            RecalculateCombatSnapshot();
            OnWeaponEquipped?.Invoke(equippedWeapon);
        }

        /// <summary>
        /// Returns true when the currently equipped weapon can be used.
        /// </summary>
        public bool CanUseEquippedWeapon()
        {
            if (equippedWeapon == null)
            {
                return false;
            }

            return equippedWeapon.IsOwned && equippedWeapon.CanUse;
        }

        /// <summary>
        /// Recalculates combat values from the equipped weapon and writes them into combat core.
        /// </summary>
        public void RecalculateCombatSnapshot()
        {
            if (combatCore == null)
            {
                return;
            }

            if (equippedWeapon == null)
            {
                combatCore.ClearWeaponSnapshot();
                return;
            }

            float strength = Mathf.Max(0.01f, combatCore.BaseStrength);
            float attackRange = equippedWeapon.Length + 0.25f;
            float damagePerHit;
            int hitCount;

            switch (equippedWeapon.Type)
            {
                case WeaponType.DualBlades:
                    damagePerHit = strength * equippedWeapon.Sharpness * 0.7f;
                    hitCount = 2;
                    break;
                case WeaponType.Heavy:
                    damagePerHit = (strength + equippedWeapon.Weight) * 1.5f;
                    hitCount = 1;
                    break;
                default:
                    damagePerHit = strength * equippedWeapon.Sharpness;
                    hitCount = 1;
                    break;
            }

            float attackSpeed = equippedWeapon.Weight / strength;

            combatCore.ApplyWeaponSnapshot(
                equippedWeapon.WeaponName,
                attackRange,
                damagePerHit,
                hitCount,
                attackSpeed
            );
        }
    }
}
