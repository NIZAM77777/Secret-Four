using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7IAPManager
    {
        private readonly MN7StoreConfig config;
        private readonly MN7InventoryManager inventoryManager;
        private readonly MN7RewardManager rewardManager;
        private readonly MN7TransactionManager transactionManager;
        private readonly HashSet<string> activePurchases = new();
        private IMN7IAPProvider provider;

        public bool IsInitialized => provider != null && provider.IsInitialized;
        public event Action<bool> OnInitializationCompleted;
        public event Action<MN7ItemData> OnPurchaseSuccessful;
        public event Action<MN7ItemData, MN7IAPResult> OnPurchaseFailed;

        public MN7IAPManager(MN7StoreConfig config, MN7InventoryManager inventoryManager, MN7RewardManager rewardManager, MN7TransactionManager transactionManager)
        {
            this.config = config;
            this.inventoryManager = inventoryManager;
            this.rewardManager = rewardManager;
            this.transactionManager = transactionManager;
        }

        public void SetProvider(IMN7IAPProvider iapProvider)
        {
            if (provider != null)
            {
                provider.PurchasePending -= HandleProviderPending;
                provider.PurchaseRestored -= HandleProviderRestored;
            }

            provider = iapProvider;

            if (provider != null)
            {
                provider.PurchasePending += HandleProviderPending;
                provider.PurchaseRestored += HandleProviderRestored;
            }
        }

        public void Initialize(Action<bool> onInitialized = null)
        {
            if (provider == null)
            {
                Debug.LogWarning("[MN7 Store] No IAP provider is configured. IAP remains disabled.");
                onInitialized?.Invoke(false);
                OnInitializationCompleted?.Invoke(false);
                return;
            }

            provider.Initialize(success =>
            {
                OnInitializationCompleted?.Invoke(success);
                onInitialized?.Invoke(success);
            });
        }

        public bool PurchaseItem(string itemId, Action<MN7IAPResult> onResult = null)
        {
            MN7ItemData item = GetItem(itemId);
            if (item == null) { onResult?.Invoke(MN7IAPResult.ProductNotFound); return false; }

            string normalizedItemId = item.itemId?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedItemId)) { onResult?.Invoke(MN7IAPResult.ProductNotFound); return false; }
            if (!activePurchases.Add(normalizedItemId))
            {
                onResult?.Invoke(MN7IAPResult.Pending);
                return false;
            }

            try
            {
                if (item.purchaseType != MN7PurchaseType.IAP)
                    return FailImmediately(item, MN7IAPResult.Failed, onResult);
                if (item.ownershipType == MN7ItemOwnershipType.Permanent && inventoryManager.IsOwned(item.itemId))
                    return FailImmediately(item, MN7IAPResult.Failed, onResult);
                if (string.IsNullOrWhiteSpace(item.iapProductId))
                    return FailImmediately(item, MN7IAPResult.ProductNotFound, onResult);
                if (!rewardManager.ValidateItemRewards(item))
                    return FailImmediately(item, MN7IAPResult.Failed, onResult);
                if (provider == null || !provider.IsInitialized)
                    return FailImmediately(item, MN7IAPResult.NotInitialized, onResult);

                provider.Purchase(item.iapProductId, result => HandleProviderResult(item, result, onResult));
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MN7 Store] IAP purchase threw an exception: {exception}");
                activePurchases.Remove(normalizedItemId);
                OnPurchaseFailed?.Invoke(item, MN7IAPResult.Failed);
                onResult?.Invoke(MN7IAPResult.Failed);
                return false;
            }
        }

        private bool FailImmediately(MN7ItemData item, MN7IAPResult result, Action<MN7IAPResult> onResult)
        {
            activePurchases.Remove(item?.itemId?.Trim());
            OnPurchaseFailed?.Invoke(item, result);
            onResult?.Invoke(result);
            return false;
        }

        private void HandleProviderResult(MN7ItemData item, MN7IAPResult result, Action<MN7IAPResult> onResult)
        {
            string itemId = item?.itemId?.Trim();
            if (result == MN7IAPResult.Pending)
            {
                OnPurchaseFailed?.Invoke(item, result);
                onResult?.Invoke(result);
                return;
            }
            if (result != MN7IAPResult.Success)
            {
                activePurchases.Remove(itemId);
                OnPurchaseFailed?.Invoke(item, result);
                onResult?.Invoke(result);
                return;
            }
            activePurchases.Remove(itemId);
            OnPurchaseSuccessful?.Invoke(item);
            onResult?.Invoke(MN7IAPResult.Success);
        }

        private void HandleProviderPending(string productId, string storeTransactionId, Action<bool> acknowledge)
        {
            MN7ItemData item = GetItemByIAPProductId(productId);
            if (item == null || string.IsNullOrWhiteSpace(storeTransactionId) || !rewardManager.ValidateItemRewards(item))
            {
                acknowledge?.Invoke(false);
                return;
            }

            string itemId = item.itemId.Trim();
            if (!activePurchases.Add(itemId))
            {
                Debug.LogWarning($"[MN7 Store] IAP item already processing: {itemId}");
                return;
            }

            try
            {
                MN7TransactionData transaction = transactionManager.CreateTransaction(
                    itemId, MN7PurchaseType.IAP, storeTransactionId);
                if (transaction == null) { acknowledge?.Invoke(false); return; }

                MN7TransactionStatus status = transactionManager.GetStatus(transaction.transactionId);
                if (status == MN7TransactionStatus.Completed)
                {
                    acknowledge?.Invoke(true);
                    OnPurchaseSuccessful?.Invoke(item);
                    return;
                }

                if (status == MN7TransactionStatus.Pending &&
                    !transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.PaymentCommitted))
                {
                    acknowledge?.Invoke(false);
                    return;
                }

                status = transactionManager.GetStatus(transaction.transactionId);
                if (status != MN7TransactionStatus.PaymentCommitted && status != MN7TransactionStatus.RewardsGranted)
                {
                    acknowledge?.Invoke(false);
                    return;
                }

                if (!GrantTransactionRewards(transaction, item))
                {
                    acknowledge?.Invoke(false);
                    return;
                }

                acknowledge?.Invoke(true);
                OnPurchaseSuccessful?.Invoke(item);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MN7 Store] Pending IAP processing failed: {exception}");
                acknowledge?.Invoke(false);
            }
            finally
            {
                activePurchases.Remove(itemId);
            }
        }

        private void HandleProviderRestored(string productId, string storeTransactionId)
        {
            MN7ItemData item = GetItemByIAPProductId(productId);
            if (item == null || item.ownershipType != MN7ItemOwnershipType.Permanent || string.IsNullOrWhiteSpace(storeTransactionId))
                return;

            try
            {
                MN7TransactionData transaction = transactionManager.CreateTransaction(
                    item.itemId, MN7PurchaseType.IAP, storeTransactionId);
                if (transaction == null) return;

                MN7TransactionStatus status = transactionManager.GetStatus(transaction.transactionId);
                if (status == MN7TransactionStatus.Completed)
                {
                    if (!inventoryManager.IsOwned(item.itemId))
                        GrantTransactionRewards(transaction, item);
                    return;
                }

                if (status == MN7TransactionStatus.Pending &&
                    !transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.PaymentCommitted))
                    return;

                GrantTransactionRewards(transaction, item);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MN7 Store] Failed to restore IAP entitlement: {exception}");
            }
        }

        private bool GrantTransactionRewards(MN7TransactionData transaction, MN7ItemData item)
        {
            if (transaction == null || item == null || item.rewards == null) return false;
            if (transactionManager.GetStatus(transaction.transactionId) == MN7TransactionStatus.Completed) return true;

            for (int rewardIndex = 0; rewardIndex < item.rewards.Count; rewardIndex++)
            {
                MN7RewardData reward = item.rewards[rewardIndex];
                if (reward == null) return false;
                string operationId = transactionManager.GetRewardOperationId(transaction.transactionId, rewardIndex);
                if (transactionManager.IsRewardOperationCompleted(transaction.transactionId, operationId)) continue;
                if (!rewardManager.GrantRewardDirect(reward, operationId)) return false;
                transactionManager.MarkRewardOperationCompleted(transaction.transactionId, operationId, rewardIndex);
            }

            return transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.RewardsGranted) &&
                   transactionManager.SetStatus(transaction.transactionId, MN7TransactionStatus.Completed);
        }

        private MN7ItemData GetItem(string itemId)
        {
            if (config?.items == null || string.IsNullOrWhiteSpace(itemId)) return null;
            string id = itemId.Trim();
            foreach (MN7ItemData item in config.items)
                if (item != null && string.Equals(item.itemId?.Trim(), id, StringComparison.Ordinal)) return item;
            return null;
        }

        private MN7ItemData GetItemByIAPProductId(string productId)
        {
            if (config?.items == null || string.IsNullOrWhiteSpace(productId)) return null;
            string id = productId.Trim();
            foreach (MN7ItemData item in config.items)
            {
                if (item == null || item.purchaseType != MN7PurchaseType.IAP) continue;
                if (string.Equals(item.iapProductId?.Trim(), id, StringComparison.Ordinal)) return item;
            }
            return null;
        }
    }
}
