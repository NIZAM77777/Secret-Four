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
            if (MN7StoreManager.Instance == null)
                return;

            if (MN7StoreManager.Instance.Purchases != null)
            {
                MN7StoreManager.Instance.Purchases
                    .OnPurchaseSuccessful
                    += HandlePurchaseSuccessful;

                MN7StoreManager.Instance.Purchases
                    .OnPurchaseFailed
                    += HandlePurchaseFailed;
            }

            if (MN7StoreManager.Instance.Inventory != null)
            {
                MN7StoreManager.Instance.Inventory
                    .OnItemAdded
                    += HandleItemAdded;

                MN7StoreManager.Instance.Inventory
                    .OnItemRemoved
                    += HandleItemRemoved;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (MN7StoreManager.Instance == null)
                return;

            if (MN7StoreManager.Instance.Purchases != null)
            {
                MN7StoreManager.Instance.Purchases
                    .OnPurchaseSuccessful
                    -= HandlePurchaseSuccessful;

                MN7StoreManager.Instance.Purchases
                    .OnPurchaseFailed
                    -= HandlePurchaseFailed;
            }

            if (MN7StoreManager.Instance.Inventory != null)
            {
                MN7StoreManager.Instance.Inventory
                    .OnItemAdded
                    -= HandleItemAdded;

                MN7StoreManager.Instance.Inventory
                    .OnItemRemoved
                    -= HandleItemRemoved;
            }
        }

        private void HandleItemAdded(
    string itemId)
        {
            if (itemData == null)
                return;

            if (itemData.itemId != itemId)
                return;

            Refresh();
        }

        private void HandleItemRemoved(
            string itemId)
        {
            if (itemData == null)
                return;

            if (itemData.itemId != itemId)
                return;

            Refresh();
        }

        //==================================================
        // SET ITEM
        //==================================================

        public void SetItem(
            MN7ItemData newItem)
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
                nameText.text =
                    itemData.displayName;

            if (descriptionText != null)
                descriptionText.text =
                    itemData.description;

            UpdatePurchaseUI();
        }

        //==================================================
        // PURCHASE UI
        //==================================================

        private void UpdatePurchaseUI()
        {
            bool owned =
                MN7StoreManager.Instance.Inventory
                    .IsOwned(itemData.itemId);

            if (buyButton != null)
                buyButton.gameObject.SetActive(false);

            if (watchAdButton != null)
                watchAdButton.gameObject.SetActive(false);

            if (statusText != null)
                statusText.text = "";

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
                    {
                        buyButton.gameObject
                            .SetActive(true);
                    }

                    if (priceText != null)
                        priceText.text = "FREE";

                    break;

                case MN7PurchaseType.Currency:

                    if (buyButton != null)
                    {
                        buyButton.gameObject
                            .SetActive(true);
                    }

                    if (priceText != null)
                    {
                        priceText.text =
                            itemData.currencyCost
                            .ToString();
                    }

                    break;

                case MN7PurchaseType.RewardedAd:

                    if (watchAdButton != null)
                    {
                        watchAdButton.gameObject
                            .SetActive(true);
                    }

                    UpdateAdText();

                    break;

                case MN7PurchaseType.CurrencyOrAd:

                    if (buyButton != null)
                    {
                        buyButton.gameObject
                            .SetActive(true);
                    }

                    if (watchAdButton != null)
                    {
                        watchAdButton.gameObject
                            .SetActive(true);
                    }

                    if (priceText != null)
                    {
                        priceText.text =
                            itemData.currencyCost
                            .ToString();
                    }

                    UpdateAdText();

                    break;

                case MN7PurchaseType.IAP:

                    if (buyButton != null)
                    {
                        buyButton.gameObject
                            .SetActive(true);
                    }

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
            if (statusText == null)
                return;

            int progress =
                MN7StoreManager.Instance
                    .AdProgress
                    .GetProgress(itemData.itemId);

            statusText.text =
                $"ADS {progress}/{itemData.requiredAds}";
        }

        //==================================================
        // BUY
        //==================================================

        public void Buy()
        {
            if (itemData == null)
                return;

            switch (itemData.purchaseType)
            {
                case MN7PurchaseType.Free:

                    MN7StoreManager.Instance.Purchases
                        .PurchaseFree(
                            itemData.itemId
                        );

                    break;

                case MN7PurchaseType.Currency:

                case MN7PurchaseType.CurrencyOrAd:

                    MN7StoreManager.Instance.Purchases
                        .PurchaseWithCurrency(
                            itemData.itemId
                        );

                    break;

                case MN7PurchaseType.IAP:

                    MN7StoreManager.Instance.Purchases
                        .PurchaseWithIAP(
                            itemData.itemId
                        );

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

            MN7StoreManager.Instance.Purchases
                .PurchaseWithRewardedAd(
                    itemData.itemId,
                    HandleAdResult
                );
        }

        //==================================================
        // AD RESULT
        //==================================================

        //==================================================
        // AD RESULT
        //==================================================

        private void HandleAdResult(
            MN7AdResult result)
        {
            UpdateAdText();

            if (result == MN7AdResult.NotReady)
            {
                if (statusText != null)
                    statusText.text =
                        "AD NOT READY";

                return;
            }

            if (result == MN7AdResult.Failed)
            {
                if (statusText != null)
                    statusText.text =
                        "AD FAILED";

                return;
            }

            if (result == MN7AdResult.Skipped)
            {
                if (statusText != null)
                    statusText.text =
                        "AD SKIPPED";

                return;
            }

            if (result == MN7AdResult.Completed)
            {
                Refresh();
            }
        }

        //==================================================
        // PURCHASE EVENTS
        //==================================================

        private void HandlePurchaseSuccessful(
            MN7ItemData purchasedItem)
        {
            if (purchasedItem == null)
                return;

            if (purchasedItem.itemId !=
                itemData.itemId)
            {
                return;
            }

            Refresh();
        }

        private void HandlePurchaseFailed(
            MN7ItemData failedItem)
        {
            if (failedItem == null)
                return;

            if (failedItem.itemId !=
                itemData.itemId)
            {
                return;
            }

            Refresh();
        }
    }
}