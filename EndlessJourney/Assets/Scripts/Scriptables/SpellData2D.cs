using UnityEngine;
using UnityEngine.Serialization;

namespace EndlessJourney.Player
{
    public enum SpellKind2D
    {
        Damage = 0,
        Heal = 1,
        Movement = 2,
        Buff = 3
    }

    /// <summary>
    /// Static spell definition asset.
    /// Runtime save data should only store spell ids, not values in this asset.
    /// </summary>
    [CreateAssetMenu(fileName = "SpellData_", menuName = "EndlessJourney/Scriptable/Spell/Spell Data 2D")]
    public class SpellData2D : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string spellId = "spell_default";
        [SerializeField] private string displayName = "New Spell";
        [SerializeField] private SpellKind2D kind = SpellKind2D.Damage;

        [Header("Book Text")]
        [SerializeField, TextArea(3, 8)] private string description = string.Empty;
        [SerializeField, TextArea(1, 4)] private string supplementaryNote = string.Empty;

        [Header("Cast Rule")]
        [SerializeField, Min(0f)] private float manaCost = 30f;
        [FormerlySerializedAs("castTime")]
        [SerializeField, Min(0f)] private float singingTime = 0f;
        [SerializeField, Min(0f)] private float castTime = 0.2f;
        [SerializeField, Min(0f)] private float castCooldown = 0.35f;
        [SerializeField] private bool allowCastWhileMovementLocked = true;

        [Header("Effect")]
        [SerializeField] private SpellEffectData2D effectData;

        [Header("Cast Effect")]
        [SerializeField] private GameObject castEffectPrefab;
        [SerializeField, Min(0f)] private float castEffectLifetime = 1.5f;
        [SerializeField] private Vector3 defaultCastOffset = new Vector3(0.8f, 0.2f, 0f);

        public string SpellId => spellId;
        public string DisplayName => displayName;
        public SpellKind2D Kind => kind;
        public string Description => description;
        public string SupplementaryNote => supplementaryNote;
        public float ManaCost => manaCost;
        public float SingingTime => singingTime;
        public float CastTime => castTime;
        public float CastCooldown => castCooldown;
        public bool AllowCastWhileMovementLocked => allowCastWhileMovementLocked;
        public SpellEffectData2D EffectData => effectData;
        public GameObject CastEffectPrefab => castEffectPrefab;
        public float CastEffectLifetime => castEffectLifetime;
        public Vector3 DefaultCastOffset => defaultCastOffset;
    }
}
