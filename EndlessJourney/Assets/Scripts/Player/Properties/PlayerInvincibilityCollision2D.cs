using System.Collections.Generic;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Temporarily ignores player/enemy physics collision while PlayerHealth2D is invincible.
    /// Damage invincibility remains owned by PlayerHealth2D; this module only handles collision layers.
    /// </summary>
    public class PlayerInvincibilityCollision2D : MonoBehaviour
    {
        [Header("References (Assign Manually)")]
        [SerializeField] private PlayerHealth2D health;

        [Header("Layer Collision")]
        [Tooltip("When true, player/enemy physics collision is ignored while PlayerHealth2D is invincible.")]
        [SerializeField] private bool ignoreCollisionWhileInvincible = true;
        [Tooltip("Layer(s) used by the player body colliders. Do not include PlayerAttack unless you intentionally want attacks to ignore enemies.")]
        [SerializeField] private LayerMask playerCollisionLayers;
        [Tooltip("Layer(s) used by enemy body/contact colliders.")]
        [SerializeField] private LayerMask enemyCollisionLayers;

        [Header("Debug")]
        [SerializeField] private bool logCollisionState;

        private readonly List<LayerPairState> _activeIgnoredPairs = new List<LayerPairState>(8);
        private bool _isIgnoringCollision;

        private struct LayerPairState
        {
            public int PlayerLayer;
            public int EnemyLayer;
            public bool WasIgnored;
        }

        private void OnEnable()
        {
            if (health == null)
            {
                Debug.LogWarning("PlayerInvincibilityCollision2D needs PlayerHealth2D assigned manually.", this);
                return;
            }

            health.OnInvincibilityChanged += HandleInvincibilityChanged;
            if (health.IsInvincible)
            {
                SetCollisionIgnored(true);
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnInvincibilityChanged -= HandleInvincibilityChanged;
            }

            SetCollisionIgnored(false);
        }

        private void Update()
        {
            if (_isIgnoringCollision && (health == null || !health.IsInvincible || !ignoreCollisionWhileInvincible))
            {
                SetCollisionIgnored(false);
            }
        }

        private void HandleInvincibilityChanged(bool invincible)
        {
            SetCollisionIgnored(invincible && ignoreCollisionWhileInvincible);
        }

        private void SetCollisionIgnored(bool ignored)
        {
            if (ignored == _isIgnoringCollision)
            {
                return;
            }

            if (ignored)
            {
                BeginIgnoreCollision();
            }
            else
            {
                RestoreCollision();
            }
        }

        private void BeginIgnoreCollision()
        {
            _activeIgnoredPairs.Clear();

            int playerMask = playerCollisionLayers.value;
            int enemyMask = enemyCollisionLayers.value;
            if (playerMask == 0 || enemyMask == 0)
            {
                if (logCollisionState)
                {
                    Debug.LogWarning("PlayerInvincibilityCollision2D has empty player/enemy collision layer mask.", this);
                }
                return;
            }

            HashSet<int> visitedPairs = new HashSet<int>();
            for (int playerLayer = 0; playerLayer < 32; playerLayer++)
            {
                if (!ContainsLayer(playerMask, playerLayer))
                {
                    continue;
                }

                for (int enemyLayer = 0; enemyLayer < 32; enemyLayer++)
                {
                    if (!ContainsLayer(enemyMask, enemyLayer))
                    {
                        continue;
                    }

                    int pairKey = BuildPairKey(playerLayer, enemyLayer);
                    if (!visitedPairs.Add(pairKey))
                    {
                        continue;
                    }

                    bool wasIgnored = Physics2D.GetIgnoreLayerCollision(playerLayer, enemyLayer);
                    _activeIgnoredPairs.Add(new LayerPairState
                    {
                        PlayerLayer = playerLayer,
                        EnemyLayer = enemyLayer,
                        WasIgnored = wasIgnored
                    });

                    Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
                }
            }

            _isIgnoringCollision = _activeIgnoredPairs.Count > 0;
            if (_isIgnoringCollision && logCollisionState)
            {
                Debug.Log($"Player invincibility collision ignored for {_activeIgnoredPairs.Count} layer pair(s).", this);
            }
        }

        private void RestoreCollision()
        {
            if (_activeIgnoredPairs.Count == 0)
            {
                _isIgnoringCollision = false;
                return;
            }

            for (int i = 0; i < _activeIgnoredPairs.Count; i++)
            {
                LayerPairState pair = _activeIgnoredPairs[i];
                Physics2D.IgnoreLayerCollision(pair.PlayerLayer, pair.EnemyLayer, pair.WasIgnored);
            }

            if (logCollisionState)
            {
                Debug.Log($"Player invincibility collision restored for {_activeIgnoredPairs.Count} layer pair(s).", this);
            }

            _activeIgnoredPairs.Clear();
            _isIgnoringCollision = false;
        }

        private static bool ContainsLayer(int mask, int layer)
        {
            return (mask & (1 << layer)) != 0;
        }

        private static int BuildPairKey(int layerA, int layerB)
        {
            int low = Mathf.Min(layerA, layerB);
            int high = Mathf.Max(layerA, layerB);
            return (low << 5) | high;
        }
    }
}
