using TMPro;
using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Reusable currency display. The prefab owns the visual layout; this component
    /// binds the selected currency and automatically hides itself when the store's
    /// Coins feature is disabled.
    /// </summary>
    public class MN7CurrencyUI : MonoBehaviour
    {
        [Header("Currency")]
        [SerializeField] private string currencyId = "soft_currency";

        [Header("UI")]
        [SerializeField] private TMP_Text amountText;

        private MN7StoreManager subscribedStore;

        private void OnEnable() => TryBindToStore();

        private void OnDisable() => UnbindFromStore();

        private void Update()
        {
            if (subscribedStore == null)
                TryBindToStore();
        }

        private void TryBindToStore()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store == null) return;

            if (subscribedStore == store)
            {
                ApplyFeatureState();
                return;
            }

            UnbindFromStore();
            subscribedStore = store;
            subscribedStore.OnStoreInitialized -= HandleStoreInitialized;
            subscribedStore.OnStoreInitialized += HandleStoreInitialized;

            if (subscribedStore.Currency != null)
            {
                subscribedStore.Currency.OnCurrencyChanged -= HandleCurrencyChanged;
                subscribedStore.Currency.OnCurrencyChanged += HandleCurrencyChanged;
            }

            ApplyFeatureState();
        }

        private void UnbindFromStore()
        {
            if (subscribedStore == null) return;

            subscribedStore.OnStoreInitialized -= HandleStoreInitialized;
            if (subscribedStore.Currency != null)
                subscribedStore.Currency.OnCurrencyChanged -= HandleCurrencyChanged;

            subscribedStore = null;
        }

        private void HandleStoreInitialized()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store == null) return;

            if (store.Currency != null)
            {
                store.Currency.OnCurrencyChanged -= HandleCurrencyChanged;
                store.Currency.OnCurrencyChanged += HandleCurrencyChanged;
            }

            ApplyFeatureState();
        }

        private void ApplyFeatureState()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            bool enabled = store != null && store.Features != null && store.Features.coins && store.Currency != null;

            if (gameObject.activeSelf != enabled)
                gameObject.SetActive(enabled);

            if (enabled)
                Refresh();
        }

        private void Refresh()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store?.Currency == null) return;
            UpdateText(store.Currency.GetBalance(currencyId));
        }

        private void HandleCurrencyChanged(string changedCurrencyId, int newBalance)
        {
            if (!string.Equals(changedCurrencyId, currencyId, System.StringComparison.OrdinalIgnoreCase))
                return;
            UpdateText(newBalance);
        }

        private void UpdateText(int amount)
        {
            if (amountText != null)
                amountText.text = amount.ToString();
        }
    }
}