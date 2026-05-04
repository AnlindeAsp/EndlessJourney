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
    [CreateAssetMenu(fileName = "Weapon_", menuName = "EndlessJourney/Scriptable/Weapon/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string weaponId = "weapon_default";
        [SerializeField] private string weaponName = "New Weapon";
        [SerializeField] private WeaponType weaponType = WeaponType.Sword;
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite detailImage;
        [TextArea(2, 6)]
        [SerializeField] private string description = string.Empty;

        [Header("Core Stats")]
        [SerializeField, Min(0.1f)] private float length = 1.5f;
        [SerializeField, Min(0f)] private float sharpness = 1f;
        [SerializeField, Min(0.01f)] private float weight = 1f;

        public string WeaponId => weaponId;
        public string WeaponName => weaponName;
        public WeaponType Type => weaponType;
        public Sprite Icon => icon;
        public Sprite DetailImage => detailImage;
        public string Description => description ?? string.Empty;
        public float Length => length;
        public float Sharpness => sharpness;
        public float Weight => weight;

        private void OnValidate()
        {
            length = Mathf.Max(0.1f, length);
            sharpness = Mathf.Max(0f, sharpness);
            weight = Mathf.Max(0.01f, weight);
            weaponId = string.IsNullOrWhiteSpace(weaponId) ? "weapon_default" : weaponId.Trim();
            weaponName = string.IsNullOrWhiteSpace(weaponName) ? "New Weapon" : weaponName.Trim();
            description ??= string.Empty;
        }
    }
}
