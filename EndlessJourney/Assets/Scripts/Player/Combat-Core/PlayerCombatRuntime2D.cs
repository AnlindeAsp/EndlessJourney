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
        [SerializeField] private PlayerHealth2D playerHealth;
        [SerializeField] private PlayerMana2D playerMana;

        [Header("Melee Hit Defaults")]
        [SerializeField] private DamageType meleeDamageType = DamageType.Physical;
        [SerializeField] private WeaponType fallbackWeaponType = WeaponType.Sword;
        [SerializeField, Min(0f)] private float fallbackWeaponWeight;

        [Header("Debug")]
        [SerializeField] private bool logMissingReferences;
        [SerializeField] private bool logDynamicInscriptionEffects;

        [Header("Runtime (Read-Only)")]
        [SerializeField, Min(0)] private int comboDamageRampStacks;

        private float _lastComboDamageRampHitTime = float.NegativeInfinity;
        private string _comboDamageRampInscriptionId = string.Empty;

        public DamageType MeleeDamageType => meleeDamageType;
        public float MeleeDamagePerHit => ResolveMeleeDamagePerHit();
        public int MeleeHitCount => combatCore != null ? Mathf.Max(1, combatCore.AttackHitCount) : 1;
        public WeaponType CurrentWeaponType => weaponSystem != null ? weaponSystem.EffectiveWeaponType : fallbackWeaponType;
        public float CurrentWeaponWeight => weaponSystem != null ? weaponSystem.EffectiveWeaponWeight : fallbackWeaponWeight;
        public WeaponInscriptionData CurrentInscription => weaponSystem != null ? weaponSystem.EquippedInscription : null;
        public int ComboDamageRampStacks => comboDamageRampStacks;

        public event Action<HitContext, HitResult, GameObject> OnMeleeHitApplied;

        private void Reset()
        {
            combatCore = GetComponent<PlayerCombatCore>();
            weaponSystem = GetComponent<PlayerWeaponSystem>();
            playerHealth = GetComponent<PlayerHealth2D>();
            playerMana = GetComponent<PlayerMana2D>();
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

        private void OnEnable()
        {
            if (weaponSystem != null)
            {
                weaponSystem.OnWeaponEquipped += HandleWeaponRuntimeChanged;
            }
        }

        private void OnDisable()
        {
            if (weaponSystem != null)
            {
                weaponSystem.OnWeaponEquipped -= HandleWeaponRuntimeChanged;
            }

            ResetComboDamageRamp();
        }

        private void Update()
        {
            WeaponInscriptionData inscription = CurrentInscription;
            if (inscription != null && inscription.EffectType == WeaponInscriptionEffectType.ComboDamageRamp)
            {
                RefreshComboDamageRampState(inscription);
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
                ResolveMeleeDamagePerHit(),
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

            ApplyDynamicInscriptionOnMeleeHitApplied();
            OnMeleeHitApplied?.Invoke(context, result, targetRoot);
        }

        private float ResolveMeleeDamagePerHit()
        {
            float damage = combatCore != null ? Mathf.Max(0f, combatCore.AttackDamagePerHit) : 0f;
            if (damage <= 0f)
            {
                return 0f;
            }

            WeaponInscriptionData inscription = CurrentInscription;
            if (inscription == null)
            {
                ResetComboDamageRampIfCurrentEffectIsNotCombo();
                return damage;
            }

            switch (inscription.EffectType)
            {
                case WeaponInscriptionEffectType.ComboDamageRamp:
                    return damage * ResolveComboDamageRampMultiplier(inscription);
                case WeaponInscriptionEffectType.MissingHealthDamageBonus:
                    return damage * ResolveMissingHealthDamageMultiplier(inscription);
                default:
                    ResetComboDamageRampIfCurrentEffectIsNotCombo();
                    return damage;
            }
        }

        private float ResolveComboDamageRampMultiplier(WeaponInscriptionData inscription)
        {
            RefreshComboDamageRampState(inscription);
            float bonusPerStack = Mathf.Max(0f, inscription.Value);
            return 1f + comboDamageRampStacks * bonusPerStack;
        }

        private float ResolveMissingHealthDamageMultiplier(WeaponInscriptionData inscription)
        {
            if (playerHealth == null)
            {
                if (logMissingReferences)
                {
                    Debug.LogWarning("PlayerCombatRuntime2D needs PlayerHealth2D for MissingHealthDamageBonus, but none is assigned.", this);
                }

                return 1f;
            }

            float missingHealthPercent = Mathf.Clamp01(1f - playerHealth.HealthNormalized);
            float bonusScale = Mathf.Max(0f, inscription.Value);
            return 1f + missingHealthPercent * bonusScale;
        }

        private void ApplyDynamicInscriptionOnMeleeHitApplied()
        {
            WeaponInscriptionData inscription = CurrentInscription;
            if (inscription == null)
            {
                ResetComboDamageRampIfCurrentEffectIsNotCombo();
                return;
            }

            switch (inscription.EffectType)
            {
                case WeaponInscriptionEffectType.ComboDamageRamp:
                    AddComboDamageRampStack(inscription);
                    break;
                case WeaponInscriptionEffectType.ManaOnHit:
                    RestoreManaOnHit(inscription);
                    break;
                default:
                    ResetComboDamageRampIfCurrentEffectIsNotCombo();
                    break;
            }
        }

        private void AddComboDamageRampStack(WeaponInscriptionData inscription)
        {
            RefreshComboDamageRampState(inscription);
            comboDamageRampStacks = Mathf.Max(0, comboDamageRampStacks) + 1;
            _lastComboDamageRampHitTime = Time.time;

            if (logDynamicInscriptionEffects)
            {
                Debug.Log($"ComboDamageRamp stack added. stacks={comboDamageRampStacks}, nextMultiplier={ResolveComboDamageRampMultiplier(inscription):0.###}", this);
            }
        }

        private void RestoreManaOnHit(WeaponInscriptionData inscription)
        {
            if (playerMana == null)
            {
                if (logMissingReferences)
                {
                    Debug.LogWarning("PlayerCombatRuntime2D needs PlayerMana2D for ManaOnHit, but none is assigned.", this);
                }

                return;
            }

            float restoreAmount = Mathf.Max(0f, inscription.Value);
            if (restoreAmount <= 0f)
            {
                return;
            }

            if (playerMana.RestoreMana(restoreAmount) && logDynamicInscriptionEffects)
            {
                Debug.Log($"ManaOnHit restored {restoreAmount:0.##} mana.", this);
            }
        }

        private void RefreshComboDamageRampState(WeaponInscriptionData inscription)
        {
            string inscriptionId = inscription != null ? inscription.InscriptionId : string.Empty;
            if (!string.Equals(_comboDamageRampInscriptionId, inscriptionId, StringComparison.Ordinal))
            {
                comboDamageRampStacks = 0;
                _comboDamageRampInscriptionId = inscriptionId;
                _lastComboDamageRampHitTime = float.NegativeInfinity;
                return;
            }

            float timeout = inscription != null ? Mathf.Max(0f, inscription.TimeoutSeconds) : 0f;
            if (timeout <= 0f || comboDamageRampStacks <= 0)
            {
                return;
            }

            if (Time.time - _lastComboDamageRampHitTime > timeout)
            {
                ResetComboDamageRamp();
                _comboDamageRampInscriptionId = inscriptionId;
            }
        }

        private void ResetComboDamageRampIfCurrentEffectIsNotCombo()
        {
            WeaponInscriptionData inscription = CurrentInscription;
            if (inscription == null || inscription.EffectType != WeaponInscriptionEffectType.ComboDamageRamp)
            {
                ResetComboDamageRamp();
            }
        }

        private void ResetComboDamageRamp()
        {
            comboDamageRampStacks = 0;
            _lastComboDamageRampHitTime = float.NegativeInfinity;
            _comboDamageRampInscriptionId = string.Empty;
        }

        private void HandleWeaponRuntimeChanged(WeaponData weapon)
        {
            ResetComboDamageRamp();
        }

        private void OnValidate()
        {
            fallbackWeaponWeight = Mathf.Max(0f, fallbackWeaponWeight);
        }
    }
}
