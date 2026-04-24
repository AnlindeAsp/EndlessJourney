using UnityEngine;

namespace EndlessJourney.Cameras
{
    /// <summary>
    /// Lightweight smooth follow camera for 2D prototypes.
    /// </summary>
    public class SimpleCameraFollow2D : MonoBehaviour
    {
        [Header("Follow Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, -10f);
        [SerializeField, Min(0f)] private float smoothTime = 0.12f;

        private Vector3 _velocity;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, smoothTime);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
