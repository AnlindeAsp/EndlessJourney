using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Player ability ownership core.
    /// Controls whether key ability modules are currently available.
    /// </summary>
    public class PlayerAbilityCore2D : MonoBehaviour
    {
        [Header("Ability Flags")]
        [SerializeField] private bool allowDoubleJump = true;
        [SerializeField] private bool allowDash = true;
        [SerializeField] private bool allowSpellCast = true;

        [Header("Ability Modules (Assign Manually)")]
        [SerializeField] private PlayerDoubleJump2D doubleJumpModule;
        [SerializeField] private PlayerDash2D dashModule;
        [SerializeField] private SpellCastSystem spellCastModule;

        public bool AllowDoubleJumpEnabled => allowDoubleJump;
        public bool AllowDashEnabled => allowDash;
        public bool AllowSpellCastEnabled => allowSpellCast;

        private void Awake()
        {
            ApplyAbilityAvailability();
        }

        private void OnEnable()
        {
            ApplyAbilityAvailability();
        }

        public void AllowDoubleJump()
        {
            allowDoubleJump = true;
            ApplyAbilityAvailability();
        }

        public void AllowDash()
        {
            allowDash = true;
            ApplyAbilityAvailability();
        }

        public void AllowSpellCast()
        {
            allowSpellCast = true;
            ApplyAbilityAvailability();
        }

        public void ApplyAbilityAvailability()
        {
            if (doubleJumpModule != null)
            {
                doubleJumpModule.enabled = allowDoubleJump;
            }

            if (dashModule != null)
            {
                dashModule.enabled = allowDash;
            }

            if (spellCastModule != null)
            {
                spellCastModule.enabled = allowSpellCast;
            }
        }
    }
}
