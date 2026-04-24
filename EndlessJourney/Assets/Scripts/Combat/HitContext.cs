using UnityEngine;

namespace EndlessJourney.Combat
{
    /// <summary>
    /// Data payload sent to hittable targets for one hit attempt.
    /// </summary>
    public struct HitContext
    {
        public GameObject Source;
        public Collider2D SourceCollider;
        public Vector2 HitPoint;
        public Vector2 HitDirection;
        public float Damage;
        public HitType Type;

        public HitContext(
            GameObject source,
            Collider2D sourceCollider,
            Vector2 hitPoint,
            Vector2 hitDirection,
            float damage,
            HitType type)
        {
            Source = source;
            SourceCollider = sourceCollider;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            Damage = damage;
            Type = type;
        }
    }
}
