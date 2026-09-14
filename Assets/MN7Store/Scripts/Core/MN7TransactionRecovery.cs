using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7TransactionRecovery
    {
        private readonly MN7TransactionManager transactionManager;
        private readonly MN7PurchaseManager purchaseManager;
        private readonly MN7StoreConfig config;
        private readonly MN7AdPurchaseProgress adProgress;

        public MN7TransactionRecovery(
            MN7TransactionManager transactionManager,
            MN7PurchaseManager purchaseManager,
            MN7StoreConfig config,
            MN7AdPurchaseProgress adProgress)
        {
            this.transactionManager = transactionManager;
            this.purchaseManager = purchaseManager;
            this.config = config;
            this.adProgress = adProgress;
        }

        public void Recover()
        {
            if (transactionManager == null)
            {
                Debug.LogError("[MN7 Store] Transaction Manager is null during recovery.");
                return;
            }

            if (purchaseManager == null)
            {
                Debug.LogError("[MN7 Store] Purchase Manager is null during recovery.");
                return;
            }

            if (config == null)
            {
                Debug.LogError("[MN7 Store] Store Config is null during recovery.");
                return;
            }

            List<MN7TransactionData> recoverable =
                transactionManager.GetRecoverableTransactions();

            if (recoverable != null && recoverable.Count > 0)
            {
                Debug.Log($"[MN7 Store] Recovering {recoverable.Count} transactions.");

                for (int i = 0; i < recoverable.Count; i++)
                    RecoverTransaction(recoverable[i]);
            }
            else
            {
                Debug.Log("[MN7 Store] No transactions require recovery.");
            }

            // Crash-window protection for rewarded ads.
            // The ad progress now records when it was last changed. We only
            // clear a threshold when that progress predates (or is from an
            // old save with no timestamp) the completed rewarded-ad transaction.
            // This prevents an old completed transaction from clearing progress
            // that belongs to a brand-new ad cycle.
            RecoverStaleRewardedAdProgress();
        }

        private void RecoverTransaction(MN7TransactionData transaction)
        {
            if (transaction == null)
                return;

            MN7ItemData item = FindItem(transaction.itemId);

            if (item == null)
            {
                Debug.LogError(
                    $"[MN7 Store] Cannot recover transaction. Item not found: {transaction.itemId}"
                );
                return;
            }

            bool recovered = purchaseManager.RecoverTransactionRewards(transaction, item);

            if (recovered)
            {
                if (IsRewardedAdTransaction(transaction))
                    ResetRewardedAdProgress(transaction.itemId, "after recovery");

                Debug.Log($"[MN7 Store] Transaction recovered: {transaction.transactionId}");
            }
            else
            {
                Debug.LogWarning(
                    $"[MN7 Store] Transaction still requires recovery: {transaction.transactionId}"
                );
            }
        }

        private void RecoverStaleRewardedAdProgress()
        {
            if (adProgress == null)
                return;

            foreach (MN7TransactionData transaction in transactionManager.GetAllTransactions())
            {
                if (transaction == null || !transactionManager.IsCompleted(transaction.transactionId))
                    continue;

                if (!IsRewardedAdTransaction(transaction))
                    continue;

                MN7ItemData item = FindItem(transaction.itemId);

                if (item == null || item.requiredAds <= 0)
                    continue;

                int currentProgress = adProgress.GetProgress(item.itemId);

                if (currentProgress < item.requiredAds)
                    continue;

                long progressUpdatedAt =
                    adProgress.GetProgressUpdatedAtUnixSeconds(item.itemId);

                // A timestamp of zero represents an older save created before
                // progress timestamps existed. In that case, preserve the old
                // recovery behavior and clear the stale threshold.
                bool belongsToCompletedTransaction =
                    progressUpdatedAt == 0 ||
                    progressUpdatedAt <= transaction.timestamp;

                if (!belongsToCompletedTransaction)
                {
                    Debug.Log(
                        $"[MN7 Store] Preserved rewarded-ad progress for a newer cycle: " +
                        $"{item.itemId}"
                    );
                    continue;
                }

                adProgress.ResetProgress(item.itemId);
                Debug.Log(
                    $"[MN7 Store] Cleared stale rewarded-ad threshold after completed " +
                    $"transaction: {item.itemId}"
                );
            }
        }

        private bool IsRewardedAdTransaction(MN7TransactionData transaction)
        {
            return transaction != null &&
                   Enum.TryParse(
                       transaction.purchaseType,
                       out MN7PurchaseType purchaseType
                   ) &&
                   purchaseType == MN7PurchaseType.RewardedAd;
        }

        private void ResetRewardedAdProgress(string itemId, string reason)
        {
            if (adProgress == null || string.IsNullOrWhiteSpace(itemId))
                return;

            adProgress.ResetProgress(itemId);

            Debug.Log(
                $"[MN7 Store] Rewarded-ad progress reset {reason}: {itemId}"
            );
        }

        private MN7ItemData FindItem(string itemId)
        {
            if (config == null ||
                config.items == null ||
                string.IsNullOrWhiteSpace(itemId))
            {
                return null;
            }

            string normalizedId = itemId.Trim();

            for (int i = 0; i < config.items.Count; i++)
            {
                MN7ItemData item = config.items[i];

                if (item == null)
                    continue;

                if (item.itemId == normalizedId)
                    return item;
            }

            return null;
        }
    }
}
