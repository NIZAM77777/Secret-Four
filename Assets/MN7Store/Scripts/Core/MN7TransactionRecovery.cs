using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7TransactionRecovery
    {
        private readonly MN7TransactionManager transactionManager;
        private readonly MN7PurchaseManager purchaseManager;
        private readonly MN7StoreConfig config;

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
            List<MN7TransactionData> transactions =
                transactionManager
                    .GetRecoverableTransactions();

            if (transactions == null ||
                transactions.Count == 0)
            {
                return;
            }

            Debug.Log(
                $"[MN7 Store] Recovering " +
                $"{transactions.Count} transaction(s)."
            );

            foreach (
                MN7TransactionData transaction
                in transactions)
            {
                RecoverTransaction(
                    transaction
                );
            }
        }

        //==================================================
        // RECOVER SINGLE
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
                    $"[MN7 Store] Cannot recover transaction. " +
                    $"Item not found: {transaction.itemId}"
                );

                return;
            }

            Debug.Log(
                $"[MN7 Store] Recovering transaction: " +
                $"{transaction.transactionId}"
            );

            bool success =
                purchaseManager
                    .RecoverTransactionRewards(
                        transaction,
                        item
                    );

            if (success)
            {
                Debug.Log(
                    $"[MN7 Store] Transaction recovered: " +
                    $"{transaction.transactionId}"
                );
            }
            else
            {
                Debug.LogWarning(
                    $"[MN7 Store] Transaction still requires " +
                    $"recovery: {transaction.transactionId}"
                );
            }
        }

        //==================================================
        // FIND ITEM
        //==================================================

        private MN7ItemData FindItem(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(
                    itemId))
            {
                return null;
            }

            foreach (
                MN7ItemData item
                in config.items)
            {
                if (item == null)
                    continue;

                if (item.itemId == itemId)
                    return item;
            }

            return null;
        }
    }
}