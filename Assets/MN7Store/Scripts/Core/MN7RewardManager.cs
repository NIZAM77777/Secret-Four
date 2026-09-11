using System;
using UnityEngine;

namespace MN7.Store
{
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
            if (item == null || item.rewards == null || item.rewards.Count == 0)
                return false;

            foreach (MN7RewardData reward in item.rewards)
            {
                if (reward == null || reward.rewardType != MN7RewardType.Item ||
                    string.IsNullOrWhiteSpace(reward.itemId) || reward.itemQuantity <= 0)
                    return false;

                MN7ItemData rewardItem = inventoryManager.GetItem(reward.itemId);
                if (rewardItem == null)
                    return false;

                if (rewardItem.ownershipType == MN7ItemOwnershipType.Permanent)
                {
                    if (reward.itemQuantity != 1 || inventoryManager.IsOwned(reward.itemId))
                        return false;
                }
                else if (!inventoryManager.CanAddItem(reward.itemId, reward.itemQuantity))
                {
                    return false;
                }
            }

            return true;
        }

        public bool GrantRewardDirect(MN7RewardData reward, string operationId = null)
        {
            if (reward == null || reward.rewardType != MN7RewardType.Item ||
                string.IsNullOrWhiteSpace(operationId))
                return false;

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
                if (!inventoryManager.AddItem(reward.itemId, reward.itemQuantity))
                    return false;
                OnRewardGranted?.Invoke(reward);
            }

            return true;
        }

        public bool GrantReward(MN7RewardData reward)
        {
            if (reward == null || reward.rewardType != MN7RewardType.Item ||
                !inventoryManager.CanAddItem(reward.itemId, reward.itemQuantity))
                return false;

            return inventoryManager.AddItem(reward.itemId, reward.itemQuantity);
        }
    }
}
