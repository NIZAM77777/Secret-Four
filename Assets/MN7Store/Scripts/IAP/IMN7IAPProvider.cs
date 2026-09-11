using System;

namespace MN7.Store
{
    /// <summary>
    /// Platform/store-specific IAP adapter used by MN7 Store.
    /// Providers surface new/pending purchases and confirmed entitlements.
    /// </summary>
    public interface IMN7IAPProvider
    {
        bool IsInitialized { get; }

        void Initialize(Action<bool> onInitialized = null);

        void Purchase(string productId, Action<MN7IAPResult> onResult);

        event Action<string, string, Action<bool>> PurchasePending;

        /// <summary>
        /// Raised for an already-confirmed store entitlement, primarily used
        /// to restore permanent/non-consumable items after reinstall.
        /// </summary>
        event Action<string, string> PurchaseRestored;
    }
}
