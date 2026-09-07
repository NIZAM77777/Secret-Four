using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    [Serializable]
    public class MN7TransactionSaveData
    {
        public List<MN7TransactionData> transactions =
            new();
    }

    public class MN7TransactionManager
    {
        private const string SaveKey =
            "MN7_STORE_TRANSACTIONS";

        private readonly MN7SaveManager saveManager;

        private readonly Dictionary<string, MN7TransactionData>
            transactions = new();

        public MN7TransactionManager(
            MN7SaveManager saveManager)
        {
            this.saveManager = saveManager;

            Load();
        }

        //==================================================
        // CREATE
        //==================================================

        public MN7TransactionData CreateTransaction(
            string itemId,
            MN7PurchaseType purchaseType)
        {
            string transactionId =
                GenerateTransactionId();

            MN7TransactionData transaction =
                new()
                {
                    transactionId = transactionId,
                    itemId = itemId,
                    purchaseType =
                        purchaseType.ToString(),
                    timestamp =
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    completed = false
                };

            transactions[transactionId] =
                transaction;

            Save();

            return transaction;
        }

        //==================================================
        // COMPLETE
        //==================================================

        public void CompleteTransaction(
            string transactionId)
        {
            if (!transactions.TryGetValue(
                transactionId,
                out MN7TransactionData transaction))
            {
                return;
            }

            transaction.completed = true;

            Save();
        }

        //==================================================
        // CHECK
        //==================================================

        public bool IsCompleted(
            string transactionId)
        {
            return transactions.TryGetValue(
                transactionId,
                out MN7TransactionData transaction)
                && transaction.completed;
        }

        //==================================================
        // GET
        //==================================================

        public MN7TransactionData GetTransaction(
            string transactionId)
        {
            transactions.TryGetValue(
                transactionId,
                out MN7TransactionData transaction);

            return transaction;
        }

        //==================================================
        // ID
        //==================================================

        private string GenerateTransactionId()
        {
            return
                $"MN7_{Guid.NewGuid():N}";
        }

        //==================================================
        // SAVE
        //==================================================

        private void Save()
        {
            MN7TransactionSaveData saveData =
                new();

            foreach (var transaction
                     in transactions.Values)
            {
                saveData.transactions.Add(
                    transaction);
            }

            string json =
                JsonUtility.ToJson(saveData);

            saveManager.Save(
                SaveKey,
                json);
        }

        //==================================================
        // LOAD
        //==================================================

        private void Load()
        {
            transactions.Clear();

            string json =
                saveManager.Load(SaveKey);

            if (string.IsNullOrEmpty(json))
                return;

            MN7TransactionSaveData saveData =
                JsonUtility.FromJson<MN7TransactionSaveData>(
                    json);

            if (saveData == null ||
                saveData.transactions == null)
            {
                return;
            }

            foreach (
                MN7TransactionData transaction
                in saveData.transactions)
            {
                if (transaction == null ||
                    string.IsNullOrWhiteSpace(
                        transaction.transactionId))
                {
                    continue;
                }

                transactions[
                    transaction.transactionId] =
                    transaction;
            }
        }
    }
}