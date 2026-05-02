using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Casts equipped spells from SpellBook slots via quick-cast keys (1..5).
    /// SlotCaster trusts SpellBook as the single source of equipped spell truth.
    /// </summary>
    public class SpellSlotCaster2D : MonoBehaviour
    {
        private const int MaxSupportedSlots = 5;

        [Header("References")]
        [SerializeField] private PlayerInput2D playerInput;
        [SerializeField] private SpellBook2D spellBook;
        [SerializeField] private SpellCastSystem spellCastSystem;

        [Header("Debug")]
        [SerializeField] private bool logCastRouting;

        private void Update()
        {
            if (playerInput == null || spellBook == null || spellCastSystem == null)
            {
                return;
            }

            int slotCount = Mathf.Clamp(spellBook.AvailableSlotCount, 1, MaxSupportedSlots);
            for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                if (playerInput.WasSpellSlotPressedThisFrame(slotIndex))
                {
                    TryCastSlot(slotIndex);
                }
            }
        }

        public bool TryCastSlot(int slotIndex)
        {
            SpellData2D spellData = spellBook.GetEquippedSpellData(slotIndex);
            if (spellData == null)
            {
                return false;
            }

            bool started = spellCastSystem.TryRequestCast(spellData);

            if (logCastRouting && started)
            {
                Debug.Log($"Spell slot {slotIndex + 1} cast -> {spellData.SpellId}", this);
            }

            return started;
        }
    }
}
