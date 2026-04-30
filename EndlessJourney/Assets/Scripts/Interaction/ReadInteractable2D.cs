using UnityEngine;

namespace EndlessJourney.Interaction
{
    /// <summary>
    /// Simple read interaction example.
    /// Current behavior: logs read content to Console.
    /// </summary>
    public class ReadInteractable2D : TriggerInteractable2D
    {
        [Header("Read Content")]
        [TextArea(2, 8)]
        [SerializeField] private string readText = "An old note...";
        [SerializeField] private bool oneTimeRead;
        [SerializeField] private bool logRead = true;

        private bool _hasRead;

        public override bool CanInteract(GameObject interactor)
        {
            if (!base.CanInteract(interactor))
            {
                return false;
            }

            return !oneTimeRead || !_hasRead;
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            _hasRead = true;

            if (logRead)
            {
                Debug.Log($"{name} read: {readText}", this);
            }
        }
    }
}
