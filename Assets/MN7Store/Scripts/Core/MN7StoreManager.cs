using UnityEngine;

namespace MN7.Store
{
    public class MN7StoreManager : MonoBehaviour
    {
        public static MN7StoreManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField]
        private MN7StoreConfig storeConfig;

        public MN7CurrencyManager Currency { get; private set; }

        public MN7PurchaseManager Purchases { get; private set; }

        public MN7InventoryManager Inventory { get; private set; }

        public MN7AdPurchaseProgress AdProgress { get; private set; }

        public MN7RewardManager Rewards { get; private set; }

        public MN7AdManager Ads { get; private set; }

        //==================================================
        // UNITY
        //==================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        //==================================================
        // INITIALIZATION
        //==================================================

        private void Initialize()
        {
            if (storeConfig == null)
            {
                Debug.LogError(
                    "[MN7 Store] Store Config is not assigned."
                );

                return;
            }

            MN7SaveManager saveManager =
                new MN7SaveManager(
                    storeConfig.saveKey
                );

            Currency =
                new MN7CurrencyManager(
                    storeConfig,
                    saveManager
                );

            Inventory =
                new MN7InventoryManager(
                    storeConfig,
                    saveManager
                );

            AdProgress =
    new MN7AdPurchaseProgress(
        saveManager
    );

            Purchases =
    new MN7PurchaseManager(
        storeConfig,
        Currency,
        Inventory,
        Ads,
        AdProgress,
        Rewards
    );

            Ads =
                new MN7AdManager();

            Debug.Log(
                "[MN7 Store] Store initialized."
            );

            Rewards =
    new MN7RewardManager(
        Currency,
        Inventory
    );
        }

        //==================================================
        // DEBUG
        //==================================================

        [ContextMenu("Debug Print Currency")]
        private void DebugPrintCurrency()
        {
            Currency?.DebugPrintBalances();
        }

        [ContextMenu("Debug Print Inventory")]
        private void DebugPrintInventory()
        {
            Inventory?.DebugPrintInventory();
        }

        [ContextMenu("Reset Store Data")]
        private void ResetStoreData()
        {
            Currency?.ResetToDefaults();
            Inventory?.ClearInventory();
        }
    }
}