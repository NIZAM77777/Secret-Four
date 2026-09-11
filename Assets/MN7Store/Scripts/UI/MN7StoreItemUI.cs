using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MN7.Store
{
    public class MN7StoreItemUI : MonoBehaviour
    {
        [Header("Item")]
        [SerializeField]
        private MN7ItemData itemData;

        [Header("UI")]
        [SerializeField]
        private Image icon;

        [SerializeField]
        private TMP_Text nameText;

        [SerializeField]
        private TMP_Text descriptionText;

        [SerializeField]
        private TMP_Text priceText;

        [SerializeField]
        private TMP_Text statusText;

        [Header("Buttons")]
        [SerializeField]
        private Button buyButton;

        [SerializeField]
        private Button watchAdButton;

        //==================================================
        // UNITY
        //==================================================

        private void OnEnable()
        {
            MN7StoreManager store = MN7StoreManager.Instance;

            if (store == null)
                return;

            if (store.Purchases != null)
            {
                store.Purchases.OnPurchaseSuccessful += HandlePurchaseSuccessful;
                store.Purchases.OnPurchaseFailed += HandlePurchaseFailed;
            }

            if (store.Inventory != null)
            {
                store.Inventory.OnItemAdded += HandleItemAdded;
                store.Inventory.OnItemRemoved += HandleItemRemoved;
            }

            Refresh();
        }

        private void OnDisable()
        {
            MN7StoreManager store = MN7StoreManager.Instance;

            if (store == null)
                return;

            if (store.Purchases != null)
            {
                store.Purchases.OnPurchaseSuccessful -= HandlePurchaseSuccessful;
                store.Purchases.OnPurchaseFailed -= HandlePurchaseFailed;
            }

            if (store.Inventory != null)
            {
                store.Inventory.OnItemAdded -= HandleItemAdded;
                store.Inventory.OnItemRemoved -= HandleItemRemoved;
            }
        }

        private void HandleItemAdded(string itemId)
        {
            if (itemData == null || itemData.itemId != itemId)
                return;

            Refresh();
        }

        private void HandleItemRemoved(string itemId)
        {
            if (itemData == null || itemData.itemId != itemId)
                return;

            Refresh();
        }

        //==================================================
        // SET ITEM
        //==================================================

        public void SetItem(MN7ItemData newItem)
        {
            itemData = newItem;
            Refresh();
        }

        //==================================================
        // REFRESH
        //==================================================

        public void Refresh()
        {
            if (itemData == null)
                return;

            if (icon != null)
                icon.sprite = itemData.icon;

            if (nameText != null)
                nameText.text = itemData.displayName;

            if (descriptionText != null)
                descriptionText.text = itemData.description;

            UpdatePurchaseUI();
        }

        //==================================================
        // PURCHASE UI
        //==================================================

        private void UpdatePurchaseUI()
        {
            MN7StoreManager store = MN7StoreManager.Instance;

            if (store == null || store.Inventory == null)
                return;

            bool owned = store.Inventory.IsOwned(itemData.itemId);

            if (buyButton != null)
                buyButton.gameObject.SetActive(false);

            if (watchAdButton != null)
                watchAdButton.gameObject.SetActive(false);

            if (priceText != null)
                priceText.text = string.Empty;

            if (statusText != null)
                statusText.text = string.Empty;

            if (owned)
            {
                if (statusText != null)
                    statusText.text = "OWNED";

                return;
            }

            switch (itemData.purchaseType)
            {
                case MN7PurchaseType.Free:
                    if (buyButton != null)
                        buyButton.gameObject.SetActive(true);

                    if (priceText != null)
                        priceText.text = "FREE";
                    break;

                case MN7PurchaseType.Currency:
                    if (buyButton != null)
                        buyButton.gameObject.SetActive(true);

                    if (priceText != null)
                        priceText.text = itemData.currencyCost.ToString();
                    break;

                case MN7PurchaseType.RewardedAd:
                    if (watchAdButton != null)
                        watchAdButton.gameObject.SetActive(true);

                    UpdateAdText();
                    break;

                case MN7PurchaseType.CurrencyOrAd:
                    if (buyButton != null)
                        buyButton.gameObject.SetActive(true);

                    if (watchAdButton != null)
                        watchAdButton.gameObject.SetActive(true);

                    if (priceText != null)
                        priceText.text = itemData.currencyCost.ToString();

                    UpdateAdText();
                    break;

                case MN7PurchaseType.IAP:
                    if (buyButton != null)
                        buyButton.gameObject.SetActive(true);

                    if (priceText != null)
                        priceText.text = "BUY";
                    break;
            }
        }

        //==================================================
        // AD TEXT
        //==================================================

        private void UpdateAdText()
        {
            if (statusText == null || itemData == null)
                return;

            MN7StoreManager store = MN7StoreManager.Instance;

            if (store == null || store.AdProgress == null)
                return;

            int progress = store.AdProgress.GetProgress(itemData.itemId);
            statusText.text = $"ADS {progress}/{itemData.requiredAds}";
        }

        //==================================================
        // BUY
        //==================================================

        public void Buy()
        {
            if (itemData == null)
                return;

            MN7StoreManager store = MN7StoreManager.Instance;

            if (store == null || store.Purchases == null)
                return;

            switch (itemData.purchaseType)
            {
                case MN7PurchaseType.Free:
                    store.Purchases.PurchaseFree(itemData.itemId);
                    break;

                case MN7PurchaseType.Currency:
                case MN7PurchaseType.CurrencyOrAd:
                    store.Purchases.PurchaseWithCurrency(itemData.itemId);
                    break;

                case MN7PurchaseType.IAP:
                    store.Purchases.PurchaseWithIAP(itemData.itemId);
                    break;
            }
        }

        //==================================================
        // WATCH AD
        //==================================================

        public void WatchAd()
        {
            if (itemData == null)
                return;

            MN7StoreManager store = MN7StoreManager.Instance;

            if (store == null || store.Purchases == null)
                return;

            store.Purchases.PurchaseWithRewardedAd(itemData.itemId, HandleAdResult);
        }

        //==================================================
        // AD RESULT
        //==================================================

        private void HandleAdResult(MN7AdResult result)
        {
            if (result == MN7AdResult.NotReady)
            {
                if (statusText != null)
                    statusText.text = "AD NOT READY";
                return;
            }

            if (result == MN7AdResult.Failed)
            {
                if (statusText != null)
                    statusText.text = "AD FAILED";
                return;
            }

            if (result == MN7AdResult.Skipped)
            {
                if (statusText != null)
                    statusText.text = "AD SKIPPED";
                return;
            }

            Refresh();
        }

        //==================================================
        // PURCHASE EVENTS
        //==================================================

        private void HandlePurchaseSuccessful(MN7ItemData purchasedItem)
        {
            if (purchasedItem == null || itemData == null ||
                purchasedItem.itemId != itemData.itemId)
            {
                return;
            }

            Refresh();
        }

        private void HandlePurchaseFailed(MN7ItemData failedItem)
        {
            if (failedItem == null || itemData == null ||
                failedItem.itemId != itemData.itemId)
            {
                return;
            }

            Refresh();
        }
    }
}
