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

        //==================================================
        // CONSTRUCTOR
        //==================================================

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
        // VALIDATE ITEM REWARDS
        //==================================================

        public bool ValidateItemRewards(
            MN7ItemData item)
        {
            if (item == null)
            {
                Debug.LogWarning(
                    "[MN7 Store] Cannot validate rewards " +
                    "for null item."
                );

                return false;
            }

            if (item.rewards == null)
            {
                Debug.LogWarning(
                    $"[MN7 Store] Rewards list is null: " +
                    $"{item.itemId}"
                );

                return false;
            }

            // Track combined currency rewards.
            Dictionary<string, long> currencyTotals =
                new();

            // Track combined item rewards.
            Dictionary<string, long> itemTotals =
                new();

            //==================================================
            // VALIDATE EACH REWARD
            //==================================================

            foreach (
                MN7RewardData reward
                in item.rewards)
            {
                if (reward == null)
                {
                    Debug.LogWarning(
                        $"[MN7 Store] Null reward found in: " +
                        $"{item.itemId}"
                    );

                    return false;
                }

                switch (reward.rewardType)
                {
                    case MN7RewardType.Currency:

                        if (!ValidateCurrencyReward(
                                reward,
                                currencyTotals))
                        {
                            return false;
                        }

                        break;

                    case MN7RewardType.Item:

                        if (!ValidateItemReward(
                                reward,
                                itemTotals))
                        {
                            return false;
                        }

                        break;

                    default:

                        Debug.LogWarning(
                            $"[MN7 Store] Unknown reward type " +
                            $"in item: {item.itemId}"
                        );

                        return false;
                }
            }

            //==================================================
            // CHECK COMBINED CURRENCY TOTALS
            //==================================================

            foreach (
                var pair
                in currencyTotals)
            {
                if (pair.Value >
                    int.MaxValue)
                {
                    Debug.LogWarning(
                        $"[MN7 Store] Combined currency reward " +
                        $"is too large: {pair.Key}"
                    );

                    return false;
                }

                if (!currencyManager.CanAdd(
                        pair.Key,
                        (int)pair.Value))
                {
                    Debug.LogWarning(
                        $"[MN7 Store] Cannot grant currency reward: " +
                        $"{pair.Key} +{pair.Value}"
                    );

                    return false;
                }
            }

            //==================================================
            // CHECK COMBINED ITEM TOTALS
            //==================================================

            foreach (
                var pair
                in itemTotals)
            {
                if (pair.Value >
                    int.MaxValue)
                {
                    Debug.LogWarning(
                        $"[MN7 Store] Combined item quantity " +
                        $"is too large: {pair.Key}"
                    );

                    return false;
                }

                MN7ItemData rewardItem =
                    inventoryManager.GetItem(
                        pair.Key
                    );

                if (rewardItem == null)
                    return false;

                // Permanent items cannot be duplicated
                // inside one reward bundle.
                if (rewardItem.ownershipType ==
                    MN7ItemOwnershipType.Permanent)
                {
                    if (pair.Value != 1)
                    {
                        Debug.LogWarning(
                            $"[MN7 Store] Permanent item appears " +
                            $"multiple times in rewards: {pair.Key}"
                        );

                        return false;
                    }

                    if (inventoryManager.IsOwned(
                            pair.Key))
                    {
                        Debug.LogWarning(
                            $"[MN7 Store] Permanent reward already owned: " +
                            $"{pair.Key}"
                        );

                        return false;
                    }

                    continue;
                }

                // Consumable
                if (!inventoryManager.CanAddItem(
                        pair.Key,
                        (int)pair.Value))
                {
                    Debug.LogWarning(
                        $"[MN7 Store] Cannot add consumable reward: " +
                        $"{pair.Key} x{pair.Value}"
                    );

                    return false;
                }
            }

            return true;
        }

        //==================================================
        // GRANT WITHOUT PRE-VALIDATION
        //==================================================

        public bool GrantRewardDirect(
     MN7RewardData reward)
        {
            if (reward == null)
                return false;

            switch (reward.rewardType)
            {
                case MN7RewardType.Currency:

                    if (!currencyManager.CanAdd(
                            reward.currencyId,
                            reward.currencyAmount))
                    {
                        return false;
                    }

                    currencyManager.Add(
                        reward.currencyId,
                        reward.currencyAmount
                    );

                    OnRewardGranted?.Invoke(
                        reward
                    );

                    return true;


                case MN7RewardType.Item:

                    bool itemGranted =
                        inventoryManager.AddItem(
                            reward.itemId,
                            reward.itemQuantity
                        );

                    if (!itemGranted)
                        return false;

                    OnRewardGranted?.Invoke(
                        reward
                    );

                    return true;


                default:

                    return false;
            }
        }

        //==================================================
        // VALIDATE CURRENCY
        //==================================================

        private bool ValidateCurrencyReward(
            MN7RewardData reward,
            Dictionary<string, long> totals)
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
            {
                Debug.LogWarning(
                    $"[MN7 Store] Invalid currency reward amount: " +
                    $"{reward.currencyAmount}"
                );

                return false;
            }

            if (!currencyManager.HasCurrency(
                    reward.currencyId))
            {
                Debug.LogWarning(
                    $"[MN7 Store] Unknown reward currency: " +
                    $"{reward.currencyId}"
                );

                return false;
            }

            if (!totals.ContainsKey(
                    reward.currencyId))
            {
                totals[reward.currencyId] = 0;
            }

            totals[reward.currencyId] +=
                reward.currencyAmount;

            return true;
        }

        //==================================================
        // VALIDATE ITEM
        //==================================================

        private bool ValidateItemReward(
            MN7RewardData reward,
            Dictionary<string, long> totals)
        {
            if (string.IsNullOrWhiteSpace(
                    reward.itemId))
            {
                Debug.LogWarning(
                    "[MN7 Store] Reward item ID is empty."
                );

                return false;
            }

            if (reward.itemQuantity <= 0)
            {
                Debug.LogWarning(
                    $"[MN7 Store] Invalid item reward quantity: " +
                    $"{reward.itemQuantity}"
                );

                return false;
            }

            MN7ItemData item =
                inventoryManager.GetItem(
                    reward.itemId
                );

            if (item == null)
            {
                Debug.LogWarning(
                    $"[MN7 Store] Unknown reward item: " +
                    $"{reward.itemId}"
                );

                return false;
            }

            // Permanent items must always be one copy.
            if (item.ownershipType ==
                MN7ItemOwnershipType.Permanent)
            {
                if (reward.itemQuantity != 1)
                {
                    Debug.LogWarning(
                        $"[MN7 Store] Permanent item reward " +
                        $"must have quantity 1: " +
                        $"{reward.itemId}"
                    );

                    return false;
                }
            }

            if (!totals.ContainsKey(
                    reward.itemId))
            {
                totals[reward.itemId] = 0;
            }

            totals[reward.itemId] +=
                reward.itemQuantity;

            return true;
        }

        //==================================================
        // GRANT ITEM REWARDS
        //==================================================

        public bool GrantItemRewards(
            MN7ItemData item)
        {
            if (!ValidateItemRewards(item))
            {
                Debug.LogError(
                    $"[MN7 Store] Reward validation failed: " +
                    $"{item?.itemId}"
                );

                return false;
            }

            foreach (
                MN7RewardData reward
                in item.rewards)
            {
                if (reward == null)
                    continue;

                if (!GrantRewardInternal(
                        reward))
                {
                    Debug.LogError(
                        $"[MN7 Store] Failed to grant reward: " +
                        $"{item.itemId}"
                    );

                    return false;
                }
            }

            return true;
        }

        //==================================================
        // GRANT SINGLE REWARD
        //==================================================

        public bool GrantReward(
            MN7RewardData reward)
        {
            if (reward == null)
                return false;

            // Validate the individual reward first.
            if (!ValidateSingleReward(
                    reward))
            {
                return false;
            }

            return GrantRewardInternal(
                reward
            );
        }

        //==================================================
        // VALIDATE SINGLE
        //==================================================

        private bool ValidateSingleReward(
            MN7RewardData reward)
        {
            if (reward == null)
                return false;

            switch (reward.rewardType)
            {
                case MN7RewardType.Currency:

                    if (string.IsNullOrWhiteSpace(
                            reward.currencyId))
                    {
                        return false;
                    }

                    if (reward.currencyAmount <= 0)
                        return false;

                    return currencyManager.CanAdd(
                        reward.currencyId,
                        reward.currencyAmount
                    );

                case MN7RewardType.Item:

                    if (string.IsNullOrWhiteSpace(
                            reward.itemId))
                    {
                        return false;
                    }

                    if (reward.itemQuantity <= 0)
                        return false;

                    return inventoryManager.CanAddItem(
                        reward.itemId,
                        reward.itemQuantity
                    );

                default:

                    return false;
            }
        }

        //==================================================
        // GRANT INTERNAL
        //==================================================

        private bool GrantRewardInternal(
            MN7RewardData reward)
        {
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
            if (!currencyManager.CanAdd(
                    reward.currencyId,
                    reward.currencyAmount))
            {
                return false;
            }

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
            bool added =
                inventoryManager.AddItem(
                    reward.itemId,
                    reward.itemQuantity
                );

            if (!added)
                return false;

            OnRewardGranted?.Invoke(
                reward
            );

            Debug.Log(
                $"[MN7 Store] Item reward granted: " +
                $"{reward.itemId} " +
                $"x{reward.itemQuantity}"
            );

            return true;
        }
    }
}