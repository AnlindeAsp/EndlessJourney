using System;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Runtime combat state for player.
    /// Stores calculated combat values and equipped weapon name only.
    /// </summary>
    public class PlayerCombatCore : MonoBehaviour
    {
        [Header("Base Stats")]
        [SerializeField, Min(0.01f)] private float baseStrength = 10f;

        [Header("Current Combat Snapshot (Read-Only At Runtime)")]
        [SerializeField] private string equippedWeaponName = "None";
        [SerializeField, Min(0f)] private float attackRange = 1.5f;
        [SerializeField, Min(0f)] private float attackDamagePerHit = 5f;
        [SerializeField, Min(1)] private int attackHitCount = 1;
        [SerializeField, Min(0f)] private float attackSpeed = 0.1f;

        public float BaseStrength => baseStrength;
        public string EquippedWeaponName => equippedWeaponName;
        public float AttackRange => attackRange;
        public float AttackDamagePerHit => attackDamagePerHit;
        public int AttackHitCount => attackHitCount;
        public float AttackDamageTotal => attackDamagePerHit * attackHitCount;
        public float AttackSpeed => attackSpeed;

        public event Action OnCombatStatsChanged;

        /// <summary>
        /// Updates base strength used by weapon formulas.
        /// </summary>
        public void SetBaseStrength(float value)
        {
            baseStrength = Mathf.Max(0.01f, value);
            OnCombatStatsChanged?.Invoke();
        }

        /// <summary>
        /// Applies a weapon result snapshot.
        /// Weapons are not stored here; only the computed values are.
        /// </summary>
        public void ApplyWeaponSnapshot(
            string weaponName,
            float computedAttackRange,
            float computedDamagePerHit,
            int computedHitCount,
            float computedAttackSpeed)
        {
            equippedWeaponName = string.IsNullOrWhiteSpace(weaponName) ? "Unknown" : weaponName.Trim();
            attackRange = Mathf.Max(0f, computedAttackRange);
            attackDamagePerHit = Mathf.Max(0f, computedDamagePerHit);
            attackHitCount = Mathf.Max(1, computedHitCount);
            attackSpeed = Mathf.Max(0f, computedAttackSpeed);
            OnCombatStatsChanged?.Invoke();
        }

        /// <summary>
        /// Clears weapon-influenced combat values.
        /// </summary>
        public void ClearWeaponSnapshot()
        {
            equippedWeaponName = "None";
            attackRange = 0f;
            attackDamagePerHit = 0f;
            attackHitCount = 1;
            attackSpeed = 0f;
            OnCombatStatsChanged?.Invoke();
        }

        private void OnValidate()
        {
            baseStrength = Mathf.Max(0.01f, baseStrength);
            attackRange = Mathf.Max(0f, attackRange);
            attackDamagePerHit = Mathf.Max(0f, attackDamagePerHit);
            attackHitCount = Mathf.Max(1, attackHitCount);
            attackSpeed = Mathf.Max(0f, attackSpeed);
            equippedWeaponName = string.IsNullOrWhiteSpace(equippedWeaponName) ? "None" : equippedWeaponName.Trim();
        }
    }
}
