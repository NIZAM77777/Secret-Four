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

        public event Action<MN7ItemData> OnPurchaseSuccessful;
        public event Action<MN7ItemData> OnPurchaseFailed;
        private readonly MN7TransactionManager transactionManager;
        private readonly HashSet<string> activePurchaseItems = new();

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
            // PURCHASE LOCK
            //==================================================

            if (activePurchaseItems.Contains(itemId))
            {
                Debug.LogWarning(
                    $"[MN7 Store] Purchase already in progress: " +
                    $"{itemId}"
                );

                OnPurchaseFailed?.Invoke(
                    item
                );

                return false;
            }


            activePurchaseItems.Add(itemId);

            try
            {
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
                    Debug.LogError(
                        $"[MN7 Store] Failed to create transaction: " +
                        $"{item.itemId}"
                    );

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
                     * Payment has already been committed.
                     *
                     * Do NOT mark this transaction as Failed.
                     *
                     * The transaction remains recoverable by
                     * MN7TransactionRecovery.
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

                //==================================================
                // SUCCESS
                //==================================================

                PurchaseSuccessful(
                    item
                );

                return true;
            }
            finally
            {
                //==================================================
                // RELEASE PURCHASE LOCK
                //==================================================

                activePurchaseItems.Remove(
                    itemId
                );
            }
        }

        //==================================================
        // PURCHASE LOCK
        //==================================================

        private bool TryLockPurchase(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(
                    itemId))
            {
                return false;
            }

            if (activePurchaseItems.Contains(
                    itemId))
            {
                return false;
            }

            activePurchaseItems.Add(
                itemId
            );

            return true;
        }

        private void UnlockPurchase(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(
                    itemId))
            {
                return;
            }

            activePurchaseItems.Remove(
                itemId
            );
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

                string operationId =
                    transactionManager.GetRewardOperationId(
                        transaction.transactionId,
                        rewardIndex
                    );

                //==================================================
                // ALREADY COMPLETED?
                //==================================================

                if (transactionManager
                    .IsRewardOperationCompleted(
                        transaction.transactionId,
                        operationId))
                {
                    Debug.Log(
                        $"[MN7 Store] Reward already processed: " +
                        $"{operationId}"
                    );

                    continue;
                }

                //==================================================
                // GRANT
                //==================================================

                bool granted =
                    rewardManager.GrantRewardDirect(
                        reward,
                        operationId
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
                // RECORD OPERATION
                //==================================================

                transactionManager
                    .MarkRewardOperationCompleted(
                        transaction.transactionId,
                        operationId,
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

        //==================================================
        // FREE PURCHASE
        //==================================================

        public bool PurchaseFree(
            string itemId
        )
        {
            MN7ItemData item =
                GetItem(itemId);

            if (item == null)
            {
                Debug.LogError(
                    $"[MN7 Store] Item not found: {itemId}"
                );

                return false;
            }

            if (!TryLockPurchase(itemId))
            {
                Debug.LogWarning(
                    $"[MN7 Store] Purchase already in progress: " +
                    $"{itemId}"
                );

                return false;
            }

            try
            {
                //==================================================
                // PURCHASE TYPE
                //==================================================

                if (item.purchaseType !=
                    MN7PurchaseType.Free)
                {
                    Debug.LogError(
                        $"[MN7 Store] Item is not a Free purchase: " +
                        $"{item.itemId}"
                    );

                    OnPurchaseFailed?.Invoke(
                        item
                    );

                    return false;
                }

                //==================================================
                // OWNERSHIP
                //==================================================

                if (item.ownershipType ==
                        MN7ItemOwnershipType.Permanent &&
                    inventoryManager.IsOwned(
                        item.itemId
                    ))
                {
                    Debug.LogWarning(
                        $"[MN7 Store] Item already owned: " +
                        $"{item.itemId}"
                    );

                    OnPurchaseFailed?.Invoke(
                        item
                    );

                    return false;
                }

                //==================================================
                // VALIDATE REWARDS
                //==================================================

                if (!rewardManager.ValidateItemRewards(
                        item
                    ))
                {
                    Debug.LogError(
                        $"[MN7 Store] Invalid rewards for item: " +
                        $"{item.itemId}"
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
                        MN7PurchaseType.Free
                    );

                if (transaction == null)
                {
                    Debug.LogError(
                        $"[MN7 Store] Failed to create Free " +
                        $"transaction: {item.itemId}"
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
                    Debug.LogError(
                        $"[MN7 Store] Failed to process Free " +
                        $"purchase rewards: {item.itemId}"
                    );

                    OnPurchaseFailed?.Invoke(
                        item
                    );

                    return false;
                }

                //==================================================
                // SUCCESS
                //==================================================

                OnPurchaseSuccessful?.Invoke(
                    item
                );

                Debug.Log(
                    $"[MN7 Store] Free purchase completed: " +
                    $"{item.itemId}"
                );

                return true;
            }
            finally
            {
                UnlockPurchase(
                    itemId
                );
            }
        }



        //==================================================
        // REWARDED AD
        //==================================================

        public void PurchaseWithRewardedAd(
     string itemId,
     Action<MN7AdResult> onAdResult = null
 )
        {
            MN7ItemData item =
                config != null
                    ? config.items.Find(
                        x => x != null &&
                             x.itemId == itemId
                    )
                    : null;

            if (item == null)
            {
                Debug.LogError(
                    $"[MN7 Store] Item not found: {itemId}"
                );

                onAdResult?.Invoke(
                    MN7AdResult.Failed
                );

                return;
            }

            if (!TryLockPurchase(itemId))
            {
                Debug.LogWarning(
                    $"[MN7 Store] Purchase already in progress: " +
                    $"{itemId}"
                );

                onAdResult?.Invoke(
                    MN7AdResult.Skipped
                );

                return;
            }

            if (!ValidateRewardedAdPurchase(
                    item
                ))
            {
                UnlockPurchase(itemId);

                onAdResult?.Invoke(
                    MN7AdResult.Failed
                );

                return;
            }

            int progress =
                adProgress.GetProgress(
                    item.itemId
                );

            if (progress >= item.requiredAds)
            {
                bool completed =
                    CompleteAdPurchase(
                        item
                    );

                UnlockPurchase(itemId);

                onAdResult?.Invoke(
                    completed
                        ? MN7AdResult.Completed
                        : MN7AdResult.Failed
                );

                return;
            }

            if (!adManager.IsRewardedAdReady(
                    item.adPlacementId
                ))
            {
                UnlockPurchase(itemId);

                onAdResult?.Invoke(
                    MN7AdResult.NotReady
                );

                return;
            }

            adManager.ShowRewardedAd(
                item.adPlacementId,
                result =>
                {
                    HandleRewardedAdResult(
                        item,
                        result,
                        onAdResult
                    );
                }
            );
        }

        private bool ValidateRewardedAdPurchase(
    MN7ItemData item
)
        {
            if (item == null)
                return false;

            if (string.IsNullOrWhiteSpace(
                    item.itemId))
            {
                Debug.LogError(
                    "[MN7 Store] Item ID is empty."
                );

                return false;
            }

            if (item.purchaseType !=
                MN7PurchaseType.RewardedAd &&
                item.purchaseType !=
                MN7PurchaseType.CurrencyOrAd)
            {
                Debug.LogError(
                    $"[MN7 Store] Item is not configured " +
                    $"for rewarded ads: {item.itemId}"
                );

                return false;
            }

            if (item.ownershipType ==
                MN7ItemOwnershipType.Permanent &&
                inventoryManager.IsOwned(
                    item.itemId
                ))
            {
                Debug.LogWarning(
                    $"[MN7 Store] Item already owned: " +
                    $"{item.itemId}"
                );

                return false;
            }

            if (item.requiredAds <= 0)
            {
                Debug.LogError(
                    $"[MN7 Store] Invalid required ads: " +
                    $"{item.itemId}"
                );

                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    item.adPlacementId))
            {
                Debug.LogError(
                    $"[MN7 Store] Ad placement ID is empty: " +
                    $"{item.itemId}"
                );

                return false;
            }

            if (!rewardManager.ValidateItemRewards(
                    item
                ))
            {
                Debug.LogError(
                    $"[MN7 Store] Invalid rewards: " +
                    $"{item.itemId}"
                );

                return false;
            }

            return true;
        }

        private void HandleRewardedAdResult(
    MN7ItemData item,
    MN7AdResult result,
    Action<MN7AdResult> onAdResult
)
        {
            if (item == null)
            {
                onAdResult?.Invoke(
                    MN7AdResult.Failed
                );

                return;
            }

            //==================================================
            // AD DID NOT COMPLETE
            //==================================================

            if (result != MN7AdResult.Completed)
            {
                UnlockPurchase(
                    item.itemId
                );

                onAdResult?.Invoke(
                    result
                );

                return;
            }

            //==================================================
            // ADD AD PROGRESS
            //==================================================

            if (!adProgress.AddProgress(
                    item.itemId,
                    1
                ))
            {
                UnlockPurchase(
                    item.itemId
                );

                onAdResult?.Invoke(
                    MN7AdResult.Failed
                );

                return;
            }

            int newProgress =
                adProgress.GetProgress(
                    item.itemId
                );

            Debug.Log(
                $"[MN7 Store] Ad progress: " +
                $"{item.itemId} " +
                $"{newProgress}/{item.requiredAds}"
            );

            //==================================================
            // REQUIRED ADS NOT YET REACHED
            //==================================================

            if (newProgress < item.requiredAds)
            {
                UnlockPurchase(
                    item.itemId
                );

                onAdResult?.Invoke(
                    MN7AdResult.Completed
                );

                return;
            }

            //==================================================
            // COMPLETE PURCHASE
            //==================================================

            bool completed =
                CompleteAdPurchase(
                    item
                );

            UnlockPurchase(
                item.itemId
            );

            onAdResult?.Invoke(
                completed
                    ? MN7AdResult.Completed
                    : MN7AdResult.Failed
            );
        }

        private bool CompleteAdPurchase(
    MN7ItemData item
)
        {
            if (item == null)
                return false;

            if (!rewardManager.ValidateItemRewards(
                    item
                ))
            {
                Debug.LogError(
                    $"[MN7 Store] Cannot complete rewarded " +
                    $"purchase because rewards are invalid: " +
                    $"{item.itemId}"
                );

                return false;
            }

            //==================================================
            // CREATE TRANSACTION
            //==================================================

            MN7TransactionData transaction =
                transactionManager.CreateTransaction(
                    item.itemId,
                    MN7PurchaseType.RewardedAd
                );

            if (transaction == null)
            {
                Debug.LogError(
                    $"[MN7 Store] Failed to create rewarded " +
                    $"ad transaction: {item.itemId}"
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
                Debug.LogError(
                    $"[MN7 Store] Reward processing failed " +
                    $"for rewarded ad transaction: " +
                    $"{transaction.transactionId}"
                );

                return false;
            }

            //==================================================
            // RESET AD PROGRESS
            //==================================================

            adProgress.ResetProgress(
                item.itemId
            );

            //==================================================
            // SUCCESS
            //==================================================

            OnPurchaseSuccessful?.Invoke(
                item
            );

            Debug.Log(
                $"[MN7 Store] Rewarded ad purchase completed: " +
                $"{item.itemId}"
            );

            return true;
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