using UnityEngine;

namespace EndlessJourney.UI
{
    public enum StoragePageType2D
    {
        Weapon,
        Spell
    }

    /// <summary>
    /// Controls page switching inside the saving library / storage canvas.
    /// </summary>
    public class StorageCanvasController2D : MonoBehaviour
    {
        [Header("Pages")]
        [SerializeField] private StoragePageType2D defaultPage = StoragePageType2D.Weapon;
        [SerializeField] private GameObject weaponPageRoot;
        [SerializeField] private GameObject spellPageRoot;

        [Header("Page Controllers")]
        [SerializeField] private WeaponPageController2D weaponPageController;

        [Header("Behavior")]
        [SerializeField] private bool openDefaultPageOnEnable = true;
        [SerializeField] private bool hidePagesOnAwake = true;

        private StoragePageType2D _currentPage;

        public StoragePageType2D CurrentPage => _currentPage;

        private void Awake()
        {
            if (hidePagesOnAwake)
            {
                SetPageRootVisible(weaponPageRoot, false);
                SetPageRootVisible(spellPageRoot, false);
            }
        }

        private void OnEnable()
        {
            if (openDefaultPageOnEnable)
            {
                OpenPage(defaultPage);
            }
        }

        public void OpenWeaponPage()
        {
            OpenPage(StoragePageType2D.Weapon);
        }

        public void OpenSpellPage()
        {
            OpenPage(StoragePageType2D.Spell);
        }

        public void OpenPage(StoragePageType2D page)
        {
            _currentPage = page;
            SetPageRootVisible(weaponPageRoot, page == StoragePageType2D.Weapon);
            SetPageRootVisible(spellPageRoot, page == StoragePageType2D.Spell);

            if (page == StoragePageType2D.Weapon && weaponPageController != null)
            {
                weaponPageController.RefreshPage();
            }
        }

        private static void SetPageRootVisible(GameObject pageRoot, bool visible)
        {
            if (pageRoot != null)
            {
                pageRoot.SetActive(visible);
            }
        }
    }
}
