using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

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
        [SerializeField] private StoragePageType2D defaultPage = StoragePageType2D.Spell;
        [SerializeField] private GameObject weaponPageRoot;
        [SerializeField] private GameObject spellPageRoot;

        [Header("Page Controllers")]
        [SerializeField] private WeaponPageController2D weaponPageController;
        [SerializeField] private SpellPageController2D spellPageController;

        [Header("Behavior")]
        [SerializeField] private bool openDefaultPageOnEnable = true;
        [SerializeField] private bool hidePagesOnAwake = true;

        [Header("Keyboard Navigation")]
        [SerializeField] private bool enableKeyboardPageNavigation = true;
        [SerializeField] private bool wrapPageNavigation = true;

        private StoragePageType2D _currentPage;
        private static readonly StoragePageType2D[] PageOrder =
        {
            StoragePageType2D.Weapon,
            StoragePageType2D.Spell
        };

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

        private void Update()
        {
            if (!enableKeyboardPageNavigation)
            {
                return;
            }

            if (WasPreviousPagePressedThisFrame())
            {
                OpenPreviousPage();
                return;
            }

            if (WasNextPagePressedThisFrame())
            {
                OpenNextPage();
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
            bool pageChanged = _currentPage != page;
            bool targetPageAlreadyActive = IsPageRootActive(page);
            _currentPage = page;
            SetPageRootVisible(weaponPageRoot, page == StoragePageType2D.Weapon);
            SetPageRootVisible(spellPageRoot, page == StoragePageType2D.Spell);

            if (!pageChanged && targetPageAlreadyActive)
            {
                RefreshCurrentPage();
            }
        }

        private bool IsPageRootActive(StoragePageType2D page)
        {
            GameObject pageRoot = page == StoragePageType2D.Weapon ? weaponPageRoot : spellPageRoot;
            return pageRoot != null && pageRoot.activeSelf;
        }

        private void RefreshCurrentPage()
        {
            if (_currentPage == StoragePageType2D.Weapon && weaponPageController != null)
            {
                weaponPageController.RefreshPage();
            }
            else if (_currentPage == StoragePageType2D.Spell && spellPageController != null)
            {
                spellPageController.RefreshPage();
            }
        }

        public void OpenPreviousPage()
        {
            MovePageSelection(-1);
        }

        public void OpenNextPage()
        {
            MovePageSelection(1);
        }

        private static void SetPageRootVisible(GameObject pageRoot, bool visible)
        {
            if (pageRoot != null)
            {
                pageRoot.SetActive(visible);
            }
        }

        private void MovePageSelection(int direction)
        {
            if (PageOrder.Length == 0)
            {
                return;
            }

            int currentIndex = FindCurrentPageIndex();
            if (currentIndex < 0)
            {
                currentIndex = 0;
            }

            int nextIndex = currentIndex + Math.Sign(direction);
            if (wrapPageNavigation)
            {
                if (nextIndex < 0)
                {
                    nextIndex = PageOrder.Length - 1;
                }
                else if (nextIndex >= PageOrder.Length)
                {
                    nextIndex = 0;
                }
            }
            else
            {
                nextIndex = Mathf.Clamp(nextIndex, 0, PageOrder.Length - 1);
            }

            OpenPage(PageOrder[nextIndex]);
        }

        private int FindCurrentPageIndex()
        {
            for (int i = 0; i < PageOrder.Length; i++)
            {
                if (PageOrder[i] == _currentPage)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool WasPreviousPagePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
#else
            return false;
#endif
        }

        private bool WasNextPagePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame))
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
#else
            return false;
#endif
        }
    }
}
