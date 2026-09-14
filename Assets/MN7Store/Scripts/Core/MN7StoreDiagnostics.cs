using UnityEngine;

namespace MN7.Store
{
    /// <summary>Lightweight runtime diagnostics. Does not modify store data.</summary>
    public static class MN7StoreDiagnostics
    {
        public static bool ValidateRuntime(bool logSuccess = true)
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store == null) return Fail("MN7StoreManager instance is missing.");
            if (!store.IsReady) return Fail($"Store is not ready. Current state: {store.State}");
            if (store.Config == null) return Fail("Store Config is missing.");
            if (store.Catalog == null) return Fail("Store Catalog is missing.");
            if (store.Inventory == null) return Fail("Inventory Manager is missing.");
            if (store.Rewards == null) return Fail("Reward Manager is missing.");
            if (store.Purchases == null) return Fail("Purchase Manager is missing.");
            if (store.Transactions == null) return Fail("Transaction Manager is missing.");
            if (store.Recovery == null) return Fail("Transaction Recovery is missing.");

            MN7StoreFeatureConfig features = store.Features ?? new MN7StoreFeatureConfig();
            if (features.coins && store.Currency == null) return Fail("Coins are enabled but Currency Manager is missing.");
            if (!features.coins && store.Currency != null) return Fail("Coins are disabled but Currency Manager was initialized.");
            if (features.adRewards && store.Ads == null) return Fail("Ad Rewards are enabled but Ad Manager is missing.");
            if (!features.adRewards && store.Ads != null) return Fail("Ad Rewards are disabled but Ad Manager was initialized.");
            if (features.adRewards && store.AdProgress == null) return Fail("Ad Rewards are enabled but Ad Progress is missing.");
            if (!features.adRewards && store.AdProgress != null) return Fail("Ad Rewards are disabled but Ad Progress was initialized.");
            if (features.iap && store.IAP == null) return Fail("IAP is enabled but IAP Manager is missing.");
            if (!features.iap && store.IAP != null) return Fail("IAP is disabled but IAP Manager was initialized.");

            if (logSuccess)
            {
                Debug.Log($"[MN7 Store] Runtime diagnostics passed. Items: {store.Catalog.GetItems().Count}, Transactions: {store.Transactions.GetTransactionCount()}");
            }
            return true;
        }

        private static bool Fail(string message)
        {
            Debug.LogError($"[MN7 Store] Runtime diagnostics failed: {message}");
            return false;
        }
    }
}
