using System;
using System.Collections.Generic;
using EndlessJourney.Combat;
using UnityEngine;

namespace EndlessJourney.Player
{
    /// <summary>
    /// Holds all weapon inscription definitions and resolves them by stable inscription id.
    /// </summary>
    public class WeaponInscriptionLibrary2D : MonoBehaviour
    {
        [Header("Inscription Definitions")]
        [SerializeField] private WeaponInscriptionData[] allInscriptions = Array.Empty<WeaponInscriptionData>();

        private readonly Dictionary<string, WeaponInscriptionData> _inscriptionById = new Dictionary<string, WeaponInscriptionData>(16);
        private bool _indexBuilt;

        public int InscriptionCount => allInscriptions != null ? allInscriptions.Length : 0;

        private void Awake()
        {
            RebuildInscriptionIndex();
        }

        public bool HasInscription(string inscriptionId)
        {
            if (string.IsNullOrWhiteSpace(inscriptionId))
            {
                return false;
            }

            EnsureInscriptionIndexReady();
            return _inscriptionById.ContainsKey(inscriptionId.Trim());
        }

        public bool TryGetInscriptionData(string inscriptionId, out WeaponInscriptionData inscriptionData)
        {
            if (string.IsNullOrWhiteSpace(inscriptionId))
            {
                inscriptionData = null;
                return false;
            }

            EnsureInscriptionIndexReady();
            return _inscriptionById.TryGetValue(inscriptionId.Trim(), out inscriptionData) && inscriptionData != null;
        }

        public WeaponInscriptionData GetInscriptionData(string inscriptionId)
        {
            TryGetInscriptionData(inscriptionId, out WeaponInscriptionData inscriptionData);
            return inscriptionData;
        }

        public WeaponInscriptionData GetInscriptionAt(int index)
        {
            if (allInscriptions == null || index < 0 || index >= allInscriptions.Length)
            {
                return null;
            }

            return allInscriptions[index];
        }

        public void RebuildInscriptionIndex()
        {
            _inscriptionById.Clear();
            _indexBuilt = true;

            if (allInscriptions == null)
            {
                return;
            }

            for (int i = 0; i < allInscriptions.Length; i++)
            {
                WeaponInscriptionData inscriptionData = allInscriptions[i];
                if (inscriptionData == null || string.IsNullOrWhiteSpace(inscriptionData.InscriptionId))
                {
                    continue;
                }

                _inscriptionById[inscriptionData.InscriptionId] = inscriptionData;
            }
        }

        private void EnsureInscriptionIndexReady()
        {
            if (!_indexBuilt)
            {
                RebuildInscriptionIndex();
            }
        }

        private void OnValidate()
        {
            _indexBuilt = false;
        }
    }
}
