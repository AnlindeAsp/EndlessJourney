using EndlessJourney.Interfaces;
using EndlessJourney.Player;
using UnityEngine;

namespace EndlessJourney.Items
{
    /// <summary>
    /// Base class for trigger-based ability pickup items.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class AbilityPickupItem2D : MonoBehaviour, IPickable
    {
        [Header("Pickup Gate")]
        [Tooltip("Only objects on these layers can trigger pickup.")]
        [SerializeField] private LayerMask pickupTargetLayers = ~0;

        [Header("Presentation")]
        [SerializeField] private bool destroyOnPicked = true;
        [SerializeField] private Collider2D pickupTrigger;
        [SerializeField] private GameObject pickupEffectPrefab;
        [SerializeField, Min(0f)] private float pickupEffectLifetime = 1.2f;
        [SerializeField] private bool logPickup;

        private bool _isPicked;

        protected virtual void Reset()
        {
            pickupTrigger = GetComponent<Collider2D>();
            if (pickupTrigger != null)
            {
                pickupTrigger.isTrigger = true;
            }
        }

        protected virtual void Awake()
        {
            if (pickupTrigger == null)
            {
                pickupTrigger = GetComponent<Collider2D>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isPicked || other == null)
            {
                return;
            }

            GameObject picker = ResolvePickerRoot(other);
            TryPick(picker);
        }

        public bool CanBePickedBy(GameObject picker)
        {
            if (_isPicked || picker == null)
            {
                return false;
            }

            if (!IsLayerAllowed(picker.layer))
            {
                return false;
            }

            return ResolveAbilityCore(picker) != null;
        }

        public bool TryPick(GameObject picker)
        {
            if (!CanBePickedBy(picker))
            {
                return false;
            }

            PlayerAbilityCore2D abilityCore = ResolveAbilityCore(picker);
            if (abilityCore == null)
            {
                return false;
            }

            if (!ApplyPickup(abilityCore))
            {
                return false;
            }

            HandlePicked();
            return true;
        }

        /// <summary>
        /// Applies the concrete ability unlock effect.
        /// Return true when pickup should be consumed.
        /// </summary>
        protected abstract bool ApplyPickup(PlayerAbilityCore2D abilityCore);

        private void HandlePicked()
        {
            _isPicked = true;

            if (logPickup)
            {
                Debug.Log($"{name} picked.", this);
            }

            if (pickupEffectPrefab != null)
            {
                GameObject instance = Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
                if (pickupEffectLifetime > 0f)
                {
                    Destroy(instance, pickupEffectLifetime);
                }
            }

            if (destroyOnPicked)
            {
                Destroy(gameObject);
                return;
            }

            if (pickupTrigger != null)
            {
                pickupTrigger.enabled = false;
            }

            SetRenderersVisible(false);
        }

        private static GameObject ResolvePickerRoot(Collider2D other)
        {
            if (other.attachedRigidbody != null)
            {
                return other.attachedRigidbody.gameObject;
            }

            return other.gameObject;
        }

        private static PlayerAbilityCore2D ResolveAbilityCore(GameObject picker)
        {
            if (picker == null)
            {
                return null;
            }

            PlayerAbilityCore2D core = picker.GetComponent<PlayerAbilityCore2D>();
            if (core != null)
            {
                return core;
            }

            return picker.GetComponentInParent<PlayerAbilityCore2D>();
        }

        private bool IsLayerAllowed(int layer)
        {
            return (pickupTargetLayers.value & (1 << layer)) != 0;
        }

        private void SetRenderersVisible(bool visible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer rendererRef = renderers[i];
                if (rendererRef != null)
                {
                    rendererRef.enabled = visible;
                }
            }
        }
    }
}
