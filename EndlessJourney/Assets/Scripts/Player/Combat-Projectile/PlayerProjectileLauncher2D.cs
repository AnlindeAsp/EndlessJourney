using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Projectile spawn/launch coordinator.
    /// Only handles spawn position/orientation and projectile initialization.
    /// Projectile movement should be handled by a separate component.
    /// </summary>
    public class PlayerProjectileLauncher2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCore2D ownerCore;
        [SerializeField] private Transform firePoint;

        [Header("Spawn")]
        [SerializeField] private Vector2 localSpawnOffset = new Vector2(0.8f, 0.2f);
        [SerializeField] private bool faceByOwnerFacingDirection = true;

        [Header("Debug")]
        [SerializeField] private bool logLaunch;

        private void Reset()
        {
            ownerCore = GetComponent<PlayerCore2D>();
            firePoint = transform;
        }

        private void Awake()
        {
            if (ownerCore == null)
            {
                ownerCore = GetComponent<PlayerCore2D>();
            }

            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        /// <summary>
        /// Launches projectile using owner facing direction when available.
        /// </summary>
        public bool TryLaunchByFacing(PlayerProjectile2D projectilePrefab, float projectileDamage, float projectileLifeTime)
        {
            int facing = ownerCore != null ? ownerCore.FacingDirection : 1;
            Vector2 direction = facing >= 0 ? Vector2.right : Vector2.left;
            return TryLaunch(projectilePrefab, direction, projectileDamage, projectileLifeTime);
        }

        /// <summary>
        /// Launches projectile in provided normalized direction.
        /// </summary>
        public bool TryLaunch(PlayerProjectile2D projectilePrefab, Vector2 direction, float projectileDamage, float projectileLifeTime)
        {
            if (projectilePrefab == null)
            {
                return false;
            }

            Vector2 launchDirection = direction.sqrMagnitude <= 0.0001f ? Vector2.right : direction.normalized;
            Vector3 spawnPosition = ResolveSpawnPosition(launchDirection);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.right, launchDirection);

            PlayerProjectile2D projectile = Instantiate(projectilePrefab, spawnPosition, rotation);
            projectile.InitializeProjectile(
                gameObject,
                true,
                Mathf.Max(0f, projectileDamage),
                true,
                Mathf.Max(0.01f, projectileLifeTime));

            if (logLaunch)
            {
                Debug.Log($"Projectile launched dir={launchDirection}", this);
            }

            return true;
        }

        private Vector3 ResolveSpawnPosition(Vector2 launchDirection)
        {
            Transform spawnRoot = firePoint != null ? firePoint : transform;
            Vector3 basePosition = spawnRoot.position;

            if (!faceByOwnerFacingDirection)
            {
                return basePosition + (Vector3)localSpawnOffset;
            }

            float facing = launchDirection.x >= 0f ? 1f : -1f;
            Vector3 offset = new Vector3(localSpawnOffset.x * facing, localSpawnOffset.y, 0f);
            return basePosition + offset;
        }
    }
}
