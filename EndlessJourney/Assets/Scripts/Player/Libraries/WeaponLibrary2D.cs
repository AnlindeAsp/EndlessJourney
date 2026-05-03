using System;
using System.Collections.Generic;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Holds all weapon definitions and resolves them by stable weapon id.
    /// Runtime state should store ids, not asset references.
    /// </summary>
    public class WeaponLibrary2D : MonoBehaviour
    {
        [Header("Weapon Definitions")]
        [SerializeField] private WeaponData[] allWeapons = Array.Empty<WeaponData>();

        private readonly Dictionary<string, WeaponData> _weaponById = new Dictionary<string, WeaponData>(32);

        private void Awake()
        {
            RebuildWeaponIndex();
        }

        public bool HasWeapon(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                return false;
            }

            return _weaponById.ContainsKey(weaponId);
        }

        public bool TryGetWeaponData(string weaponId, out WeaponData weaponData)
        {
            if (string.IsNullOrWhiteSpace(weaponId))
            {
                weaponData = null;
                return false;
            }

            return _weaponById.TryGetValue(weaponId, out weaponData) && weaponData != null;
        }

        public WeaponData GetWeaponData(string weaponId)
        {
            TryGetWeaponData(weaponId, out WeaponData weaponData);
            return weaponData;
        }

        public void RebuildWeaponIndex()
        {
            _weaponById.Clear();

            if (allWeapons == null)
            {
                return;
            }

            for (int i = 0; i < allWeapons.Length; i++)
            {
                WeaponData weaponData = allWeapons[i];
                if (weaponData == null || string.IsNullOrWhiteSpace(weaponData.WeaponId))
                {
                    continue;
                }

                _weaponById[weaponData.WeaponId] = weaponData;
            }
        }
    }
}
