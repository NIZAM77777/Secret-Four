using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public static class MN7StoreValidator
    {
        public static bool ValidateConfig(MN7StoreConfig config, bool logMessages = true)
        {
            return ValidateConfig(config, null, logMessages);
        }

        public static bool ValidateConfig(MN7StoreConfig config, MN7StoreFeatureConfig features, bool logMessages = true)
        {
            bool valid = true;
            if (config == null)
            {
                LogError("Store Config is null.", logMessages);
                return false;
            }

            string effectiveSaveKey = config.GetEffectiveSaveKey();
            if (string.IsNullOrWhiteSpace(config.gameId))
            {
                LogError("Game ID is empty. Set a unique Game ID for every game using MN7 Store before shipping.", logMessages);
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(effectiveSaveKey) || effectiveSaveKey == "MN7_STORE_DATA")
            {
                LogError("Store Config does not have a unique valid save namespace. Set a unique Game ID.", logMessages);
                valid = false;
            }

            bool coinsEnabled = features == null || features.coins;
            bool iapEnabled = features == null || features.iap;
            bool adsEnabled = features == null || features.adRewards;
            bool freeEnabled = features == null || features.freeItems;

            HashSet<string> currencyIds = new(StringComparer.OrdinalIgnoreCase);
            if (config.currencies == null)
            {
                if (coinsEnabled)
                {
                    LogError("Currencies list is null while Coins are enabled.", logMessages);
                    valid = false;
                }
            }
            else
            {
                for (int i = 0; i < config.currencies.Count; i++)
                {
                    MN7CurrencyData currency = config.currencies[i];
                    if (currency == null)
                    {
                        LogError($"Currency at index {i} is null.", logMessages);
                        valid = false;
                        continue;
                    }
                    string id = currency.currencyId?.Trim();
                    if (string.IsNullOrWhiteSpace(id) || !currencyIds.Add(id))
                    {
                        LogError($"Currency at index {i} has an empty or duplicate ID.", logMessages);
                        valid = false;
                    }
                    if (currency.startingAmount < 0)
                    {
                        LogError($"Currency '{currency.currencyId}' has a negative starting amount.", logMessages);
                        valid = false;
                    }
                }
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
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        LogError($"Store category at index {i} has an empty category ID.", logMessages);
                        valid = false;
                        continue;
                    }
                    if (id.Equals(MN7StoreConfig.AllCategoryId, StringComparison.OrdinalIgnoreCase) || !categoryIds.Add(id))
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
            HashSet<string> iapProductIds = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> defaultSelectionGroups = new(StringComparer.OrdinalIgnoreCase);

            if (config.items == null)
            {
                LogError("Store items list is null.", logMessages);
                valid = false;
            }
            else
            {
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
                }

                foreach (MN7ItemData item in config.items)
                {
                    if (item == null) continue;

                    if (item.categoryIds != null)
                    {
                        foreach (string categoryValue in item.categoryIds)
                        {
                            string categoryId = categoryValue?.Trim();
                            if (string.IsNullOrWhiteSpace(categoryId)) continue;
                            if (categoryId.Equals(MN7StoreConfig.AllCategoryId, StringComparison.OrdinalIgnoreCase) || !categoryIds.Contains(categoryId))
                            {
                                LogError($"Item '{item.itemId}' references unknown/reserved category '{categoryValue}'.", logMessages);
                                valid = false;
                            }
                        }
                    }

                    if (item.rewards == null)
                    {
                        LogError($"Item '{item.itemId}' has a null rewards list.", logMessages);
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
                    if (item.defaultSelected && !item.defaultOwned)
                    {
                        LogError($"Item '{item.itemId}' cannot be Default Selected unless Default Owned is enabled.", logMessages);
                        valid = false;
                    }
                    if (item.defaultSelected && item.selectable)
                    {
                        string group = item.selectionGroupId.Trim();
                        if (!defaultSelectionGroups.Add(group))
                        {
                            LogError($"Multiple Default Selected items use Selection Group '{group}'. Only one should be default-selected.", logMessages);
                            valid = false;
                        }
                    }

                    switch (item.purchaseType)
                    {
                        case MN7PurchaseType.Free:
                            if (!freeEnabled)
                                LogWarning($"Item '{item.itemId}' is Free, but Free Items are disabled. The item will be unavailable.", logMessages);
                            break;

                        case MN7PurchaseType.Currency:
                            if (!coinsEnabled)
                            {
                                LogWarning($"Item '{item.itemId}' uses Currency, but Coins are disabled. Its currency route is unavailable.", logMessages);
                            }
                            else
                            {
                                ValidateCurrencyPurchase(item, currencyIds, logMessages, ref valid);
                            }
                            break;

                        case MN7PurchaseType.CurrencyOrAd:
                            if (coinsEnabled)
                                ValidateCurrencyPurchase(item, currencyIds, logMessages, ref valid);
                            else
                                LogWarning($"Item '{item.itemId}' has a Currency route, but Coins are disabled. That route will be unavailable.", logMessages);

                            if (adsEnabled)
                                ValidateRewardedAdSettings(item, logMessages, ref valid);
                            else
                                LogWarning($"Item '{item.itemId}' has an Ad route, but Ad Rewards are disabled. That route will be unavailable.", logMessages);

                            if (!coinsEnabled && !adsEnabled)
                                LogWarning($"Item '{item.itemId}' has no enabled CurrencyOrAd purchase route.", logMessages);
                            break;

                        case MN7PurchaseType.RewardedAd:
                            if (!adsEnabled)
                                LogWarning($"Item '{item.itemId}' uses Rewarded Ad, but Ad Rewards are disabled. The item will be unavailable.", logMessages);
                            else
                                ValidateRewardedAdSettings(item, logMessages, ref valid);
                            break;

                        case MN7PurchaseType.IAP:
                            if (!iapEnabled)
                            {
                                LogWarning($"Item '{item.itemId}' uses IAP, but IAP is disabled. The item will be unavailable.", logMessages);
                            }
                            else
                            {
                                string productId = item.iapProductId?.Trim();
                                if (string.IsNullOrWhiteSpace(productId) || !iapProductIds.Add(productId))
                                {
                                    LogError($"IAP item '{item.itemId}' has an empty or duplicate product ID.", logMessages);
                                    valid = false;
                                }
                            }
                            break;
                    }

                    bool hasSelfItemReward = false;
                    bool hasMatchingItemReward = false;

                    if (item.rewards == null) continue;
                    foreach (MN7RewardData reward in item.rewards)
                    {
                        if (reward == null)
                        {
                            LogError($"Item '{item.itemId}' contains a null reward.", logMessages);
                            valid = false;
                            continue;
                        }

                        if (reward.rewardType == MN7RewardType.Currency)
                        {
                            if (reward.currencyAmount <= 0 || string.IsNullOrWhiteSpace(reward.currencyId) || !currencyIds.Contains(reward.currencyId.Trim()))
                            {
                                LogError($"Item '{item.itemId}' has an invalid currency reward.", logMessages);
                                valid = false;
                            }
                            else if (!coinsEnabled)
                            {
                                // CurrencyManager is not initialized when Coins
                                // are disabled, so allowing this would create a
                                // purchase that can succeed but cannot grant its reward.
                                LogError($"Item '{item.itemId}' contains a Currency reward while Coins are disabled. Remove that reward or enable Coins.", logMessages);
                                valid = false;
                            }
                        }
                        else if (reward.rewardType == MN7RewardType.Item)
                        {
                            if (reward.itemQuantity <= 0 || string.IsNullOrWhiteSpace(reward.itemId) || !itemIds.Contains(reward.itemId.Trim()))
                            {
                                LogError($"Item '{item.itemId}' has an invalid item reward reference.", logMessages);
                                valid = false;
                            }
                            else
                            {
                                string rewardItemId = reward.itemId.Trim();
                                hasMatchingItemReward |= rewardItemId.Equals(item.itemId?.Trim(), StringComparison.OrdinalIgnoreCase);
                                hasSelfItemReward |= rewardItemId.Equals(item.itemId?.Trim(), StringComparison.OrdinalIgnoreCase);
                            }
                        }
                        else
                        {
                            LogError($"Item '{item.itemId}' contains an unsupported reward type.", logMessages);
                            valid = false;
                        }
                    }

                    bool isPurchasable =
                        item.purchaseType == MN7PurchaseType.Free ||
                        item.purchaseType == MN7PurchaseType.Currency ||
                        item.purchaseType == MN7PurchaseType.CurrencyOrAd ||
                        item.purchaseType == MN7PurchaseType.RewardedAd ||
                        item.purchaseType == MN7PurchaseType.IAP;

                    if (item.selectable &&
                        item.ownershipType == MN7ItemOwnershipType.Permanent &&
                        isPurchasable &&
                        !item.defaultOwned &&
                        !hasMatchingItemReward)
                    {
                        LogError($"Selectable permanent item '{item.itemId}' must reward itself as an Item (quantity 1) so ownership can be granted after purchase.", logMessages);
                        valid = false;
                    }

                    if (hasSelfItemReward && item.ownershipType == MN7ItemOwnershipType.Permanent)
                    {
                        foreach (MN7RewardData reward in item.rewards)
                        {
                            if (reward == null || reward.rewardType != MN7RewardType.Item) continue;
                            if (!string.Equals(reward.itemId?.Trim(), item.itemId?.Trim(), StringComparison.OrdinalIgnoreCase)) continue;
                            if (reward.itemQuantity != 1)
                            {
                                LogError($"Permanent item '{item.itemId}' must reward itself with quantity 1.", logMessages);
                                valid = false;
                            }
                        }
                    }
                }
            }

            if (valid) LogMessage("Store configuration validation passed. Save namespace: " + effectiveSaveKey, logMessages);
            else LogError("Store configuration validation failed.", logMessages);
            return valid;
        }

        private static void ValidateCurrencyPurchase(MN7ItemData item, HashSet<string> currencyIds, bool logMessages, ref bool valid)
        {
            if (string.IsNullOrWhiteSpace(item.currencyId) || !currencyIds.Contains(item.currencyId.Trim()))
            {
                LogError($"Item '{item.itemId}' references an unknown currency.", logMessages);
                valid = false;
            }
            if (item.currencyCost <= 0)
            {
                LogError($"Item '{item.itemId}' must have a currency cost greater than zero.", logMessages);
                valid = false;
            }
        }

        private static void ValidateRewardedAdSettings(MN7ItemData item, bool logMessages, ref bool valid)
        {
            if (item.requiredAds <= 0 || string.IsNullOrWhiteSpace(item.adPlacementId))
            {
                LogError($"Item '{item.itemId}' has invalid rewarded-ad settings.", logMessages);
                valid = false;
            }
        }

        private static void LogMessage(string message, bool enabled)
        {
            if (enabled) Debug.Log("[MN7 Store] " + message);
        }

        private static void LogWarning(string message, bool enabled)
        {
            if (enabled) Debug.LogWarning("[MN7 Store] " + message);
        }

        private static void LogError(string message, bool enabled)
        {
            if (enabled) Debug.LogError("[MN7 Store] " + message);
        }
    }
}
