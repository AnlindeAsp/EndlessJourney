using System.Collections.Generic;
using EndlessJourney.Interfaces;
using EndlessJourney.Player;
using UnityEngine;

namespace EndlessJourney.Interaction
{
    /// <summary>
    /// Base class for trigger-zone interactables.
    /// Player can interact only while inside this trigger.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class TriggerInteractable2D : MonoBehaviour, IInteractable
    {
        [Header("Interaction")]
        [SerializeField] private int interactionPriority;
        [SerializeField] private string interactionPrompt = "Interact";
        [SerializeField] private Collider2D triggerCollider;

        private readonly HashSet<PlayerInteractor2D> _insideInteractors = new HashSet<PlayerInteractor2D>();

        public int InteractionPriority => interactionPriority;

        protected virtual void Reset()
        {
            triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
        }

        protected virtual void Awake()
        {
            if (triggerCollider == null)
            {
                triggerCollider = GetComponent<Collider2D>();
            }
        }

        protected virtual void OnDisable()
        {
            foreach (PlayerInteractor2D interactor in _insideInteractors)
            {
                if (interactor != null)
                {
                    interactor.UnregisterInteractable(this);
                }
            }

            _insideInteractors.Clear();
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            PlayerInteractor2D interactor = ResolveInteractor(other);
            if (interactor == null)
            {
                return;
            }

            _insideInteractors.Add(interactor);
            interactor.RegisterInteractable(this);
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            PlayerInteractor2D interactor = ResolveInteractor(other);
            if (interactor == null)
            {
                return;
            }

            _insideInteractors.Remove(interactor);
            interactor.UnregisterInteractable(this);
        }

        public virtual bool CanInteract(GameObject interactor)
        {
            return interactor != null && gameObject.activeInHierarchy;
        }

        public virtual string GetInteractionPrompt(GameObject interactor)
        {
            return interactionPrompt;
        }

        public abstract void Interact(GameObject interactor);

        private static PlayerInteractor2D ResolveInteractor(Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            PlayerInteractor2D interactor = null;
            if (other.attachedRigidbody != null)
            {
                interactor = other.attachedRigidbody.GetComponent<PlayerInteractor2D>();
            }

            if (interactor != null)
            {
                return interactor;
            }

            interactor = other.GetComponent<PlayerInteractor2D>();
            if (interactor != null)
            {
                return interactor;
            }

            return other.GetComponentInParent<PlayerInteractor2D>();
        }
    }
}
