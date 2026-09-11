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
            {
                LogError("Secret Four Store Config is null.", logMessages);
                return false;
            }

            if (string.IsNullOrWhiteSpace(config.gameId))
            {
                LogError("Secret Four Game ID is empty.", logMessages);
                valid = false;
            }

            HashSet<string> itemIds = new HashSet<string>();
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
                        valid = false;
                        continue;
                    }

                    string itemId = item.itemId?.Trim();
                    if (string.IsNullOrWhiteSpace(itemId) || !itemIds.Add(itemId))
                    {
                        LogError($"Invalid or duplicate item ID: {item.itemId}", logMessages);
                        valid = false;
                        continue;
                    }

                    if (item.purchaseType == MN7PurchaseType.RewardedAd)
                    {
                        if (item.requiredAds <= 0 || string.IsNullOrWhiteSpace(item.adPlacementId))
                        {
                            LogError($"Rewarded-ad settings are invalid for item '{item.itemId}'.", logMessages);
                            valid = false;
                        }
                    }

                    if (item.rewards == null || item.rewards.Count == 0)
                    {
                        LogError($"Item '{item.itemId}' has no rewards.", logMessages);
                        valid = false;
                        continue;
                    }

                    foreach (MN7RewardData reward in item.rewards)
                    {
                        if (reward == null || reward.rewardType != MN7RewardType.Item ||
                            string.IsNullOrWhiteSpace(reward.itemId) || reward.itemQuantity <= 0)
                        {
                            LogError($"Item '{item.itemId}' contains an invalid item reward.", logMessages);
                            valid = false;
                        }
                    }
                }

                // Validate reward references after all item IDs have been collected.
                foreach (MN7ItemData item in config.items)
                {
                    if (item?.rewards == null) continue;
                    foreach (MN7RewardData reward in item.rewards)
                    {
                        if (reward != null && !string.IsNullOrWhiteSpace(reward.itemId) && !itemIds.Contains(reward.itemId.Trim()))
                        {
                            LogError($"Item '{item.itemId}' references unknown reward item '{reward.itemId}'.", logMessages);
                            valid = false;
                        }
                    }
                }
            }

            if (valid) LogMessage("Secret Four store configuration validation passed.", logMessages);
            else LogError("Secret Four store configuration validation failed.", logMessages);
            return valid;
        }

        private static void LogMessage(string message, bool enabled)
        {
            if (enabled) Debug.Log($"[MN7 Store] {message}");
        }

        private static void LogError(string message, bool enabled)
        {
            if (enabled) Debug.LogError($"[MN7 Store] {message}");
        }
    }
}
