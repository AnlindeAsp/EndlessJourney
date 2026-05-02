using System;
using System.Collections.Generic;
using EndlessJourney.Interfaces;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EndlessJourney.Player
{
    /// <summary>
    /// Player-side interaction entry.
    /// Interacts only with interactables currently registered by trigger zones.
    /// </summary>
    public class PlayerInteractor2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerCore2D core;

        [Header("Input")]
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private Key interactKey = Key.E;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        [SerializeField] private KeyCode legacyInteractKey = KeyCode.E;
#endif

        [Header("Behavior")]
        [SerializeField, Min(0f)] private float interactCooldown = 0.05f;
        [SerializeField] private bool logInteract;

        private readonly List<IInteractable> _nearbyInteractables = new List<IInteractable>(8);
        private float _cooldownTimer;
        private IInteractable _currentBest;

        /// <summary>
        /// Fired when best interactable changes.
        /// string = current prompt text, null/empty means no prompt.
        /// </summary>
        public event Action<string> OnPromptChanged;

        private void Awake()
        {
            if (core == null)
            {
                core = GetComponent<PlayerCore2D>();
            }
        }

        private void Update()
        {
            if (_cooldownTimer > 0f)
            {
                _cooldownTimer = Mathf.Max(0f, _cooldownTimer - Time.deltaTime);
            }

            if (core != null && core.IsActionLocked)
            {
                return;
            }

            IInteractable best = SelectBestInteractable();
            if (!ReferenceEquals(best, _currentBest))
            {
                _currentBest = best;
                OnPromptChanged?.Invoke(_currentBest != null ? _currentBest.GetInteractionPrompt(gameObject) : string.Empty);
            }

            if (_currentBest == null || _cooldownTimer > 0f)
            {
                return;
            }

            if (!WasInteractPressedThisFrame())
            {
                return;
            }

            if (!_currentBest.CanInteract(gameObject))
            {
                return;
            }

            _currentBest.Interact(gameObject);
            _cooldownTimer = interactCooldown;

            if (logInteract)
            {
                Debug.Log($"Interacted with {_currentBest}.", this);
            }
        }

        public void RegisterInteractable(IInteractable interactable)
        {
            if (interactable == null)
            {
                return;
            }

            if (_nearbyInteractables.Contains(interactable))
            {
                return;
            }

            _nearbyInteractables.Add(interactable);
        }

        public void UnregisterInteractable(IInteractable interactable)
        {
            if (interactable == null)
            {
                return;
            }

            _nearbyInteractables.Remove(interactable);
        }

        private IInteractable SelectBestInteractable()
        {
            IInteractable best = null;
            int bestPriority = int.MinValue;
            float bestDistance = float.MaxValue;

            for (int i = _nearbyInteractables.Count - 1; i >= 0; i--)
            {
                IInteractable candidate = _nearbyInteractables[i];
                if (candidate == null)
                {
                    _nearbyInteractables.RemoveAt(i);
                    continue;
                }

                if (!candidate.CanInteract(gameObject))
                {
                    continue;
                }

                int priority = candidate.InteractionPriority;
                float distance = GetCandidateDistance(candidate);
                bool better = priority > bestPriority ||
                              (priority == bestPriority && distance < bestDistance);

                if (!better)
                {
                    continue;
                }

                best = candidate;
                bestPriority = priority;
                bestDistance = distance;
            }

            return best;
        }

        private float GetCandidateDistance(IInteractable interactable)
        {
            Component component = interactable as Component;
            if (component == null)
            {
                return float.MaxValue;
            }

            return Vector2.Distance(transform.position, component.transform.position);
        }

        private bool WasInteractPressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard[interactKey].wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(legacyInteractKey);
#else
            return false;
#endif
        }
    }
}
