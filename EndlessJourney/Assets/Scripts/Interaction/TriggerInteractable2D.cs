using System.Collections.Generic;
using EndlessJourney.Interfaces;
using EndlessJourney.Player;
using TMPro;
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
        [SerializeField] private string interactionPrompt = "interact";
        [SerializeField] private Collider2D triggerCollider;

        [Header("Prompt Display")]
        [SerializeField] private bool autoCreateWorldPrompt = true;
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private Vector3 promptWorldOffset = new Vector3(0f, 1.5f, 0f);
        [SerializeField, Min(0.1f)] private float promptFontSize = 3f;
        [SerializeField] private Color promptColor = Color.white;
        [SerializeField] private int promptSortingOrder = 50;

        private readonly Dictionary<PlayerInteractor2D, int> _insideInteractors = new Dictionary<PlayerInteractor2D, int>();
        private GameObject _promptDisplayRoot;
        private TMP_Text _promptDisplayText;

        public int InteractionPriority => interactionPriority;
        protected bool HasInsideInteractors => _insideInteractors.Count > 0;

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

            EnsurePromptDisplay();
            UpdatePromptPosition();
            HidePromptDisplay();
        }

        protected virtual void OnDisable()
        {
            foreach (PlayerInteractor2D interactor in _insideInteractors.Keys)
            {
                if (interactor != null)
                {
                    interactor.UnregisterInteractable(this);
                }
            }

            _insideInteractors.Clear();
            HidePromptDisplay();
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            PlayerInteractor2D interactor = ResolveInteractor(other);
            if (interactor == null)
            {
                return;
            }

            if (_insideInteractors.TryGetValue(interactor, out int count))
            {
                _insideInteractors[interactor] = count + 1;
            }
            else
            {
                _insideInteractors.Add(interactor, 1);
                interactor.RegisterInteractable(this);
            }

            RefreshPromptDisplay();
        }

        protected virtual void OnTriggerExit2D(Collider2D other)
        {
            PlayerInteractor2D interactor = ResolveInteractor(other);
            if (interactor == null)
            {
                return;
            }

            if (!_insideInteractors.TryGetValue(interactor, out int count))
            {
                return;
            }

            if (count > 1)
            {
                _insideInteractors[interactor] = count - 1;
                return;
            }

            _insideInteractors.Remove(interactor);
            interactor.UnregisterInteractable(this);
            RefreshPromptDisplay();
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

        protected void RefreshPromptDisplay()
        {
            PlayerInteractor2D interactor = GetAnyInsideInteractor();
            if (interactor == null || !CanInteract(interactor.gameObject))
            {
                HidePromptDisplay();
                return;
            }

            string prompt = GetInteractionPrompt(interactor.gameObject);
            if (string.IsNullOrWhiteSpace(prompt))
            {
                HidePromptDisplay();
                return;
            }

            EnsurePromptDisplay();
            if (_promptDisplayText != null)
            {
                _promptDisplayText.text = prompt;
            }

            UpdatePromptPosition();
            if (_promptDisplayRoot != null)
            {
                _promptDisplayRoot.SetActive(true);
            }
            else if (_promptDisplayText != null)
            {
                _promptDisplayText.gameObject.SetActive(true);
            }
        }

        protected void HidePromptDisplay()
        {
            if (_promptDisplayText != null)
            {
                _promptDisplayText.text = string.Empty;
            }

            if (_promptDisplayRoot != null)
            {
                _promptDisplayRoot.SetActive(false);
            }
            else if (_promptDisplayText != null)
            {
                _promptDisplayText.gameObject.SetActive(false);
            }
        }

        private void EnsurePromptDisplay()
        {
            if (_promptDisplayText != null || !autoCreateWorldPrompt || string.IsNullOrWhiteSpace(interactionPrompt))
            {
                return;
            }

            if (_promptDisplayRoot == null)
            {
                _promptDisplayRoot = new GameObject($"{name}_InteractionPrompt");
                _promptDisplayRoot.transform.SetParent(transform, false);
            }

            _promptDisplayText = _promptDisplayRoot.AddComponent<TextMeshPro>();
            _promptDisplayText.alignment = TextAlignmentOptions.Center;
            _promptDisplayText.fontSize = promptFontSize;
            _promptDisplayText.color = promptColor;
            _promptDisplayText.text = string.Empty;

            MeshRenderer renderer = _promptDisplayRoot.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = promptSortingOrder;
            }
        }

        private void UpdatePromptPosition()
        {
            Transform root = _promptDisplayRoot != null ? _promptDisplayRoot.transform :
                _promptDisplayText != null ? _promptDisplayText.transform : null;

            if (root == null)
            {
                return;
            }

            Transform anchor = promptAnchor != null ? promptAnchor : transform;
            root.position = anchor.position + promptWorldOffset;
        }

        private PlayerInteractor2D GetAnyInsideInteractor()
        {
            foreach (PlayerInteractor2D interactor in _insideInteractors.Keys)
            {
                if (interactor != null)
                {
                    return interactor;
                }
            }

            return null;
        }

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
