using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Centralized recoil handler for player attack feedback.
    /// Keeps recoil calculation and physics response outside of specific attack scripts.
    /// </summary>
    [RequireComponent(typeof(PlayerCore2D))]
    public class PlayerAttackRecoil2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCore2D core;

        [Header("Melee Hit Recoil")]
        [SerializeField] private bool enableMeleeHitRecoil = true;
        [SerializeField, Min(0f)] private float baseHorizontalRecoil = 3.2f;
        [SerializeField, Min(0f)] private float weightInfluence = 0.45f;
        [SerializeField, Min(0f)] private float minRecoilMultiplier = 0.2f;
        [SerializeField, Min(0f)] private float unarmedWeight = 0.8f;
        [SerializeField, Min(0f)] private float recoilCooldown = 0.03f;

        [Header("Optional Vertical Response")]
        [SerializeField] private bool addVerticalRecoil = false;
        [SerializeField] private float baseVerticalRecoil = 0f;

        private float _nextAllowedRecoilTime;

        private void Reset()
        {
            core = GetComponent<PlayerCore2D>();
        }

        private void Awake()
        {
            if (core == null)
            {
                core = GetComponent<PlayerCore2D>();
            }

            if (core == null || core.Body == null)
            {
                Debug.LogError("PlayerAttackRecoil2D is missing PlayerCore2D/Rigidbody2D reference.");
                enabled = false;
            }
        }

        /// <summary>
        /// Applies recoil when melee successfully hits a target.
        /// Returns true when recoil was applied.
        /// </summary>
        public bool TryApplyMeleeHitRecoil(int attackFacingDirection, WeaponData equippedWeapon)
        {
            if (!enableMeleeHitRecoil || core == null || core.Body == null)
            {
                return false;
            }

            if (Time.time < _nextAllowedRecoilTime)
            {
                return false;
            }

            int facing = attackFacingDirection >= 0 ? 1 : -1;
            float recoilMultiplier = ResolveWeightMultiplier(equippedWeapon);
            float horizontalRecoil = baseHorizontalRecoil * recoilMultiplier;
            float verticalRecoil = addVerticalRecoil ? baseVerticalRecoil * recoilMultiplier : 0f;

            Vector2 velocity = core.Body.linearVelocity;
            velocity.x += -facing * horizontalRecoil;
            velocity.y += verticalRecoil;
            core.Body.linearVelocity = velocity;

            _nextAllowedRecoilTime = Time.time + recoilCooldown;
            Debug.Log("Applied melee hit recoil: " + horizontalRecoil);
            return true;
        }

        private float ResolveWeightMultiplier(WeaponData equippedWeapon)
        {
            float weight = equippedWeapon != null ? equippedWeapon.Weight : unarmedWeight;
            float multiplier = 1f / (1f + Mathf.Max(0f, weight) * weightInfluence);
            return Mathf.Max(minRecoilMultiplier, multiplier);
        }

        private void OnValidate()
        {
            baseHorizontalRecoil = Mathf.Max(0f, baseHorizontalRecoil);
            weightInfluence = Mathf.Max(0f, weightInfluence);
            minRecoilMultiplier = Mathf.Max(0f, minRecoilMultiplier);
            unarmedWeight = Mathf.Max(0f, unarmedWeight);
            recoilCooldown = Mathf.Max(0f, recoilCooldown);
        }
    }
}
