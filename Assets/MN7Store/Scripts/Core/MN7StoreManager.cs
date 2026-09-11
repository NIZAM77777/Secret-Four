using System;
using UnityEngine;

namespace MN7.Store
{
    public class MN7StoreManager : MonoBehaviour
    {
        public static MN7StoreManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private MN7StoreConfig storeConfig;

        public MN7StoreState State { get; private set; } = MN7StoreState.Uninitialized;
        public bool IsInitialized => State == MN7StoreState.Initialized;
        public bool IsReady => IsInitialized;

        public event Action OnStoreInitialized;
        public event Action OnStoreInitializationFailed;

        public MN7StoreConfig Config => storeConfig;
        public MN7StoreCatalog Catalog { get; private set; }
        public MN7IAPManager IAP { get; private set; }
        public MN7CurrencyManager Currency { get; private set; }
        public MN7PurchaseManager Purchases { get; private set; }
        public MN7InventoryManager Inventory { get; private set; }
        public MN7AdPurchaseProgress AdProgress { get; private set; }
        public MN7RewardManager Rewards { get; private set; }
        public MN7AdManager Ads { get; private set; }
        public MN7TransactionManager Transactions { get; private set; }
        public MN7TransactionRecovery Recovery { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            State = MN7StoreState.Uninitialized;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            if (State == MN7StoreState.Initializing || State == MN7StoreState.Initialized)
                return;

            State = MN7StoreState.Initializing;
            ClearManagers();

            if (storeConfig == null)
            {
                InitializationFailed("Store Config is not assigned.");
                return;
            }

            string effectiveSaveKey = storeConfig.GetEffectiveSaveKey();

            if (string.IsNullOrWhiteSpace(effectiveSaveKey))
            {
                InitializationFailed("Store Config does not produce a valid save key.");
                return;
            }

            if (!MN7StoreValidator.ValidateConfig(storeConfig))
            {
                InitializationFailed("Initialization stopped because the Store Config is invalid.");
                return;
            }

            Catalog = new MN7StoreCatalog(storeConfig);
            MN7SaveManager saveManager = new MN7SaveManager(effectiveSaveKey);

            Currency = new MN7CurrencyManager(storeConfig, saveManager);
            Inventory = new MN7InventoryManager(storeConfig, saveManager, Catalog);
            AdProgress = new MN7AdPurchaseProgress(saveManager, effectiveSaveKey);
            Transactions = new MN7TransactionManager(saveManager, $"{effectiveSaveKey}_TRANSACTIONS");
            Ads = new MN7AdManager();
            Rewards = new MN7RewardManager(Currency, Inventory);

            IAP = new MN7IAPManager(
                storeConfig,
                Inventory,
                Rewards,
                Transactions
            );

            Purchases = new MN7PurchaseManager(
                storeConfig,
                Currency,
                Inventory,
                Ads,
                AdProgress,
                Rewards,
                Transactions
            );

            Recovery = new MN7TransactionRecovery(
                Transactions,
                Purchases,
                storeConfig,
                AdProgress
            );

            Recovery.Recover();
            Transactions.CleanupCompletedTransactions(7L * 24L * 60L * 60L);

            State = MN7StoreState.Initialized;

            Debug.Log($"[MN7 Store] Store initialized. Save namespace: {effectiveSaveKey}");
            OnStoreInitialized?.Invoke();
        }

        public void SetIAPProvider(IMN7IAPProvider provider)
        {
            if (IAP == null)
            {
                Debug.LogError("[MN7 Store] Cannot set IAP provider before store initialization.");
                return;
            }

            IAP.SetProvider(provider);
        }

        public void InitializeIAP(Action<bool> onInitialized = null)
        {
            if (IAP == null)
            {
                onInitialized?.Invoke(false);
                return;
            }

            IAP.Initialize(onInitialized);
        }

        private void InitializationFailed(string message)
        {
            State = MN7StoreState.Failed;
            Debug.LogError($"[MN7 Store] {message}");
            OnStoreInitializationFailed?.Invoke();
        }

        private void ClearManagers()
        {
            Catalog = null;
            IAP = null;
            Currency = null;
            Purchases = null;
            Inventory = null;
            AdProgress = null;
            Rewards = null;
            Ads = null;
            Transactions = null;
            Recovery = null;
        }

#if UNITY_EDITOR
        [ContextMenu("Validate Store Config")]
        private void ValidateStoreConfig() => MN7StoreValidator.ValidateConfig(storeConfig);

        [ContextMenu("Run Runtime Diagnostics")]
        private void RunRuntimeDiagnostics() => MN7StoreDiagnostics.ValidateRuntime();

        [ContextMenu("Debug Print Currency")]
        private void DebugPrintCurrency() => Currency?.DebugPrintBalances();

        [ContextMenu("Debug Print Inventory")]
        private void DebugPrintInventory() => Inventory?.DebugPrintInventory();

        [ContextMenu("Reset Store Data")]
        private void ResetStoreData()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("[MN7 Store] Cannot reset store data before initialization.");
                return;
            }

            Currency?.ResetToDefaults();
            Inventory?.ClearInventory();
            AdProgress?.ResetAll();
            Debug.Log("[MN7 Store] Store data reset.");
        }
#endif
    }
}
