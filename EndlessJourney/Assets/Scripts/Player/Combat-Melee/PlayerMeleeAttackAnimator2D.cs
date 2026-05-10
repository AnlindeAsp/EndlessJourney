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

        [Header("Event Source")]
        [Tooltip("Disable this when a PlayerMeleeAttackAnimationController2D is responsible for calling this animator.")]
        [SerializeField] private bool listenToMeleeAttackEvents = true;

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

        [Header("Forward Attack Transform Change")]
        [SerializeField] private bool applyForwardAttackTransformChange;
        [SerializeField] private Transform forwardAttackTransform;
        [SerializeField] private Vector2 forwardAttackPositionOffset;
        [SerializeField] private float forwardAttackRotationZ;
        [SerializeField] private Vector2 forwardAttackScale = Vector2.one;

        [Header("Up Attack Transform Change")]
        [SerializeField] private bool applyUpAttackTransformChange;
        [SerializeField] private Transform upAttackTransform;
        [SerializeField] private Vector2 upAttackPositionOffset;
        [SerializeField] private float upAttackRotationZ;
        [SerializeField] private Vector2 upAttackScale = Vector2.one;

        [Header("Range Scaling")]
        [SerializeField] private bool scaleWithAttackRange;
        [SerializeField] private Transform scaledTransform;
        [SerializeField, Min(0.01f)] private float referenceAttackRange = 1f;
        [SerializeField] private bool captureBaseScaleOnAwake = true;
        [SerializeField] private Vector3 baseLocalScale = Vector3.one;
        [SerializeField, Min(0.01f)] private float minScaleMultiplier = 0.25f;
        [SerializeField, Min(0.01f)] private float maxScaleMultiplier = 3f;
        [SerializeField] private bool scaleWithAttackDirection;
        [SerializeField] private Vector3 forwardScaleMultiplier = Vector3.one;
        [SerializeField] private Vector3 upScaleMultiplier = Vector3.one;
        [SerializeField] private Vector3 downScaleMultiplier = Vector3.one;

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
        private Vector3 _attackBaseLocalPosition;
        private Quaternion _attackBaseLocalRotation = Quaternion.identity;
        private bool _warnedUnsafePivotMirror;

        public bool ListenToMeleeAttackEvents => listenToMeleeAttackEvents;

        private void Awake()
        {
            RebuildHashes();
            CaptureMirrorPivotScale();
            CaptureMirrorTargetPosition();
            CaptureAttackBaseTransform();
            CaptureBaseScaleIfNeeded();
        }

        private void OnEnable()
        {
            if (!listenToMeleeAttackEvents)
            {
                return;
            }

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

        public void PlayAttackAnimation(AttackDirection2D attackDirection, int facingDirection)
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
            ApplyAttackTransformChange(attackDirection);
            ApplyAttackScale(attackDirection, facingDirection);

            if (resetAttackTriggersBeforePlay)
            {
                ResetAttackTriggers();
            }

            animator.SetTrigger(GetTriggerHash(attackDirection));
        }

        private void HandleAttackStarted(AttackDirection2D attackDirection, int facingDirection)
        {
            PlayAttackAnimation(attackDirection, facingDirection);
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

        private void ApplyAttackTransformChange(AttackDirection2D attackDirection)
        {
            if (!HasAnyAttackTransformChange())
            {
                return;
            }

            Transform targetTransform = ResolveAttackTransformTarget();
            if (targetTransform == null)
            {
                return;
            }

            targetTransform.localPosition = _attackBaseLocalPosition;
            targetTransform.localRotation = _attackBaseLocalRotation;
            switch (attackDirection)
            {
                case AttackDirection2D.Up:
                    ApplyTransformOffset(targetTransform, applyUpAttackTransformChange, upAttackPositionOffset, upAttackRotationZ);
                    break;
                case AttackDirection2D.Down:
                    ApplyMirroredUpAttackTransformOverXAxis(targetTransform);
                    break;
                default:
                    ApplyTransformOffset(targetTransform, applyForwardAttackTransformChange, forwardAttackPositionOffset, forwardAttackRotationZ);
                    break;
            }
        }

        private void ApplyTransformOffset(Transform targetTransform, bool shouldApply, Vector2 positionOffset, float rotationZ)
        {
            if (!shouldApply || targetTransform == null)
            {
                return;
            }

            targetTransform.localPosition = _attackBaseLocalPosition + new Vector3(positionOffset.x, positionOffset.y, 0f);
            targetTransform.localRotation = _attackBaseLocalRotation * Quaternion.Euler(0f, 0f, rotationZ);
        }

        private void ApplyMirroredUpAttackTransformOverXAxis(Transform targetTransform)
        {
            ApplyTransformOffset(
                targetTransform,
                applyUpAttackTransformChange,
                MirrorPositionOffsetOverXAxis(upAttackPositionOffset),
                -upAttackRotationZ);
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

            targetRenderer.flipX = ShouldMirrorForFacing(facingDirection);
        }

        private void ApplyPivotMirror(int facingDirection)
        {
            Transform targetPivot = mirrorPivotTransform != null ? mirrorPivotTransform : transform;
            if (IsUnsafePivotMirrorTarget(targetPivot))
            {
                if (!_warnedUnsafePivotMirror)
                {
                    Debug.LogWarning("PlayerMeleeAttackAnimator2D PivotScaleX mirror is targeting the player/root hierarchy. Use a visual-only pivot child or PositionAroundPivotX to avoid changing melee hitbox space.", this);
                    _warnedUnsafePivotMirror = true;
                }

                return;
            }

            bool shouldMirror = ShouldMirrorForFacing(facingDirection);
            float sign = shouldMirror ? -1f : 1f;

            Vector3 scale = _baseMirrorPivotScale;
            scale.x = Mathf.Abs(scale.x) * sign;
            targetPivot.localScale = scale;
        }

        private bool IsUnsafePivotMirrorTarget(Transform targetPivot)
        {
            if (targetPivot == null || meleeAttack == null)
            {
                return false;
            }

            Transform meleeRoot = meleeAttack.transform;
            return targetPivot == meleeRoot || meleeRoot.IsChildOf(targetPivot);
        }

        private void ApplyPositionMirror(int facingDirection)
        {
            Transform targetTransform = ResolveMirrorTargetTransform();
            Transform pivotTransform = mirrorPivotTransform != null ? mirrorPivotTransform : targetTransform.parent;
            if (targetTransform == null || pivotTransform == null)
            {
                return;
            }

            bool shouldMirror = ShouldMirrorForFacing(facingDirection);
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

        private bool ShouldMirrorForFacing(int facingDirection)
        {
            bool facingRight = facingDirection >= 0;
            return sourceArtFacesLeft ? facingRight : !facingRight;
        }

        private void ApplyAttackScale(AttackDirection2D attackDirection, int facingDirection)
        {
            if (!scaleWithAttackRange && !scaleWithAttackDirection && !HasAnyAttackTransformChange() && !IsUpDownAttack(attackDirection))
            {
                return;
            }

            Transform targetTransform = ResolveAttackTransformTarget();
            if (targetTransform == null)
            {
                return;
            }

            float rangeMultiplier = 1f;
            if (scaleWithAttackRange && combatCore != null)
            {
                rangeMultiplier = combatCore.AttackRange / Mathf.Max(0.01f, referenceAttackRange);
                rangeMultiplier = Mathf.Clamp(rangeMultiplier, minScaleMultiplier, Mathf.Max(minScaleMultiplier, maxScaleMultiplier));
            }

            Vector3 directionMultiplier = scaleWithAttackDirection ? GetDirectionScaleMultiplier(attackDirection) : Vector3.one;
            Vector3 transformMultiplier = GetAttackTransformScaleMultiplier(attackDirection);
            float xSign = GetBaseScaleXSign(targetTransform, attackDirection, facingDirection);
            targetTransform.localScale = new Vector3(
                Mathf.Abs(baseLocalScale.x) * rangeMultiplier * directionMultiplier.x * transformMultiplier.x * xSign,
                baseLocalScale.y * rangeMultiplier * directionMultiplier.y * transformMultiplier.y,
                baseLocalScale.z * rangeMultiplier * directionMultiplier.z * transformMultiplier.z);
        }

        private float GetBaseScaleXSign(Transform targetTransform, AttackDirection2D attackDirection, int facingDirection)
        {
            float sign = baseLocalScale.x < 0f ? -1f : 1f;

            if (!mirrorWithAttackFacing || mirrorMode != AttackAnimationMirrorMode2D.PivotScaleX)
            {
                return sign;
            }

            if (!mirrorVerticalAttacks && attackDirection != AttackDirection2D.Forward)
            {
                return sign;
            }

            Transform mirrorTarget = mirrorPivotTransform != null ? mirrorPivotTransform : transform;
            if (targetTransform != mirrorTarget)
            {
                return sign;
            }

            return ShouldMirrorForFacing(facingDirection) ? -sign : sign;
        }

        private Vector3 GetDirectionScaleMultiplier(AttackDirection2D attackDirection)
        {
            switch (attackDirection)
            {
                case AttackDirection2D.Up:
                    return upScaleMultiplier;
                case AttackDirection2D.Down:
                    return downScaleMultiplier;
                default:
                    return forwardScaleMultiplier;
            }
        }

        private Vector3 GetAttackTransformScaleMultiplier(AttackDirection2D attackDirection)
        {
            Vector3 multiplier;
            switch (attackDirection)
            {
                case AttackDirection2D.Up:
                    multiplier = applyUpAttackTransformChange
                        ? new Vector3(upAttackScale.x, upAttackScale.y, 1f)
                        : Vector3.one;
                    return ApplyUpDownYAxisMirrorIfNeeded(attackDirection, multiplier);
                case AttackDirection2D.Down:
                    multiplier = GetUpAttackScaleMirroredOverXAxis();
                    return ApplyUpDownYAxisMirrorIfNeeded(attackDirection, multiplier);
                default:
                    return applyForwardAttackTransformChange
                        ? new Vector3(forwardAttackScale.x, forwardAttackScale.y, 1f)
                        : Vector3.one;
            }
        }

        private bool HasAnyAttackTransformChange()
        {
            return applyForwardAttackTransformChange || applyUpAttackTransformChange;
        }

        private static Vector2 MirrorPositionOffsetOverXAxis(Vector2 positionOffset)
        {
            return new Vector2(positionOffset.x, -positionOffset.y);
        }

        private Vector3 GetUpAttackScaleMirroredOverXAxis()
        {
            return applyUpAttackTransformChange
                ? new Vector3(upAttackScale.x, -upAttackScale.y, 1f)
                : Vector3.one;
        }

        private Vector3 ApplyUpDownYAxisMirrorIfNeeded(AttackDirection2D attackDirection, Vector3 multiplier)
        {
            if (!IsUpDownAttack(attackDirection))
            {
                return multiplier;
            }

            multiplier.x *= -1f;
            return multiplier;
        }

        private static bool IsUpDownAttack(AttackDirection2D attackDirection)
        {
            return attackDirection == AttackDirection2D.Up || attackDirection == AttackDirection2D.Down;
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

        private Transform ResolveAttackTransformTarget()
        {
            if (scaledTransform != null)
            {
                return scaledTransform;
            }

            if (forwardAttackTransform != null)
            {
                return forwardAttackTransform;
            }

            if (upAttackTransform != null)
            {
                return upAttackTransform;
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

            Transform targetTransform = ResolveAttackTransformTarget();
            baseLocalScale = targetTransform.localScale;
        }

        private void CaptureAttackBaseTransform()
        {
            Transform targetTransform = ResolveAttackTransformTarget();
            if (targetTransform == null)
            {
                return;
            }

            _attackBaseLocalPosition = targetTransform.localPosition;
            _attackBaseLocalRotation = targetTransform.localRotation;
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
            ClampAttackScale(ref forwardAttackScale);
            ClampAttackScale(ref upAttackScale);
            RebuildHashes();
        }

        private static void ClampAttackScale(ref Vector2 scale)
        {
            if (Mathf.Abs(scale.x) < 0.01f)
            {
                scale.x = 0.01f;
            }

            if (Mathf.Abs(scale.y) < 0.01f)
            {
                scale.y = 0.01f;
            }
        }
    }
}
