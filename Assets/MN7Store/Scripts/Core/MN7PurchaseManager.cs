using System;
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

        public event Action<MN7ItemData> OnPurchaseSuccessful;
        public event Action<MN7ItemData> OnPurchaseFailed;
        private readonly MN7TransactionManager transactionManager;

        private readonly MN7RewardManager rewardManager;

        public MN7PurchaseManager(
    MN7StoreConfig config,
    MN7CurrencyManager currencyManager,
    MN7InventoryManager inventoryManager,
    MN7AdManager adManager,
    MN7AdPurchaseProgress adProgress,
    MN7RewardManager rewardManager,
    MN7TransactionManager transactionManager)
        {
            this.config = config;
            this.currencyManager = currencyManager;
            this.inventoryManager = inventoryManager;
            this.adManager = adManager;
            this.adProgress = adProgress;
            this.rewardManager = rewardManager;
            this.transactionManager = transactionManager;
        }

        //==================================================
        // FIND ITEM
        //==================================================

        public MN7ItemData GetItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            foreach (MN7ItemData item in config.items)
            {
                if (item == null)
                    continue;

                if (item.itemId == itemId)
                    return item;
            }

            return null;
        }

        //==================================================
        // OWNERSHIP
        //==================================================

        public bool IsOwned(string itemId)
        {
            return inventoryManager.IsOwned(itemId);
        }

        //==================================================
        // CURRENCY PURCHASE
        //==================================================

        public bool PurchaseWithCurrency(
    string itemId)
        {
            MN7ItemData item =
                GetItem(itemId);

            //==================================================
            // ITEM
            //==================================================

            if (item == null)
            {
                Debug.LogWarning(
                    $"[MN7 Store] Item not found: {itemId}"
                );

                OnPurchaseFailed?.Invoke(
                    null
                );

                return false;
            }

            //==================================================
            // OWNERSHIP
            //==================================================

            if (item.ownershipType ==
                    MN7ItemOwnershipType.Permanent &&
                inventoryManager.IsOwned(itemId))
            {
                OnPurchaseFailed?.Invoke(
                    item
                );

                return false;
            }

            //==================================================
            // PURCHASE TYPE
            //==================================================

            if (item.purchaseType !=
                    MN7PurchaseType.Currency &&
                item.purchaseType !=
                    MN7PurchaseType.CurrencyOrAd)
            {
                OnPurchaseFailed?.Invoke(
                    item
                );

                return false;
            }

            //==================================================
            // VALIDATE REWARDS
            //==================================================

            if (!rewardManager.ValidateItemRewards(
                    item))
            {
                Debug.LogError(
                    $"[MN7 Store] Reward validation failed: " +
                    $"{item.itemId}"
                );

                OnPurchaseFailed?.Invoke(
                    item
                );

                return false;
            }

            //==================================================
            // VALIDATE PAYMENT
            //==================================================

            if (!currencyManager.CanSpend(
                    item.currencyId,
                    item.currencyCost))
            {
                Debug.Log(
                    $"[MN7 Store] Not enough currency: " +
                    $"{item.displayName}"
                );

                OnPurchaseFailed?.Invoke(
                    item
                );

                return false;
            }

            //==================================================
            // CREATE TRANSACTION
            //==================================================

            MN7TransactionData transaction =
                transactionManager.CreateTransaction(
                    item.itemId,
                    item.purchaseType
                );

            if (transaction == null)
            {
                OnPurchaseFailed?.Invoke(
                    item
                );

                return false;
            }

            //==================================================
            // PAYMENT
            //==================================================

            bool spent =
                currencyManager.Spend(
                    item.currencyId,
                    item.currencyCost
                );

            if (!spent)
            {
                transactionManager.SetStatus(
                    transaction.transactionId,
                    MN7TransactionStatus.Failed
                );

                OnPurchaseFailed?.Invoke(
                    item
                );

                return false;
            }

            //==================================================
            // PAYMENT COMMITTED
            //==================================================

            transactionManager.SetStatus(
                transaction.transactionId,
                MN7TransactionStatus.PaymentCommitted
            );

            //==================================================
            // REWARDS
            //==================================================

            bool rewardsGranted =
                ProcessTransactionRewards(
                    transaction,
                    item
                );

            if (!rewardsGranted)
            {
                /*
                 * IMPORTANT:
                 *
                 * Do NOT mark this transaction as Failed.
                 *
                 * Payment already happened.
                 * The transaction must remain recoverable.
                 */

                Debug.LogError(
                    $"[MN7 Store] Transaction requires recovery: " +
                    $"{transaction.transactionId}"
                );

                OnPurchaseFailed?.Invoke(
                    item
                );

                return false;
            }

            PurchaseSuccessful(
                item
            );

            return true;
        }

        //==================================================
        // RECOVER TRANSACTION
        //==================================================

        public bool RecoverTransactionRewards(
            MN7TransactionData transaction,
            MN7ItemData item)
        {
            if (transaction == null)
                return false;

            if (item == null)
                return false;

            MN7TransactionStatus status =
                transactionManager.GetStatus(
                    transaction.transactionId
                );

            if (status !=
                    MN7TransactionStatus.PaymentCommitted &&
                status !=
                    MN7TransactionStatus.RewardsGranted)
            {
                return false;
            }

            return ProcessTransactionRewards(
                transaction,
                item
            );
        }

        //==================================================
        // PROCESS TRANSACTION REWARDS
        //==================================================

        private bool ProcessTransactionRewards(
            MN7TransactionData transaction,
            MN7ItemData item)
        {
            if (transaction == null)
                return false;

            if (item == null)
                return false;

            if (item.rewards == null)
                return false;

            //==================================================
            // PROCESS EACH REWARD
            //==================================================

            for (
                int rewardIndex = 0;
                rewardIndex < item.rewards.Count;
                rewardIndex++)
            {
                // Already granted?
                if (transactionManager.IsRewardApplied(
                        transaction.transactionId,
                        rewardIndex))
                {
                    continue;
                }

                MN7RewardData reward =
                    item.rewards[rewardIndex];

                if (reward == null)
                {
                    Debug.LogError(
                        $"[MN7 Store] Null reward at index " +
                        $"{rewardIndex}: {item.itemId}"
                    );

                    return false;
                }

                //==================================================
                // GRANT
                //==================================================

                bool granted =
    rewardManager.GrantRewardDirect(
        reward
    );

                if (!granted)
                {
                    Debug.LogError(
                        $"[MN7 Store] Failed to grant reward " +
                        $"{rewardIndex} for transaction " +
                        $"{transaction.transactionId}"
                    );

                    return false;
                }

                //==================================================
                // SAVE IMMEDIATELY
                //==================================================

                transactionManager.MarkRewardApplied(
                    transaction.transactionId,
                    rewardIndex
                );
            }

            //==================================================
            // ALL REWARDS COMPLETE
            //==================================================

            transactionManager.SetStatus(
                transaction.transactionId,
                MN7TransactionStatus.RewardsGranted
            );

            transactionManager.SetStatus(
                transaction.transactionId,
                MN7TransactionStatus.Completed
            );

            return true;
        }

        //==================================================
        // FREE PURCHASE
        //==================================================

        public bool PurchaseFree(string itemId)
        {
            MN7ItemData item = GetItem(itemId);

            if (item == null)
            {
                OnPurchaseFailed?.Invoke(null);
                return false;
            }

            if (inventoryManager.IsOwned(itemId))
            {
                OnPurchaseFailed?.Invoke(item);
                return false;
            }

            if (item.purchaseType != MN7PurchaseType.Free)
            {
                OnPurchaseFailed?.Invoke(item);
                return false;
            }

            bool added =
                inventoryManager.AddItem(itemId);

            if (!added)
            {
                OnPurchaseFailed?.Invoke(item);
                return false;
            }

            PurchaseSuccessful(item);

            return true;
        }

        //==================================================
        // REWARDED AD
        //==================================================

        public void PurchaseWithRewardedAd(
     string itemId,
     Action<MN7AdResult, int, int> onResult)
        {
            MN7ItemData item =
                GetItem(itemId);

            if (item == null)
            {
                onResult?.Invoke(
                    MN7AdResult.Failed,
                    0,
                    0
                );

                return;
            }

            if (inventoryManager.IsOwned(itemId))
            {
                onResult?.Invoke(
                    MN7AdResult.Failed,
                    item.requiredAds,
                    item.requiredAds
                );

                return;
            }

            if (item.purchaseType !=
                    MN7PurchaseType.RewardedAd &&
                item.purchaseType !=
                    MN7PurchaseType.CurrencyOrAd)
            {
                onResult?.Invoke(
                    MN7AdResult.Failed,
                    0,
                    item.requiredAds
                );

                return;
            }

            int currentProgress =
                adProgress.GetProgress(itemId);

            if (currentProgress >= item.requiredAds)
            {
                CompleteAdPurchase(
                    item,
                    onResult
                );

                return;
            }

            if (!adManager.IsRewardedAdReady(
                    item.adPlacementId))
            {
                Debug.Log(
                    $"[MN7 Store] Rewarded ad not ready."
                );

                onResult?.Invoke(
                    MN7AdResult.NotReady,
                    currentProgress,
                    item.requiredAds
                );

                return;
            }

            adManager.ShowRewardedAd(
                item.adPlacementId,
                result =>
                {
                    if (result != MN7AdResult.Completed)
                    {
                        onResult?.Invoke(
                            result,
                            adProgress.GetProgress(itemId),
                            item.requiredAds
                        );

                        return;
                    }

                    adProgress.AddProgress(
                        itemId,
                        1
                    );

                    int newProgress =
                        adProgress.GetProgress(itemId);

                    Debug.Log(
                        $"[MN7 Store] Ad progress: " +
                        $"{newProgress}/{item.requiredAds}"
                    );

                    if (newProgress >=
                        item.requiredAds)
                    {
                        CompleteAdPurchase(
                            item,
                            onResult
                        );

                        return;
                    }

                    onResult?.Invoke(
                        MN7AdResult.Completed,
                        newProgress,
                        item.requiredAds
                    );
                }
            );
        }

        private void CompleteAdPurchase(
    MN7ItemData item,
    Action<MN7AdResult, int, int> onResult)
        {
            bool added =
                inventoryManager.AddItem(
                    item.itemId
                );

            if (!added)
            {
                onResult?.Invoke(
                    MN7AdResult.Failed,
                    adProgress.GetProgress(item.itemId),
                    item.requiredAds
                );

                return;
            }

            adProgress.ResetProgress(
                item.itemId
            );

            PurchaseSuccessful(item);

            onResult?.Invoke(
                MN7AdResult.Completed,
                item.requiredAds,
                item.requiredAds
            );
        }

        //==================================================
        // IAP
        //==================================================

        public bool PurchaseWithIAP(string itemId)
        {
            MN7ItemData item = GetItem(itemId);

            if (item == null)
            {
                OnPurchaseFailed?.Invoke(null);
                return false;
            }

            if (inventoryManager.IsOwned(itemId))
            {
                OnPurchaseFailed?.Invoke(item);
                return false;
            }

            if (item.purchaseType != MN7PurchaseType.IAP)
            {
                OnPurchaseFailed?.Invoke(item);
                return false;
            }

            /*
             * Actual IAP implementation will be added
             * in the IAP phase.
             */

            Debug.Log(
                $"[MN7 Store] IAP purchase requested: " +
                $"{item.iapProductId}"
            );

            return false;
        }

        //==================================================
        // CAN PURCHASE
        //==================================================

        public bool CanPurchaseWithCurrency(string itemId)
        {
            MN7ItemData item = GetItem(itemId);

            if (item == null)
                return false;

            if (inventoryManager.IsOwned(itemId))
                return false;

            if (item.purchaseType != MN7PurchaseType.Currency &&
                item.purchaseType != MN7PurchaseType.CurrencyOrAd)
            {
                return false;
            }

            return currencyManager.CanSpend(
                item.currencyId,
                item.currencyCost
            );
        }

        //==================================================
        // SUCCESS
        //==================================================

        private void PurchaseSuccessful(
            MN7ItemData item)
        {
            Debug.Log(
                $"[MN7 Store] Purchase successful: " +
                $"{item.displayName}"
            );

            OnPurchaseSuccessful?.Invoke(item);
        }
    }
}