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
        }

        private readonly Dictionary<string, int> balances = new();

        private readonly MN7StoreConfig config;
        private readonly MN7SaveManager saveManager;

        public event Action<string, int> OnCurrencyChanged;

        public MN7CurrencyManager(
            MN7StoreConfig config,
            MN7SaveManager saveManager)
        {
            this.config = config;
            this.saveManager = saveManager;

            Initialize();
        }

        //==================================================
        // INITIALIZATION
        //==================================================

        private void Initialize()
        {
            balances.Clear();

            foreach (MN7CurrencyData currency in config.currencies)
            {
                if (currency == null)
                    continue;

                if (string.IsNullOrWhiteSpace(currency.currencyId))
                    continue;

                balances[currency.currencyId] = currency.startingAmount;
            }

            Load();
        }

        //==================================================
        // BALANCE
        //==================================================

        public int GetBalance(string currencyId)
        {
            if (string.IsNullOrWhiteSpace(currencyId))
                return 0;

            if (!balances.TryGetValue(currencyId, out int amount))
                return 0;

            return amount;
        }

        //==================================================
        // ADD
        //==================================================

        public void Add(string currencyId, int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning(
                    $"[MN7 Store] Cannot add negative currency amount: {amount}"
                );

                return;
            }

            if (!balances.ContainsKey(currencyId))
            {
                Debug.LogWarning(
                    $"[MN7 Store] Unknown currency: {currencyId}"
                );

                return;
            }

            balances[currencyId] += amount;

            Save();

            OnCurrencyChanged?.Invoke(
                currencyId,
                balances[currencyId]
            );
        }

        //==================================================
        // SPEND
        //==================================================

        public bool CanSpend(string currencyId, int amount)
        {
            if (amount < 0)
                return false;

            return GetBalance(currencyId) >= amount;
        }

        public bool Spend(string currencyId, int amount)
        {
            if (amount < 0)
            {
                Debug.LogWarning(
                    $"[MN7 Store] Cannot spend negative amount: {amount}"
                );

                return false;
            }

            if (!balances.ContainsKey(currencyId))
            {
                Debug.LogWarning(
                    $"[MN7 Store] Unknown currency: {currencyId}"
                );

                return false;
            }

            if (!CanSpend(currencyId, amount))
                return false;

            balances[currencyId] -= amount;

            Save();

            OnCurrencyChanged?.Invoke(
                currencyId,
                balances[currencyId]
            );

            return true;
        }

        //==================================================
        // SET
        //==================================================

        public void SetBalance(string currencyId, int amount)
        {
            if (amount < 0)
                amount = 0;

            if (!balances.ContainsKey(currencyId))
            {
                Debug.LogWarning(
                    $"[MN7 Store] Unknown currency: {currencyId}"
                );

                return;
            }

            balances[currencyId] = amount;

            Save();

            OnCurrencyChanged?.Invoke(
                currencyId,
                balances[currencyId]
            );
        }

        //==================================================
        // SAVE
        //==================================================

        public void Save()
        {
            CurrencySaveData saveData = new();

            foreach (var pair in balances)
            {
                saveData.balances.Add(
                    new CurrencyBalance
                    {
                        currencyId = pair.Key,
                        amount = pair.Value
                    }
                );
            }

            string json = JsonUtility.ToJson(saveData);

            saveManager.Save(json);
        }

        //==================================================
        // LOAD
        //==================================================

        private void Load()
        {
            string json = saveManager.Load();

            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                CurrencySaveData saveData =
                    JsonUtility.FromJson<CurrencySaveData>(json);

                if (saveData == null || saveData.balances == null)
                    return;

                foreach (CurrencyBalance balance in saveData.balances)
                {
                    if (string.IsNullOrWhiteSpace(balance.currencyId))
                        continue;

                    if (!balances.ContainsKey(balance.currencyId))
                        continue;

                    balances[balance.currencyId] =
                        Mathf.Max(0, balance.amount);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[MN7 Store] Failed to load currency data.\n{exception}"
                );
            }
        }

        //==================================================
        // RESET
        //==================================================

        public void ResetToDefaults()
        {
            foreach (MN7CurrencyData currency in config.currencies)
            {
                if (currency == null)
                    continue;

                if (string.IsNullOrWhiteSpace(currency.currencyId))
                    continue;

                balances[currency.currencyId] =
                    Mathf.Max(0, currency.startingAmount);

                OnCurrencyChanged?.Invoke(
                    currency.currencyId,
                    balances[currency.currencyId]
                );
            }

            Save();
        }

        //==================================================
        // DEBUG
        //==================================================

        public void DebugPrintBalances()
        {
            foreach (var pair in balances)
            {
                Debug.Log(
                    $"[MN7 Store] {pair.Key}: {pair.Value}"
                );
            }
        }
    }
}