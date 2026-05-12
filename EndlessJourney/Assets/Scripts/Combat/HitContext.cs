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
        public DamageType DamageType;
        public WeaponType WeaponType;
        public float WeaponWeight;
        public int HitIndex;
        public int HitCount;

        public HitContext(
            GameObject source,
            Collider2D sourceCollider,
            Vector2 hitPoint,
            Vector2 hitDirection,
            float damage,
            HitType type)
            : this(
                source,
                sourceCollider,
                hitPoint,
                hitDirection,
                damage,
                type,
                DamageType.Physical,
                WeaponType.Sword,
                0f,
                0,
                1)
        {
        }

        public HitContext(
            GameObject source,
            Collider2D sourceCollider,
            Vector2 hitPoint,
            Vector2 hitDirection,
            float damage,
            HitType type,
            DamageType damageType,
            WeaponType weaponType,
            float weaponWeight,
            int hitIndex,
            int hitCount)
        {
            Source = source;
            SourceCollider = sourceCollider;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            Damage = Mathf.Max(0f, damage);
            Type = type;
            DamageType = damageType;
            WeaponType = weaponType;
            WeaponWeight = Mathf.Max(0f, weaponWeight);
            HitCount = Mathf.Max(1, hitCount);
            HitIndex = Mathf.Clamp(hitIndex, 0, HitCount - 1);
        }
    }
}
