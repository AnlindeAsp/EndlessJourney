using System;
using System.IO;
using UnityEngine;

namespace EndlessJourney.Player
{
    [Serializable]
    public class PlayerData2D
    {
        public int SpellSlotNum = 5;
        public int CharmSlotNum = 0;
        public string[] unlockedAbilityIds = Array.Empty<string>();
    }

    /// <summary>
    /// Shared JSON reader/writer for player progression values.
    /// </summary>
    public static class PlayerDataStore2D
    {
        public static string GetPlayerDataPath(string fileName)
        {
            string safeFileName = string.IsNullOrWhiteSpace(fileName) ? "PlayerData.json" : fileName.Trim();
            return Path.Combine(Application.persistentDataPath, safeFileName);
        }

        public static string GetDefaultPlayerDataPath(string fileName)
        {
            string safeFileName = string.IsNullOrWhiteSpace(fileName) ? "PlayerData.json" : fileName.Trim();
            return Path.Combine(Application.streamingAssetsPath, safeFileName);
        }

        public static bool TryLoad(string playerDataPath, out PlayerData2D data)
        {
            data = null;

            if (string.IsNullOrWhiteSpace(playerDataPath))
            {
                return false;
            }

            string fileName = Path.GetFileName(playerDataPath);
            string defaultPath = GetDefaultPlayerDataPath(fileName);

            if (TryReadPlayerDataFromPath(playerDataPath, out data))
            {
                return true;
            }

            if (!string.Equals(defaultPath, playerDataPath, StringComparison.OrdinalIgnoreCase)
                && TryReadPlayerDataFromPath(defaultPath, out data))
            {
                return true;
            }

            return false;
        }

        private static bool TryReadPlayerDataFromPath(string path, out PlayerData2D data)
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

                data = JsonUtility.FromJson<PlayerData2D>(json);
                if (data == null)
                {
                    return false;
                }

                data.SpellSlotNum = Mathf.Max(1, data.SpellSlotNum);
                data.CharmSlotNum = Mathf.Max(0, data.CharmSlotNum);
                data.unlockedAbilityIds ??= Array.Empty<string>();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load player data at '{path}'. {ex.Message}");
                return false;
            }
        }

        public static bool Save(string playerDataPath, PlayerData2D data, bool prettyPrint)
        {
            if (string.IsNullOrWhiteSpace(playerDataPath) || data == null)
            {
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(playerDataPath);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                data.SpellSlotNum = Mathf.Max(1, data.SpellSlotNum);
                data.CharmSlotNum = Mathf.Max(0, data.CharmSlotNum);
                data.unlockedAbilityIds ??= Array.Empty<string>();

                string json = JsonUtility.ToJson(data, prettyPrint);
                File.WriteAllText(playerDataPath, json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save player data at '{playerDataPath}'. {ex.Message}");
                return false;
            }
        }
    }
}
