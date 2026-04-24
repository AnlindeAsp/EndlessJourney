using UnityEngine;

namespace EndlessJourney.Combat
{
    /// <summary>
    /// Supported weapon archetypes for combat formulas.
    /// </summary>
    public enum WeaponType
    {
        Sword,
        DualBlades,
        Heavy
    }

    /// <summary>
    /// ScriptableObject data container for weapon definitions.
    /// Keep this asset focused on weapon configuration data.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon_", menuName = "EndlessJourney/Combat/Weapon")]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string weaponName = "New Weapon";
        [SerializeField] private WeaponType weaponType = WeaponType.Sword;
        [SerializeField] private Sprite icon;

        [Header("Core Stats")]
        [SerializeField, Min(0.1f)] private float length = 1.5f;
        [SerializeField, Min(0f)] private float sharpness = 1f;
        [SerializeField, Min(0.01f)] private float weight = 1f;

        [Header("State (Prototype)")]
        [Tooltip("Prototype ownership flag. Runtime save/state systems can override this.")]
        [SerializeField] private bool isOwned;
        [Tooltip("Whether this weapon is currently allowed to be used.")]
        [SerializeField] private bool canUse = true;

        public string WeaponName => weaponName;
        public WeaponType Type => weaponType;
        public Sprite Icon => icon;
        public float Length => length;
        public float Sharpness => sharpness;
        public float Weight => weight;
        public bool IsOwned => isOwned;
        public bool CanUse => canUse;

        private void OnValidate()
        {
            length = Mathf.Max(0.1f, length);
            sharpness = Mathf.Max(0f, sharpness);
            weight = Mathf.Max(0.01f, weight);
            weaponName = string.IsNullOrWhiteSpace(weaponName) ? "New Weapon" : weaponName.Trim();
        }
    }
}
