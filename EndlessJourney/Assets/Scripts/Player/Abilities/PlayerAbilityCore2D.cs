using System;
using System.Collections.Generic;
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
        [SerializeField] private bool allowDualWielding;

        [Header("Ability Ids")]
        [SerializeField] private string doubleJumpAbilityId = "double_jump";
        [SerializeField] private string dashAbilityId = "dash";
        [SerializeField] private string spellCastAbilityId = "spell_cast";
        [SerializeField] private string dualWieldingAbilityId = "dual_wielding";

        [Header("Ability Modules (Assign Manually)")]
        [SerializeField] private PlayerDoubleJump2D doubleJumpModule;
        [SerializeField] private PlayerDash2D dashModule;
        [SerializeField] private SpellCastSystem spellCastModule;

        [Header("Player Data")]
        [SerializeField] private bool loadFromPlayerDataOnAwake = true;
        [SerializeField] private bool saveToPlayerDataOnChange = true;
        [SerializeField] private string playerDataFileName = "PlayerData.json";
        [SerializeField] private bool prettyPrintPlayerDataJson = true;

        private string _playerDataPath;

        public bool AllowDoubleJumpEnabled => allowDoubleJump;
        public bool AllowDashEnabled => allowDash;
        public bool AllowSpellCastEnabled => allowSpellCast;
        public bool AllowDualWieldingEnabled => allowDualWielding;

        public event Action OnAbilityStateChanged;

        private void Awake()
        {
            _playerDataPath = PlayerDataStore2D.GetPlayerDataPath(playerDataFileName);
            TryLoadAbilityStateFromPlayerData();
            ApplyAbilityAvailability();
        }

        private void OnEnable()
        {
            ApplyAbilityAvailability();
        }

        public void AllowDoubleJump()
        {
            SetDoubleJumpAllowed(true);
        }

        public void AllowDash()
        {
            SetDashAllowed(true);
        }

        public void AllowSpellCast()
        {
            SetSpellCastAllowed(true);
        }

        public void AllowDualWielding()
        {
            SetDualWieldingAllowed(true);
        }

        public bool HasAbility(string abilityId)
        {
            string normalizedId = NormalizeAbilityId(abilityId);
            if (string.IsNullOrEmpty(normalizedId))
            {
                return false;
            }

            return normalizedId == NormalizeAbilityId(doubleJumpAbilityId) && allowDoubleJump
                   || normalizedId == NormalizeAbilityId(dashAbilityId) && allowDash
                   || normalizedId == NormalizeAbilityId(spellCastAbilityId) && allowSpellCast
                   || normalizedId == NormalizeAbilityId(dualWieldingAbilityId) && allowDualWielding;
        }

        public void SetDoubleJumpAllowed(bool allowed)
        {
            SetAbilityAllowed(ref allowDoubleJump, doubleJumpAbilityId, allowed);
        }

        public void SetDashAllowed(bool allowed)
        {
            SetAbilityAllowed(ref allowDash, dashAbilityId, allowed);
        }

        public void SetSpellCastAllowed(bool allowed)
        {
            SetAbilityAllowed(ref allowSpellCast, spellCastAbilityId, allowed);
        }

        public void SetDualWieldingAllowed(bool allowed)
        {
            SetAbilityAllowed(ref allowDualWielding, dualWieldingAbilityId, allowed);
        }

        private void SetAbilityAllowed(ref bool abilityFlag, string abilityId, bool allowed)
        {
            if (abilityFlag == allowed)
            {
                return;
            }

            abilityFlag = allowed;
            ApplyAbilityAvailability();
            SaveAbilityStateToPlayerData();
            OnAbilityStateChanged?.Invoke();
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

        private void TryLoadAbilityStateFromPlayerData()
        {
            if (!loadFromPlayerDataOnAwake)
            {
                return;
            }

            if (!PlayerDataStore2D.TryLoad(_playerDataPath, out PlayerData2D playerData) || playerData == null)
            {
                return;
            }

            HashSet<string> unlockedAbilityIds = BuildAbilityIdSet(playerData.unlockedAbilityIds);
            allowDoubleJump = unlockedAbilityIds.Contains(NormalizeAbilityId(doubleJumpAbilityId));
            allowDash = unlockedAbilityIds.Contains(NormalizeAbilityId(dashAbilityId));
            allowSpellCast = unlockedAbilityIds.Contains(NormalizeAbilityId(spellCastAbilityId));
            allowDualWielding = unlockedAbilityIds.Contains(NormalizeAbilityId(dualWieldingAbilityId));
        }

        private void SaveAbilityStateToPlayerData()
        {
            if (!saveToPlayerDataOnChange)
            {
                return;
            }

            PlayerData2D playerData;
            if (!PlayerDataStore2D.TryLoad(_playerDataPath, out playerData) || playerData == null)
            {
                playerData = new PlayerData2D();
            }

            HashSet<string> unlockedAbilityIds = BuildAbilityIdSet(playerData.unlockedAbilityIds);
            SetAbilityIdInSet(unlockedAbilityIds, doubleJumpAbilityId, allowDoubleJump);
            SetAbilityIdInSet(unlockedAbilityIds, dashAbilityId, allowDash);
            SetAbilityIdInSet(unlockedAbilityIds, spellCastAbilityId, allowSpellCast);
            SetAbilityIdInSet(unlockedAbilityIds, dualWieldingAbilityId, allowDualWielding);

            string[] abilityIds = new string[unlockedAbilityIds.Count];
            unlockedAbilityIds.CopyTo(abilityIds);
            Array.Sort(abilityIds, StringComparer.Ordinal);
            playerData.unlockedAbilityIds = abilityIds;
            PlayerDataStore2D.Save(_playerDataPath, playerData, prettyPrintPlayerDataJson);
        }

        private static HashSet<string> BuildAbilityIdSet(string[] abilityIds)
        {
            HashSet<string> idSet = new HashSet<string>(StringComparer.Ordinal);
            if (abilityIds == null)
            {
                return idSet;
            }

            for (int i = 0; i < abilityIds.Length; i++)
            {
                string normalizedId = NormalizeAbilityId(abilityIds[i]);
                if (!string.IsNullOrEmpty(normalizedId))
                {
                    idSet.Add(normalizedId);
                }
            }

            return idSet;
        }

        private static void SetAbilityIdInSet(HashSet<string> unlockedAbilityIds, string abilityId, bool unlocked)
        {
            string normalizedId = NormalizeAbilityId(abilityId);
            if (string.IsNullOrEmpty(normalizedId))
            {
                return;
            }

            if (unlocked)
            {
                unlockedAbilityIds.Add(normalizedId);
                return;
            }

            unlockedAbilityIds.Remove(normalizedId);
        }

        private static string NormalizeAbilityId(string abilityId)
        {
            return string.IsNullOrWhiteSpace(abilityId) ? string.Empty : abilityId.Trim();
        }

        private void OnValidate()
        {
            doubleJumpAbilityId = NormalizeAbilityId(doubleJumpAbilityId);
            dashAbilityId = NormalizeAbilityId(dashAbilityId);
            spellCastAbilityId = NormalizeAbilityId(spellCastAbilityId);
            dualWieldingAbilityId = NormalizeAbilityId(dualWieldingAbilityId);
            playerDataFileName = string.IsNullOrWhiteSpace(playerDataFileName) ? "PlayerData.json" : playerDataFileName.Trim();
        }
    }
}
