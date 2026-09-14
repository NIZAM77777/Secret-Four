using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7CurrencyManager
    {
        [Serializable]
        private class CurrencyBalance
        {
            public string currencyId;
            public int amount;
        }

        [Serializable]
        private class CurrencySaveData
        {
            public List<CurrencyBalance> balances = new();
            public List<string> completedRewardOperations = new();
            public List<string> completedPaymentOperations = new();
        }

        [Serializable]
        private class CurrencySaveEnvelope
        {
            public int version = MN7StoreSaveVersion.CurrentVersion;
            public CurrencySaveData data;
        }

        private const string CurrencySuffix = "_CURRENCY";
        private readonly Dictionary<string, int> balances = new();
        private readonly HashSet<string> completedRewardOperations = new();
        private readonly HashSet<string> completedPaymentOperations = new();
        private readonly MN7StoreConfig config;
        private readonly MN7SaveManager saveManager;
        private readonly string saveKey;

        public event Action<string, int> OnCurrencyChanged;

        public MN7CurrencyManager(MN7StoreConfig config, MN7SaveManager saveManager)
        {
            this.config = config;
            this.saveManager = saveManager;
            saveKey = $"{config.GetEffectiveSaveKey()}{CurrencySuffix}";
            Initialize();
        }

        private void Initialize()
        {
            balances.Clear();
            completedRewardOperations.Clear();
            completedPaymentOperations.Clear();

            if (config?.currencies == null)
                return;

            foreach (MN7CurrencyData currency in config.currencies)
            {
                if (currency == null || string.IsNullOrWhiteSpace(currency.currencyId))
                    continue;

                balances[currency.currencyId.Trim()] = Mathf.Max(0, currency.startingAmount);
            }

            Load();
        }

        public int GetBalance(string currencyId)
        {
            return !string.IsNullOrWhiteSpace(currencyId) &&
                   balances.TryGetValue(currencyId.Trim(), out int amount)
                ? amount
                : 0;
        }

        public bool HasCurrency(string currencyId)
        {
            return !string.IsNullOrWhiteSpace(currencyId) &&
                   balances.ContainsKey(currencyId.Trim());
        }

        public bool CanAdd(string currencyId, int amount)
        {
            currencyId = currencyId?.Trim();

            if (string.IsNullOrWhiteSpace(currencyId) ||
                amount <= 0 ||
                !balances.ContainsKey(currencyId))
            {
                return false;
            }

            return (long)balances[currencyId] + amount <= int.MaxValue;
        }

        public bool IsRewardOperationCompleted(string operationId)
        {
            return !string.IsNullOrWhiteSpace(operationId) &&
                   completedRewardOperations.Contains(operationId);
        }

        public bool IsPaymentOperationCompleted(string operationId)
        {
            return !string.IsNullOrWhiteSpace(operationId) &&
                   completedPaymentOperations.Contains(operationId);
        }

        public bool Add(string currencyId, int amount, string operationId = null)
        {
            currencyId = currencyId?.Trim();

            if (!string.IsNullOrWhiteSpace(operationId) &&
                completedRewardOperations.Contains(operationId))
            {
                return true;
            }

            if (!CanAdd(currencyId, amount))
                return false;

            balances[currencyId] += amount;

            if (!string.IsNullOrWhiteSpace(operationId))
                completedRewardOperations.Add(operationId);

            Save();
            OnCurrencyChanged?.Invoke(currencyId, balances[currencyId]);
            return true;
        }

        public bool CanSpend(string currencyId, int amount)
        {
            return amount >= 0 && GetBalance(currencyId) >= amount;
        }

        /// <summary>
        /// Spends currency exactly once when an operation ID is supplied.
        /// The payment operation marker and new balance are persisted together.
        /// This lets transaction recovery distinguish a completed debit from a
        /// purchase that never reached the debit step.
        /// </summary>
        public bool Spend(string currencyId, int amount, string operationId = null)
        {
            currencyId = currencyId?.Trim();

            if (!string.IsNullOrWhiteSpace(operationId) &&
                completedPaymentOperations.Contains(operationId))
            {
                return true;
            }

            if (amount < 0 ||
                string.IsNullOrWhiteSpace(currencyId) ||
                !balances.ContainsKey(currencyId) ||
                !CanSpend(currencyId, amount))
            {
                return false;
            }

            balances[currencyId] -= amount;

            if (!string.IsNullOrWhiteSpace(operationId))
                completedPaymentOperations.Add(operationId);

            Save();
            OnCurrencyChanged?.Invoke(currencyId, balances[currencyId]);
            return true;
        }

        internal void SetBalance(string currencyId, int amount)
        {
            currencyId = currencyId?.Trim();

            if (string.IsNullOrWhiteSpace(currencyId) || !balances.ContainsKey(currencyId))
                return;

            balances[currencyId] = Mathf.Max(0, amount);
            Save();
            OnCurrencyChanged?.Invoke(currencyId, balances[currencyId]);
        }

        public void Save()
        {
            CurrencySaveData data = BuildSaveData();
            CurrencySaveEnvelope envelope = new()
            {
                version = MN7StoreSaveVersion.CurrentVersion,
                data = data
            };

            saveManager?.Save(saveKey, JsonUtility.ToJson(envelope));
        }

        private CurrencySaveData BuildSaveData()
        {
            CurrencySaveData data = new();

            foreach (var pair in balances)
            {
                data.balances.Add(
                    new CurrencyBalance
                    {
                        currencyId = pair.Key,
                        amount = pair.Value
                    }
                );
            }

            foreach (string operationId in completedRewardOperations)
            {
                if (!string.IsNullOrWhiteSpace(operationId))
                    data.completedRewardOperations.Add(operationId);
            }

            foreach (string operationId in completedPaymentOperations)
            {
                if (!string.IsNullOrWhiteSpace(operationId))
                    data.completedPaymentOperations.Add(operationId);
            }

            return data;
        }

        private void Load()
        {
            string json = saveManager?.Load(saveKey);

            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                if (TryLoadCurrentEnvelope(json))
                    return;

                // Version 0/legacy format: the payload itself was CurrencySaveData.
                CurrencySaveData legacyData = JsonUtility.FromJson<CurrencySaveData>(json);

                if (legacyData == null || legacyData.balances == null)
                    return;

                MN7SaveMigration.LogMigration(0, MN7StoreSaveVersion.CurrentVersion, "currency");
                ApplySaveData(legacyData);
                Save();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MN7 Store] Failed to load currency data.\n{exception}");
            }
        }

        private bool TryLoadCurrentEnvelope(string json)
        {
            CurrencySaveEnvelope envelope =
                JsonUtility.FromJson<CurrencySaveEnvelope>(json);

            if (envelope == null || envelope.data == null)
                return false;

            if (!MN7SaveMigration.IsSupportedVersion(envelope.version))
            {
                Debug.LogError(
                    $"[MN7 Store] Unsupported currency save version: {envelope.version}"
                );
                return true;
            }

            ApplySaveData(envelope.data);

            if (envelope.version < MN7StoreSaveVersion.CurrentVersion)
            {
                MN7SaveMigration.LogMigration(
                    envelope.version,
                    MN7StoreSaveVersion.CurrentVersion,
                    "currency"
                );
                Save();
            }

            return true;
        }

        private void ApplySaveData(CurrencySaveData saveData)
        {
            if (saveData?.balances != null)
            {
                foreach (CurrencyBalance balance in saveData.balances)
                {
                    if (balance == null || string.IsNullOrWhiteSpace(balance.currencyId))
                        continue;

                    string id = balance.currencyId.Trim();

                    if (balances.ContainsKey(id))
                        balances[id] = Mathf.Max(0, balance.amount);
                }
            }

            if (saveData?.completedRewardOperations != null)
            {
                foreach (string operationId in saveData.completedRewardOperations)
                {
                    if (!string.IsNullOrWhiteSpace(operationId))
                        completedRewardOperations.Add(operationId);
                }
            }

            if (saveData?.completedPaymentOperations != null)
            {
                foreach (string operationId in saveData.completedPaymentOperations)
                {
                    if (!string.IsNullOrWhiteSpace(operationId))
                        completedPaymentOperations.Add(operationId);
                }
            }
        }

        internal void ResetToDefaults()
        {
            if (config?.currencies == null)
                return;

            foreach (MN7CurrencyData currency in config.currencies)
            {
                if (currency == null || string.IsNullOrWhiteSpace(currency.currencyId))
                    continue;

                string id = currency.currencyId.Trim();
                balances[id] = Mathf.Max(0, currency.startingAmount);
                OnCurrencyChanged?.Invoke(id, balances[id]);
            }

            completedRewardOperations.Clear();
            completedPaymentOperations.Clear();
            Save();
        }

        internal void DebugPrintBalances()
        {
            foreach (var pair in balances)
                Debug.Log($"[MN7 Store] {pair.Key}: {pair.Value}");
        }
    }
}
