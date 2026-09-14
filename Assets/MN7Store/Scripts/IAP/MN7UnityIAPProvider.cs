using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;

namespace MN7.Store
{
    public sealed class MN7UnityIAPProvider : MonoBehaviour, IMN7IAPProvider
    {
        private StoreController storeController;
        private bool initializing;
        private bool purchaseFlowInitialized;
        private readonly Dictionary<string, Queue<Action<MN7IAPResult>>> pendingCallbacks = new();
        private readonly HashSet<string> productsFetched = new();

        public bool IsInitialized => storeController != null && purchaseFlowInitialized;
        public event Action<string, string, Action<bool>> PurchasePending;
        public event Action<string, string> PurchaseRestored;

        private void Start()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store == null)
            {
                Debug.LogWarning("[MN7 Store] MN7UnityIAPProvider could not find MN7StoreManager.");
                return;
            }

            store.SetIAPProvider(this);
            store.InitializeIAP();
        }

        public void Initialize(Action<bool> onInitialized = null)
        {
            if (IsInitialized) { onInitialized?.Invoke(true); return; }
            if (initializing) { StartCoroutine(WaitForInitialization(onInitialized)); return; }
            initializing = true;
            _ = InitializeAsync(onInitialized);
        }

        public void Purchase(string productId, Action<MN7IAPResult> onResult)
        {
            if (string.IsNullOrWhiteSpace(productId)) { onResult?.Invoke(MN7IAPResult.ProductNotFound); return; }
            if (!IsInitialized) { onResult?.Invoke(MN7IAPResult.NotInitialized); return; }
            Product product = storeController.GetProductById(productId.Trim());
            if (product == null || !productsFetched.Contains(product.definition.id))
            {
                onResult?.Invoke(MN7IAPResult.ProductNotFound);
                return;
            }
            EnqueueCallback(product.definition.id, onResult);
            // Unity IAP 5.4.x exposes PurchaseProduct(string) for a single product.
            // StoreController.Purchase(Product) is not a valid IAP 5.4.x overload because
            // that API expects a Cart, not a Product.
            storeController.PurchaseProduct(product.definition.id);
        }

        private async Task InitializeAsync(Action<bool> onInitialized)
        {
            bool success = false;
            try
            {
                storeController = UnityIAPServices.StoreController();
                storeController.OnStoreDisconnected += OnStoreDisconnected;
                storeController.OnProductsFetched += OnProductsFetched;
                storeController.OnProductsFetchFailed += OnProductsFetchFailed;
                storeController.OnPurchasesFetched += OnPurchasesFetched;
                storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
                storeController.OnPurchasePending += OnPurchasePending;
                storeController.OnPurchaseFailed += OnPurchaseFailed;

                await storeController.Connect();
                List<ProductDefinition> definitions = BuildProductDefinitions();
                if (definitions.Count == 0)
                    Debug.LogWarning("[MN7 Store] No IAP products are configured.");
                else
                {
                    storeController.FetchProducts(definitions);
                    success = true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MN7 Store] Unity IAP initialization failed: {exception}");
            }
            finally
            {
                initializing = false;
                purchaseFlowInitialized = success;
                onInitialized?.Invoke(success);
            }
        }

        private List<ProductDefinition> BuildProductDefinitions()
        {
            List<ProductDefinition> definitions = new();
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store?.Config?.items == null) return definitions;
            HashSet<string> seen = new();
            foreach (MN7ItemData item in store.Config.items)
            {
                if (item == null || item.purchaseType != MN7PurchaseType.IAP) continue;
                string productId = item.iapProductId?.Trim();
                if (string.IsNullOrWhiteSpace(productId) || !seen.Add(productId)) continue;
                ProductType type = item.ownershipType == MN7ItemOwnershipType.Permanent
                    ? ProductType.NonConsumable
                    : ProductType.Consumable;
                definitions.Add(new ProductDefinition(productId, type));
            }
            return definitions;
        }

        private void OnProductsFetched(List<Product> products)
        {
            productsFetched.Clear();
            foreach (Product product in products)
            {
                if (product?.definition == null) continue;
                productsFetched.Add(product.definition.id);
            }
            storeController?.ProcessPendingOrdersOnPurchasesFetched(false);
            storeController?.FetchPurchases();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            purchaseFlowInitialized = false;
            Debug.LogError($"[MN7 Store] Unity IAP product fetch failed: {failure}");
        }

        private void OnPurchasesFetched(Orders orders)
        {
            if (orders == null) return;
            foreach (PendingOrder order in orders.PendingOrders)
                ProcessPendingOrder(order);

            foreach (ConfirmedOrder order in orders.ConfirmedOrders)
            {
                if (order == null) continue;
                string transactionId = order.Info?.TransactionID;
                if (string.IsNullOrWhiteSpace(transactionId)) continue;
                foreach (CartItem cartItem in order.CartOrdered.Items())
                {
                    string productId = cartItem?.Product?.definition?.id;
                    if (!string.IsNullOrWhiteSpace(productId))
                        PurchaseRestored?.Invoke(productId, transactionId);
                }
            }
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            Debug.LogWarning($"[MN7 Store] Unity IAP purchase fetch failed: {failure}");
        }

        private void OnPurchasePending(PendingOrder order) => ProcessPendingOrder(order);

        private void OnPurchaseFailed(FailedOrder failedOrder)
        {
            if (failedOrder == null) return;
            foreach (CartItem cartItem in failedOrder.CartOrdered.Items())
            {
                string productId = cartItem?.Product?.definition?.id;
                if (string.IsNullOrWhiteSpace(productId)) continue;
                Action<MN7IAPResult> callback = DequeueCallback(productId);
                callback?.Invoke(failedOrder.FailureReason == PurchaseFailureReason.UserCancelled
                    ? MN7IAPResult.Cancelled
                    : MN7IAPResult.Failed);
                return;
            }
        }

        private void ProcessPendingOrder(PendingOrder order)
        {
            if (order == null) return;
            string transactionId = order.Info?.TransactionID;
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                Debug.LogError("[MN7 Store] Pending IAP order has no transaction ID. It will remain unconfirmed.");
                return;
            }

            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                string productId = cartItem?.Product?.definition?.id;
                if (string.IsNullOrWhiteSpace(productId)) continue;
                Action<bool> acknowledge = confirmed =>
                {
                    if (!confirmed) return;
                    storeController?.ConfirmPurchase(order);
                    Action<MN7IAPResult> callback = DequeueCallback(productId);
                    callback?.Invoke(MN7IAPResult.Success);
                };
                PurchasePending?.Invoke(productId, transactionId, acknowledge);
            }
        }

        private void EnqueueCallback(string productId, Action<MN7IAPResult> callback)
        {
            if (callback == null) return;
            if (!pendingCallbacks.TryGetValue(productId, out Queue<Action<MN7IAPResult>> queue))
            {
                queue = new Queue<Action<MN7IAPResult>>();
                pendingCallbacks[productId] = queue;
            }
            queue.Enqueue(callback);
        }

        private Action<MN7IAPResult> DequeueCallback(string productId)
        {
            if (!pendingCallbacks.TryGetValue(productId, out Queue<Action<MN7IAPResult>> queue) || queue.Count == 0)
                return null;
            Action<MN7IAPResult> callback = queue.Dequeue();
            if (queue.Count == 0) pendingCallbacks.Remove(productId);
            return callback;
        }

        private System.Collections.IEnumerator WaitForInitialization(Action<bool> callback)
        {
            float timeout = 30f;
            while (initializing && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
            callback?.Invoke(IsInitialized);
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            purchaseFlowInitialized = false;
            Debug.LogWarning($"[MN7 Store] Unity IAP disconnected: {failure}");
        }

        private void OnDestroy()
        {
            if (storeController == null) return;
            storeController.OnStoreDisconnected -= OnStoreDisconnected;
            storeController.OnProductsFetched -= OnProductsFetched;
            storeController.OnProductsFetchFailed -= OnProductsFetchFailed;
            storeController.OnPurchasesFetched -= OnPurchasesFetched;
            storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            storeController.OnPurchasePending -= OnPurchasePending;
            storeController.OnPurchaseFailed -= OnPurchaseFailed;
        }
    }
}
