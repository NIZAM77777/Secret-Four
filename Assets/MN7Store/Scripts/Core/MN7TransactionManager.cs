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

        private readonly Dictionary<
            string,
            MN7TransactionData>
            transactions = new();

        //==================================================
        // CONSTRUCTOR
        //==================================================

        public MN7TransactionManager(
            MN7SaveManager saveManager)
        {
            this.saveManager =
                saveManager;

            Load();
        }

        //==================================================
        // CREATE
        //==================================================

        public MN7TransactionData CreateTransaction(
            string itemId,
            MN7PurchaseType purchaseType)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            string transactionId =
                GenerateTransactionId();

            MN7TransactionData transaction =
                new()
                {
                    transactionId =
                        transactionId,

                    itemId =
                        itemId,

                    purchaseType =
                        purchaseType.ToString(),

                    timestamp =
                        DateTimeOffset.UtcNow
                            .ToUnixTimeSeconds(),

                    status =
                        MN7TransactionStatus
                            .Pending
                            .ToString(),

                    appliedRewardIndexes =
                        new List<int>()
                };

            transactions[
                transactionId] =
                transaction;

            Save();

            Debug.Log(
                $"[MN7 Store] Transaction created: " +
                $"{transactionId}"
            );

            return transaction;
        }

        //==================================================
        // STATUS
        //==================================================

        public void SetStatus(
            string transactionId,
            MN7TransactionStatus status)
        {
            if (!transactions.TryGetValue(
                    transactionId,
                    out MN7TransactionData transaction))
            {
                Debug.LogWarning(
                    $"[MN7 Store] Transaction not found: " +
                    $"{transactionId}"
                );

                return;
            }

            transaction.status =
                status.ToString();

            Save();

            Debug.Log(
                $"[MN7 Store] Transaction status: " +
                $"{transactionId} → {status}"
            );
        }

        //==================================================
        // GET STATUS
        //==================================================

        public MN7TransactionStatus GetStatus(
            string transactionId)
        {
            if (!transactions.TryGetValue(
                    transactionId,
                    out MN7TransactionData transaction))
            {
                return MN7TransactionStatus.Failed;
            }

            if (Enum.TryParse(
                    transaction.status,
                    out MN7TransactionStatus status))
            {
                return status;
            }

            return MN7TransactionStatus.Failed;
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
        // REWARD APPLIED
        //==================================================

        public bool IsRewardApplied(
            string transactionId,
            int rewardIndex)
        {
            MN7TransactionData transaction =
                GetTransaction(transactionId);

            if (transaction == null)
                return false;

            if (transaction.appliedRewardIndexes == null)
            {
                transaction.appliedRewardIndexes =
                    new List<int>();
            }

            return transaction.appliedRewardIndexes
                .Contains(rewardIndex);
        }

        //==================================================
        // MARK REWARD APPLIED
        //==================================================

        public void MarkRewardApplied(
            string transactionId,
            int rewardIndex)
        {
            MN7TransactionData transaction =
                GetTransaction(transactionId);

            if (transaction == null)
                return;

            if (rewardIndex < 0)
                return;

            if (transaction.appliedRewardIndexes == null)
            {
                transaction.appliedRewardIndexes =
                    new List<int>();
            }

            if (transaction.appliedRewardIndexes
                .Contains(rewardIndex))
            {
                return;
            }

            transaction.appliedRewardIndexes.Add(
                rewardIndex
            );

            Save();

            Debug.Log(
                $"[MN7 Store] Reward marked applied: " +
                $"{transactionId} / Reward {rewardIndex}"
            );
        }

        //==================================================
        // ALL REWARDS APPLIED
        //==================================================

        public bool AreAllRewardsApplied(
            string transactionId,
            int rewardCount)
        {
            MN7TransactionData transaction =
                GetTransaction(transactionId);

            if (transaction == null)
                return false;

            if (rewardCount <= 0)
                return true;

            if (transaction.appliedRewardIndexes == null)
                return false;

            return transaction.appliedRewardIndexes.Count
                >= rewardCount;
        }

        //==================================================
        // INCOMPLETE TRANSACTIONS
        //==================================================

        public List<MN7TransactionData>
            GetRecoverableTransactions()
        {
            List<MN7TransactionData> result =
                new();

            foreach (
                MN7TransactionData transaction
                in transactions.Values)
            {
                if (transaction == null)
                    continue;

                MN7TransactionStatus status =
                    GetStatus(
                        transaction.transactionId
                    );

                if (status ==
                        MN7TransactionStatus.PaymentCommitted ||
                    status ==
                        MN7TransactionStatus.RewardsGranted)
                {
                    result.Add(
                        transaction
                    );
                }
            }

            return result;
        }

        //==================================================
        // COMPLETED
        //==================================================

        public bool IsCompleted(
            string transactionId)
        {
            return GetStatus(
                transactionId)
                ==
                MN7TransactionStatus.Completed;
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

            foreach (
                MN7TransactionData transaction
                in transactions.Values)
            {
                saveData.transactions.Add(
                    transaction
                );
            }

            string json =
                JsonUtility.ToJson(
                    saveData
                );

            saveManager.Save(
                SaveKey,
                json
            );
        }

        //==================================================
        // LOAD
        //==================================================

        private void Load()
        {
            transactions.Clear();

            string json =
                saveManager.Load(
                    SaveKey
                );

            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                MN7TransactionSaveData saveData =
                    JsonUtility.FromJson<
                        MN7TransactionSaveData>(
                            json
                        );

                if (saveData == null ||
                    saveData.transactions == null)
                {
                    return;
                }

                foreach (
                    MN7TransactionData transaction
                    in saveData.transactions)
                {
                    if (transaction == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(
                            transaction.transactionId))
                    {
                        continue;
                    }

                    if (transaction
                        .appliedRewardIndexes == null)
                    {
                        transaction
                            .appliedRewardIndexes =
                            new List<int>();
                    }

                    transactions[
                        transaction.transactionId] =
                        transaction;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[MN7 Store] Failed to load transactions.\n" +
                    $"{exception}"
                );
            }
        }
    }
}