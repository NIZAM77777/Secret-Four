using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public static class MN7StoreValidator
    {
        public static bool ValidateConfig(MN7StoreConfig config, bool logMessages = true)
        {
            bool valid = true;

            if (config == null)
                return LogResult(false, "Store Config is null.", logMessages);

            string saveKey = config.GetEffectiveSaveKey();
            if (string.IsNullOrWhiteSpace(config.gameId))
            {
                LogError("Game ID is empty. Set a unique Game ID before shipping.", logMessages);
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(saveKey) || saveKey == "MN7_STORE_DATA")
            {
                LogError("Store Config does not have a unique valid save namespace.", logMessages);
                valid = false;
            }

            HashSet<string> categoryIds = new(StringComparer.OrdinalIgnoreCase);
            if (config.categories != null)
            {
                for (int i = 0; i < config.categories.Count; i++)
                {
                    MN7StoreCategoryData category = config.categories[i];
                    if (category == null)
                    {
                        LogError($"Store category at index {i} is null.", logMessages);
                        valid = false;
                        continue;
                    }

                    string id = category.categoryId?.Trim();
                    if (string.IsNullOrWhiteSpace(id) ||
                        id.Equals(MN7StoreConfig.AllCategoryId, StringComparison.OrdinalIgnoreCase) ||
                        !categoryIds.Add(id))
                    {
                        LogError($"Invalid or duplicate store category ID: {category.categoryId}", logMessages);
                        valid = false;
                    }

                    if (string.IsNullOrWhiteSpace(category.displayName))
                    {
                        LogError($"Store category '{category.categoryId}' has an empty display name.", logMessages);
                        valid = false;
                    }
                }
            }

            HashSet<string> itemIds = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> defaultSelectionGroups = new(StringComparer.OrdinalIgnoreCase);

            if (config.items == null)
            {
                LogError("Store items list is null.", logMessages);
                valid = false;
                return LogResult(valid, saveKey, logMessages);
            }

            foreach (MN7ItemData item in config.items)
            {
                if (item == null)
                {
                    LogError("Store contains a null item.", logMessages);
                    valid = false;
                    continue;
                }

                string itemId = item.itemId?.Trim();
                if (string.IsNullOrWhiteSpace(itemId) || !itemIds.Add(itemId))
                {
                    LogError($"Store item has an empty or duplicate ID: {item.itemId}", logMessages);
                    valid = false;
                }

                if (string.IsNullOrWhiteSpace(item.displayName))
                {
                    LogError($"Store item '{item.itemId}' has an empty display name.", logMessages);
                    valid = false;
                }

                if (item.purchaseType != MN7PurchaseType.Free && item.purchaseType != MN7PurchaseType.RewardedAd)
                {
                    LogError($"Item '{item.itemId}' has an unsupported acquisition method.", logMessages);
                    valid = false;
                }

                if (item.purchaseType == MN7PurchaseType.RewardedAd &&
                    (item.requiredAds <= 0 || string.IsNullOrWhiteSpace(item.adPlacementId)))
                {
                    LogError($"Ad item '{item.itemId}' has invalid rewarded-ad settings.", logMessages);
                    valid = false;
                }

                if (item.purchaseType == MN7PurchaseType.Free && item.defaultSelected && !item.defaultOwned)
                {
                    LogError($"Free item '{item.itemId}' cannot be Default Selected unless Default Owned is enabled.", logMessages);
                    valid = false;
                }

                if (item.selectable && string.IsNullOrWhiteSpace(item.selectionGroupId))
                {
                    LogError($"Selectable item '{item.itemId}' must have a Selection Group ID.", logMessages);
                    valid = false;
                }

                if (!item.selectable && item.defaultSelected)
                {
                    LogError($"Item '{item.itemId}' cannot be Default Selected when Selectable is disabled.", logMessages);
                    valid = false;
                }

                if (item.defaultSelected && item.selectable)
                {
                    string group = item.selectionGroupId.Trim();
                    if (!defaultSelectionGroups.Add(group))
                    {
                        LogError($"Multiple Default Selected items use Selection Group '{group}'.", logMessages);
                        valid = false;
                    }
                }

                if (item.categoryIds != null)
                {
                    foreach (string categoryValue in item.categoryIds)
                    {
                        string categoryId = categoryValue?.Trim();
                        if (string.IsNullOrWhiteSpace(categoryId))
                            continue;

                        if (categoryId.Equals(MN7StoreConfig.AllCategoryId, StringComparison.OrdinalIgnoreCase) ||
                            !categoryIds.Contains(categoryId))
                        {
                            LogError($"Item '{item.itemId}' references unknown/reserved category '{categoryValue}'.", logMessages);
                            valid = false;
                        }
                    }
                }

                if (item.rewards == null || item.rewards.Count == 0)
                {
                    LogError($"Item '{item.itemId}' must contain at least one Item reward.", logMessages);
                    valid = false;
                    continue;
                }

                bool rewardsSelf = false;
                foreach (MN7RewardData reward in item.rewards)
                {
                    if (reward == null ||
                        reward.rewardType != MN7RewardType.Item ||
                        string.IsNullOrWhiteSpace(reward.itemId) ||
                        reward.itemQuantity <= 0 ||
                        !itemIds.Contains(reward.itemId.Trim()))
                    {
                        LogError($"Item '{item.itemId}' has an invalid Item reward reference.", logMessages);
                        valid = false;
                        continue;
                    }

                    if (string.Equals(reward.itemId.Trim(), item.itemId.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        rewardsSelf = true;
                        if (item.ownershipType == MN7ItemOwnershipType.Permanent && reward.itemQuantity != 1)
                        {
                            LogError($"Permanent item '{item.itemId}' must reward itself with quantity 1.", logMessages);
                            valid = false;
                        }
                    }
                }

                if (item.selectable && item.ownershipType == MN7ItemOwnershipType.Permanent &&
                    !item.defaultOwned && !rewardsSelf)
                {
                    LogError($"Selectable permanent item '{item.itemId}' must reward itself so ownership can be granted.", logMessages);
                    valid = false;
                }
            }

            return LogResult(valid, saveKey, logMessages);
        }

        private static bool LogResult(bool valid, string message, bool enabled)
        {
            if (enabled)
            {
                if (valid)
                    Debug.Log("[MN7 Store] Store configuration validation passed. Save namespace: " + message);
                else
                    Debug.LogError("[MN7 Store] Store configuration validation failed. " + message);
            }

            return valid;
        }

        private static void LogError(string message, bool enabled)
        {
            if (enabled) Debug.LogError("[MN7 Store] " + message);
        }
    }
}
