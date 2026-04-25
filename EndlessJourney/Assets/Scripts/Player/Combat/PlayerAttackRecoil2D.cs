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
        [SerializeField, Min(0f)] private float unarmedWeight = 0.8f;
        [SerializeField, Min(0f)] private float recoilCooldown = 0.03f;

        [Header("Down Attack Recoil")]
        [SerializeField] private bool enableDownAttackRecoil = true;
        [SerializeField, Min(0f)] private float downAttackUpwardRecoil = 6f;

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
        public bool TryApplyMeleeHitRecoil(AttackDirection2D attackDirection, int attackFacingDirection, WeaponData equippedWeapon)
        {
            Debug.Log($"TryApplyMeleeHitRecoil called: direction={attackDirection}, facing={attackFacingDirection}, weapon={(equippedWeapon != null ? equippedWeapon.WeaponName : "None")}");
            if (!enableMeleeHitRecoil || core == null || core.Body == null)
            {
                return false;
            }

            if (Time.time < _nextAllowedRecoilTime)
            {
                return false;
            }

            if (attackDirection == AttackDirection2D.Up)
            {
                // attack up should not go recoil
                return false;
            }
            Vector2 velocity = core.Body.linearVelocity;
            if (attackDirection == AttackDirection2D.Down)
            {
                if (!enableDownAttackRecoil)
                {
                    // Down attack recoil is disabled.
                    return false;
                }
                velocity.y += downAttackUpwardRecoil;
                Debug.Log($"Applying down attack recoil: upwardRecoil={downAttackUpwardRecoil}");
            }
            else
            {
                int facing = attackFacingDirection >= 0 ? 1 : -1;
                float weight = equippedWeapon != null ? equippedWeapon.Weight : unarmedWeight;
                float horizontalRecoil = baseHorizontalRecoil - weight * 1.5f;
                velocity.x += -facing * horizontalRecoil;
                Debug.Log($"Applying melee hit recoil: direction={attackDirection}, facing={facing}, weight={weight}, horizontalRecoil={horizontalRecoil}");
            }

            core.Body.linearVelocity = velocity;
            _nextAllowedRecoilTime = Time.time + recoilCooldown;
            return true;
        }

        private void OnValidate()
        {
            baseHorizontalRecoil = Mathf.Max(0f, baseHorizontalRecoil);
            unarmedWeight = Mathf.Max(0f, unarmedWeight);
            recoilCooldown = Mathf.Max(0f, recoilCooldown);
            downAttackUpwardRecoil = Mathf.Max(0f, downAttackUpwardRecoil);
        }
    }
}
