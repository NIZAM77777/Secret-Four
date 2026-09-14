using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Handles Secret Four Store acquisition.
    /// Items are either free or unlocked through rewarded ads. There is no currency or IAP path.
    /// </summary>
    public class MN7PurchaseManager
    {
        private readonly MN7StoreConfig config;
        private readonly MN7InventoryManager inventoryManager;
        private readonly MN7AdManager adManager;
        private readonly MN7AdPurchaseProgress adProgress;
        private readonly MN7RewardManager rewardManager;
        private readonly MN7TransactionManager transactionManager;
        private readonly HashSet<string> activePurchaseItems = new();

        public event Action<MN7ItemData> OnPurchaseSuccessful;
        public event Action<MN7ItemData> OnPurchaseFailed;

        public MN7PurchaseManager(
            MN7StoreConfig config,
            MN7InventoryManager inventoryManager,
            MN7AdManager adManager,
            MN7AdPurchaseProgress adProgress,
            MN7RewardManager rewardManager,
            MN7TransactionManager transactionManager)
        {
            this.config = config;
            this.inventoryManager = inventoryManager;
            this.adManager = adManager;
            this.adProgress = adProgress;
            this.rewardManager = rewardManager;
            this.transactionManager = transactionManager;
        }

        public MN7ItemData GetItem(string itemId)
        {
            if (config?.items == null || string.IsNullOrWhiteSpace(itemId)) return null;

            string id = itemId.Trim();
            foreach (MN7ItemData item in config.items)
            {
                if (item != null && string.Equals(item.itemId?.Trim(), id, StringComparison.Ordinal))
                    return item;
            }

            return null;
        }

        public bool IsOwned(string itemId) =>
            inventoryManager != null && inventoryManager.IsOwned(itemId);

        /// <summary>Claims a free item. Free items never require ads, currency, or IAP.</summary>
        public bool PurchaseFree(string itemId)
        {
            MN7ItemData item = GetItem(itemId);
            if (item == null || item.purchaseType != MN7PurchaseType.Free)
            {
                PurchaseFailed(item);
                return false;
            }

            if (!TryLockPurchase(item.itemId))
                return false;

            try
            {
                if (inventoryManager == null || inventoryManager.IsOwned(item.itemId) ||
                    rewardManager == null || !rewardManager.ValidateItemRewards(item))
                {
                    PurchaseFailed(item);
                    return false;
                }

                bool granted = rewardManager.GrantItemRewards(item);
                if (granted)
                {
                    PurchaseSuccessful(item);
                    return true;
                }

                PurchaseFailed(item);
                return false;
            }
            finally
            {
                UnlockPurchase(item.itemId);
            }
        }

        /// <summary>
        /// Unlocks an item by watching its configured number of rewarded ads.
        /// There is no currency or IAP payment path in Secret Four.
        /// </summary>
        public void PurchaseWithRewardedAd(string itemId, Action<MN7AdResult> onAdResult = null)
        {
            MN7ItemData item = GetItem(itemId);
            if (item == null)
            {
                onAdResult?.Invoke(MN7AdResult.Failed);
                return;
            }

            if (!TryLockPurchase(item.itemId))
            {
                onAdResult?.Invoke(MN7AdResult.Skipped);
                return;
            }

            if (!ValidateRewardedAdPurchase(item))
            {
                UnlockPurchase(item.itemId);
                onAdResult?.Invoke(MN7AdResult.Failed);
                return;
            }

            int progress = adProgress.GetProgress(item.itemId);
            if (progress >= item.requiredAds)
            {
                bool completed = CompleteAdPurchase(item);
                UnlockPurchase(item.itemId);
                onAdResult?.Invoke(completed ? MN7AdResult.Completed : MN7AdResult.Failed);
                return;
            }

            if (adManager == null || !adManager.IsRewardedAdReady(item.adPlacementId))
            {
                UnlockPurchase(item.itemId);
                onAdResult?.Invoke(MN7AdResult.NotReady);
                return;
            }

            adManager.ShowRewardedAd(
                item.adPlacementId,
                result => HandleRewardedAdResult(item, result, onAdResult));
        }

        private bool ValidateRewardedAdPurchase(MN7ItemData item)
        {
            return item != null &&
                   item.purchaseType == MN7PurchaseType.RewardedAd &&
                   item.requiredAds > 0 &&
                   !string.IsNullOrWhiteSpace(item.adPlacementId) &&
                   adProgress != null &&
                   inventoryManager != null &&
                   rewardManager != null &&
                   rewardManager.ValidateItemRewards(item) &&
                   !(item.ownershipType == MN7ItemOwnershipType.Permanent && inventoryManager.IsOwned(item.itemId));
        }

        private void HandleRewardedAdResult(
            MN7ItemData item,
            MN7AdResult result,
            Action<MN7AdResult> onAdResult)
        {
            if (result != MN7AdResult.Completed)
            {
                UnlockPurchase(item.itemId);
                onAdResult?.Invoke(result);
                return;
            }

            if (!adProgress.AddProgress(item.itemId, 1))
            {
                UnlockPurchase(item.itemId);
                onAdResult?.Invoke(MN7AdResult.Failed);
                return;
            }

            int newProgress = adProgress.GetProgress(item.itemId);
            if (newProgress < item.requiredAds)
            {
                UnlockPurchase(item.itemId);
                onAdResult?.Invoke(MN7AdResult.Completed);
                return;
            }

            bool completed = CompleteAdPurchase(item);
            UnlockPurchase(item.itemId);
            onAdResult?.Invoke(completed ? MN7AdResult.Completed : MN7AdResult.Failed);
        }

        private bool CompleteAdPurchase(MN7ItemData item)
        {
            if (item == null || !rewardManager.ValidateItemRewards(item))
                return false;

            MN7TransactionData transaction =
                transactionManager.CreateTransaction(item.itemId, MN7PurchaseType.RewardedAd);

            if (transaction == null)
                return false;

            if (!transactionManager.SetStatus(
                    transaction.transactionId,
                    MN7TransactionStatus.PaymentCommitted))
                return false;

            if (!ProcessTransactionRewards(transaction, item))
                return false;

            adProgress.ResetProgress(item.itemId);
            PurchaseSuccessful(item);
            return true;
        }

        public bool RecoverTransactionRewards(
            MN7TransactionData transaction,
            MN7ItemData item)
        {
            if (transaction == null || item == null ||
                transaction.purchaseType != MN7PurchaseType.RewardedAd.ToString())
                return false;

            MN7TransactionStatus status =
                transactionManager.GetStatus(transaction.transactionId);

            if (status == MN7TransactionStatus.Pending)
            {
                if (!transactionManager.SetStatus(
                        transaction.transactionId,
                        MN7TransactionStatus.PaymentCommitted))
                    return false;

                status = transactionManager.GetStatus(transaction.transactionId);
            }

            if (status != MN7TransactionStatus.PaymentCommitted &&
                status != MN7TransactionStatus.RewardsGranted)
                return false;

            return ProcessTransactionRewards(transaction, item);
        }

        private bool ProcessTransactionRewards(
            MN7TransactionData transaction,
            MN7ItemData item)
        {
            if (transaction == null || item == null || item.rewards == null)
                return false;

            for (int rewardIndex = 0; rewardIndex < item.rewards.Count; rewardIndex++)
            {
                MN7RewardData reward = item.rewards[rewardIndex];
                if (reward == null)
                    return false;

                string operationId = transactionManager.GetRewardOperationId(
                    transaction.transactionId,
                    rewardIndex);

                if (transactionManager.IsRewardOperationCompleted(
                        transaction.transactionId,
                        operationId))
                    continue;

                if (!rewardManager.GrantRewardDirect(reward, operationId))
                    return false;

                transactionManager.MarkRewardOperationCompleted(
                    transaction.transactionId,
                    operationId,
                    rewardIndex);
            }

            if (!transactionManager.SetStatus(
                    transaction.transactionId,
                    MN7TransactionStatus.RewardsGranted))
                return false;

            return transactionManager.SetStatus(
                transaction.transactionId,
                MN7TransactionStatus.Completed);
        }

        private bool TryLockPurchase(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || activePurchaseItems.Contains(itemId))
                return false;

            activePurchaseItems.Add(itemId);
            return true;
        }

        private void UnlockPurchase(string itemId)
        {
            if (!string.IsNullOrWhiteSpace(itemId))
                activePurchaseItems.Remove(itemId);
        }

        private void PurchaseSuccessful(MN7ItemData item)
        {
            Debug.Log($"[MN7 Store] Store item acquired: {item?.displayName}");
            OnPurchaseSuccessful?.Invoke(item);
        }

        private void PurchaseFailed(MN7ItemData item)
        {
            if (item != null)
                Debug.LogWarning($"[MN7 Store] Store item acquisition failed: {item.displayName}");
            OnPurchaseFailed?.Invoke(item);
        }
    }
}
