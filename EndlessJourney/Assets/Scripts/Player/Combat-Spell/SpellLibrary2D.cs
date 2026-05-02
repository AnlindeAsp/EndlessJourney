using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Holds all SpellData definitions and tracks unlocked state by spell id.
    /// </summary>
    public class SpellLibrary2D : MonoBehaviour
    {
        [Serializable]
        private struct SpellUnlockEntry
        {
            public string spellId;
            public bool unlocked;
        }

        [Header("Spell Definitions")]
        [SerializeField] private SpellData2D[] allSpells = Array.Empty<SpellData2D>();

        [Header("Initial Unlock State")]
        [SerializeField] private SpellUnlockEntry[] initialUnlockedEntries = Array.Empty<SpellUnlockEntry>();

        [Header("Record")]
        [SerializeField] private bool loadFromRecordOnAwake = true;
        [SerializeField] private bool saveToRecordOnChange = true;
        [SerializeField] private string recordFileName = "record.json";
        [SerializeField] private bool prettyPrintRecordJson = true;

        private readonly Dictionary<string, SpellData2D> _spellById = new Dictionary<string, SpellData2D>(32);
        private readonly Dictionary<string, bool> _unlockedById = new Dictionary<string, bool>(32);
        private string _recordPath;

        public event Action<string, bool> OnSpellUnlockStateChanged;

        private void Awake()
        {
            _recordPath = SpellRecordStore2D.GetRecordPath(recordFileName);
            RebuildSpellIndex();
            RebuildInitialUnlockState();
            TryLoadUnlockStateFromRecord();
        }

        public bool HasSpell(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
            {
                return false;
            }

            return _spellById.ContainsKey(spellId);
        }

        public bool TryGetSpellData(string spellId, out SpellData2D spellData)
        {
            if (string.IsNullOrWhiteSpace(spellId))
            {
                spellData = null;
                return false;
            }

            return _spellById.TryGetValue(spellId, out spellData) && spellData != null;
        }

        public SpellData2D GetSpellData(string spellId)
        {
            TryGetSpellData(spellId, out SpellData2D spellData);
            return spellData;
        }

        public bool IsUnlocked(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId))
            {
                return false;
            }

            return _unlockedById.TryGetValue(spellId, out bool unlocked) && unlocked;
        }

        public void UnlockSpell(string spellId)
        {
            SetSpellUnlocked(spellId, true);
        }

        public void SetSpellUnlocked(string spellId, bool unlocked)
        {
            if (string.IsNullOrWhiteSpace(spellId))
            {
                return;
            }

            bool previous = _unlockedById.TryGetValue(spellId, out bool existing) && existing;
            _unlockedById[spellId] = unlocked;

            if (previous != unlocked)
            {
                SaveUnlockStateToRecord();
                OnSpellUnlockStateChanged?.Invoke(spellId, unlocked);
            }
        }

        private void RebuildSpellIndex()
        {
            _spellById.Clear();

            if (allSpells == null)
            {
                return;
            }

            for (int i = 0; i < allSpells.Length; i++)
            {
                SpellData2D spellData = allSpells[i];
                if (spellData == null || string.IsNullOrWhiteSpace(spellData.SpellId))
                {
                    continue;
                }

                _spellById[spellData.SpellId] = spellData;
            }
        }

        private void RebuildInitialUnlockState()
        {
            _unlockedById.Clear();

            if (allSpells != null)
            {
                for (int i = 0; i < allSpells.Length; i++)
                {
                    SpellData2D spellData = allSpells[i];
                    if (spellData == null || string.IsNullOrWhiteSpace(spellData.SpellId))
                    {
                        continue;
                    }

                    _unlockedById[spellData.SpellId] = false;
                }
            }

            if (initialUnlockedEntries == null)
            {
                return;
            }

            for (int i = 0; i < initialUnlockedEntries.Length; i++)
            {
                SpellUnlockEntry entry = initialUnlockedEntries[i];
                if (string.IsNullOrWhiteSpace(entry.spellId))
                {
                    continue;
                }

                _unlockedById[entry.spellId] = entry.unlocked;
            }
        }

        private void TryLoadUnlockStateFromRecord()
        {
            if (!loadFromRecordOnAwake)
            {
                return;
            }

            if (!SpellRecordStore2D.TryLoad(_recordPath, out SpellRecordData2D recordData) || recordData == null)
            {
                return;
            }

            SpellUnlockStateEntry2D[] entries = recordData.unlockedSpellIds;
            if (entries == null || entries.Length == 0)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                SpellUnlockStateEntry2D entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry.spellId))
                {
                    continue;
                }

                _unlockedById[entry.spellId] = entry.unlocked;
            }
        }

        private void SaveUnlockStateToRecord()
        {
            if (!saveToRecordOnChange)
            {
                return;
            }

            SpellRecordData2D recordData;
            if (!SpellRecordStore2D.TryLoad(_recordPath, out recordData) || recordData == null)
            {
                recordData = new SpellRecordData2D();
            }

            recordData.unlockedSpellIds = SpellRecordStore2D.BuildUnlockEntries(_unlockedById);
            SpellRecordStore2D.Save(_recordPath, recordData, prettyPrintRecordJson);
        }
    }
}
