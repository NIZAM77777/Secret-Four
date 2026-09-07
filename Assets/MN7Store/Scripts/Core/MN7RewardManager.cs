using System;
using UnityEngine;

namespace MN7.Store
{
    public class MN7RewardManager
    {
        private readonly MN7CurrencyManager currencyManager;
        private readonly MN7InventoryManager inventoryManager;

        public event Action<MN7RewardData> OnRewardGranted;

        public MN7RewardManager(
            MN7CurrencyManager currencyManager,
            MN7InventoryManager inventoryManager)
        {
            this.currencyManager =
                currencyManager;

            this.inventoryManager =
                inventoryManager;
        }

        //==================================================
        // GRANT ITEM REWARDS
        //==================================================

        public bool GrantItemRewards(
            MN7ItemData item)
        {
            if (item == null)
                return false;

            bool allGranted = true;

            foreach (
                MN7RewardData reward
                in item.rewards)
            {
                if (reward == null)
                    continue;

                bool success =
                    GrantReward(reward);

                if (!success)
                    allGranted = false;
            }

            return allGranted;
        }

        //==================================================
        // GRANT SINGLE REWARD
        //==================================================

        public bool GrantReward(
            MN7RewardData reward)
        {
            if (reward == null)
                return false;

            switch (reward.rewardType)
            {
                case MN7RewardType.Currency:

                    return GrantCurrency(
                        reward
                    );

                case MN7RewardType.Item:

                    return GrantItem(
                        reward
                    );

                default:

                    return false;
            }
        }

        //==================================================
        // CURRENCY
        //==================================================

        private bool GrantCurrency(
            MN7RewardData reward)
        {
            if (string.IsNullOrWhiteSpace(
                    reward.currencyId))
            {
                Debug.LogWarning(
                    "[MN7 Store] Reward currency ID is empty."
                );

                return false;
            }

            if (reward.currencyAmount <= 0)
                return false;

            currencyManager.Add(
                reward.currencyId,
                reward.currencyAmount
            );

            OnRewardGranted?.Invoke(
                reward
            );

            Debug.Log(
                $"[MN7 Store] Reward granted: " +
                $"+{reward.currencyAmount} " +
                $"{reward.currencyId}"
            );

            return true;
        }

        //==================================================
        // ITEM
        //==================================================

        private bool GrantItem(
            MN7RewardData reward)
        {
            if (string.IsNullOrWhiteSpace(
                    reward.itemId))
            {
                Debug.LogWarning(
                    "[MN7 Store] Reward item ID is empty."
                );

                return false;
            }

            bool added =
                inventoryManager.AddItem(
                    reward.itemId
                );

            if (!added)
                return false;

            OnRewardGranted?.Invoke(
                reward
            );

            Debug.Log(
                $"[MN7 Store] Item reward granted: " +
                $"{reward.itemId}"
            );

            return true;
        }
    }
}