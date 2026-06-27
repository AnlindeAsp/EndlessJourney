using System;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Bridges equipped armor data into PlayerArmor2D runtime values.
    /// Forge UI can call ArmorEquipped2D to switch armor and this component will apply the stats.
    /// </summary>
    public class PlayerArmorEquipmentSystem2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ArmorEquipped2D armorEquipped;
        [SerializeField] private PlayerArmor2D armorRuntime;

        [Header("Runtime")]
        [SerializeField] private ArmorData equippedArmor;
        [SerializeField] private bool restoreFullDurabilityOnEquip = true;
        [SerializeField] private bool disableRuntimeArmorWhenNoArmorEquipped;

        public ArmorData EquippedArmor => equippedArmor;
        public bool HasEquippedArmor => equippedArmor != null;

        public event Action<ArmorData> OnArmorEquipped;

        private void OnEnable()
        {
            if (armorEquipped != null)
            {
                armorEquipped.OnEquippedArmorChanged += HandleEquippedArmorChanged;
            }
        }

        private void Start()
        {
            SyncFromEquippedState();
        }

        private void OnDisable()
        {
            if (armorEquipped != null)
            {
                armorEquipped.OnEquippedArmorChanged -= HandleEquippedArmorChanged;
            }
        }

        public bool EquipArmorById(string armorId)
        {
            if (armorEquipped == null)
            {
                return false;
            }

            return armorEquipped.EquipArmor(armorId);
        }

        public void EquipArmor(ArmorData armorData)
        {
            equippedArmor = armorData;
            ApplyEquippedArmorToRuntime();
            OnArmorEquipped?.Invoke(equippedArmor);
        }

        public void RepairEquippedArmorFull()
        {
            if (armorRuntime == null)
            {
                return;
            }

            armorRuntime.RestoreFullDurability();
        }

        public void SyncFromEquippedState()
        {
            if (armorEquipped == null)
            {
                ApplyEquippedArmorToRuntime();
                return;
            }

            EquipArmor(armorEquipped.GetEquippedArmorData());
        }

        private void ApplyEquippedArmorToRuntime()
        {
            if (armorRuntime == null)
            {
                return;
            }

            if (equippedArmor == null)
            {
                if (disableRuntimeArmorWhenNoArmorEquipped)
                {
                    armorRuntime.SetArmorEnabled(false);
                }

                return;
            }

            armorRuntime.SetArmorEnabled(true);
            armorRuntime.ApplyArmorStats(
                equippedArmor.MaxDurability,
                equippedArmor.DamageReductionEfficiency,
                restoreFullDurabilityOnEquip);
        }

        private void HandleEquippedArmorChanged(string armorId)
        {
            SyncFromEquippedState();
        }
    }
}
