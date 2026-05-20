using System;
using System.Collections.Generic;
using EndlessJourney.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessJourney.UI
{
    public readonly struct WeaponInscriptionWeaponViewData2D
    {
        public WeaponInscriptionWeaponViewData2D(
            WeaponData weaponData,
            bool unlocked,
            bool selected,
            bool equipped,
            WeaponInscriptionData inscriptionData)
        {
            WeaponData = weaponData;
            Unlocked = unlocked;
            Selected = selected;
            Equipped = equipped;
            InscriptionData = inscriptionData;
        }

        public WeaponData WeaponData { get; }
        public bool Unlocked { get; }
        public bool Selected { get; }
        public bool Equipped { get; }
        public bool HasInscription => InscriptionData != null;
        public WeaponInscriptionData InscriptionData { get; }
    }

    public readonly struct WeaponInscriptionChoiceViewData2D
    {
        public WeaponInscriptionChoiceViewData2D(
            WeaponInscriptionData inscriptionData,
            bool selected,
            bool engravedOnSelectedWeapon)
        {
            InscriptionData = inscriptionData;
            Selected = selected;
            EngravedOnSelectedWeapon = engravedOnSelectedWeapon;
        }

        public WeaponInscriptionData InscriptionData { get; }
        public bool Selected { get; }
        public bool EngravedOnSelectedWeapon { get; }
    }

    /// <summary>
    /// Renders the forge weapon inscription page. Record changes stay in WeaponInscriptionPageController2D.
    /// </summary>
    public class WeaponInscriptionPageDisplayer2D : MonoBehaviour
    {
        [Header("Weapon List")]
        [SerializeField] private Transform weaponRowParent;
        [SerializeField] private WeaponInscriptionWeaponRow2D weaponRowPrefab;
        [SerializeField] private GameObject emptyWeaponStateRoot;
        [SerializeField] private GameObject weaponFocusIndicator;

        [Header("Inscription List")]
        [SerializeField] private Transform inscriptionRowParent;
        [SerializeField] private WeaponInscriptionChoiceRow2D inscriptionRowPrefab;
        [SerializeField] private GameObject emptyInscriptionStateRoot;
        [SerializeField] private GameObject inscriptionFocusIndicator;

        [Header("Selected Weapon")]
        [SerializeField] private TMP_Text weaponNameText;
        [SerializeField] private TMP_Text weaponIdText;
        [SerializeField] private TMP_Text weaponTypeText;
        [SerializeField] private TMP_Text weaponStatsText;
        [SerializeField] private TMP_Text currentInscriptionText;
        [SerializeField] private Image weaponIconImage;
        [SerializeField] private Image weaponDetailImage;

        [Header("Selected Inscription")]
        [SerializeField] private TMP_Text inscriptionNameText;
        [SerializeField] private TMP_Text inscriptionIdText;
        [SerializeField] private TMP_Text inscriptionEffectText;
        [SerializeField] private TMP_Text inscriptionFlavorText;
        [SerializeField] private TMP_Text inscriptionDescriptionText;

        [Header("Actions")]
        [SerializeField] private Button engraveButton;
        [SerializeField] private TMP_Text engraveButtonText;
        [SerializeField] private Button clearButton;
        [SerializeField] private TMP_Text clearButtonText;
        [SerializeField] private Button focusWeaponListButton;
        [SerializeField] private Button focusInscriptionListButton;

        private readonly List<WeaponInscriptionWeaponRow2D> _spawnedWeaponRows = new List<WeaponInscriptionWeaponRow2D>(16);
        private readonly List<WeaponInscriptionChoiceRow2D> _spawnedInscriptionRows = new List<WeaponInscriptionChoiceRow2D>(16);

        private void OnDisable()
        {
            ClearActionButtons();
        }

        public void Render(
            IReadOnlyList<WeaponInscriptionWeaponViewData2D> weapons,
            IReadOnlyList<WeaponInscriptionChoiceViewData2D> inscriptions,
            WeaponData selectedWeapon,
            WeaponInscriptionData selectedInscription,
            WeaponInscriptionData currentWeaponInscription,
            WeaponInscriptionPageFocus2D currentFocus,
            Action<string> onSelectWeapon,
            Action<string> onSelectInscription,
            Action onEngraveSelected,
            Action onClearSelectedWeapon,
            Action onFocusWeaponList,
            Action onFocusInscriptionList)
        {
            RenderWeaponList(weapons, onSelectWeapon);
            RenderInscriptionList(inscriptions, onSelectInscription);
            RenderFocusState(currentFocus);
            RenderWeaponDetails(selectedWeapon, currentWeaponInscription);
            RenderInscriptionDetails(selectedInscription);
            BindActionButtons(selectedWeapon, selectedInscription, currentWeaponInscription, weapons, onEngraveSelected, onClearSelectedWeapon, onFocusWeaponList, onFocusInscriptionList);
        }

        private void RenderWeaponList(IReadOnlyList<WeaponInscriptionWeaponViewData2D> weapons, Action<string> onSelectWeapon)
        {
            ClearWeaponRows();

            bool hasRows = weapons != null && weapons.Count > 0;
            if (emptyWeaponStateRoot != null)
            {
                emptyWeaponStateRoot.SetActive(!hasRows);
            }

            if (!hasRows || weaponRowParent == null || weaponRowPrefab == null)
            {
                return;
            }

            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponInscriptionWeaponRow2D row = Instantiate(weaponRowPrefab, weaponRowParent);
                row.Bind(weapons[i], onSelectWeapon);
                _spawnedWeaponRows.Add(row);
            }
        }

        private void RenderInscriptionList(IReadOnlyList<WeaponInscriptionChoiceViewData2D> inscriptions, Action<string> onSelectInscription)
        {
            ClearInscriptionRows();

            bool hasRows = inscriptions != null && inscriptions.Count > 0;
            if (emptyInscriptionStateRoot != null)
            {
                emptyInscriptionStateRoot.SetActive(!hasRows);
            }

            if (!hasRows || inscriptionRowParent == null || inscriptionRowPrefab == null)
            {
                return;
            }

            for (int i = 0; i < inscriptions.Count; i++)
            {
                WeaponInscriptionChoiceRow2D row = Instantiate(inscriptionRowPrefab, inscriptionRowParent);
                row.Bind(inscriptions[i], onSelectInscription);
                _spawnedInscriptionRows.Add(row);
            }
        }

        private void RenderFocusState(WeaponInscriptionPageFocus2D currentFocus)
        {
            SetIndicator(weaponFocusIndicator, currentFocus == WeaponInscriptionPageFocus2D.WeaponList);
            SetIndicator(inscriptionFocusIndicator, currentFocus == WeaponInscriptionPageFocus2D.InscriptionList);
        }

        private void RenderWeaponDetails(WeaponData selectedWeapon, WeaponInscriptionData currentWeaponInscription)
        {
            if (selectedWeapon == null)
            {
                SetText(weaponNameText, "No weapon selected");
                SetText(weaponIdText, string.Empty);
                SetText(weaponTypeText, string.Empty);
                SetText(weaponStatsText, string.Empty);
                SetText(currentInscriptionText, string.Empty);
                SetImage(weaponIconImage, null);
                SetImage(weaponDetailImage, null);
                return;
            }

            float effectiveSharpness = currentWeaponInscription != null
                ? currentWeaponInscription.ModifySharpness(selectedWeapon.Sharpness)
                : selectedWeapon.Sharpness;
            float effectiveWeight = currentWeaponInscription != null
                ? currentWeaponInscription.ModifyWeight(selectedWeapon.Weight)
                : selectedWeapon.Weight;

            SetText(weaponNameText, selectedWeapon.WeaponName);
            SetText(weaponIdText, selectedWeapon.WeaponId);
            SetText(weaponTypeText, selectedWeapon.Type.ToString());
            SetText(weaponStatsText,
                $"Length {selectedWeapon.Length:0.##}\nSharpness {selectedWeapon.Sharpness:0.##} -> {effectiveSharpness:0.##}\nWeight {selectedWeapon.Weight:0.##} -> {effectiveWeight:0.##}");
            SetText(currentInscriptionText, currentWeaponInscription != null ? currentWeaponInscription.InscriptionName : "No inscription");
            SetImage(weaponIconImage, selectedWeapon.Icon);
            SetImage(weaponDetailImage, selectedWeapon.DetailImage);
        }

        private void RenderInscriptionDetails(WeaponInscriptionData selectedInscription)
        {
            if (selectedInscription == null)
            {
                SetText(inscriptionNameText, "No inscription selected");
                SetText(inscriptionIdText, string.Empty);
                SetText(inscriptionEffectText, string.Empty);
                SetText(inscriptionFlavorText, string.Empty);
                SetText(inscriptionDescriptionText, string.Empty);
                return;
            }

            SetText(inscriptionNameText, selectedInscription.InscriptionName);
            SetText(inscriptionIdText, selectedInscription.InscriptionId);
            SetText(inscriptionEffectText, BuildEffectText(selectedInscription));
            SetText(inscriptionFlavorText, selectedInscription.FlavorText);
            SetText(inscriptionDescriptionText, selectedInscription.Description);
        }

        private void BindActionButtons(
            WeaponData selectedWeapon,
            WeaponInscriptionData selectedInscription,
            WeaponInscriptionData currentWeaponInscription,
            IReadOnlyList<WeaponInscriptionWeaponViewData2D> weapons,
            Action onEngraveSelected,
            Action onClearSelectedWeapon,
            Action onFocusWeaponList,
            Action onFocusInscriptionList)
        {
            ClearActionButtons();

            bool selectedWeaponUnlocked = TryFindWeaponItem(weapons, selectedWeapon, out WeaponInscriptionWeaponViewData2D item) && item.Unlocked;
            bool hasWeapon = selectedWeapon != null && selectedWeaponUnlocked;
            bool hasInscriptionSelection = selectedInscription != null;
            bool sameAsCurrent = selectedInscription != null
                && currentWeaponInscription != null
                && selectedInscription.InscriptionId == currentWeaponInscription.InscriptionId;

            if (engraveButton != null)
            {
                engraveButton.interactable = hasWeapon && hasInscriptionSelection && !sameAsCurrent;
                engraveButton.onClick.AddListener(() => onEngraveSelected?.Invoke());
            }

            SetText(engraveButtonText, sameAsCurrent ? "Engraved" : "Engrave");

            if (clearButton != null)
            {
                clearButton.interactable = hasWeapon && currentWeaponInscription != null;
                clearButton.onClick.AddListener(() => onClearSelectedWeapon?.Invoke());
            }

            SetText(clearButtonText, "Erase");

            if (focusWeaponListButton != null)
            {
                focusWeaponListButton.onClick.AddListener(() => onFocusWeaponList?.Invoke());
            }

            if (focusInscriptionListButton != null)
            {
                focusInscriptionListButton.onClick.AddListener(() => onFocusInscriptionList?.Invoke());
            }
        }

        private static string BuildEffectText(WeaponInscriptionData inscription)
        {
            if (inscription == null)
            {
                return string.Empty;
            }

            string timeout = inscription.TimeoutSeconds > 0f ? $"\nTimeout {inscription.TimeoutSeconds:0.##}s" : string.Empty;
            return $"{inscription.EffectType}\nValue {inscription.Value:0.##}{timeout}";
        }

        private void ClearWeaponRows()
        {
            for (int i = 0; i < _spawnedWeaponRows.Count; i++)
            {
                WeaponInscriptionWeaponRow2D row = _spawnedWeaponRows[i];
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _spawnedWeaponRows.Clear();
        }

        private void ClearInscriptionRows()
        {
            for (int i = 0; i < _spawnedInscriptionRows.Count; i++)
            {
                WeaponInscriptionChoiceRow2D row = _spawnedInscriptionRows[i];
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            _spawnedInscriptionRows.Clear();
        }

        private void ClearActionButtons()
        {
            if (engraveButton != null)
            {
                engraveButton.onClick.RemoveAllListeners();
            }

            if (clearButton != null)
            {
                clearButton.onClick.RemoveAllListeners();
            }

            if (focusWeaponListButton != null)
            {
                focusWeaponListButton.onClick.RemoveAllListeners();
            }

            if (focusInscriptionListButton != null)
            {
                focusInscriptionListButton.onClick.RemoveAllListeners();
            }
        }

        private static bool TryFindWeaponItem(
            IReadOnlyList<WeaponInscriptionWeaponViewData2D> weapons,
            WeaponData selectedWeapon,
            out WeaponInscriptionWeaponViewData2D item)
        {
            if (weapons != null && selectedWeapon != null)
            {
                for (int i = 0; i < weapons.Count; i++)
                {
                    WeaponInscriptionWeaponViewData2D candidate = weapons[i];
                    if (candidate.WeaponData != null && candidate.WeaponData.WeaponId == selectedWeapon.WeaponId)
                    {
                        item = candidate;
                        return true;
                    }
                }
            }

            item = default;
            return false;
        }

        private static void SetIndicator(GameObject indicator, bool visible)
        {
            if (indicator != null)
            {
                indicator.SetActive(visible);
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
    }
}
