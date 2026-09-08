using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    //==================================================
    // SAVE DATA
    //==================================================

    [Serializable]
    public class MN7TransactionSaveData
    {
        public int version = 1;

        public List<MN7TransactionData> transactions =
            new();
    }


    //==================================================
    // TRANSACTION MANAGER
    //==================================================

    public class MN7TransactionManager
    {
        private const int CurrentSaveVersion = 1;

        private const string DefaultSaveKey =
            "MN7_STORE_TRANSACTIONS";

        private readonly MN7SaveManager saveManager;

        private readonly string saveKey;

        private readonly Dictionary<
            string,
            MN7TransactionData>
            transactions = new();


        //==================================================
        // CONSTRUCTOR
        //==================================================

        public MN7TransactionManager(
            MN7SaveManager saveManager)
            : this(
                saveManager,
                DefaultSaveKey
            )
        {
        }


        public MN7TransactionManager(
            MN7SaveManager saveManager,
            string saveKey)
        {
            this.saveManager =
                saveManager;

            this.saveKey =
                string.IsNullOrWhiteSpace(saveKey)
                    ? DefaultSaveKey
                    : saveKey;

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
            {
                Debug.LogError(
                    "[MN7 Store] Cannot create transaction. " +
                    "Item ID is empty."
                );

                return null;
            }

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
                        new List<int>(),

                    completedRewardOperations =
                        new List<string>()
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

        public bool SetStatus(
     string transactionId,
     MN7TransactionStatus status)
        {
            MN7TransactionData transaction =
                GetTransaction(
                    transactionId
                );

            if (transaction == null)
            {
                Debug.LogWarning(
                    $"[MN7 Store] Transaction not found: " +
                    $"{transactionId}"
                );

                return false;
            }

            MN7TransactionStatus currentStatus =
                GetStatus(
                    transactionId
                );

            if (currentStatus == status)
            {
                return true;
            }

            if (!CanChangeStatus(
                    transactionId,
                    status))
            {
                Debug.LogError(
                    $"[MN7 Store] Invalid transaction " +
                    $"status transition: " +
                    $"{currentStatus} → {status} " +
                    $"({transactionId})"
                );

                return false;
            }

            transaction.status =
                status.ToString();

            Save();

            Debug.Log(
                $"[MN7 Store] Transaction status: " +
                $"{transactionId} → {status}"
            );

            return true;
        }


        //==================================================
        // GET STATUS
        //==================================================

        public MN7TransactionStatus GetStatus(
            string transactionId)
        {
            MN7TransactionData transaction =
                GetTransaction(
                    transactionId
                );

            if (transaction == null)
                return MN7TransactionStatus.Failed;

            if (string.IsNullOrWhiteSpace(
                    transaction.status))
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
            if (string.IsNullOrWhiteSpace(
                    transactionId))
            {
                return null;
            }

            transactions.TryGetValue(
                transactionId,
                out MN7TransactionData transaction
            );

            return transaction;
        }


        //==================================================
        // REWARD APPLIED
        //==================================================

        public bool IsRewardApplied(
            string transactionId,
            int rewardIndex)
        {
            if (rewardIndex < 0)
                return false;

            MN7TransactionData transaction =
                GetTransaction(
                    transactionId
                );

            if (transaction == null)
                return false;

            EnsureRewardLists(
                transaction
            );

            return transaction
                .appliedRewardIndexes
                .Contains(rewardIndex);
        }


        //==================================================
        // MARK REWARD APPLIED
        //==================================================

        public void MarkRewardApplied(
            string transactionId,
            int rewardIndex)
        {
            if (rewardIndex < 0)
                return;

            MN7TransactionData transaction =
                GetTransaction(
                    transactionId
                );

            if (transaction == null)
                return;

            EnsureRewardLists(
                transaction
            );

            if (transaction
                .appliedRewardIndexes
                .Contains(rewardIndex))
            {
                return;
            }

            transaction
                .appliedRewardIndexes
                .Add(rewardIndex);

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
                GetTransaction(
                    transactionId
                );

            if (transaction == null)
                return false;

            if (rewardCount <= 0)
                return true;

            EnsureRewardLists(
                transaction
            );

            for (
                int i = 0;
                i < rewardCount;
                i++)
            {
                if (!transaction
                    .appliedRewardIndexes
                    .Contains(i))
                {
                    return false;
                }
            }

            return true;
        }


        //==================================================
        // RECOVERABLE TRANSACTIONS
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
                transactionId
            ) ==
            MN7TransactionStatus.Completed;
        }


        //==================================================
        // FAILED
        //==================================================

        public bool IsFailed(
            string transactionId)
        {
            return GetStatus(
                transactionId
            ) ==
            MN7TransactionStatus.Failed;
        }


        //==================================================
        // PENDING
        //==================================================

        public bool IsPending(
            string transactionId)
        {
            return GetStatus(
                transactionId
            ) ==
            MN7TransactionStatus.Pending;
        }


        //==================================================
        // REWARD OPERATION CHECK
        //==================================================

        public bool IsRewardOperationCompleted(
            string transactionId,
            string operationId)
        {
            MN7TransactionData transaction =
                GetTransaction(
                    transactionId
                );

            if (transaction == null)
                return false;

            if (string.IsNullOrWhiteSpace(
                    operationId))
            {
                return false;
            }

            EnsureRewardLists(
                transaction
            );

            return transaction
                .completedRewardOperations
                .Contains(operationId);
        }


        //==================================================
        // MARK REWARD OPERATION COMPLETED
        //==================================================

        public void MarkRewardOperationCompleted(
            string transactionId,
            string operationId,
            int rewardIndex)
        {
            MN7TransactionData transaction =
                GetTransaction(
                    transactionId
                );

            if (transaction == null)
                return;

            if (string.IsNullOrWhiteSpace(
                    operationId))
            {
                return;
            }

            if (rewardIndex < 0)
                return;

            EnsureRewardLists(
                transaction
            );

            if (!transaction
                .completedRewardOperations
                .Contains(operationId))
            {
                transaction
                    .completedRewardOperations
                    .Add(operationId);
            }

            if (!transaction
                .appliedRewardIndexes
                .Contains(rewardIndex))
            {
                transaction
                    .appliedRewardIndexes
                    .Add(rewardIndex);
            }

            Save();

            Debug.Log(
                $"[MN7 Store] Reward operation completed: " +
                $"{operationId}"
            );
        }


        //==================================================
        // REWARD OPERATION ID
        //==================================================

        public string GetRewardOperationId(
            string transactionId,
            int rewardIndex)
        {
            if (string.IsNullOrWhiteSpace(
                    transactionId))
            {
                return string.Empty;
            }

            if (rewardIndex < 0)
                return string.Empty;

            return
                $"{transactionId}_REWARD_{rewardIndex}";
        }


        //==================================================
        // REMOVE TRANSACTION
        //==================================================

        public bool RemoveTransaction(
            string transactionId)
        {
            if (string.IsNullOrWhiteSpace(
                    transactionId))
            {
                return false;
            }

            if (!transactions.ContainsKey(
                    transactionId))
            {
                return false;
            }

            MN7TransactionStatus status =
                GetStatus(
                    transactionId
                );

            // Never manually remove an active
            // or recoverable transaction.
            if (status !=
                MN7TransactionStatus.Completed)
            {
                Debug.LogWarning(
                    $"[MN7 Store] Cannot remove " +
                    $"non-completed transaction: " +
                    $"{transactionId}"
                );

                return false;
            }

            transactions.Remove(
                transactionId
            );

            Save();

            Debug.Log(
                $"[MN7 Store] Transaction removed: " +
                $"{transactionId}"
            );

            return true;
        }


        //==================================================
        // TRANSACTION COUNT
        //==================================================

        public int GetTransactionCount()
        {
            return transactions.Count;
        }


        //==================================================
        // CLEANUP COMPLETED
        //==================================================

        public int CleanupCompletedTransactions(
            long maxAgeSeconds)
        {
            if (maxAgeSeconds < 0)
                return 0;

            long currentTime =
                DateTimeOffset.UtcNow
                    .ToUnixTimeSeconds();

            List<string> removeIds =
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

                if (status !=
                    MN7TransactionStatus.Completed)
                {
                    continue;
                }

                long age =
                    currentTime -
                    transaction.timestamp;

                if (age < 0)
                    continue;

                if (age >= maxAgeSeconds)
                {
                    removeIds.Add(
                        transaction.transactionId
                    );
                }
            }

            for (
                int i = 0;
                i < removeIds.Count;
                i++)
            {
                transactions.Remove(
                    removeIds[i]
                );
            }

            if (removeIds.Count > 0)
            {
                Save();

                Debug.Log(
                    $"[MN7 Store] Cleaned up " +
                    $"{removeIds.Count} completed " +
                    $"transactions."
                );
            }

            return removeIds.Count;
        }


        //==================================================
        // SAVE
        //==================================================

        private void Save()
        {
            if (saveManager == null)
            {
                Debug.LogError(
                    "[MN7 Store] Save Manager is null."
                );

                return;
            }

            MN7TransactionSaveData saveData =
                new()
                {
                    version =
                        CurrentSaveVersion
                };

            foreach (
                MN7TransactionData transaction
                in transactions.Values)
            {
                if (transaction == null)
                    continue;

                saveData.transactions.Add(
                    transaction
                );
            }

            string json =
                JsonUtility.ToJson(
                    saveData
                );

            saveManager.Save(
                saveKey,
                json
            );
        }


        //==================================================
        // LOAD
        //==================================================

        private void Load()
        {
            transactions.Clear();

            if (saveManager == null)
            {
                Debug.LogError(
                    "[MN7 Store] Save Manager is null."
                );

                return;
            }

            string json =
                saveManager.Load(
                    saveKey
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

                if (saveData == null)
                    return;

                if (saveData.transactions == null)
                    return;

                int loadedCount = 0;

                foreach (
                    MN7TransactionData transaction
                    in saveData.transactions)
                {
                    if (!IsValidTransaction(
                            transaction))
                    {
                        continue;
                    }

                    EnsureRewardLists(
                        transaction
                    );

                    // Dictionary prevents duplicate IDs.
                    transactions[
                        transaction.transactionId] =
                        transaction;

                    loadedCount++;
                }

                Debug.Log(
                    $"[MN7 Store] Loaded " +
                    $"{loadedCount} transactions. " +
                    $"Save version: " +
                    $"{saveData.version}"
                );
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "[MN7 Store] Failed to load transactions.\n" +
                    $"{exception}"
                );
            }
        }


        //==================================================
        // VALIDATE TRANSACTION
        //==================================================

        private bool IsValidTransaction(
            MN7TransactionData transaction)
        {
            if (transaction == null)
                return false;

            if (string.IsNullOrWhiteSpace(
                    transaction.transactionId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    transaction.itemId))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    transaction.purchaseType))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(
                    transaction.status))
            {
                return false;
            }

            if (!Enum.TryParse(
                    transaction.status,
                    out MN7TransactionStatus status))
            {
                return false;
            }

            if (!Enum.TryParse(
                    transaction.purchaseType,
                    out MN7PurchaseType purchaseType))
            {
                return false;
            }

            return true;
        }

        public bool CanChangeStatus(
    string transactionId,
    MN7TransactionStatus newStatus)
        {
            MN7TransactionData transaction =
                GetTransaction(
                    transactionId
                );

            if (transaction == null)
                return false;

            MN7TransactionStatus currentStatus =
                GetStatus(
                    transactionId
                );

            switch (currentStatus)
            {
                case MN7TransactionStatus.Pending:

                    return
                        newStatus ==
                            MN7TransactionStatus.PaymentCommitted
                        ||
                        newStatus ==
                            MN7TransactionStatus.Failed;


                case MN7TransactionStatus.PaymentCommitted:

                    return
                        newStatus ==
                            MN7TransactionStatus.RewardsGranted
                        ||
                        newStatus ==
                            MN7TransactionStatus.Failed;


                case MN7TransactionStatus.RewardsGranted:

                    return
                        newStatus ==
                            MN7TransactionStatus.Completed
                        ||
                        newStatus ==
                            MN7TransactionStatus.Failed;


                case MN7TransactionStatus.Completed:

                    return false;


                case MN7TransactionStatus.Failed:

                    return false;


                default:

                    return false;
            }
        }
        //==================================================
        // ENSURE REWARD LISTS
        //==================================================

        private void EnsureRewardLists(
            MN7TransactionData transaction)
        {
            if (transaction == null)
                return;

            if (transaction.appliedRewardIndexes == null)
            {
                transaction.appliedRewardIndexes =
                    new List<int>();
            }

            if (transaction.completedRewardOperations == null)
            {
                transaction.completedRewardOperations =
                    new List<string>();
            }
        }


        //==================================================
        // ID
        //==================================================

        private string GenerateTransactionId()
        {
            return
                $"MN7_{Guid.NewGuid():N}";
        }
    }
}