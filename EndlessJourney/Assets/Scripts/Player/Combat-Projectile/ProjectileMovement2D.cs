using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Minimal straight-line projectile movement.
    /// Moves projectile along its local right axis.
    /// </summary>
    public class ProjectileMovement2D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 14f;
        [SerializeField] private bool useFixedUpdate = false;
        [SerializeField] private bool ignoreTimeScale = false;

        [Header("Direction")]
        [Tooltip("If true, direction is locked at startup and no longer follows transform rotation changes.")]
        [SerializeField] private bool lockDirectionOnAwake = true;

        private Vector2 _lockedDirection = Vector2.right;

        private void Awake()
        {
            if (lockDirectionOnAwake)
            {
                _lockedDirection = transform.right.normalized;
            }
        }

        private void Update()
        {
            if (useFixedUpdate)
            {
                return;
            }

            MoveStep(GetDeltaTime());
        }

        private void FixedUpdate()
        {
            if (!useFixedUpdate)
            {
                return;
            }

            float dt = ignoreTimeScale ? Time.fixedUnscaledDeltaTime : Time.fixedDeltaTime;
            MoveStep(dt);
        }

        private void MoveStep(float deltaTime)
        {
            if (moveSpeed <= 0f || deltaTime <= 0f)
            {
                return;
            }

            Vector2 direction = lockDirectionOnAwake
                ? _lockedDirection
                : (Vector2)transform.right.normalized;

            transform.position += (Vector3)(direction * moveSpeed * deltaTime);
        }

        private float GetDeltaTime()
        {
            return ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
        }
    }
}
