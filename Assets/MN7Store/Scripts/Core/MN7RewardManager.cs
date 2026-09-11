using System;
using System.Collections.Generic;
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
            this.currencyManager = currencyManager;
            this.inventoryManager = inventoryManager;
        }

        public bool ValidateItemRewards(MN7ItemData item)
        {
            if (item == null || item.rewards == null)
                return false;

            Dictionary<string, long> currencyTotals = new();
            Dictionary<string, long> itemTotals = new();

            foreach (MN7RewardData reward in item.rewards)
            {
                if (reward == null)
                    return false;

                switch (reward.rewardType)
                {
                    case MN7RewardType.Currency:
                        if (!ValidateCurrencyReward(reward, currencyTotals))
                            return false;
                        break;

                    case MN7RewardType.Item:
                        if (!ValidateItemReward(reward, itemTotals))
                            return false;
                        break;

                    default:
                        return false;
                }
            }

            foreach (var pair in currencyTotals)
            {
                if (pair.Value > int.MaxValue ||
                    !currencyManager.CanAdd(pair.Key, (int)pair.Value))
                {
                    return false;
                }
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

        /// <summary>
        /// Grants one reward using the transaction operation ID as the
        /// idempotency key. The currency/inventory manager persists the
        /// operation marker together with the actual reward mutation.
        /// </summary>
        public bool GrantRewardDirect(
            MN7RewardData reward,
            string operationId = null)
        {
            if (reward == null)
                return false;

            if (string.IsNullOrWhiteSpace(operationId))
            {
                Debug.LogError(
                    "[MN7 Store] Transactional reward grants require a non-empty operation ID."
                );
                return false;
            }

            switch (reward.rewardType)
            {
                case MN7RewardType.Currency:
                    if (!currencyManager.CanAdd(
                            reward.currencyId,
                            reward.currencyAmount) &&
                        !currencyManager.IsRewardOperationCompleted(operationId))
                    {
                        return false;
                    }

                    if (!currencyManager.Add(
                            reward.currencyId,
                            reward.currencyAmount,
                            operationId))
                    {
                        return false;
                    }

                    OnRewardGranted?.Invoke(reward);
                    return true;

                case MN7RewardType.Item:
                    if (!inventoryManager.CanAddItem(
                            reward.itemId,
                            reward.itemQuantity) &&
                        !inventoryManager.IsRewardOperationCompleted(operationId))
                    {
                        return false;
                    }

                    if (!inventoryManager.AddItem(
                            reward.itemId,
                            reward.itemQuantity,
                            operationId))
                    {
                        return false;
                    }

                    OnRewardGranted?.Invoke(reward);
                    return true;

                default:
                    return false;
            }
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

        private bool ValidateCurrencyReward(
            MN7RewardData reward,
            Dictionary<string, long> totals)
        {
            if (string.IsNullOrWhiteSpace(reward.currencyId) ||
                reward.currencyAmount <= 0 ||
                !currencyManager.HasCurrency(reward.currencyId))
            {
                return false;
            }

            if (!totals.ContainsKey(reward.currencyId))
                totals[reward.currencyId] = 0;

            totals[reward.currencyId] += reward.currencyAmount;
            return true;
        }

        private bool ValidateItemReward(
            MN7RewardData reward,
            Dictionary<string, long> totals)
        {
            if (string.IsNullOrWhiteSpace(reward.itemId) ||
                reward.itemQuantity <= 0)
            {
                return false;
            }

            MN7ItemData item = inventoryManager.GetItem(reward.itemId);

            if (item == null)
                return false;

            if (item.ownershipType == MN7ItemOwnershipType.Permanent &&
                reward.itemQuantity != 1)
            {
                return false;
            }

            if (!totals.ContainsKey(reward.itemId))
                totals[reward.itemId] = 0;

            totals[reward.itemId] += reward.itemQuantity;
            return true;
        }

        private bool ValidateSingleReward(MN7RewardData reward)
        {
            switch (reward.rewardType)
            {
                case MN7RewardType.Currency:
                    return !string.IsNullOrWhiteSpace(reward.currencyId) &&
                           reward.currencyAmount > 0 &&
                           currencyManager.CanAdd(
                               reward.currencyId,
                               reward.currencyAmount);

                case MN7RewardType.Item:
                    return !string.IsNullOrWhiteSpace(reward.itemId) &&
                           reward.itemQuantity > 0 &&
                           inventoryManager.CanAddItem(
                               reward.itemId,
                               reward.itemQuantity);

                default:
                    return false;
            }
        }

        private bool GrantRewardInternal(MN7RewardData reward)
        {
            switch (reward.rewardType)
            {
                case MN7RewardType.Currency:
                    return GrantCurrency(reward);

                case MN7RewardType.Item:
                    return GrantItem(reward);

                default:
                    return false;
            }
        }

        private bool GrantCurrency(MN7RewardData reward)
        {
            if (!currencyManager.CanAdd(reward.currencyId, reward.currencyAmount))
                return false;

            if (!currencyManager.Add(
                    reward.currencyId,
                    reward.currencyAmount))
            {
                return false;
            }

            OnRewardGranted?.Invoke(reward);
            return true;
        }

        private bool GrantItem(MN7RewardData reward)
        {
            if (!inventoryManager.AddItem(
                    reward.itemId,
                    reward.itemQuantity))
            {
                return false;
            }

            OnRewardGranted?.Invoke(reward);
            return true;
        }
    }
}
