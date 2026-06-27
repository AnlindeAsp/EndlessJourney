using UnityEngine;

namespace EndlessJourney.Combat
{
    /// <summary>
    /// ScriptableObject data for one armor definition.
    /// Armor stays intentionally simple: durability and harm damage reduction only.
    /// </summary>
    [CreateAssetMenu(fileName = "Armor_", menuName = "EndlessJourney/Scriptable/Armor/Armor Data")]
    public class ArmorData : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string armorId = "armor_default";
        [SerializeField] private string armorName = "New Armor";
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite detailImage;
        [TextArea(2, 6)]
        [SerializeField] private string description = string.Empty;

        [Header("Core Stats")]
        [SerializeField, Min(1f)] private float maxDurability = 1000f;
        [SerializeField, Range(0f, 1f)] private float damageReductionEfficiency = 0.5f;

        public string ArmorId => armorId;
        public string ArmorName => armorName;
        public Sprite Icon => icon;
        public Sprite DetailImage => detailImage;
        public string Description => description ?? string.Empty;
        public float MaxDurability => maxDurability;
        public float DamageReductionEfficiency => damageReductionEfficiency;

        private void OnValidate()
        {
            armorId = string.IsNullOrWhiteSpace(armorId) ? "armor_default" : armorId.Trim();
            armorName = string.IsNullOrWhiteSpace(armorName) ? "New Armor" : armorName.Trim();
            description ??= string.Empty;
            maxDurability = Mathf.Max(1f, maxDurability);
            damageReductionEfficiency = Mathf.Clamp01(damageReductionEfficiency);
        }
    }
}
