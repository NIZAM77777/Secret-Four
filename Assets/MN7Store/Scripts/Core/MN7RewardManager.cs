using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Grants store items. Secret Four has no currency rewards.
    /// </summary>
    public class MN7RewardManager
    {
        private readonly MN7InventoryManager inventoryManager;

        public event Action<MN7RewardData> OnRewardGranted;

        public MN7RewardManager(MN7InventoryManager inventoryManager)
        {
            this.inventoryManager = inventoryManager;
        }

        public bool ValidateItemRewards(MN7ItemData item)
        {
            if (item == null || item.rewards == null || inventoryManager == null)
                return false;

            Dictionary<string, long> itemTotals = new();

            foreach (MN7RewardData reward in item.rewards)
            {
                if (reward == null || !ValidateItemReward(reward, itemTotals))
                    return false;
            }

            foreach (var pair in itemTotals)
            {
                if (pair.Value > int.MaxValue)
                    return false;

                MN7ItemData rewardItem = inventoryManager.GetItem(pair.Key);
                if (rewardItem == null)
                    return false;

                if (rewardItem.ownershipType == MN7ItemOwnershipType.Permanent)
                {
                    if (pair.Value != 1 || inventoryManager.IsOwned(pair.Key))
                        return false;
                }
                else if (!inventoryManager.CanAddItem(pair.Key, (int)pair.Value))
                {
                    return false;
                }
            }

            return true;
        }

        public bool GrantRewardDirect(MN7RewardData reward, string operationId = null)
        {
            if (reward == null || reward.rewardType != MN7RewardType.Item)
                return false;

            if (string.IsNullOrWhiteSpace(operationId))
            {
                Debug.LogError("[MN7 Store] Transactional item grants require a non-empty operation ID.");
                return false;
            }

            if (!inventoryManager.CanAddItem(reward.itemId, reward.itemQuantity) &&
                !inventoryManager.IsRewardOperationCompleted(operationId))
                return false;

            if (!inventoryManager.AddItem(reward.itemId, reward.itemQuantity, operationId))
                return false;

            OnRewardGranted?.Invoke(reward);
            return true;
        }

        public bool GrantItemRewards(MN7ItemData item)
        {
            if (!ValidateItemRewards(item))
                return false;

            foreach (MN7RewardData reward in item.rewards)
            {
                if (reward == null || !GrantRewardInternal(reward))
                    return false;
            }

            return true;
        }

        public bool GrantReward(MN7RewardData reward)
        {
            if (reward == null || !ValidateSingleReward(reward))
                return false;

            return GrantRewardInternal(reward);
        }

        private bool ValidateItemReward(
            MN7RewardData reward,
            Dictionary<string, long> totals)
        {
            if (reward.rewardType != MN7RewardType.Item ||
                string.IsNullOrWhiteSpace(reward.itemId) ||
                reward.itemQuantity <= 0)
                return false;

            MN7ItemData item = inventoryManager.GetItem(reward.itemId);
            if (item == null)
                return false;

            if (item.ownershipType == MN7ItemOwnershipType.Permanent &&
                reward.itemQuantity != 1)
                return false;

            if (!totals.ContainsKey(reward.itemId))
                totals[reward.itemId] = 0;

            totals[reward.itemId] += reward.itemQuantity;
            return true;
        }

        private bool ValidateSingleReward(MN7RewardData reward)
        {
            return reward.rewardType == MN7RewardType.Item &&
                   !string.IsNullOrWhiteSpace(reward.itemId) &&
                   reward.itemQuantity > 0 &&
                   inventoryManager.CanAddItem(reward.itemId, reward.itemQuantity);
        }

        private bool GrantRewardInternal(MN7RewardData reward)
        {
            if (reward.rewardType != MN7RewardType.Item)
                return false;

            if (!inventoryManager.AddItem(reward.itemId, reward.itemQuantity))
                return false;

            OnRewardGranted?.Invoke(reward);
            return true;
        }
    }
}
