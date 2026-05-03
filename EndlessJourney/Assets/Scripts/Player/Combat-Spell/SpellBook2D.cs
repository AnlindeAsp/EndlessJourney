using System;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Stores currently equipped spells in quick-cast slots.
    /// </summary>
    public class SpellBook2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpellLibrary2D spellLibrary;

        [Header("Slots")]
        [SerializeField, Range(1, 5)] private int slotCount = 5;
        [SerializeField, Range(1, 5)] private int availableSlotCount = 5;
        [SerializeField] private bool allowDuplicateSpells = false;
        [SerializeField] private bool requireSpellUnlockedForEquip = true;
        [SerializeField] private string[] equippedSpellIds = new string[5];

        [Header("Record")]
        [SerializeField] private bool loadFromRecordOnAwake = true;
        [SerializeField] private bool saveToRecordOnChange = true;
        [SerializeField] private string recordFileName = "record.json";
        [SerializeField] private bool prettyPrintRecordJson = true;

        private string _recordPath;

        public event Action<int, string> OnSlotChanged;

        private void Awake()
        {
            _recordPath = PlayerRecordStore2D.GetRecordPath(recordFileName);
            EnsureSlotArraySize();
            TryLoadEquippedStateFromRecord();
        }

        public int SlotCount => slotCount;
        public int AvailableSlotCount => Mathf.Clamp(availableSlotCount, 1, slotCount);

        public string GetEquippedSpellId(int slotIndex)
        {
            if (!IsSlotIndexValid(slotIndex) || !IsSlotAvailable(slotIndex))
            {
                return string.Empty;
            }

            return equippedSpellIds[slotIndex] ?? string.Empty;
        }

        public bool EquipSpellToSlot(int slotIndex, string spellId)
        {
            return EquipSpellToSlot(slotIndex, spellId, false);
        }

        public bool EquipSpellToSlot(int slotIndex, string spellId, bool ignoreUnlockCheck)
        {
            if (!IsSlotIndexValid(slotIndex) || !IsSlotAvailable(slotIndex))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(spellId))
            {
                return false;
            }

            if (spellLibrary != null && !spellLibrary.HasSpell(spellId))
            {
                return false;
            }

            if (!ignoreUnlockCheck && requireSpellUnlockedForEquip && spellLibrary != null && !spellLibrary.IsUnlocked(spellId))
            {
                return false;
            }

            if (!allowDuplicateSpells && IsSpellEquipped(spellId))
            {
                return false;
            }

            equippedSpellIds[slotIndex] = spellId;
            SaveEquippedStateToRecord();
            OnSlotChanged?.Invoke(slotIndex, spellId);
            return true;
        }

        public SpellData2D GetEquippedSpellData(int slotIndex)
        {
            if (spellLibrary == null)
            {
                return null;
            }

            string spellId = GetEquippedSpellId(slotIndex);
            if (string.IsNullOrWhiteSpace(spellId))
            {
                return null;
            }

            return spellLibrary.GetSpellData(spellId);
        }

        public bool IsSlotAvailable(int slotIndex)
        {
            return IsSlotIndexValid(slotIndex) && slotIndex < AvailableSlotCount;
        }

        public void SetAvailableSlotCount(int newAvailableCount, bool clearNowUnavailableSlots = false)
        {
            int clamped = Mathf.Clamp(newAvailableCount, 1, slotCount);
            if (availableSlotCount == clamped)
            {
                return;
            }

            availableSlotCount = clamped;
            if (clearNowUnavailableSlots)
            {
                ClearUnavailableSlots();
            }
        }

        public bool UnlockNextSlot()
        {
            int current = AvailableSlotCount;
            if (current >= slotCount)
            {
                return false;
            }

            SetAvailableSlotCount(current + 1, false);
            return true;
        }

        public void UnequipSlot(int slotIndex)
        {
            if (!IsSlotIndexValid(slotIndex))
            {
                return;
            }

            equippedSpellIds[slotIndex] = string.Empty;
            SaveEquippedStateToRecord();
            OnSlotChanged?.Invoke(slotIndex, string.Empty);
        }

        public bool IsSpellEquipped(string spellId)
        {
            if (string.IsNullOrWhiteSpace(spellId) || equippedSpellIds == null)
            {
                return false;
            }

            for (int i = 0; i < equippedSpellIds.Length; i++)
            {
                if (equippedSpellIds[i] == spellId)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsSlotIndexValid(int slotIndex)
        {
            return slotIndex >= 0 && equippedSpellIds != null && slotIndex < equippedSpellIds.Length;
        }

        private void EnsureSlotArraySize()
        {
            slotCount = Mathf.Clamp(slotCount, 1, 5);
            availableSlotCount = Mathf.Clamp(availableSlotCount, 1, slotCount);

            if (equippedSpellIds == null || equippedSpellIds.Length != slotCount)
            {
                string[] previous = equippedSpellIds ?? Array.Empty<string>();
                equippedSpellIds = new string[slotCount];
                int copyCount = Mathf.Min(previous.Length, equippedSpellIds.Length);
                for (int i = 0; i < copyCount; i++)
                {
                    equippedSpellIds[i] = previous[i];
                }
            }
        }

        private void OnValidate()
        {
            EnsureSlotArraySize();
        }

        private void TryLoadEquippedStateFromRecord()
        {
            if (!loadFromRecordOnAwake)
            {
                return;
            }

            if (!PlayerRecordStore2D.TryLoad(_recordPath, out PlayerRecordData2D recordData) || recordData == null)
            {
                return;
            }

            if (recordData.equippedSpellIds == null || recordData.equippedSpellIds.Length == 0)
            {
                return;
            }

            int copyCount = Mathf.Min(equippedSpellIds.Length, recordData.equippedSpellIds.Length);
            for (int i = 0; i < copyCount; i++)
            {
                equippedSpellIds[i] = recordData.equippedSpellIds[i] ?? string.Empty;
            }

            for (int i = copyCount; i < equippedSpellIds.Length; i++)
            {
                equippedSpellIds[i] = string.Empty;
            }
        }

        private void SaveEquippedStateToRecord()
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

            recordData.equippedSpellIds = CopyEquippedSpellIds();
            PlayerRecordStore2D.Save(_recordPath, recordData, prettyPrintRecordJson);
        }

        private string[] CopyEquippedSpellIds()
        {
            if (equippedSpellIds == null || equippedSpellIds.Length == 0)
            {
                return Array.Empty<string>();
            }

            string[] copy = new string[equippedSpellIds.Length];
            for (int i = 0; i < equippedSpellIds.Length; i++)
            {
                copy[i] = equippedSpellIds[i] ?? string.Empty;
            }

            return copy;
        }

        private void ClearUnavailableSlots()
        {
            if (equippedSpellIds == null)
            {
                return;
            }

            int startIndex = AvailableSlotCount;
            for (int i = startIndex; i < equippedSpellIds.Length; i++)
            {
                if (string.IsNullOrEmpty(equippedSpellIds[i]))
                {
                    continue;
                }

                equippedSpellIds[i] = string.Empty;
                OnSlotChanged?.Invoke(i, string.Empty);
            }

            SaveEquippedStateToRecord();
        }
    }
}
