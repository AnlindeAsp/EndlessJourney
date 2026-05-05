using System;
using System.Collections.Generic;
using EndlessJourney.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessJourney.UI
{
    public readonly struct WeaponPageItemViewData2D
    {
        public WeaponPageItemViewData2D(WeaponData weaponData, bool unlocked, bool equipped, bool selected)
        {
            WeaponData = weaponData;
            Unlocked = unlocked;
            Equipped = equipped;
            Selected = selected;
        }

        public WeaponData WeaponData { get; }
        public bool Unlocked { get; }
        public bool Equipped { get; }
        public bool Selected { get; }
    }

    /// <summary>
    /// Renders the storage weapon page. It receives already-resolved view data from WeaponPageController2D.
    /// </summary>
    public class WeaponPageDisplayer2D : MonoBehaviour
    {
        [Header("List")]
        [SerializeField] private Transform rowParent;
        [SerializeField] private WeaponPageRow2D rowPrefab;
        [SerializeField] private GameObject emptyStateRoot;

        [Header("Details")]
        [SerializeField] private TMP_Text weaponNameText;
        [SerializeField] private TMP_Text weaponIdText;
        [SerializeField] private TMP_Text weaponTypeText;
        [SerializeField] private TMP_Text weaponStatsText;
        [SerializeField] private TMP_Text weaponStateText;
        [SerializeField] private TMP_Text weaponDescriptionText;
        [SerializeField] private Image weaponIconImage;
        [SerializeField] private Image weaponDetailImage;

        [Header("Actions")]
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipButtonText;

        private readonly List<WeaponPageRow2D> _spawnedRows = new List<WeaponPageRow2D>(16);

        private void OnDisable()
        {
            ClearActionButtons();
        }

        public void Render(
            IReadOnlyList<WeaponPageItemViewData2D> weapons,
            WeaponData selectedWeapon,
            string selectedWeaponId,
            string equippedWeaponId,
            Action<string> onSelectWeapon,
            Action onEquipSelected)
        {
            RenderList(weapons, onSelectWeapon);
            RenderDetails(selectedWeapon, selectedWeaponId, equippedWeaponId, weapons);
            BindActionButtons(selectedWeapon, selectedWeaponId, equippedWeaponId, weapons, onEquipSelected);
        }

        private void RenderList(IReadOnlyList<WeaponPageItemViewData2D> weapons, Action<string> onSelectWeapon)
        {
            ClearRows();

            bool hasRows = weapons != null && weapons.Count > 0;
            if (emptyStateRoot != null)
            {
                emptyStateRoot.SetActive(!hasRows);
            }

            if (!hasRows || rowParent == null || rowPrefab == null)
            {
                return;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponPageRow2D row = Instantiate(rowPrefab, rowParent);
                row.Bind(weapons[i], onSelectWeapon);
                _spawnedRows.Add(row);
            }
        }

        private void RenderDetails(
            WeaponData selectedWeapon,
            string selectedWeaponId,
            string equippedWeaponId,
            IReadOnlyList<WeaponPageItemViewData2D> weapons)
        {
            if (selectedWeapon == null)
            {
                SetText(weaponNameText, "No weapon selected");
                SetText(weaponIdText, string.Empty);
                SetText(weaponTypeText, string.Empty);
                SetText(weaponStatsText, string.Empty);
                SetText(weaponStateText, string.Empty);
                SetText(weaponDescriptionText, string.Empty);
                SetImage(weaponIconImage, null);
                SetImage(weaponDetailImage, null);
                return;
            }

            bool unlocked = TryFindItem(weapons, selectedWeaponId, out WeaponPageItemViewData2D item) && item.Unlocked;
            bool equipped = string.Equals(selectedWeaponId, equippedWeaponId, StringComparison.Ordinal);

            SetText(weaponNameText, selectedWeapon.WeaponName);
            SetText(weaponIdText, selectedWeapon.WeaponId);
            SetText(weaponTypeText, selectedWeapon.Type.ToString());
            SetText(weaponStatsText, $"Length {selectedWeapon.Length:0.##}\nSharpness {selectedWeapon.Sharpness:0.##}\nWeight {selectedWeapon.Weight:0.##}");
            SetText(weaponStateText, equipped ? "Equipped" : unlocked ? "Unlocked" : "Locked");
            SetText(weaponDescriptionText, selectedWeapon.Description);
            SetImage(weaponIconImage, selectedWeapon.Icon);
            SetImage(weaponDetailImage, selectedWeapon.DetailImage);
        }

        private void BindActionButtons(
            WeaponData selectedWeapon,
            string selectedWeaponId,
            string equippedWeaponId,
            IReadOnlyList<WeaponPageItemViewData2D> weapons,
            Action onEquipSelected)
        {
            ClearActionButtons();

            bool hasSelection = selectedWeapon != null && !string.IsNullOrWhiteSpace(selectedWeaponId);
            bool unlocked = TryFindItem(weapons, selectedWeaponId, out WeaponPageItemViewData2D item) && item.Unlocked;
            bool equipped = hasSelection && string.Equals(selectedWeaponId, equippedWeaponId, StringComparison.Ordinal);

            if (equipButton != null)
            {
                equipButton.interactable = hasSelection && unlocked && !equipped;
                equipButton.onClick.AddListener(() => onEquipSelected?.Invoke());
            }

            SetText(equipButtonText, equipped ? "Equipped" : "Equip");
        }

        private void ClearRows()
        {
            for (int i = 0; i < _spawnedRows.Count; i++)
            {
                WeaponPageRow2D row = _spawnedRows[i];
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _spawnedRows.Clear();
        }

        private void ClearActionButtons()
        {
            if (equipButton != null)
            {
                equipButton.onClick.RemoveAllListeners();
            }
        }

        private static void SetImage(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value ?? string.Empty;
            }
        }

        private static bool TryFindItem(IReadOnlyList<WeaponPageItemViewData2D> weapons, string weaponId, out WeaponPageItemViewData2D item)
        {
            if (weapons != null && !string.IsNullOrWhiteSpace(weaponId))
            {
                for (int i = 0; i < weapons.Count; i++)
                {
                    WeaponPageItemViewData2D candidate = weapons[i];
                    if (candidate.WeaponData != null && candidate.WeaponData.WeaponId == weaponId)
                    {
                        item = candidate;
                        return true;
                    }
                }
            }

            item = default;
            return false;
        }
    }
}
