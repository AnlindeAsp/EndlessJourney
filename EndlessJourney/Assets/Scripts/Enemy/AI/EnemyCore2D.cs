using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Shared enemy core context.
    /// Centralizes references and common runtime state so enemy modules
    /// can avoid direct cross-coupling.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyCore2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private EnemyHittable hittable;
        [Tooltip("Visual root to flip. If null, flips this transform.")]
        [SerializeField] private Transform visualRoot;

        [Header("Facing")]
        [Tooltip("Start facing direction: -1 = left, +1 = right.")]
        [SerializeField] private int startFacingDirection = -1;

        private int _facingDirection = -1;
        private Vector2 _spawnPosition;

        /// <summary>Cached Rigidbody2D for enemy movement modules.</summary>
        public Rigidbody2D Body => body;

        /// <summary>Enemy health/damage module reference (optional).</summary>
        public EnemyHittable Hittable => hittable;

        /// <summary>True when enemy is dead.</summary>
        public bool IsDead => hittable != null && hittable.IsDead;

        /// <summary>Current horizontal facing direction (-1 or +1).</summary>
        public int FacingDirection => _facingDirection;

        /// <summary>Initial world position captured at startup.</summary>
        public Vector2 SpawnPosition => _spawnPosition;

        private void Reset()
        {
            body = GetComponent<Rigidbody2D>();
            hittable = GetComponent<EnemyHittable>();
            visualRoot = transform;
        }

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (hittable == null) hittable = GetComponent<EnemyHittable>();
            if (visualRoot == null) visualRoot = transform;

            if (body == null)
            {
                Debug.LogError("EnemyCore2D is missing Rigidbody2D.");
                enabled = false;
                return;
            }

            _spawnPosition = transform.position;
            FaceDirection(startFacingDirection);
        }

        /// <summary>
        /// Sets horizontal facing direction and flips visual root.
        /// </summary>
        public void FaceDirection(int direction)
        {
            _facingDirection = direction >= 0 ? 1 : -1;
            ApplyVisualFacing();
        }

        /// <summary>
        /// Faces toward target world X coordinate.
        /// </summary>
        public void FaceTowardX(float targetWorldX)
        {
            FaceDirection(targetWorldX >= transform.position.x ? 1 : -1);
        }

        /// <summary>
        /// Sets horizontal velocity while preserving vertical velocity.
        /// </summary>
        public void SetHorizontalVelocity(float velocityX)
        {
            if (body == null)
            {
                return;
            }

            Vector2 velocity = body.linearVelocity;
            velocity.x = velocityX;
            body.linearVelocity = velocity;
        }

        /// <summary>
        /// Stops only horizontal movement.
        /// </summary>
        public void StopMovement()
        {
            SetHorizontalVelocity(0f);
        }

        private void ApplyVisualFacing()
        {
            if (visualRoot == null)
            {
                return;
            }

            Vector3 scale = visualRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * _facingDirection;
            visualRoot.localScale = scale;
        }

        private void OnValidate()
        {
            if (startFacingDirection == 0)
            {
                startFacingDirection = 1;
            }
        }
    }
}
