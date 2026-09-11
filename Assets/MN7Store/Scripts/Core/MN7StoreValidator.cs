using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Runtime/editor-safe validation for MN7 Store configuration.
    /// This validator does not modify the configuration.
    /// </summary>
    public static class MN7StoreValidator
    {
        public static bool ValidateConfig(
            MN7StoreConfig config,
            bool logMessages = true)
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
                LogError(
                    "Game ID is empty. Set a unique Game ID for every game using MN7 Store before shipping.",
                    logMessages);
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(effectiveSaveKey))
            {
                LogError(
                    "Store Config does not produce a valid save key.",
                    logMessages);
                valid = false;
            }
            else if (effectiveSaveKey == "MN7_STORE_DATA")
            {
                LogError(
                    "Store Config is still using the default save key 'MN7_STORE_DATA'. Set a unique Game ID before shipping.",
                    logMessages);
                valid = false;
            }

            HashSet<string> currencyIds = new HashSet<string>();

            if (config.currencies == null)
            {
                LogError("Currencies list is null.", logMessages);
                valid = false;
            }
            else
            {
                for (int i = 0; i < config.currencies.Count; i++)
                {
                    MN7CurrencyData currency = config.currencies[i];

                    if (currency == null)
                    {
                        LogError(
                            $"Currency at index {i} is null.",
                            logMessages);
                        valid = false;
                        continue;
                    }

                    string currencyId = currency.currencyId?.Trim();

                    if (string.IsNullOrWhiteSpace(currencyId))
                    {
                        LogError(
                            $"Currency at index {i} has an empty currency ID.",
                            logMessages);
                        valid = false;
                        continue;
                    }

                    if (!currencyIds.Add(currencyId))
                    {
                        LogError(
                            $"Duplicate currency ID: {currency.currencyId}",
                            logMessages);
                        valid = false;
                    }

                    if (currency.startingAmount < 0)
                    {
                        LogError(
                            $"Currency '{currency.currencyId}' has a negative starting amount.",
                            logMessages);
                        valid = false;
                    }
                }
            }

            HashSet<string> itemIds = new HashSet<string>();
            HashSet<string> iapProductIds = new HashSet<string>();

            if (config.items == null)
            {
                LogError("Store items list is null.", logMessages);
                valid = false;
            }
            else
            {
                // First collect every item ID so rewards can reference any item.
                for (int i = 0; i < config.items.Count; i++)
                {
                    MN7ItemData item = config.items[i];

                    if (item == null)
                    {
                        LogError(
                            $"Store item at index {i} is null.",
                            logMessages);
                        valid = false;
                        continue;
                    }

                    string itemId = item.itemId?.Trim();

                    if (string.IsNullOrWhiteSpace(itemId))
                    {
                        LogError(
                            $"Store item at index {i} has an empty item ID.",
                            logMessages);
                        valid = false;
                        continue;
                    }

                    if (!itemIds.Add(itemId))
                    {
                        LogError(
                            $"Duplicate item ID: {item.itemId}",
                            logMessages);
                        valid = false;
                    }
                }

                for (int i = 0; i < config.items.Count; i++)
                {
                    MN7ItemData item = config.items[i];

                    if (item == null)
                        continue;

                    string itemId = item.itemId?.Trim() ?? string.Empty;

                    if (item.rewards == null)
                    {
                        LogError(
                            $"Item '{item.itemId}' has a null rewards list.",
                            logMessages);
                        valid = false;
                    }

                    switch (item.purchaseType)
                    {
                        case MN7PurchaseType.Currency:
                            ValidateCurrencyPurchase(
                                item,
                                currencyIds,
                                logMessages,
                                ref valid);
                            break;

                        case MN7PurchaseType.CurrencyOrAd:
                            ValidateCurrencyPurchase(
                                item,
                                currencyIds,
                                logMessages,
                                ref valid);
                            ValidateRewardedAdSettings(
                                item,
                                logMessages,
                                ref valid);
                            break;

                        case MN7PurchaseType.RewardedAd:
                            ValidateRewardedAdSettings(
                                item,
                                logMessages,
                                ref valid);
                            break;

                        case MN7PurchaseType.IAP:
                            string iapProductId = item.iapProductId?.Trim();

                            if (string.IsNullOrWhiteSpace(iapProductId))
                            {
                                LogError(
                                    $"IAP item '{item.itemId}' has an empty IAP product ID.",
                                    logMessages);
                                valid = false;
                            }
                            else if (!iapProductIds.Add(iapProductId))
                            {
                                LogError(
                                    $"Duplicate IAP product ID: {item.iapProductId}",
                                    logMessages);
                                valid = false;
                            }
                            break;
                    }

                    if (item.rewards == null)
                        continue;

                    for (int rewardIndex = 0; rewardIndex < item.rewards.Count; rewardIndex++)
                    {
                        MN7RewardData reward = item.rewards[rewardIndex];

                        if (reward == null)
                        {
                            LogError(
                                $"Item '{item.itemId}' has a null reward at index {rewardIndex}.",
                                logMessages);
                            valid = false;
                            continue;
                        }

                        if (reward.rewardType == MN7RewardType.Currency)
                        {
                            if (reward.currencyAmount <= 0)
                            {
                                LogError(
                                    $"Item '{item.itemId}' has an invalid currency reward amount at index {rewardIndex}.",
                                    logMessages);
                                valid = false;
                            }

                            string rewardCurrencyId = reward.currencyId?.Trim();

                            if (string.IsNullOrWhiteSpace(rewardCurrencyId) ||
                                !currencyIds.Contains(rewardCurrencyId))
                            {
                                LogError(
                                    $"Item '{item.itemId}' has a currency reward referencing unknown currency '{reward.currencyId}'.",
                                    logMessages);
                                valid = false;
                            }
                        }
                        else if (reward.rewardType == MN7RewardType.Item)
                        {
                            if (reward.itemQuantity <= 0)
                            {
                                LogError(
                                    $"Item '{item.itemId}' has an invalid item reward quantity at index {rewardIndex}.",
                                    logMessages);
                                valid = false;
                            }

                            string rewardItemId = reward.itemId?.Trim();

                            if (string.IsNullOrWhiteSpace(rewardItemId))
                            {
                                LogError(
                                    $"Item '{item.itemId}' has an item reward with an empty item ID.",
                                    logMessages);
                                valid = false;
                            }
                            else if (!itemIds.Contains(rewardItemId))
                            {
                                LogError(
                                    $"Item '{item.itemId}' has an item reward referencing unknown item '{reward.itemId}'.",
                                    logMessages);
                                valid = false;
                            }
                        }
                    }
                }
            }

            if (valid)
            {
                LogMessage(
                    $"Store configuration validation passed. Save namespace: {effectiveSaveKey}",
                    logMessages);
            }
            else
            {
                LogError(
                    "Store configuration validation failed.",
                    logMessages);
            }

            return valid;
        }

        private static void ValidateCurrencyPurchase(
            MN7ItemData item,
            HashSet<string> currencyIds,
            bool logMessages,
            ref bool valid)
        {
            if (string.IsNullOrWhiteSpace(item.currencyId))
            {
                LogError(
                    $"Item '{item.itemId}' requires a currency ID.",
                    logMessages);
                valid = false;
            }
            else if (!currencyIds.Contains(item.currencyId.Trim()))
            {
                LogError(
                    $"Item '{item.itemId}' references unknown currency '{item.currencyId}'.",
                    logMessages);
                valid = false;
            }

            if (item.currencyCost <= 0)
            {
                LogError(
                    $"Item '{item.itemId}' must have a currency cost greater than zero.",
                    logMessages);
                valid = false;
            }
        }

        private static void ValidateRewardedAdSettings(
            MN7ItemData item,
            bool logMessages,
            ref bool valid)
        {
            if (item.requiredAds <= 0)
            {
                LogError(
                    $"Item '{item.itemId}' must require at least one rewarded ad.",
                    logMessages);
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(item.adPlacementId))
            {
                LogError(
                    $"Item '{item.itemId}' has an empty ad placement ID.",
                    logMessages);
                valid = false;
            }
        }

        private static void LogMessage(string message, bool enabled)
        {
            if (enabled)
                Debug.Log($"[MN7 Store] {message}");
        }

        private static void LogError(string message, bool enabled)
        {
            if (enabled)
                Debug.LogError($"[MN7 Store] {message}");
        }
    }
}
