using System;
using System.Collections.Generic;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Holds all armor definitions and tracks unlocked state by stable armor id.
    /// Runtime state should store ids, not asset references.
    /// </summary>
    public class ArmorLibrary2D : MonoBehaviour
    {
        [Serializable]
        private struct ArmorUnlockEntry
        {
            public string armorId;
            public bool unlocked;
        }

        [Header("Armor Definitions")]
        [SerializeField] private ArmorData[] allArmors = Array.Empty<ArmorData>();

        [Header("Initial Unlock State")]
        [SerializeField] private ArmorUnlockEntry[] initialUnlockedEntries = Array.Empty<ArmorUnlockEntry>();

        [Header("Record")]
        [SerializeField] private bool loadFromRecordOnAwake = true;
        [SerializeField] private bool saveToRecordOnChange = true;
        [SerializeField] private string recordFileName = "record.json";
        [SerializeField] private bool prettyPrintRecordJson = true;

        private readonly Dictionary<string, ArmorData> _armorById = new Dictionary<string, ArmorData>(8);
        private readonly Dictionary<string, bool> _unlockedById = new Dictionary<string, bool>(8);
        private string _recordPath;
        private bool _initialized;

        public int ArmorCount => allArmors != null ? allArmors.Length : 0;

        public event Action<string, bool> OnArmorUnlockStateChanged;

        private void Awake()
        {
            EnsureInitialized();
        }

        public bool HasArmor(string armorId)
        {
            if (string.IsNullOrWhiteSpace(armorId))
            {
                return false;
            }

            EnsureInitialized();
            return _armorById.ContainsKey(armorId.Trim());
        }

        public bool TryGetArmorData(string armorId, out ArmorData armorData)
        {
            if (string.IsNullOrWhiteSpace(armorId))
            {
                armorData = null;
                return false;
            }

            EnsureInitialized();
            return _armorById.TryGetValue(armorId.Trim(), out armorData) && armorData != null;
        }

        public ArmorData GetArmorData(string armorId)
        {
            TryGetArmorData(armorId, out ArmorData armorData);
            return armorData;
        }

        public ArmorData GetArmorAt(int index)
        {
            EnsureInitialized();
            if (allArmors == null || index < 0 || index >= allArmors.Length)
            {
                return null;
            }

            return allArmors[index];
        }

        public bool IsUnlocked(string armorId)
        {
            if (string.IsNullOrWhiteSpace(armorId))
            {
                return false;
            }

            EnsureInitialized();
            return _unlockedById.TryGetValue(armorId.Trim(), out bool unlocked) && unlocked;
        }

        public void UnlockArmor(string armorId)
        {
            SetArmorUnlocked(armorId, true);
        }

        public void SetArmorUnlocked(string armorId, bool unlocked)
        {
            if (string.IsNullOrWhiteSpace(armorId))
            {
                return;
            }

            EnsureInitialized();
            string normalizedId = armorId.Trim();
            bool previous = _unlockedById.TryGetValue(normalizedId, out bool existing) && existing;
            _unlockedById[normalizedId] = unlocked;

            if (previous != unlocked)
            {
                SaveUnlockStateToRecord();
                OnArmorUnlockStateChanged?.Invoke(normalizedId, unlocked);
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _recordPath = PlayerRecordStore2D.GetRecordPath(recordFileName);
            RebuildArmorIndex();
            RebuildInitialUnlockState();
            TryLoadUnlockStateFromRecord();
            _initialized = true;
        }

        public void RebuildArmorIndex()
        {
            _armorById.Clear();

            if (allArmors == null)
            {
                return;
            }

            for (int i = 0; i < allArmors.Length; i++)
            {
                ArmorData armorData = allArmors[i];
                if (armorData == null || string.IsNullOrWhiteSpace(armorData.ArmorId))
                {
                    continue;
                }

                _armorById[armorData.ArmorId.Trim()] = armorData;
            }
        }

        private void RebuildInitialUnlockState()
        {
            _unlockedById.Clear();

            if (allArmors != null)
            {
                for (int i = 0; i < allArmors.Length; i++)
                {
                    ArmorData armorData = allArmors[i];
                    if (armorData == null || string.IsNullOrWhiteSpace(armorData.ArmorId))
                    {
                        continue;
                    }

                    _unlockedById[armorData.ArmorId.Trim()] = false;
                }
            }

            if (initialUnlockedEntries == null)
            {
                return;
            }

            for (int i = 0; i < initialUnlockedEntries.Length; i++)
            {
                ArmorUnlockEntry entry = initialUnlockedEntries[i];
                if (string.IsNullOrWhiteSpace(entry.armorId))
                {
                    continue;
                }

                _unlockedById[entry.armorId.Trim()] = entry.unlocked;
            }
        }

        private void TryLoadUnlockStateFromRecord()
        {
            if (!loadFromRecordOnAwake)
            {
                return;
            }

            if (!PlayerRecordStore2D.TryLoad(_recordPath, out PlayerRecordData2D recordData) || recordData == null)
            {
                return;
            }

            ArmorUnlockStateEntry2D[] entries = recordData.unlockedArmorIds;
            if (entries == null || entries.Length == 0)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                ArmorUnlockStateEntry2D entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry.armorId))
                {
                    continue;
                }

                _unlockedById[entry.armorId.Trim()] = entry.unlocked;
            }
        }

        private void SaveUnlockStateToRecord()
        {
            if (!saveToRecordOnChange)
            {
                return;
            }

            PlayerRecordData2D recordData;
            if (!PlayerRecordStore2D.TryLoad(_recordPath, out recordData) || recordData == null)
            {
                recordData = new PlayerRecordData2D();
            }

            recordData.unlockedArmorIds = PlayerRecordStore2D.BuildArmorUnlockEntries(_unlockedById);
            PlayerRecordStore2D.Save(_recordPath, recordData, prettyPrintRecordJson);
        }

        private void OnValidate()
        {
            _initialized = false;
        }
    }
}
