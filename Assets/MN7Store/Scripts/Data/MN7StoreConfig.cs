using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MN7.Store
{
    [CreateAssetMenu(
        fileName = "MN7StoreConfig",
        menuName = "MN7 Store/Store Config"
    )]
    public class MN7StoreConfig : ScriptableObject
    {
        public const string AllCategoryId = "__ALL__";

        [Header("Game")]
        [Tooltip("Unique ID for the game using this store.")]
        public string gameId;

        [Header("Save")]
        [Tooltip("Fallback/legacy key. Game ID is preferred for isolated saves.")]
        public string saveKey = "MN7_STORE_DATA";

        [Header("Currencies")]
        public List<MN7CurrencyData> currencies = new();

        [Header("Categories")]
        [Tooltip("Add only the categories used by this game. ALL is automatic and always appears first.")]
        public List<MN7StoreCategoryData> categories = new();

        [Header("Store Items")]
        public List<MN7ItemData> items = new();

        public string GetEffectiveSaveKey()
        {
            if (!string.IsNullOrWhiteSpace(gameId))
            {
                string normalized = NormalizeGameId(gameId);
                if (!string.IsNullOrWhiteSpace(normalized))
                    return $"MN7_STORE_{normalized}";
            }

            return saveKey?.Trim();
        }

        public List<MN7StoreCategoryData> GetOrderedCategories()
        {
            List<MN7StoreCategoryData> result = new()
            {
                new MN7StoreCategoryData
                {
                    categoryId = AllCategoryId,
                    displayName = "ALL",
                    sortOrder = int.MinValue
                }
            };

            if (categories == null)
                return result;

            List<MN7StoreCategoryData> configured = new();
            foreach (MN7StoreCategoryData category in categories)
            {
                if (category == null || string.IsNullOrWhiteSpace(category.categoryId))
                    continue;

                string id = category.categoryId.Trim();
                if (id.Equals(AllCategoryId, StringComparison.OrdinalIgnoreCase))
                    continue;

                bool duplicate = false;
                foreach (MN7StoreCategoryData existing in configured)
                {
                    if (existing != null && string.Equals(existing.categoryId, id, StringComparison.OrdinalIgnoreCase))
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (duplicate)
                    continue;

                configured.Add(new MN7StoreCategoryData
                {
                    categoryId = id,
                    displayName = string.IsNullOrWhiteSpace(category.displayName) ? id : category.displayName.Trim(),
                    sortOrder = category.sortOrder
                });
            }

            configured.Sort((a, b) =>
            {
                int order = a.sortOrder.CompareTo(b.sortOrder);
                return order != 0
                    ? order
                    : string.Compare(a.categoryId, b.categoryId, StringComparison.OrdinalIgnoreCase);
            });

            result.AddRange(configured);
            return result;
        }

        private static string NormalizeGameId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            StringBuilder builder = new StringBuilder();
            foreach (char character in value.Trim())
            {
                if (char.IsLetterOrDigit(character) || character == '_' || character == '-')
                    builder.Append(char.ToUpperInvariant(character));
                else if (char.IsWhiteSpace(character))
                    builder.Append('_');
            }

            return builder.ToString();
        }
    }
}
