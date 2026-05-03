using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EndlessJourney.Player
{
    [Serializable]
    public struct SpellUnlockStateEntry2D
    {
        public string spellId;
        public bool unlocked;
    }

    [Serializable]
    public class SpellRecordData2D
    {
        public SpellUnlockStateEntry2D[] unlockedSpellIds = Array.Empty<SpellUnlockStateEntry2D>();
        public string[] equippedSpellIds = Array.Empty<string>();
        public string equippedWeaponId = string.Empty;
    }

    /// <summary>
    /// Shared JSON record reader/writer for spell unlock/equip and lightweight equipment state.
    /// </summary>
    public static class SpellRecordStore2D
    {
        public static string GetRecordPath(string fileName)
        {
            string safeFileName = string.IsNullOrWhiteSpace(fileName) ? "record.json" : fileName.Trim();
            return Path.Combine(Application.persistentDataPath, safeFileName);
        }

        public static string GetDefaultRecordPath(string fileName)
        {
            string safeFileName = string.IsNullOrWhiteSpace(fileName) ? "record.json" : fileName.Trim();
            return Path.Combine(Application.streamingAssetsPath, safeFileName);
        }

        public static bool TryLoad(string recordPath, out SpellRecordData2D data)
        {
            data = null;

            if (string.IsNullOrWhiteSpace(recordPath))
            {
                return false;
            }

            string fileName = Path.GetFileName(recordPath);
            string defaultPath = GetDefaultRecordPath(fileName);

            if (TryReadRecordFromPath(recordPath, out data))
            {
                return true;
            }

            if (!string.Equals(defaultPath, recordPath, StringComparison.OrdinalIgnoreCase)
                && TryReadRecordFromPath(defaultPath, out data))
            {
                return true;
            }

            return false;
        }

        private static bool TryReadRecordFromPath(string path, out SpellRecordData2D data)
        {
            data = null;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return false;
                }

                data = JsonUtility.FromJson<SpellRecordData2D>(json);
                if (data == null)
                {
                    return false;
                }

                data.unlockedSpellIds ??= Array.Empty<SpellUnlockStateEntry2D>();
                data.equippedSpellIds ??= Array.Empty<string>();
                data.equippedWeaponId ??= string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load spell record at '{path}'. {ex.Message}");
                return false;
            }
        }

        public static bool Save(string recordPath, SpellRecordData2D data, bool prettyPrint)
        {
            if (string.IsNullOrWhiteSpace(recordPath) || data == null)
            {
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(recordPath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                data.unlockedSpellIds ??= Array.Empty<SpellUnlockStateEntry2D>();
                data.equippedSpellIds ??= Array.Empty<string>();
                data.equippedWeaponId ??= string.Empty;

                string json = JsonUtility.ToJson(data, prettyPrint);
                File.WriteAllText(recordPath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save spell record at '{recordPath}'. {ex.Message}");
                return false;
            }
        }

        public static SpellUnlockStateEntry2D[] BuildUnlockEntries(Dictionary<string, bool> unlockedById)
        {
            if (unlockedById == null || unlockedById.Count == 0)
            {
                return Array.Empty<SpellUnlockStateEntry2D>();
            }

            SpellUnlockStateEntry2D[] entries = new SpellUnlockStateEntry2D[unlockedById.Count];
            int index = 0;
            foreach (KeyValuePair<string, bool> pair in unlockedById)
            {
                entries[index++] = new SpellUnlockStateEntry2D
                {
                    spellId = pair.Key,
                    unlocked = pair.Value
                };
            }

            return entries;
        }
    }
}
