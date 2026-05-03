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
        [SerializeField] private WeaponEquipped2D weaponEquipped;
        [SerializeField] private WeaponData equippedWeapon;
        [SerializeField] private PlayerCombatCore combatCore;

        public WeaponData EquippedWeapon => equippedWeapon;

        public event Action<WeaponData> OnWeaponEquipped;

        private void Reset()
        {
            combatCore = GetComponent<PlayerCombatCore>();
            weaponEquipped = GetComponent<WeaponEquipped2D>();
        }

        private void Awake()
        {
            if (combatCore == null)
            {
                combatCore = GetComponent<PlayerCombatCore>();
            }

            if (weaponEquipped == null)
            {
                weaponEquipped = GetComponent<WeaponEquipped2D>();
            }
        }

        private void OnEnable()
        {
            if (weaponEquipped != null)
            {
                weaponEquipped.OnEquippedWeaponChanged += HandleEquippedWeaponChanged;
            }
        }

        private void Start()
        {
            SyncFromEquippedState();
        }

        private void OnDisable()
        {
            if (weaponEquipped != null)
            {
                weaponEquipped.OnEquippedWeaponChanged -= HandleEquippedWeaponChanged;
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
        /// Equips a weapon id through WeaponEquipped2D when available.
        /// </summary>
        public bool EquipWeaponById(string weaponId)
        {
            if (weaponEquipped == null)
            {
                return false;
            }

            return weaponEquipped.EquipWeapon(weaponId);
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

            if (weaponEquipped != null)
            {
                return weaponEquipped.IsEquippedWeaponUnlocked();
            }

            return true;
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
            // Developer notes, 0.1 is the fastest possible attack speed
            // 10 weight: 1.2s, 1s
            // 1 weight: 0.3s, 0.25s
            float attackSpeed = (equippedWeapon.Weight + 1f)/ (strength - 0f);

            combatCore.ApplyWeaponSnapshot(
                equippedWeapon.WeaponName,
                attackRange,
                damagePerHit,
                hitCount,
                attackSpeed
            );
        }

        private void HandleEquippedWeaponChanged(string weaponId)
        {
            SyncFromEquippedState();
        }

        private void SyncFromEquippedState()
        {
            if (weaponEquipped == null)
            {
                if (equippedWeapon != null)
                {
                    RecalculateCombatSnapshot();
                }

                return;
            }

            EquipWeapon(weaponEquipped.GetEquippedWeaponData());
        }
    }
}
