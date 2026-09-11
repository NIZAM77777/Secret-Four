using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MN7.Store
{
    public class MN7StoreItemUI : MonoBehaviour
    {
        [SerializeField] private MN7ItemData itemData;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button watchAdButton;

        private void OnEnable()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store == null) return;

            if (store.Purchases != null)
            {
                store.Purchases.OnPurchaseSuccessful += HandlePurchaseSuccessful;
                store.Purchases.OnPurchaseFailed += HandlePurchaseFailed;
            }
            if (store.Inventory != null)
            {
                store.Inventory.OnItemAdded += HandleItemChanged;
                store.Inventory.OnItemRemoved += HandleItemChanged;
            }
            Refresh();
        }

        private void OnDisable()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store == null) return;

            if (store.Purchases != null)
            {
                store.Purchases.OnPurchaseSuccessful -= HandlePurchaseSuccessful;
                store.Purchases.OnPurchaseFailed -= HandlePurchaseFailed;
            }
            if (store.Inventory != null)
            {
                store.Inventory.OnItemAdded -= HandleItemChanged;
                store.Inventory.OnItemRemoved -= HandleItemChanged;
            }
        }

        private void HandleItemChanged(string itemId)
        {
            if (itemData != null && itemData.itemId == itemId) Refresh();
        }

        public void SetItem(MN7ItemData newItem)
        {
            itemData = newItem;
            Refresh();
        }

        public void Refresh()
        {
            if (itemData == null) return;
            if (icon != null) icon.sprite = itemData.icon;
            if (nameText != null) nameText.text = itemData.displayName;
            if (descriptionText != null) descriptionText.text = itemData.description;
            UpdatePurchaseUI();
        }

        private void UpdatePurchaseUI()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store == null || store.Inventory == null) return;

            bool owned = store.Inventory.IsOwned(itemData.itemId);
            if (buyButton != null) buyButton.gameObject.SetActive(false);
            if (watchAdButton != null) watchAdButton.gameObject.SetActive(false);
            if (priceText != null) priceText.text = string.Empty;
            if (statusText != null) statusText.text = string.Empty;

            if (owned)
            {
                if (statusText != null) statusText.text = "OWNED";
                return;
            }

            if (itemData.purchaseType == MN7PurchaseType.Free)
            {
                if (buyButton != null) buyButton.gameObject.SetActive(true);
                if (priceText != null) priceText.text = "FREE";
            }
            else if (itemData.purchaseType == MN7PurchaseType.RewardedAd)
            {
                if (watchAdButton != null) watchAdButton.gameObject.SetActive(true);
                UpdateAdText();
            }
        }

        private void UpdateAdText()
        {
            if (statusText == null || itemData == null) return;
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store?.AdProgress == null) return;
            statusText.text = $"ADS {store.AdProgress.GetProgress(itemData.itemId)}/{itemData.requiredAds}";
        }

        public void Buy()
        {
            if (itemData == null || itemData.purchaseType != MN7PurchaseType.Free) return;
            MN7StoreManager.Instance?.Purchases?.PurchaseFree(itemData.itemId);
        }

        public void WatchAd()
        {
            if (itemData == null || itemData.purchaseType != MN7PurchaseType.RewardedAd) return;
            MN7StoreManager.Instance?.Purchases?.PurchaseWithRewardedAd(itemData.itemId, HandleAdResult);
        }

        private void HandleAdResult(MN7AdResult result)
        {
            if (statusText == null) return;
            if (result == MN7AdResult.NotReady) statusText.text = "AD NOT READY";
            else if (result == MN7AdResult.Failed) statusText.text = "AD FAILED";
            else if (result == MN7AdResult.Skipped) statusText.text = "AD SKIPPED";
            else Refresh();
        }

        private void HandlePurchaseSuccessful(MN7ItemData purchasedItem)
        {
            if (purchasedItem != null && itemData != null && purchasedItem.itemId == itemData.itemId) Refresh();
        }

        private void HandlePurchaseFailed(MN7ItemData failedItem)
        {
            if (failedItem != null && itemData != null && failedItem.itemId == itemData.itemId) Refresh();
        }
    }
}
