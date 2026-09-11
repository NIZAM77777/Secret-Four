using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
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
            if (string.IsNullOrWhiteSpace(itemId) || config?.items == null) return null;
            foreach (MN7ItemData item in config.items)
                if (item != null && item.itemId == itemId) return item;
            return null;
        }

        public bool IsOwned(string itemId) => inventoryManager != null && inventoryManager.IsOwned(itemId);

        public bool PurchaseFree(string itemId)
        {
            MN7ItemData item = GetItem(itemId);
            if (item == null || item.purchaseType != MN7PurchaseType.Free)
            {
                OnPurchaseFailed?.Invoke(item);
                return false;
            }

            if (!TryLockPurchase(itemId)) return false;
            try
            {
                if (item.ownershipType == MN7ItemOwnershipType.Permanent && inventoryManager.IsOwned(itemId))
                {
                    OnPurchaseFailed?.Invoke(item);
                    return false;
                }

                return GrantPurchase(item, MN7PurchaseType.Free);
            }
            finally { UnlockPurchase(itemId); }
        }

        public void PurchaseWithRewardedAd(string itemId, Action<MN7AdResult> onAdResult = null)
        {
            MN7ItemData item = GetItem(itemId);
            if (item == null || item.purchaseType != MN7PurchaseType.RewardedAd)
            {
                onAdResult?.Invoke(MN7AdResult.Failed);
                return;
            }

            if (!TryLockPurchase(itemId))
            {
                onAdResult?.Invoke(MN7AdResult.Skipped);
                return;
            }

            if (!ValidateRewardedAdPurchase(item))
            {
                UnlockPurchase(itemId);
                onAdResult?.Invoke(MN7AdResult.Failed);
                return;
            }

            int progress = adProgress.GetProgress(item.itemId);
            if (progress >= item.requiredAds)
            {
                bool completed = CompleteAdPurchase(item);
                UnlockPurchase(itemId);
                onAdResult?.Invoke(completed ? MN7AdResult.Completed : MN7AdResult.Failed);
                return;
            }

            if (!adManager.IsRewardedAdReady(item.adPlacementId))
            {
                UnlockPurchase(itemId);
                onAdResult?.Invoke(MN7AdResult.NotReady);
                return;
            }

            adManager.ShowRewardedAd(item.adPlacementId, result => HandleRewardedAdResult(item, result, onAdResult));
        }

        public bool RecoverTransactionRewards(MN7TransactionData transaction, MN7ItemData item)
        {
            if (transaction == null || item == null) return false;
            MN7TransactionStatus status = transactionManager.GetStatus(transaction.transactionId);
            if (status != MN7TransactionStatus.PaymentCommitted && status != MN7TransactionStatus.RewardsGranted) return false;
            return ProcessTransactionRewards(transaction, item);
        }

        private void HandleRewardedAdResult(MN7ItemData item, MN7AdResult result, Action<MN7AdResult> callback)
        {
            if (result != MN7AdResult.Completed)
            {
                UnlockPurchase(item.itemId);
                callback?.Invoke(result);
                return;
            }

            if (!adProgress.AddProgress(item.itemId, 1))
            {
                UnlockPurchase(item.itemId);
                callback?.Invoke(MN7AdResult.Failed);
                return;
            }

            int progress = adProgress.GetProgress(item.itemId);
            Debug.Log($"[MN7 Store] Ad progress: {item.itemId} {progress}/{item.requiredAds}");

            if (progress < item.requiredAds)
            {
                UnlockPurchase(item.itemId);
                callback?.Invoke(MN7AdResult.Completed);
                return;
            }

            bool completed = CompleteAdPurchase(item);
            UnlockPurchase(item.itemId);
            callback?.Invoke(completed ? MN7AdResult.Completed : MN7AdResult.Failed);
        }

        private bool CompleteAdPurchase(MN7ItemData item)
        {
            if (!rewardManager.ValidateItemRewards(item)) return false;
            MN7TransactionData transaction = transactionManager.CreateTransaction(item.itemId, MN7PurchaseType.RewardedAd);
            if (transaction == null) return false;

            transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.PaymentCommitted);
            if (!ProcessTransactionRewards(transaction, item)) return false;

            adProgress.ResetProgress(item.itemId);
            OnPurchaseSuccessful?.Invoke(item);
            Debug.Log($"[MN7 Store] Rewarded ad purchase completed: {item.itemId}");
            return true;
        }

        private bool GrantPurchase(MN7ItemData item, MN7PurchaseType type)
        {
            if (!rewardManager.ValidateItemRewards(item))
            {
                OnPurchaseFailed?.Invoke(item);
                return false;
            }

            MN7TransactionData transaction = transactionManager.CreateTransaction(item.itemId, type);
            if (transaction == null)
            {
                OnPurchaseFailed?.Invoke(item);
                return false;
            }

            transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.PaymentCommitted);
            if (!ProcessTransactionRewards(transaction, item))
            {
                OnPurchaseFailed?.Invoke(item);
                return false;
            }

            OnPurchaseSuccessful?.Invoke(item);
            Debug.Log($"[MN7 Store] Free purchase completed: {item.itemId}");
            return true;
        }

        private bool ProcessTransactionRewards(MN7TransactionData transaction, MN7ItemData item)
        {
            if (transaction == null || item == null || item.rewards == null) return false;

            for (int i = 0; i < item.rewards.Count; i++)
            {
                MN7RewardData reward = item.rewards[i];
                if (reward == null) return false;

                string operationId = transactionManager.GetRewardOperationId(transaction.transactionId, i);
                if (transactionManager.IsRewardOperationCompleted(transaction.transactionId, operationId)) continue;

                if (!rewardManager.GrantRewardDirect(reward, operationId)) return false;
                transactionManager.MarkRewardOperationCompleted(transaction.transactionId, operationId, i);
            }

            transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.RewardsGranted);
            transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.Completed);
            return true;
        }

        private bool ValidateRewardedAdPurchase(MN7ItemData item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemId) || item.requiredAds <= 0 ||
                string.IsNullOrWhiteSpace(item.adPlacementId)) return false;

            if (item.ownershipType == MN7ItemOwnershipType.Permanent && inventoryManager.IsOwned(item.itemId)) return false;
            return rewardManager.ValidateItemRewards(item);
        }

        private bool TryLockPurchase(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || activePurchaseItems.Contains(itemId)) return false;
            activePurchaseItems.Add(itemId);
            return true;
        }

        private void UnlockPurchase(string itemId)
        {
            if (!string.IsNullOrWhiteSpace(itemId)) activePurchaseItems.Remove(itemId);
        }
    }
}
