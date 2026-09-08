using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7TransactionRecovery
    {
        private readonly MN7TransactionManager
            transactionManager;

        private readonly MN7PurchaseManager
            purchaseManager;

        private readonly MN7StoreConfig
            config;


        //==================================================
        // CONSTRUCTOR
        //==================================================

        public MN7TransactionRecovery(
            MN7TransactionManager transactionManager,
            MN7PurchaseManager purchaseManager,
            MN7StoreConfig config)
        {
            this.transactionManager =
                transactionManager;

            this.purchaseManager =
                purchaseManager;

            this.config =
                config;
        }


        //==================================================
        // RECOVER
        //==================================================

        public void Recover()
        {
            if (transactionManager == null)
            {
                Debug.LogError(
                    "[MN7 Store] Transaction Manager " +
                    "is null during recovery."
                );

                return;
            }

            if (purchaseManager == null)
            {
                Debug.LogError(
                    "[MN7 Store] Purchase Manager " +
                    "is null during recovery."
                );

                return;
            }

            if (config == null)
            {
                Debug.LogError(
                    "[MN7 Store] Store Config " +
                    "is null during recovery."
                );

                return;
            }

            List<MN7TransactionData>
                recoverable =
                    transactionManager
                        .GetRecoverableTransactions();

            if (recoverable == null ||
                recoverable.Count == 0)
            {
                Debug.Log(
                    "[MN7 Store] No transactions " +
                    "require recovery."
                );

                return;
            }

            Debug.Log(
                $"[MN7 Store] Recovering " +
                $"{recoverable.Count} transactions."
            );

            for (
                int i = 0;
                i < recoverable.Count;
                i++)
            {
                RecoverTransaction(
                    recoverable[i]
                );
            }
        }


        //==================================================
        // RECOVER TRANSACTION
        //==================================================

        private void RecoverTransaction(
            MN7TransactionData transaction)
        {
            if (transaction == null)
                return;

            MN7ItemData item =
                FindItem(
                    transaction.itemId
                );

            if (item == null)
            {
                Debug.LogError(
                    $"[MN7 Store] Cannot recover " +
                    $"transaction. Item not found: " +
                    $"{transaction.itemId}"
                );

                return;
            }

            bool recovered =
                purchaseManager
                    .RecoverTransactionRewards(
                        transaction,
                        item
                    );

            if (recovered)
            {
                Debug.Log(
                    $"[MN7 Store] Transaction recovered: " +
                    $"{transaction.transactionId}"
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[MN7 Store] Transaction still " +
                    $"requires recovery: " +
                    $"{transaction.transactionId}"
                );
            }
        }


        //==================================================
        // FIND ITEM
        //==================================================

        private MN7ItemData FindItem(
            string itemId)
        {
            if (config == null ||
                config.items == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(
                    itemId))
            {
                return null;
            }

            for (
                int i = 0;
                i < config.items.Count;
                i++)
            {
                MN7ItemData item =
                    config.items[i];

                if (item == null)
                    continue;

                if (item.itemId == itemId)
                    return item;
            }

            return null;
        }
    }
}