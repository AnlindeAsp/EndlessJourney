using EndlessJourney.Player;
using UnityEngine;

namespace EndlessJourney.Player
{
    public enum AttackAnimationMirrorMode2D
    {
        SpriteFlipX,
        PivotScaleX,
        PositionAroundPivotX
    }

    /// <summary>
    /// Presentation bridge from melee attack logic to the player Animator.
    /// Combat rules stay in PlayerMeleeAttack2D.
    /// </summary>
    public class PlayerMeleeAttackAnimator2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerMeleeAttack2D meleeAttack;
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerCombatCore combatCore;

        [Header("Triggers")]
        [SerializeField] private string forwardAttackTrigger = "AttackForward";
        [SerializeField] private string upAttackTrigger = "AttackUp";
        [SerializeField] private string downAttackTrigger = "AttackDown";
        [SerializeField] private bool resetAttackTriggersBeforePlay = true;

        [Header("Optional Parameters")]
        [SerializeField] private bool setAttackDirectionParameter = true;
        [SerializeField] private string attackDirectionParameter = "AttackDirection";
        [SerializeField] private bool setAttackFacingParameter = true;
        [SerializeField] private string attackFacingParameter = "AttackFacing";

        [Header("Facing")]
        [SerializeField] private bool mirrorWithAttackFacing = true;
        [SerializeField] private AttackAnimationMirrorMode2D mirrorMode = AttackAnimationMirrorMode2D.SpriteFlipX;
        [SerializeField] private SpriteRenderer mirroredSpriteRenderer;
        [SerializeField] private Transform mirrorPivotTransform;
        [SerializeField] private bool sourceArtFacesLeft = true;
        [SerializeField] private bool mirrorVerticalAttacks = true;

        [Header("Range Scaling")]
        [SerializeField] private bool scaleWithAttackRange;
        [SerializeField] private Transform scaledTransform;
        [SerializeField, Min(0.01f)] private float referenceAttackRange = 1f;
        [SerializeField] private bool captureBaseScaleOnAwake = true;
        [SerializeField] private Vector3 baseLocalScale = Vector3.one;
        [SerializeField, Min(0.01f)] private float minScaleMultiplier = 0.25f;
        [SerializeField, Min(0.01f)] private float maxScaleMultiplier = 3f;

        [Header("Debug")]
        [SerializeField] private bool logMissingReferences = true;

        private int _forwardAttackTriggerHash;
        private int _upAttackTriggerHash;
        private int _downAttackTriggerHash;
        private int _attackDirectionParameterHash;
        private int _attackFacingParameterHash;
        private Vector3 _baseMirrorPivotScale = Vector3.one;
        private Vector3 _baseMirrorTargetLocalPosition;
        private Vector3 _baseMirrorTargetWorldPosition;

        private void Awake()
        {
            RebuildHashes();
            CaptureMirrorPivotScale();
            CaptureMirrorTargetPosition();
            CaptureBaseScaleIfNeeded();
        }

        private void OnEnable()
        {
            if (meleeAttack == null || animator == null)
            {
                if (logMissingReferences)
                {
                    Debug.LogError("PlayerMeleeAttackAnimator2D requires PlayerMeleeAttack2D and Animator references assigned in Inspector.", this);
                }

                return;
            }

            meleeAttack.OnAttackStartedWithDirection += HandleAttackStarted;
        }

        private void OnDisable()
        {
            if (meleeAttack != null)
            {
                meleeAttack.OnAttackStartedWithDirection -= HandleAttackStarted;
            }
        }

        private void HandleAttackStarted(AttackDirection2D attackDirection, int facingDirection)
        {
            if (animator == null)
            {
                return;
            }

            if (setAttackDirectionParameter && !string.IsNullOrWhiteSpace(attackDirectionParameter))
            {
                animator.SetInteger(_attackDirectionParameterHash, ToAnimatorDirection(attackDirection));
            }

            if (setAttackFacingParameter && !string.IsNullOrWhiteSpace(attackFacingParameter))
            {
                animator.SetInteger(_attackFacingParameterHash, facingDirection >= 0 ? 1 : -1);
            }

            ApplyFacingMirror(attackDirection, facingDirection);
            ApplyAttackRangeScale();

            if (resetAttackTriggersBeforePlay)
            {
                ResetAttackTriggers();
            }

            animator.SetTrigger(GetTriggerHash(attackDirection));
        }

        private void ApplyFacingMirror(AttackDirection2D attackDirection, int facingDirection)
        {
            if (!mirrorWithAttackFacing)
            {
                return;
            }

            if (!mirrorVerticalAttacks && attackDirection != AttackDirection2D.Forward)
            {
                return;
            }

            if (mirrorMode == AttackAnimationMirrorMode2D.PivotScaleX)
            {
                ApplyPivotMirror(facingDirection);
                return;
            }

            if (mirrorMode == AttackAnimationMirrorMode2D.PositionAroundPivotX)
            {
                ApplyPositionMirror(facingDirection);
                return;
            }

            ApplySpriteMirror(facingDirection);
        }

        private void ApplySpriteMirror(int facingDirection)
        {
            SpriteRenderer targetRenderer = mirroredSpriteRenderer != null
                ? mirroredSpriteRenderer
                : animator != null
                    ? animator.GetComponent<SpriteRenderer>()
                    : GetComponent<SpriteRenderer>();
            if (targetRenderer == null)
            {
                return;
            }

            bool facingRight = facingDirection >= 0;
            targetRenderer.flipX = sourceArtFacesLeft ? facingRight : !facingRight;
        }

        private void ApplyPivotMirror(int facingDirection)
        {
            Transform targetPivot = mirrorPivotTransform != null ? mirrorPivotTransform : transform;
            bool facingRight = facingDirection >= 0;
            bool shouldMirror = sourceArtFacesLeft ? facingRight : !facingRight;
            float sign = shouldMirror ? -1f : 1f;

            Vector3 scale = _baseMirrorPivotScale;
            scale.x = Mathf.Abs(scale.x) * sign;
            targetPivot.localScale = scale;
        }

        private void ApplyPositionMirror(int facingDirection)
        {
            Transform targetTransform = ResolveMirrorTargetTransform();
            Transform pivotTransform = mirrorPivotTransform != null ? mirrorPivotTransform : targetTransform.parent;
            if (targetTransform == null || pivotTransform == null)
            {
                return;
            }

            bool facingRight = facingDirection >= 0;
            bool shouldMirror = sourceArtFacesLeft ? facingRight : !facingRight;
            if (targetTransform.parent == pivotTransform.parent)
            {
                Vector3 position = _baseMirrorTargetLocalPosition;
                if (shouldMirror)
                {
                    position.x = (pivotTransform.localPosition.x * 2f) - _baseMirrorTargetLocalPosition.x;
                }

                targetTransform.localPosition = position;
                return;
            }

            Vector3 worldPosition = _baseMirrorTargetWorldPosition;
            if (shouldMirror)
            {
                worldPosition.x = (pivotTransform.position.x * 2f) - _baseMirrorTargetWorldPosition.x;
            }

            targetTransform.position = worldPosition;
        }

        private void ApplyAttackRangeScale()
        {
            if (!scaleWithAttackRange || combatCore == null)
            {
                return;
            }

            Transform targetTransform = scaledTransform != null ? scaledTransform : transform;
            float multiplier = combatCore.AttackRange / Mathf.Max(0.01f, referenceAttackRange);
            multiplier = Mathf.Clamp(multiplier, minScaleMultiplier, Mathf.Max(minScaleMultiplier, maxScaleMultiplier));
            float currentXSign = targetTransform.localScale.x < 0f ? -1f : 1f;
            targetTransform.localScale = new Vector3(
                Mathf.Abs(baseLocalScale.x) * multiplier * currentXSign,
                baseLocalScale.y * multiplier,
                baseLocalScale.z * multiplier);
        }

        private void CaptureMirrorPivotScale()
        {
            Transform targetPivot = mirrorPivotTransform != null ? mirrorPivotTransform : transform;
            _baseMirrorPivotScale = targetPivot.localScale;
        }

        private void CaptureMirrorTargetPosition()
        {
            Transform targetTransform = ResolveMirrorTargetTransform();
            if (targetTransform == null)
            {
                return;
            }

            _baseMirrorTargetLocalPosition = targetTransform.localPosition;
            _baseMirrorTargetWorldPosition = targetTransform.position;
        }

        private Transform ResolveMirrorTargetTransform()
        {
            if (scaledTransform != null)
            {
                return scaledTransform;
            }

            if (animator != null)
            {
                return animator.transform;
            }

            return transform;
        }

        private void CaptureBaseScaleIfNeeded()
        {
            if (!captureBaseScaleOnAwake)
            {
                return;
            }

            Transform targetTransform = scaledTransform != null ? scaledTransform : transform;
            baseLocalScale = targetTransform.localScale;
        }

        private void ResetAttackTriggers()
        {
            if (!string.IsNullOrWhiteSpace(forwardAttackTrigger))
            {
                animator.ResetTrigger(_forwardAttackTriggerHash);
            }

            if (!string.IsNullOrWhiteSpace(upAttackTrigger))
            {
                animator.ResetTrigger(_upAttackTriggerHash);
            }

            if (!string.IsNullOrWhiteSpace(downAttackTrigger))
            {
                animator.ResetTrigger(_downAttackTriggerHash);
            }
        }

        private int GetTriggerHash(AttackDirection2D attackDirection)
        {
            switch (attackDirection)
            {
                case AttackDirection2D.Up:
                    return _upAttackTriggerHash;
                case AttackDirection2D.Down:
                    return _downAttackTriggerHash;
                default:
                    return _forwardAttackTriggerHash;
            }
        }

        private static int ToAnimatorDirection(AttackDirection2D attackDirection)
        {
            switch (attackDirection)
            {
                case AttackDirection2D.Up:
                    return 1;
                case AttackDirection2D.Down:
                    return -1;
                default:
                    return 0;
            }
        }

        private void RebuildHashes()
        {
            _forwardAttackTriggerHash = Animator.StringToHash(forwardAttackTrigger);
            _upAttackTriggerHash = Animator.StringToHash(upAttackTrigger);
            _downAttackTriggerHash = Animator.StringToHash(downAttackTrigger);
            _attackDirectionParameterHash = Animator.StringToHash(attackDirectionParameter);
            _attackFacingParameterHash = Animator.StringToHash(attackFacingParameter);
        }

        private void OnValidate()
        {
            forwardAttackTrigger = string.IsNullOrWhiteSpace(forwardAttackTrigger) ? "AttackForward" : forwardAttackTrigger.Trim();
            upAttackTrigger = string.IsNullOrWhiteSpace(upAttackTrigger) ? "AttackUp" : upAttackTrigger.Trim();
            downAttackTrigger = string.IsNullOrWhiteSpace(downAttackTrigger) ? "AttackDown" : downAttackTrigger.Trim();
            attackDirectionParameter = string.IsNullOrWhiteSpace(attackDirectionParameter) ? "AttackDirection" : attackDirectionParameter.Trim();
            attackFacingParameter = string.IsNullOrWhiteSpace(attackFacingParameter) ? "AttackFacing" : attackFacingParameter.Trim();
            referenceAttackRange = Mathf.Max(0.01f, referenceAttackRange);
            minScaleMultiplier = Mathf.Max(0.01f, minScaleMultiplier);
            maxScaleMultiplier = Mathf.Max(minScaleMultiplier, maxScaleMultiplier);
            RebuildHashes();
        }
    }
}
