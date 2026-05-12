using UnityEngine;

namespace EndlessJourney.Combat
{
    public enum WeaponInscriptionEffectType
    {
        WeightMultiplier,
        SharpnessMultiplier,
        ComboDamageRamp,
        MissingHealthDamageBonus,
        ManaOnHit
    }

    /// <summary>
    /// ScriptableObject data for one weapon inscription.
    /// A weapon can equip one inscription id at a time.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponInscription_", menuName = "EndlessJourney/Scriptable/Weapon/Weapon Inscription Data")]
    public class WeaponInscriptionData : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private string inscriptionId = "inscription_default";
        [SerializeField] private string inscriptionName = "New Inscription";
        [TextArea(2, 4)]
        [SerializeField] private string flavorText = string.Empty;
        [TextArea(2, 6)]
        [SerializeField] private string description = string.Empty;

        [Header("Effect")]
        [SerializeField] private WeaponInscriptionEffectType effectType = WeaponInscriptionEffectType.WeightMultiplier;
        [SerializeField] private float value = 1f;
        [SerializeField, Min(0f)] private float timeoutSeconds;

        public string InscriptionId => inscriptionId;
        public string InscriptionName => inscriptionName;
        public string FlavorText => flavorText ?? string.Empty;
        public string Description => description ?? string.Empty;
        public WeaponInscriptionEffectType EffectType => effectType;
        public float Value => value;
        public float TimeoutSeconds => timeoutSeconds;

        public bool ModifiesStaticWeaponStats =>
            effectType == WeaponInscriptionEffectType.WeightMultiplier
            || effectType == WeaponInscriptionEffectType.SharpnessMultiplier;

        public float ModifyWeight(float baseWeight)
        {
            float weight = Mathf.Max(0.01f, baseWeight);
            if (effectType != WeaponInscriptionEffectType.WeightMultiplier)
            {
                return weight;
            }

            return Mathf.Max(0.01f, weight * Mathf.Max(0.01f, value));
        }

        public float ModifySharpness(float baseSharpness)
        {
            float sharpness = Mathf.Max(0f, baseSharpness);
            if (effectType != WeaponInscriptionEffectType.SharpnessMultiplier)
            {
                return sharpness;
            }

            return Mathf.Max(0f, sharpness * Mathf.Max(0f, value));
        }

        private void OnValidate()
        {
            inscriptionId = string.IsNullOrWhiteSpace(inscriptionId) ? "inscription_default" : inscriptionId.Trim();
            inscriptionName = string.IsNullOrWhiteSpace(inscriptionName) ? "New Inscription" : inscriptionName.Trim();
            flavorText ??= string.Empty;
            description ??= string.Empty;
            timeoutSeconds = Mathf.Max(0f, timeoutSeconds);
        }
    }
}
