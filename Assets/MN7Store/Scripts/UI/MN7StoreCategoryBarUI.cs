using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MN7.Store
{
    /// <summary>
    /// Reusable category bar. Layout belongs to the prefab; category data supplies only IDs and text.
    /// </summary>
    public class MN7StoreCategoryBarUI : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;
        [SerializeField] private MN7StoreCategoryButtonUI categoryButtonPrefab;

        private readonly List<MN7StoreCategoryButtonUI> buttons = new();
        private Action<string> onCategorySelected;
        private string selectedCategoryId = MN7StoreConfig.AllCategoryId;

        public RectTransform Content => content;
        public string SelectedCategoryId => selectedCategoryId;

        private void Awake() => ValidateReferences();

        public void Initialize(MN7StoreConfig config, string initialCategoryId, Action<string> categorySelected)
        {
            ValidateReferences();
            ClearButtons();
            onCategorySelected = categorySelected;
            selectedCategoryId = string.IsNullOrWhiteSpace(initialCategoryId)
                ? MN7StoreConfig.AllCategoryId
                : initialCategoryId;

            if (config == null || content == null || categoryButtonPrefab == null)
            {
                Debug.LogError("[MN7 Store] Category Bar cannot initialize. Assign Content and Category Button Prefab on the Category Bar prefab.");
                return;
            }

            foreach (MN7StoreCategoryData category in config.GetOrderedCategories())
            {
                if (category == null) continue;
                MN7StoreCategoryButtonUI button = Instantiate(categoryButtonPrefab, content);
                if (button == null) continue;
                button.name = $"Category_{category.categoryId}";
                button.Configure(category, () => SelectCategory(category.categoryId));
                buttons.Add(button);
            }

            SelectCategoryButton(selectedCategoryId);
        }

        public void SetSelectedCategory(string categoryId)
        {
            selectedCategoryId = string.IsNullOrWhiteSpace(categoryId)
                ? MN7StoreConfig.AllCategoryId
                : categoryId;
            SelectCategoryButton(selectedCategoryId);
        }

        private void SelectCategory(string categoryId)
        {
            selectedCategoryId = string.IsNullOrWhiteSpace(categoryId)
                ? MN7StoreConfig.AllCategoryId
                : categoryId;
            SelectCategoryButton(selectedCategoryId);
            onCategorySelected?.Invoke(selectedCategoryId);
        }

        private void SelectCategoryButton(string categoryId)
        {
            foreach (MN7StoreCategoryButtonUI button in buttons)
                if (button != null)
                    button.SetSelected(string.Equals(button.CategoryId, categoryId, StringComparison.OrdinalIgnoreCase));
        }

        private void ClearButtons()
        {
            foreach (MN7StoreCategoryButtonUI button in buttons)
                if (button != null)
                    Destroy(button.gameObject);
            buttons.Clear();
        }

        private void ValidateReferences()
        {
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
            if (content == null && scrollRect != null) content = scrollRect.content;

            if (scrollRect == null)
                Debug.LogError("[MN7 Store] Category Bar prefab is missing its ScrollRect reference.");
            if (content == null)
                Debug.LogError("[MN7 Store] Category Bar prefab is missing its Content reference.");
            if (categoryButtonPrefab == null)
                Debug.LogError("[MN7 Store] Category Bar prefab is missing its Category Button Prefab reference.");
        }
    }
}
