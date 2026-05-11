using System.Collections.Generic;
using EndlessJourney.Combat;
using EndlessJourney.Interfaces;
using UnityEngine;

namespace EndlessJourney.Enemy
{
    /// <summary>
    /// Generic enemy spawner.
    /// Supports manual spawn, spawn on enable, interval spawn, and spawn when hit.
    /// </summary>
    public class EnemySpawner2D : MonoBehaviour, IHittable, IDamageable2D
    {
        [Header("Spawn Prefabs")]
        [SerializeField] private GameObject[] enemyPrefabs;

        [Header("Spawn Points")]
        [Tooltip("If empty, this spawner transform is used as the spawn point.")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private bool chooseRandomSpawnPoint = true;
        [SerializeField] private Vector2 randomPositionOffset;

        [Header("Spawn Rules")]
        [SerializeField] private bool spawnOnEnable;
        [SerializeField, Min(0f)] private float spawnOnEnableDelay;
        [SerializeField] private bool spawnOnInterval;
        [SerializeField, Min(0.01f)] private float spawnInterval = 5f;
        [SerializeField, Min(1)] private int spawnCountPerTrigger = 1;
        [SerializeField, Min(0)] private int maxAliveEnemies = 3;

        [Header("Hit Trigger")]
        [SerializeField] private bool spawnOnHit;
        [SerializeField, Min(0f)] private float hitSpawnCooldown = 0.25f;
        [SerializeField] private bool requireDamageToSpawn = true;

        [Header("Alive Tracking")]
        [SerializeField] private bool removeFromAliveWhenEnemyDies = true;
        [SerializeField] private bool ignoreInactiveAliveEnemies = true;

        [Header("Debug")]
        [SerializeField] private bool logSpawnBlocked;
        [SerializeField] private bool logSpawned;

        private readonly List<GameObject> _aliveEnemies = new List<GameObject>(16);
        private float _intervalTimer;
        private float _spawnOnEnableTimer;
        private float _nextHitSpawnTime;
        private bool _waitingForSpawnOnEnable;

        public int AliveCount
        {
            get
            {
                PruneAliveEnemies();
                return _aliveEnemies.Count;
            }
        }

        private void OnEnable()
        {
            _intervalTimer = spawnInterval;
            _waitingForSpawnOnEnable = spawnOnEnable;
            _spawnOnEnableTimer = spawnOnEnableDelay;
        }

        private void OnDisable()
        {
            _waitingForSpawnOnEnable = false;
        }

        private void Update()
        {
            TickSpawnOnEnable();
            TickIntervalSpawn();
        }

        public void SpawnNow()
        {
            TrySpawn(spawnCountPerTrigger);
        }

        public bool CanBeHit(HitContext context)
        {
            return spawnOnHit && isActiveAndEnabled && Time.time >= _nextHitSpawnTime;
        }

        public HitResult ReceiveHit(HitContext context)
        {
            if (!CanBeHit(context))
            {
                return HitResult.Blocked("Spawner hit trigger is not ready.");
            }

            if (requireDamageToSpawn && context.Damage <= 0f)
            {
                return HitResult.Blocked("Spawner requires positive hit damage.");
            }

            _nextHitSpawnTime = Time.time + hitSpawnCooldown;
            int spawned = TrySpawn(spawnCountPerTrigger);
            return spawned > 0
                ? HitResult.Applied(0f)
                : HitResult.Blocked("Spawner could not spawn enemy.");
        }

        public bool ReceiveDamage(float amount, GameObject source)
        {
            HitContext context = new HitContext(
                source,
                null,
                transform.position,
                Vector2.zero,
                Mathf.Max(0f, amount),
                HitType.Melee);

            return ReceiveHit(context).WasApplied;
        }

        private void TickSpawnOnEnable()
        {
            if (!_waitingForSpawnOnEnable)
            {
                return;
            }

            _spawnOnEnableTimer -= Time.deltaTime;
            if (_spawnOnEnableTimer > 0f)
            {
                return;
            }

            _waitingForSpawnOnEnable = false;
            TrySpawn(spawnCountPerTrigger);
        }

        private void TickIntervalSpawn()
        {
            if (!spawnOnInterval)
            {
                return;
            }

            _intervalTimer -= Time.deltaTime;
            if (_intervalTimer > 0f)
            {
                return;
            }

            _intervalTimer = spawnInterval;
            TrySpawn(spawnCountPerTrigger);
        }

        private int TrySpawn(int requestedCount)
        {
            PruneAliveEnemies();

            if (!CanSpawnAny())
            {
                return 0;
            }

            int spawned = 0;
            int count = Mathf.Max(1, requestedCount);
            for (int i = 0; i < count; i++)
            {
                if (maxAliveEnemies > 0 && _aliveEnemies.Count >= maxAliveEnemies)
                {
                    LogBlocked("max alive enemy count reached.");
                    break;
                }

                GameObject prefab = PickPrefab();
                if (prefab == null)
                {
                    LogBlocked("no enemy prefab assigned.");
                    break;
                }

                Transform spawnPoint = PickSpawnPoint();
                Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
                Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
                spawnPosition += (Vector3)RandomizeOffset();

                GameObject enemy = Instantiate(prefab, spawnPosition, spawnRotation);
                _aliveEnemies.Add(enemy);
                TrySubscribeToEnemyDeath(enemy);
                spawned++;

                if (logSpawned)
                {
                    Debug.Log($"EnemySpawner2D spawned [{enemy.name}] at {spawnPosition}.", this);
                }
            }

            return spawned;
        }

        private bool CanSpawnAny()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                LogBlocked("no enemy prefabs assigned.");
                return false;
            }

            if (maxAliveEnemies > 0 && _aliveEnemies.Count >= maxAliveEnemies)
            {
                LogBlocked("max alive enemy count reached.");
                return false;
            }

            return true;
        }

        private GameObject PickPrefab()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            {
                return null;
            }

            int startIndex = Random.Range(0, enemyPrefabs.Length);
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                int index = (startIndex + i) % enemyPrefabs.Length;
                if (enemyPrefabs[index] != null)
                {
                    return enemyPrefabs[index];
                }
            }

            return null;
        }

        private Transform PickSpawnPoint()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return transform;
            }

            if (chooseRandomSpawnPoint)
            {
                int startIndex = Random.Range(0, spawnPoints.Length);
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    int index = (startIndex + i) % spawnPoints.Length;
                    if (spawnPoints[index] != null)
                    {
                        return spawnPoints[index];
                    }
                }

                return transform;
            }

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    return spawnPoints[i];
                }
            }

            return transform;
        }

        private Vector2 RandomizeOffset()
        {
            if (randomPositionOffset == Vector2.zero)
            {
                return Vector2.zero;
            }

            return new Vector2(
                Random.Range(-Mathf.Abs(randomPositionOffset.x), Mathf.Abs(randomPositionOffset.x)),
                Random.Range(-Mathf.Abs(randomPositionOffset.y), Mathf.Abs(randomPositionOffset.y)));
        }

        private void TrySubscribeToEnemyDeath(GameObject enemy)
        {
            if (!removeFromAliveWhenEnemyDies || enemy == null)
            {
                return;
            }

            EnemyHittable hittable = enemy.GetComponentInChildren<EnemyHittable>();
            if (hittable == null)
            {
                return;
            }

            hittable.OnDied += () => RemoveAliveEnemy(enemy);
        }

        private void RemoveAliveEnemy(GameObject enemy)
        {
            if (enemy != null)
            {
                _aliveEnemies.Remove(enemy);
            }
        }

        private void PruneAliveEnemies()
        {
            for (int i = _aliveEnemies.Count - 1; i >= 0; i--)
            {
                GameObject enemy = _aliveEnemies[i];
                if (enemy == null || ignoreInactiveAliveEnemies && !enemy.activeInHierarchy)
                {
                    _aliveEnemies.RemoveAt(i);
                }
            }
        }

        private void LogBlocked(string reason)
        {
            if (logSpawnBlocked)
            {
                Debug.Log($"EnemySpawner2D spawn blocked: {reason}", this);
            }
        }

        private void OnValidate()
        {
            spawnInterval = Mathf.Max(0.01f, spawnInterval);
            spawnCountPerTrigger = Mathf.Max(1, spawnCountPerTrigger);
            maxAliveEnemies = Mathf.Max(0, maxAliveEnemies);
            hitSpawnCooldown = Mathf.Max(0f, hitSpawnCooldown);
            spawnOnEnableDelay = Mathf.Max(0f, spawnOnEnableDelay);
        }
    }
}
