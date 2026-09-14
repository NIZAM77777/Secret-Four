using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    [Serializable]
    public class MN7TransactionSaveData
    {
        public int version = MN7StoreSaveVersion.CurrentVersion;
        public List<MN7TransactionData> transactions = new();
    }

    /// <summary>
    /// Persistent transaction tracking for rewarded-ad item unlocks.
    /// </summary>
    public class MN7TransactionManager
    {
        private const int CurrentSaveVersion = MN7StoreSaveVersion.CurrentVersion;
        private const string DefaultSaveKey = "MN7_STORE_TRANSACTIONS";

        private readonly MN7SaveManager saveManager;
        private readonly string saveKey;
        private readonly Dictionary<string, MN7TransactionData> transactions = new();

        public MN7TransactionManager(MN7SaveManager saveManager)
            : this(saveManager, DefaultSaveKey) { }

        public MN7TransactionManager(MN7SaveManager saveManager, string saveKey)
        {
            this.saveManager = saveManager;
            this.saveKey = string.IsNullOrWhiteSpace(saveKey) ? DefaultSaveKey : saveKey;
            Load();
        }

        public MN7TransactionData CreateTransaction(string itemId, MN7PurchaseType purchaseType)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogError("[MN7 Store] Cannot create transaction. Item ID is empty.");
                return null;
            }

            if (purchaseType != MN7PurchaseType.RewardedAd)
            {
                Debug.LogError("[MN7 Store] Only Rewarded Ad transactions are supported.");
                return null;
            }

            MN7TransactionData transaction = new()
            {
                transactionId = GenerateTransactionId(),
                itemId = itemId.Trim(),
                purchaseType = purchaseType.ToString(),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                status = MN7TransactionStatus.Pending.ToString(),
                appliedRewardIndexes = new List<int>(),
                completedRewardOperations = new List<string>()
            };

            transactions[transaction.transactionId] = transaction;
            Save();
            Debug.Log($"[MN7 Store] Transaction created: {transaction.transactionId}");
            return transaction;
        }

        public bool SetStatus(string transactionId, MN7TransactionStatus status)
        {
            MN7TransactionData transaction = GetTransaction(transactionId);
            if (transaction == null)
            {
                Debug.LogWarning($"[MN7 Store] Transaction not found: {transactionId}");
                return false;
            }

            MN7TransactionStatus currentStatus = GetStatus(transactionId);
            if (currentStatus == status) return true;

            if (!CanChangeStatus(transactionId, status))
            {
                Debug.LogError($"[MN7 Store] Invalid transaction status transition: {currentStatus} → {status} ({transactionId})");
                return false;
            }

            transaction.status = status.ToString();
            Save();
            return true;
        }

        public MN7TransactionStatus GetStatus(string transactionId)
        {
            MN7TransactionData transaction = GetTransaction(transactionId);
            if (transaction == null || string.IsNullOrWhiteSpace(transaction.status))
                return MN7TransactionStatus.Failed;

            return Enum.TryParse(transaction.status, out MN7TransactionStatus status)
                ? status
                : MN7TransactionStatus.Failed;
        }

        public MN7TransactionData GetTransaction(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId)) return null;
            transactions.TryGetValue(transactionId.Trim(), out MN7TransactionData transaction);
            return transaction;
        }

        public IReadOnlyList<MN7TransactionData> GetAllTransactions() =>
            new List<MN7TransactionData>(transactions.Values);

        public List<MN7TransactionData> GetRecoverableTransactions()
        {
            List<MN7TransactionData> result = new();
            foreach (MN7TransactionData transaction in transactions.Values)
            {
                if (transaction == null) continue;
                MN7TransactionStatus status = GetStatus(transaction.transactionId);
                if (status == MN7TransactionStatus.Pending ||
                    status == MN7TransactionStatus.PaymentCommitted ||
                    status == MN7TransactionStatus.RewardsGranted)
                {
                    result.Add(transaction);
                }
            }
            return result;
        }

        public bool IsCompleted(string transactionId) =>
            GetStatus(transactionId) == MN7TransactionStatus.Completed;

        public bool IsFailed(string transactionId) =>
            GetStatus(transactionId) == MN7TransactionStatus.Failed;

        public bool IsPending(string transactionId) =>
            GetStatus(transactionId) == MN7TransactionStatus.Pending;

        public bool IsRewardOperationCompleted(string transactionId, string operationId)
        {
            MN7TransactionData transaction = GetTransaction(transactionId);
            if (transaction == null || string.IsNullOrWhiteSpace(operationId)) return false;
            EnsureRewardLists(transaction);
            return transaction.completedRewardOperations.Contains(operationId);
        }

        public void MarkRewardOperationCompleted(string transactionId, string operationId, int rewardIndex)
        {
            MN7TransactionData transaction = GetTransaction(transactionId);
            if (transaction == null || string.IsNullOrWhiteSpace(operationId) || rewardIndex < 0) return;
            EnsureRewardLists(transaction);
            if (!transaction.completedRewardOperations.Contains(operationId))
                transaction.completedRewardOperations.Add(operationId);
            if (!transaction.appliedRewardIndexes.Contains(rewardIndex))
                transaction.appliedRewardIndexes.Add(rewardIndex);
            Save();
        }

        public string GetRewardOperationId(string transactionId, int rewardIndex)
        {
            if (string.IsNullOrWhiteSpace(transactionId) || rewardIndex < 0) return string.Empty;
            return $"{transactionId.Trim()}_REWARD_{rewardIndex}";
        }

        public bool RemoveTransaction(string transactionId)
        {
            if (string.IsNullOrWhiteSpace(transactionId) || !transactions.ContainsKey(transactionId)) return false;
            if (!IsCompleted(transactionId)) return false;
            transactions.Remove(transactionId);
            Save();
            return true;
        }

        public int GetTransactionCount() => transactions.Count;

        public int CleanupCompletedTransactions(long maxAgeSeconds)
        {
            if (maxAgeSeconds < 0) return 0;
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            List<string> removeIds = new();

            foreach (MN7TransactionData transaction in transactions.Values)
            {
                if (transaction == null || !IsCompleted(transaction.transactionId)) continue;
                if (currentTime - transaction.timestamp >= maxAgeSeconds)
                    removeIds.Add(transaction.transactionId);
            }

            foreach (string id in removeIds)
                transactions.Remove(id);

            if (removeIds.Count > 0) Save();
            return removeIds.Count;
        }

        internal void ResetAllTransactions()
        {
            transactions.Clear();
            Save();
        }

        private void Save()
        {
            if (saveManager == null)
            {
                Debug.LogError("[MN7 Store] Save Manager is null.");
                return;
            }

            MN7TransactionSaveData saveData = new() { version = CurrentSaveVersion };
            foreach (MN7TransactionData transaction in transactions.Values)
            {
                if (transaction == null) continue;
                EnsureRewardLists(transaction);
                saveData.transactions.Add(transaction);
            }

            saveManager.Save(saveKey, JsonUtility.ToJson(saveData));
        }

        private void Load()
        {
            transactions.Clear();
            if (saveManager == null) return;

            string json = saveManager.Load(saveKey);
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                MN7TransactionSaveData saveData = JsonUtility.FromJson<MN7TransactionSaveData>(json);
                if (saveData == null || saveData.transactions == null) return;

                if (!MN7SaveMigration.IsSupportedVersion(saveData.version))
                {
                    Debug.LogError($"[MN7 Store] Unsupported transaction save version: {saveData.version}");
                    return;
                }

                foreach (MN7TransactionData transaction in saveData.transactions)
                {
                    if (!IsValidTransaction(transaction)) continue;
                    EnsureRewardLists(transaction);
                    transactions[transaction.transactionId] = transaction;
                }

                if (saveData.version < CurrentSaveVersion)
                {
                    MN7SaveMigration.LogMigration(saveData.version, CurrentSaveVersion, "transactions");
                    Save();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[MN7 Store] Failed to load transactions.\n" + exception);
            }
        }

        private bool IsValidTransaction(MN7TransactionData transaction)
        {
            if (transaction == null ||
                string.IsNullOrWhiteSpace(transaction.transactionId) ||
                string.IsNullOrWhiteSpace(transaction.itemId) ||
                string.IsNullOrWhiteSpace(transaction.status))
                return false;

            if (!Enum.TryParse(transaction.status, out MN7TransactionStatus status))
                return false;

            if (transaction.purchaseType != MN7PurchaseType.RewardedAd.ToString())
                return false;

            return status == MN7TransactionStatus.Pending ||
                   status == MN7TransactionStatus.PaymentCommitted ||
                   status == MN7TransactionStatus.RewardsGranted ||
                   status == MN7TransactionStatus.Completed ||
                   status == MN7TransactionStatus.Failed;
        }

        private void EnsureRewardLists(MN7TransactionData transaction)
        {
            if (transaction.appliedRewardIndexes == null)
                transaction.appliedRewardIndexes = new List<int>();
            if (transaction.completedRewardOperations == null)
                transaction.completedRewardOperations = new List<string>();
        }

        private bool CanChangeStatus(string transactionId, MN7TransactionStatus newStatus)
        {
            MN7TransactionStatus currentStatus = GetStatus(transactionId);
            return currentStatus switch
            {
                MN7TransactionStatus.Pending => newStatus == MN7TransactionStatus.PaymentCommitted || newStatus == MN7TransactionStatus.Failed,
                MN7TransactionStatus.PaymentCommitted => newStatus == MN7TransactionStatus.RewardsGranted || newStatus == MN7TransactionStatus.Failed,
                MN7TransactionStatus.RewardsGranted => newStatus == MN7TransactionStatus.Completed,
                _ => false
            };
        }

        private string GenerateTransactionId()
        {
            string transactionId;
            do
            {
                transactionId = $"TX_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
            }
            while (transactions.ContainsKey(transactionId));

            return transactionId;
        }
    }
}
