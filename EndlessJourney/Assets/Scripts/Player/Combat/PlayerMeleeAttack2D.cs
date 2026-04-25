using System;
using System.Collections.Generic;
using EndlessJourney.Combat;
using EndlessJourney.Interfaces;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Lightweight melee attack module.
    /// - Reads attack input edge from PlayerInput2D
    /// - Opens/closes an attack window
    /// - Uses one assigned collider as the real melee hitbox
    /// </summary>
    // [RequireComponent(typeof(PlayerCore2D))]
    [RequireComponent(typeof(PlayerCombatCore))]
    public class PlayerMeleeAttack2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCore2D core;
        [SerializeField] private PlayerCombatCore combatCore;
        [SerializeField] private PlayerWeaponSystem weaponSystem;
        [SerializeField] private PlayerAttackRecoil2D attackRecoil;
        [SerializeField] private PlayerAttackDirectionResolver2D directionResolver;

        [Header("Hitbox")]
        [Tooltip("This collider is the real melee attack range.")]
        [SerializeField] private Collider2D hitboxCollider;
        [SerializeField] private bool disableHitboxWhenInactive = true;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private bool includeTriggerTargets = false;
        [SerializeField] private bool syncPhysicsTransformsBeforeHitScan = true;

        [Header("Hitbox Pose")]
        [Tooltip("Local offset applied to hitbox. X is mirrored by facing direction.")]
        [SerializeField] private Vector2 hitboxOffset = new Vector2(0.5f, 0f);
        [Tooltip("Runtime scale multiplier applied on top of hitbox base local scale.")]
        [SerializeField] private Vector2 hitboxScaleMultiplier = Vector2.one;
        [SerializeField] private bool lockFacingAtAttackStart = true;
        [SerializeField] private bool enableUpAttack = true;
        [SerializeField] private Vector2 upAttackHitboxOffset = new Vector2(0f, 0.9f);
        [SerializeField] private Vector2 upAttackHitboxScaleMultiplier = new Vector2(1f, 1f);

        [Header("Attack Rules")]
        [SerializeField] private bool enableMeleeAttack = true;
        [SerializeField] private bool requireUsableWeapon = true;
        [SerializeField] private bool allowAttackWhileMovementLocked = true;
        [SerializeField, Min(0.01f)] private float fallbackAttackInterval = 0.25f;
        [SerializeField, Min(0.01f)] private float minimumAttackInterval = 0.08f;
        [Tooltip("When enabled, attack interval uses PlayerCombatCore.AttackSpeed.")]
        [SerializeField] private bool useCombatAttackSpeedAsInterval = true;
        [Tooltip("How long one melee hitbox stays active.")]
        [SerializeField, Min(0.01f)] private float attackActiveDuration = 0.18f;

        [Header("Debug")]
        [SerializeField] private bool drawHitboxGizmo = true;
        [SerializeField] private bool logHitDebug = true;
        [SerializeField] private bool logAttackBlockReason = true;
        [SerializeField] private bool logAttackSuccess = true;

        private readonly Collider2D[] _overlapResults = new Collider2D[32];
        private readonly HashSet<int> _hitTargetIds = new HashSet<int>();

        private float _cooldownTimer;
        private float _attackActiveTimer;
        private bool _isAttackActive;
        private int _attackFacingDirection = 1;
        private AttackDirection2D _attackDirection = AttackDirection2D.Forward;
        private bool _hasAppliedHitRecoilThisAttack;
        private bool _loggedNoTargetThisAttack;

        private Vector3 _hitboxBaseLocalPosition;
        private Vector3 _hitboxBaseLocalScale = Vector3.one;

        /// <summary>True when attack input is currently blocked by cooldown.</summary>
        public bool IsOnCooldown => _cooldownTimer > 0f;

        /// <summary>Seconds remaining before another attack can start.</summary>
        public float CooldownRemaining => Mathf.Max(0f, _cooldownTimer);

        /// <summary>True when melee hitbox is currently active.</summary>
        public bool IsAttackActive => _isAttackActive;

        /// <summary>Raised when one melee attack action starts.</summary>
        public event Action OnAttackStarted;

        /// <summary>Raised when a target takes damage from this melee attack.</summary>
        public event Action<GameObject, float> OnTargetHit;
        private Quaternion _hitboxBaseLocalRotation;

        private void Awake()
        {
            if (attackRecoil == null)
            {
                attackRecoil = GetComponent<PlayerAttackRecoil2D>();
            }
            if (directionResolver == null)
            {
                directionResolver = GetComponent<PlayerAttackDirectionResolver2D>();
            }

            if (core == null || combatCore == null || hitboxCollider == null)
            {
                Debug.LogError("PlayerMeleeAttack2D is missing required references. Please assign core, combatCore, and hitboxCollider manually in Inspector.");
                enabled = false;
                return;
            }

            if (hitboxCollider != null && !hitboxCollider.isTrigger)
            {
                Debug.LogWarning("PlayerMeleeAttack2D hitboxCollider is recommended to be trigger.");
            }

            Transform zoneTransform = hitboxCollider.transform;
            _hitboxBaseLocalPosition = zoneTransform.localPosition;
            _hitboxBaseLocalScale = zoneTransform.localScale;
            _hitboxBaseLocalRotation = hitboxCollider.transform.localRotation;

            SetHitboxActive(false, core.FacingDirection);
        }

        private void OnDisable()
        {
            _isAttackActive = false;
            _attackActiveTimer = 0f;
            _attackDirection = AttackDirection2D.Forward;
            _hasAppliedHitRecoilThisAttack = false;
            SetHitboxActive(false, _attackFacingDirection);
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);
            }

            if (_isAttackActive)
            {
                TickActiveAttack(Time.deltaTime);
            }

            if (!enableMeleeAttack)
            {
                return;
            }

            if (!core.Input.AttackPressedThisFrame)
            {
                return;
            }

            TryStartAttack();
        }

        private void TryStartAttack()
        {
            if (_cooldownTimer > 0f || _isAttackActive)
            {
                return;
            }

            if (!allowAttackWhileMovementLocked && core.IsMovementLocked)
            {
                return;
            }

            if (hitboxCollider == null)
            {
                if (logAttackBlockReason)
                {
                    Debug.LogWarning("Melee blocked: no hitboxCollider assigned/found.");
                }
                return;
            }

            if (requireUsableWeapon && weaponSystem != null && !weaponSystem.CanUseEquippedWeapon())
            {
                // Allow combat-stat testing even when weapon is not usable.
                if (combatCore.AttackDamagePerHit <= 0f)
                {
                    if (logAttackBlockReason)
                    {
                        Debug.LogWarning("Melee blocked: weapon is not usable and AttackDamagePerHit <= 0.");
                    }
                    return;
                }
            }

            float damagePerHit = Mathf.Max(0f, combatCore.AttackDamagePerHit);
            if (damagePerHit <= 0f)
            {
                if (logAttackBlockReason)
                {
                    Debug.LogWarning("Melee blocked: AttackDamagePerHit is 0.");
                }
                return;
            }

            StartAttackWindow();
            _cooldownTimer = ResolveAttackInterval();
            OnAttackStarted?.Invoke();
        }

        private void StartAttackWindow()
        {
            _isAttackActive = true;
            _attackActiveTimer = Mathf.Max(0.01f, attackActiveDuration);
            _attackFacingDirection = core.FacingDirection;
            _attackDirection = ResolveAttackDirection();
            _hasAppliedHitRecoilThisAttack = false;
            _loggedNoTargetThisAttack = false;
            _hitTargetIds.Clear();

            SetHitboxActive(true, _attackFacingDirection);
            PerformHitScan();

            if (logAttackSuccess)
            {
                Debug.Log($"Melee attack started: dir={_attackDirection}, facing={_attackFacingDirection}, duration={_attackActiveTimer:0.##}s");
            }
        }

        private void TickActiveAttack(float deltaTime)
        {
            if (!_isAttackActive)
            {
                return;
            }

            if (!lockFacingAtAttackStart)
            {
                _attackFacingDirection = core.FacingDirection;
            }

            ApplyHitboxPoseAndScale(_attackFacingDirection, _attackDirection);
            PerformHitScan();

            _attackActiveTimer -= deltaTime;
            if (_attackActiveTimer > 0f)
            {
                return;
            }

            _isAttackActive = false;
            _attackActiveTimer = 0f;
            SetHitboxActive(false, _attackFacingDirection);
        }

        private void PerformHitScan()
        {
            if (!_isAttackActive || hitboxCollider == null || !hitboxCollider.enabled)
            {
                return;
            }

            ContactFilter2D filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = targetLayers,
                useTriggers = includeTriggerTargets
            };

            if (syncPhysicsTransformsBeforeHitScan)
            {
                Physics2D.SyncTransforms();
            }

            int hitCount = hitboxCollider.Overlap(filter, _overlapResults);
            if (hitCount <= 0 && logHitDebug && !_loggedNoTargetThisAttack)
            {
                _loggedNoTargetThisAttack = true;
                Debug.Log(
                    $"Melee hit scan found no target. attackDir={_attackDirection}, facing={_attackFacingDirection}, " +
                    $"targetLayers={targetLayers.value}, includeTriggers={includeTriggerTargets}.");
            }

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D other = _overlapResults[i];
                if (other == null)
                {
                    continue;
                }

                if (IsSelfCollider(other)){
                    continue;
                }

                if (!TryResolveTargetRoot(other, out GameObject targetRoot))
                {
                    continue;
                }

                int targetId = targetRoot.GetInstanceID();
                if (_hitTargetIds.Contains(targetId))
                {
                    continue;
                }

                float totalDamageApplied = TryApplyDamage(other, targetRoot);
                if (totalDamageApplied <= 0f)
                {
                    if (logHitDebug)
                    {
                        Debug.Log($"Melee overlap but no damage applied: target=[{targetRoot.name}] collider=[{other.name}]");
                    }
                    continue;
                }

                _hitTargetIds.Add(targetId);
                TryApplyHitRecoilOnce();

                if (logHitDebug)
                {
                    Debug.Log($"hi target -> hit [{targetRoot.name}], damage={totalDamageApplied:0.##}");
                }

                OnTargetHit?.Invoke(targetRoot, totalDamageApplied);
            }
        }

        private float TryApplyDamage(Collider2D col, GameObject targetRoot)
        {
            ResolveReceivers(targetRoot, out IHittable hittable, out IDamageable2D damageable, out PlayerHealth2D health);
            int hitRepeats = Mathf.Max(1, combatCore.AttackHitCount);
            float damagePerHit = Mathf.Max(0f, combatCore.AttackDamagePerHit);
            float totalApplied = 0f;

            Vector2 hitDirection = ResolveHitDirection();
            Vector2 hitPoint = col.ClosestPoint(transform.position);

            if (hittable != null)
            {
                for (int i = 0; i < hitRepeats; i++)
                {
                    HitContext context = new HitContext(
                        gameObject,
                        hitboxCollider,
                        hitPoint,
                        hitDirection,
                        damagePerHit,
                        HitType.Melee);

                    HitResult result = hittable.ReceiveHit(context);
                    if (result.WasApplied)
                    {
                        totalApplied += result.DamageApplied > 0f ? result.DamageApplied : damagePerHit;
                    }
                }

                return totalApplied;
            }

            if (damageable != null)
            {
                for (int i = 0; i < hitRepeats; i++)
                {
                    if (damageable.ReceiveDamage(damagePerHit, gameObject))
                    {
                        totalApplied += damagePerHit;
                    }
                }

                return totalApplied;
            }

            if (health == null || health.gameObject == gameObject)
            {
                return 0f;
            }

            for (int i = 0; i < hitRepeats; i++)
            {
                if (health.TakeDamage(damagePerHit))
                {
                    totalApplied += damagePerHit;
                }
            }

            return totalApplied;
        }

        private void TryApplyHitRecoilOnce()
        {
            if (_hasAppliedHitRecoilThisAttack || attackRecoil == null)
            {
                return;
            }

            WeaponData weapon = weaponSystem != null ? weaponSystem.EquippedWeapon : null;
            if (attackRecoil.TryApplyMeleeHitRecoil(_attackDirection, _attackFacingDirection, weapon))
            {
                _hasAppliedHitRecoilThisAttack = true;
            }
        }

        private static void ResolveReceivers(
            GameObject targetRoot,
            out IHittable hittable,
            out IDamageable2D damageable,
            out PlayerHealth2D health)
        {
            hittable = targetRoot.GetComponent(typeof(IHittable)) as IHittable;
            damageable = targetRoot.GetComponent(typeof(IDamageable2D)) as IDamageable2D;
            health = targetRoot.GetComponent<PlayerHealth2D>();

            // Compatibility fallback for targets whose combat receiver lives on parent.
            if (hittable == null)
            {
                hittable = targetRoot.GetComponentInParent(typeof(IHittable)) as IHittable;
            }

            if (damageable == null)
            {
                damageable = targetRoot.GetComponentInParent(typeof(IDamageable2D)) as IDamageable2D;
            }

            if (health == null)
            {
                health = targetRoot.GetComponentInParent<PlayerHealth2D>();
            }
        }

        private bool IsSelfCollider(Collider2D col)
        {
            if (col == hitboxCollider)
            {
                return true;
            }

            if (core.Body != null && col.attachedRigidbody == core.Body)
            {
                return true;
            }

            return col.transform.IsChildOf(transform);
        }

        private static bool TryResolveTargetRoot(Collider2D hitCollider, out GameObject targetRoot)
        {
            if (hitCollider == null)
            {
                targetRoot = null;
                return false;
            }

            targetRoot = hitCollider.attachedRigidbody != null
                ? hitCollider.attachedRigidbody.gameObject
                : hitCollider.gameObject;

            return targetRoot != null;
        }

        private float ResolveAttackInterval()
        {
            if (useCombatAttackSpeedAsInterval && combatCore.AttackSpeed > 0f)
            {
                return Mathf.Max(minimumAttackInterval, combatCore.AttackSpeed);
            }

            return Mathf.Max(minimumAttackInterval, fallbackAttackInterval);
        }

        private AttackDirection2D ResolveAttackDirection()
        {
            AttackDirection2D resolved = directionResolver != null
                ? directionResolver.ResolveDirectionForNewAttack()
                : AttackDirection2D.Forward;

            if (resolved == AttackDirection2D.Up && !enableUpAttack)
            {
                return AttackDirection2D.Forward;
            }

            return resolved;
        }

        private Vector2 ResolveHitDirection()
        {
            switch (_attackDirection)
            {
                case AttackDirection2D.Up:
                    return Vector2.up;
                case AttackDirection2D.Down:
                    return Vector2.down;
                default:
                    return new Vector2(_attackFacingDirection, 0f);
            }
        }

        private void SetHitboxActive(bool active, int facingDirection)
        {
            ApplyHitboxPoseAndScale(facingDirection, _attackDirection);

            if (hitboxCollider != null && disableHitboxWhenInactive)
            {
                hitboxCollider.enabled = active;
            }
        }

        private void ApplyHitboxPoseAndScale(int facingDirection, AttackDirection2D attackDirection)
        {
            if (hitboxCollider == null)
            {
                return;
            }

            Transform zoneTransform = hitboxCollider.transform;
            Vector3 scale = _hitboxBaseLocalScale;
            zoneTransform.localRotation = _hitboxBaseLocalRotation;
            float scaleXMultiplier;
            float scaleYMultiplier;

            if (attackDirection == AttackDirection2D.Up && enableUpAttack)
            {
                zoneTransform.localPosition = _hitboxBaseLocalPosition + new Vector3(
                    upAttackHitboxOffset.x,
                    upAttackHitboxOffset.y + combatCore.AttackRange *0.5f,
                    0f
                );
                zoneTransform.localRotation = _hitboxBaseLocalRotation * Quaternion.Euler(0f, 0f, 90f);

                scaleXMultiplier = Mathf.Max(0.01f, Mathf.Abs(upAttackHitboxScaleMultiplier.x));
                scaleYMultiplier = Mathf.Max(0.01f, Mathf.Abs(upAttackHitboxScaleMultiplier.y));
                scale.x = Mathf.Abs(scale.x) * scaleXMultiplier * combatCore.AttackRange * 0.5f;
                scale.y = Mathf.Abs(scale.y) * scaleYMultiplier * combatCore.AttackRange * 2f;
            }
            else
            {
                float facing = facingDirection >= 0 ? 1f : -1f;
                zoneTransform.localPosition = _hitboxBaseLocalPosition + new Vector3(
                    (hitboxOffset.x + combatCore.AttackRange) * facing,
                    hitboxOffset.y,
                    0f
                );

                scaleXMultiplier = Mathf.Max(0.01f, Mathf.Abs(hitboxScaleMultiplier.x));
                scaleYMultiplier = Mathf.Max(0.01f, Mathf.Abs(hitboxScaleMultiplier.y));
                scale.x = Mathf.Abs(scale.x) * scaleXMultiplier * facing * combatCore.AttackRange;
                scale.y = Mathf.Abs(scale.y) * scaleYMultiplier * combatCore.AttackRange;
            }
            zoneTransform.localScale = scale;
        }

        private void OnValidate()
        {
            fallbackAttackInterval = Mathf.Max(0.01f, fallbackAttackInterval);
            minimumAttackInterval = Mathf.Max(0.01f, minimumAttackInterval);
            attackActiveDuration = Mathf.Max(0.01f, attackActiveDuration);

            if (Mathf.Abs(hitboxScaleMultiplier.x) < 0.01f)
            {
                hitboxScaleMultiplier.x = 0.01f;
            }

            if (Mathf.Abs(hitboxScaleMultiplier.y) < 0.01f)
            {
                hitboxScaleMultiplier.y = 0.01f;
            }

            if (Mathf.Abs(upAttackHitboxScaleMultiplier.x) < 0.01f)
            {
                upAttackHitboxScaleMultiplier.x = 0.01f;
            }

            if (Mathf.Abs(upAttackHitboxScaleMultiplier.y) < 0.01f)
            {
                upAttackHitboxScaleMultiplier.y = 0.01f;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawHitboxGizmo || hitboxCollider == null)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.85f);
            DrawHitboxGizmo(hitboxCollider);
        }

        private static void DrawHitboxGizmo(Collider2D collider)
        {
            if (collider is BoxCollider2D box)
            {
                Matrix4x4 old = Gizmos.matrix;
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.offset, box.size);
                Gizmos.matrix = old;
                return;
            }

            if (collider is CircleCollider2D circle)
            {
                Matrix4x4 old = Gizmos.matrix;
                Gizmos.matrix = circle.transform.localToWorldMatrix;
                Gizmos.DrawWireSphere(circle.offset, circle.radius);
                Gizmos.matrix = old;
                return;
            }

            if (collider is PolygonCollider2D polygon)
            {
                for (int p = 0; p < polygon.pathCount; p++)
                {
                    Vector2[] points = polygon.GetPath(p);
                    if (points == null || points.Length < 2)
                    {
                        continue;
                    }

                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector3 a = polygon.transform.TransformPoint(points[i]);
                        Vector3 b = polygon.transform.TransformPoint(points[(i + 1) % points.Length]);
                        Gizmos.DrawLine(a, b);
                    }
                }

                return;
            }

            Bounds bounds = collider.bounds;
            if (bounds.size.sqrMagnitude > 0f)
            {
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}
