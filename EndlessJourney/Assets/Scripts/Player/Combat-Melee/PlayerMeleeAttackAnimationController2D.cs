using System.Collections;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Coordinates one or two melee attack animation players.
    /// Use this when Dual Wielding should play a secondary attack visual.
    /// </summary>
    public class PlayerMeleeAttackAnimationController2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMeleeAttack2D meleeAttack;
        [SerializeField] private PlayerWeaponSystem weaponSystem;
        [SerializeField] private PlayerMeleeAttackAnimator2D primaryAnimator;
        [SerializeField] private PlayerMeleeAttackAnimator2D secondaryAnimator;

        [Header("Dual Wielding")]
        [SerializeField] private bool playSecondaryOnlyWhenDualWielding = true;
        [SerializeField, Min(0f)] private float secondaryDelay = 0f;

        [Header("Debug")]
        [SerializeField] private bool logSetupWarnings = true;

        private Coroutine _secondaryRoutine;

        private void OnEnable()
        {
            if (meleeAttack == null)
            {
                if (logSetupWarnings)
                {
                    Debug.LogError("PlayerMeleeAttackAnimationController2D requires PlayerMeleeAttack2D assigned in Inspector.", this);
                }

                return;
            }

            WarnIfAnimatorStillListens(primaryAnimator, "Primary");
            WarnIfAnimatorStillListens(secondaryAnimator, "Secondary");
            meleeAttack.OnAttackStartedWithDirection += HandleAttackStarted;
        }

        private void OnDisable()
        {
            if (meleeAttack != null)
            {
                meleeAttack.OnAttackStartedWithDirection -= HandleAttackStarted;
            }

            if (_secondaryRoutine != null)
            {
                StopCoroutine(_secondaryRoutine);
                _secondaryRoutine = null;
            }
        }

        private void HandleAttackStarted(AttackDirection2D attackDirection, int facingDirection)
        {
            primaryAnimator?.PlayAttackAnimation(attackDirection, facingDirection);

            if (!ShouldPlaySecondary())
            {
                return;
            }

            if (_secondaryRoutine != null)
            {
                StopCoroutine(_secondaryRoutine);
                _secondaryRoutine = null;
            }

            if (secondaryDelay <= 0f)
            {
                PlaySecondaryAnimation(attackDirection, facingDirection);
                return;
            }

            _secondaryRoutine = StartCoroutine(PlaySecondaryAfterDelay(attackDirection, facingDirection));
        }

        private bool ShouldPlaySecondary()
        {
            if (secondaryAnimator == null)
            {
                return false;
            }

            if (!playSecondaryOnlyWhenDualWielding)
            {
                return true;
            }

            return weaponSystem != null && weaponSystem.EffectiveWeaponType == WeaponType.DualBlades;
        }

        private IEnumerator PlaySecondaryAfterDelay(AttackDirection2D attackDirection, int facingDirection)
        {
            yield return new WaitForSeconds(secondaryDelay);
            PlaySecondaryAnimation(attackDirection, facingDirection);
            _secondaryRoutine = null;
        }

        private void PlaySecondaryAnimation(AttackDirection2D attackDirection, int facingDirection)
        {
            if (attackDirection == AttackDirection2D.Up || attackDirection == AttackDirection2D.Down)
            {
                // For vertical attacks, flip the secondary animation to mirror the primary, for better visual distinction.
                secondaryAnimator.PlayAttackAnimation(attackDirection, -facingDirection);
                return;
            }

            secondaryAnimator.PlayAttackAnimation(attackDirection, facingDirection);
        }

        private void WarnIfAnimatorStillListens(PlayerMeleeAttackAnimator2D attackAnimator, string label)
        {
            if (!logSetupWarnings || attackAnimator == null || !attackAnimator.ListenToMeleeAttackEvents)
            {
                return;
            }

            Debug.LogWarning($"{label} PlayerMeleeAttackAnimator2D is still listening to melee attack events. Disable its Listen To Melee Attack Events field when this controller drives it.", attackAnimator);
        }

        private void OnValidate()
        {
            secondaryDelay = Mathf.Max(0f, secondaryDelay);
        }
    }
}
