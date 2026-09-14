using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7PurchaseManager
    {
        private readonly MN7StoreConfig config;
        private readonly MN7CurrencyManager currencyManager;
        private readonly MN7InventoryManager inventoryManager;
        private readonly MN7AdManager adManager;
        private readonly MN7AdPurchaseProgress adProgress;
        private readonly MN7RewardManager rewardManager;
        private readonly MN7TransactionManager transactionManager;
        private readonly MN7StoreFeatureConfig features;
        private readonly HashSet<string> activePurchaseItems = new();
        private MN7IAPManager iapManager;

        public event Action<MN7ItemData> OnPurchaseSuccessful;
        public event Action<MN7ItemData> OnPurchaseFailed;

        public MN7PurchaseManager(
            MN7StoreConfig config,
            MN7CurrencyManager currencyManager,
            MN7InventoryManager inventoryManager,
            MN7AdManager adManager,
            MN7AdPurchaseProgress adProgress,
            MN7RewardManager rewardManager,
            MN7TransactionManager transactionManager,
            MN7StoreFeatureConfig features = null)
        {
            this.config = config;
            this.currencyManager = currencyManager;
            this.inventoryManager = inventoryManager;
            this.adManager = adManager;
            this.adProgress = adProgress;
            this.rewardManager = rewardManager;
            this.transactionManager = transactionManager;
            this.features = features ?? new MN7StoreFeatureConfig();
        }

        public void SetIAPManager(MN7IAPManager manager)
        {
            if (iapManager != null)
            {
                iapManager.OnPurchaseSuccessful -= HandleIAPSuccessful;
                iapManager.OnPurchaseFailed -= HandleIAPFailed;
            }

            iapManager = manager;

            if (iapManager != null)
            {
                iapManager.OnPurchaseSuccessful += HandleIAPSuccessful;
                iapManager.OnPurchaseFailed += HandleIAPFailed;
            }
        }

        private void HandleIAPSuccessful(MN7ItemData item) => OnPurchaseSuccessful?.Invoke(item);
        private void HandleIAPFailed(MN7ItemData item, MN7IAPResult result) => OnPurchaseFailed?.Invoke(item);

        public MN7ItemData GetItem(string itemId)
        {
            if (config?.items == null || string.IsNullOrWhiteSpace(itemId)) return null;
            string id = itemId.Trim();
            foreach (MN7ItemData item in config.items)
                if (item != null && string.Equals(item.itemId?.Trim(), id, StringComparison.Ordinal)) return item;
            return null;
        }

        public bool IsOwned(string itemId) => inventoryManager != null && inventoryManager.IsOwned(itemId);

        public bool PurchaseWithCurrency(string itemId)
        {
            if (!features.coins)
            {
                Debug.LogWarning("[MN7 Store] Currency purchases are disabled in Store Features.");
                return false;
            }

            MN7ItemData item = GetItem(itemId);
            if (item == null) { OnPurchaseFailed?.Invoke(null); return false; }
            if (!TryLockPurchase(item.itemId)) { OnPurchaseFailed?.Invoke(item); return false; }

            try
            {
                if (item.ownershipType == MN7ItemOwnershipType.Permanent && inventoryManager.IsOwned(item.itemId)) return FailPurchase(item);
                if (item.purchaseType != MN7PurchaseType.Currency && item.purchaseType != MN7PurchaseType.CurrencyOrAd) return FailPurchase(item);
                if (!rewardManager.ValidateItemRewards(item)) return FailPurchase(item);
                if (currencyManager == null || !currencyManager.CanSpend(item.currencyId, item.currencyCost)) return FailPurchase(item);

                MN7TransactionData transaction = transactionManager.CreateTransaction(
                    item.itemId,
                    item.purchaseType,
                    null,
                    item.currencyId,
                    item.currencyCost
                );
                if (transaction == null) return FailPurchase(item);

                string paymentOperationId = transactionManager.GetPaymentOperationId(transaction.transactionId);
                if (!currencyManager.Spend(item.currencyId, item.currencyCost, paymentOperationId))
                {
                    transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.Failed);
                    return FailPurchase(item);
                }

                if (!transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.PaymentCommitted)) return FailPurchase(item);
                if (!ProcessTransactionRewards(transaction, item))
                {
                    Debug.LogError($"[MN7 Store] Transaction requires recovery: {transaction.transactionId}");
                    OnPurchaseFailed?.Invoke(item);
                    return false;
                }

                PurchaseSuccessful(item);
                return true;
            }
            finally
            {
                UnlockPurchase(item.itemId);
            }
        }

        public bool RecoverTransactionRewards(MN7TransactionData transaction, MN7ItemData item)
        {
            if (transaction == null || item == null) return false;

            MN7TransactionStatus status = transactionManager.GetStatus(transaction.transactionId);

            if (status == MN7TransactionStatus.Pending)
            {
                if (!TryCommitPendingTransaction(transaction))
                    return false;

                status = transactionManager.GetStatus(transaction.transactionId);
            }

            if (status != MN7TransactionStatus.PaymentCommitted && status != MN7TransactionStatus.RewardsGranted)
                return false;

            return ProcessTransactionRewards(transaction, item);
        }

        /// <summary>
        /// Resolves a transaction that was persisted as Pending before the app
        /// stopped. Local currency transactions are only committed when their
        /// persisted payment operation proves the debit already happened.
        /// Free and completed rewarded-ad transactions can safely advance to
        /// PaymentCommitted because no additional currency payment is required.
        /// </summary>
        private bool TryCommitPendingTransaction(MN7TransactionData transaction)
        {
            if (transaction == null || string.IsNullOrWhiteSpace(transaction.purchaseType))
                return false;

            if (!Enum.TryParse(transaction.purchaseType, out MN7PurchaseType purchaseType))
                return false;

            switch (purchaseType)
            {
                case MN7PurchaseType.Currency:
                case MN7PurchaseType.CurrencyOrAd:
                    if (currencyManager == null ||
                        string.IsNullOrWhiteSpace(transaction.paymentCurrencyId) ||
                        transaction.paymentAmount <= 0)
                    {
                        transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.Failed);
                        return false;
                    }

                    string paymentOperationId = transactionManager.GetPaymentOperationId(transaction.transactionId);
                    if (!currencyManager.IsPaymentOperationCompleted(paymentOperationId))
                    {
                        // The debit marker does not exist, so this transaction
                        // never reached a committed payment. Never charge again
                        // during recovery; fail it safely instead.
                        transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.Failed);
                        return false;
                    }
                    break;

                case MN7PurchaseType.Free:
                case MN7PurchaseType.RewardedAd:
                    break;

                default:
                    // IAP has its own external store/provider recovery path.
                    // Do not manufacture a local payment result here.
                    return false;
            }

            return transactionManager.SetStatus(
                transaction.transactionId,
                MN7TransactionStatus.PaymentCommitted
            );
        }

        private bool ProcessTransactionRewards(MN7TransactionData transaction, MN7ItemData item)
        {
            if (transaction == null || item == null || item.rewards == null) return false;
            for (int rewardIndex = 0; rewardIndex < item.rewards.Count; rewardIndex++)
            {
                MN7RewardData reward = item.rewards[rewardIndex];
                if (reward == null) return false;
                string operationId = transactionManager.GetRewardOperationId(transaction.transactionId, rewardIndex);
                if (transactionManager.IsRewardOperationCompleted(transaction.transactionId, operationId)) continue;
                if (!rewardManager.GrantRewardDirect(reward, operationId)) return false;
                transactionManager.MarkRewardOperationCompleted(transaction.transactionId, operationId, rewardIndex);
            }

            if (!transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.RewardsGranted)) return false;
            return transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.Completed);
        }

        public bool PurchaseFree(string itemId)
        {
            if (!features.freeItems)
            {
                Debug.LogWarning("[MN7 Store] Free item purchases are disabled in Store Features.");
                return false;
            }

            MN7ItemData item = GetItem(itemId);
            if (item == null) { OnPurchaseFailed?.Invoke(null); return false; }
            if (!TryLockPurchase(item.itemId)) { OnPurchaseFailed?.Invoke(item); return false; }

            try
            {
                if (item.purchaseType != MN7PurchaseType.Free) return FailPurchase(item);
                if (item.ownershipType == MN7ItemOwnershipType.Permanent && inventoryManager.IsOwned(item.itemId)) return FailPurchase(item);
                if (!rewardManager.ValidateItemRewards(item)) return FailPurchase(item);

                MN7TransactionData transaction = transactionManager.CreateTransaction(item.itemId, MN7PurchaseType.Free);
                if (transaction == null) return FailPurchase(item);
                if (!transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.PaymentCommitted)) return FailPurchase(item);
                if (!ProcessTransactionRewards(transaction, item)) return FailPurchase(item);

                PurchaseSuccessful(item);
                return true;
            }
            finally
            {
                UnlockPurchase(item.itemId);
            }
        }

        public void PurchaseWithRewardedAd(string itemId, Action<MN7AdResult> onAdResult = null)
        {
            if (!features.adRewards)
            {
                Debug.LogWarning("[MN7 Store] Rewarded Ad purchases are disabled in Store Features.");
                onAdResult?.Invoke(MN7AdResult.Failed);
                return;
            }

            MN7ItemData item = GetItem(itemId);
            if (item == null) { onAdResult?.Invoke(MN7AdResult.Failed); return; }
            if (!TryLockPurchase(item.itemId)) { onAdResult?.Invoke(MN7AdResult.Skipped); return; }

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

            adManager.ShowRewardedAd(item.adPlacementId, result => HandleRewardedAdResult(item, result, onAdResult));
        }

        private bool ValidateRewardedAdPurchase(MN7ItemData item)
        {
            return item != null &&
                   (item.purchaseType == MN7PurchaseType.RewardedAd || item.purchaseType == MN7PurchaseType.CurrencyOrAd) &&
                   item.requiredAds > 0 &&
                   !string.IsNullOrWhiteSpace(item.adPlacementId) &&
                   adProgress != null &&
                   !(item.ownershipType == MN7ItemOwnershipType.Permanent && inventoryManager.IsOwned(item.itemId)) &&
                   rewardManager.ValidateItemRewards(item);
        }

        private void HandleRewardedAdResult(MN7ItemData item, MN7AdResult result, Action<MN7AdResult> onAdResult)
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
            if (item == null || !rewardManager.ValidateItemRewards(item)) return false;
            MN7TransactionData transaction = transactionManager.CreateTransaction(item.itemId, MN7PurchaseType.RewardedAd);
            if (transaction == null) return false;
            if (!transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.PaymentCommitted)) return false;
            if (!ProcessTransactionRewards(transaction, item)) return false;
            adProgress.ResetProgress(item.itemId);
            PurchaseSuccessful(item);
            return true;
        }

        public bool PurchaseWithIAP(string itemId)
        {
            if (!features.iap)
            {
                Debug.LogWarning("[MN7 Store] IAP purchases are disabled in Store Features.");
                return false;
            }

            MN7ItemData item = GetItem(itemId);
            if (item == null) { OnPurchaseFailed?.Invoke(null); return false; }
            if (iapManager == null)
            {
                Debug.LogWarning("[MN7 Store] IAP manager is not configured.");
                OnPurchaseFailed?.Invoke(item);
                return false;
            }
            return iapManager.PurchaseItem(item.itemId);
        }

        public bool CanPurchaseWithCurrency(string itemId)
        {
            if (!features.coins) return false;
            MN7ItemData item = GetItem(itemId);
            if (item == null || inventoryManager.IsOwned(itemId)) return false;
            if (item.purchaseType != MN7PurchaseType.Currency && item.purchaseType != MN7PurchaseType.CurrencyOrAd) return false;
            return currencyManager != null && currencyManager.CanSpend(item.currencyId, item.currencyCost);
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

        private bool FailPurchase(MN7ItemData item)
        {
            OnPurchaseFailed?.Invoke(item);
            return false;
        }

        private void PurchaseSuccessful(MN7ItemData item)
        {
            Debug.Log($"[MN7 Store] Purchase successful: {item?.displayName}");
            OnPurchaseSuccessful?.Invoke(item);
        }
    }
}
