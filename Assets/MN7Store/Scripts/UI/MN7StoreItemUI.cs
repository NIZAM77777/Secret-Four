using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MN7.Store
{
    /// <summary>
    /// Visual and interaction layer for one Secret Four Store item.
    /// Acquisition is limited to Free claims and Rewarded Ads.
    /// </summary>
    public class MN7StoreItemUI : MonoBehaviour
    {
        [Header("Item")]
        [SerializeField] private MN7ItemData itemData;

        [Header("Base UI References")]
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text statusText;

        [Header("Action Buttons")]
        [SerializeField] private MN7StoreItemActionSetUI actionSet;

        [Header("Optional State Objects")]
        [SerializeField] private GameObject ownedState;
        [SerializeField] private GameObject selectionState;
        [SerializeField] private GameObject lockedState;

        public MN7ItemData ItemData => itemData;

        private void Awake()
        {
            CacheReferences();
            WireButtons();
            ValidateReferences();
        }

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
                store.Inventory.OnItemAdded += HandleItemAdded;
                store.Inventory.OnItemRemoved += HandleItemRemoved;
            }

            if (store.Selection != null)
                store.Selection.OnSelectionChanged += HandleSelectionChanged;

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
                store.Inventory.OnItemAdded -= HandleItemAdded;
                store.Inventory.OnItemRemoved -= HandleItemRemoved;
            }

            if (store.Selection != null)
                store.Selection.OnSelectionChanged -= HandleSelectionChanged;
        }

        private void CacheReferences()
        {
            if (background == null) background = FindImage("BackGround", "Background");
            if (icon == null) icon = FindImage("Icon");
            if (nameText == null) nameText = FindText("Name");
            if (descriptionText == null) descriptionText = FindText("Description");
            if (priceText == null) priceText = FindText("Price");
            if (statusText == null) statusText = FindText("Status");
            if (actionSet == null) actionSet = GetComponentInChildren<MN7StoreItemActionSetUI>(true);
        }

        private void WireButtons() => actionSet?.Bind(Buy, WatchAd, Select);

        public void SetItem(MN7ItemData newItem)
        {
            itemData = newItem;
            Refresh();
        }

        public void Refresh()
        {
            if (itemData == null) return;

            if (icon != null)
            {
                icon.sprite = itemData.icon;
                icon.enabled = itemData.icon != null;
            }

            if (nameText != null) nameText.text = itemData.displayName;
            if (descriptionText != null) descriptionText.text = itemData.description;

            UpdateStateUI();
        }

        private void UpdateStateUI()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            bool owned = store?.Inventory != null && store.Inventory.IsOwned(itemData.itemId);
            bool selectable = itemData.selectable && !string.IsNullOrWhiteSpace(itemData.selectionGroupId);
            bool selected = owned && selectable && store?.Selection != null && store.Selection.IsSelected(itemData);

            if (ownedState != null) ownedState.SetActive(owned);
            if (selectionState != null) selectionState.SetActive(selected);
            if (lockedState != null) lockedState.SetActive(false);

            actionSet?.HideAll();
            if (priceText != null) priceText.text = string.Empty;
            if (statusText != null) statusText.text = string.Empty;

            if (selected)
            {
                actionSet?.ShowSelected();
                return;
            }

            if (owned && selectable)
            {
                actionSet?.ShowSelect();
                return;
            }

            if (owned)
            {
                if (statusText != null) statusText.text = "OWNED";
                return;
            }

            if (itemData.purchaseType == MN7PurchaseType.Free)
            {
                if (store?.Features == null || store.Features.freeItems)
                {
                    actionSet?.ShowBuy();
                    if (priceText != null) priceText.text = "FREE";
                }
                else if (statusText != null)
                {
                    statusText.text = "UNAVAILABLE";
                }

                return;
            }

            if (itemData.purchaseType == MN7PurchaseType.RewardedAd)
            {
                if (store?.Features == null || store.Features.adRewards)
                {
                    actionSet?.ShowBuyWithAd();
                    UpdateAdText(store);
                }
                else if (statusText != null)
                {
                    statusText.text = "UNAVAILABLE";
                }

                return;
            }

            if (statusText != null)
                statusText.text = "UNAVAILABLE";
        }

        private void UpdateAdText(MN7StoreManager store)
        {
            if (statusText == null || itemData == null || store?.AdProgress == null)
                return;

            int progress = store.AdProgress.GetProgress(itemData.itemId);
            statusText.text = itemData.requiredAds > 1
                ? $"ADS {progress}/{itemData.requiredAds}"
                : "WATCH AD";
        }

        public void Buy()
        {
            if (itemData == null) return;

            MN7StoreManager store = MN7StoreManager.Instance;
            if (store == null || store.Purchases == null) return;

            if (store.Inventory != null && store.Inventory.IsOwned(itemData.itemId))
            {
                if (itemData.selectable)
                    Select();
                return;
            }

            if (itemData.purchaseType == MN7PurchaseType.Free &&
                (store.Features == null || store.Features.freeItems))
            {
                store.Purchases.PurchaseFree(itemData.itemId);
            }
        }

        public void WatchAd()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (itemData == null || store == null || store.Purchases == null)
                return;

            if (store.Features != null && !store.Features.adRewards)
                return;

            store.Purchases.PurchaseWithRewardedAd(itemData.itemId, HandleAdResult);
        }

        public void Select()
        {
            if (itemData == null || !itemData.selectable)
                return;

            MN7StoreManager.Instance?.Selection?.SelectItem(itemData.itemId);
        }

        private void HandleAdResult(MN7AdResult result)
        {
            if (result == MN7AdResult.Completed)
            {
                Refresh();
                return;
            }

            if (statusText == null) return;

            statusText.text = result switch
            {
                MN7AdResult.NotReady => "AD NOT READY",
                MN7AdResult.Skipped => "AD SKIPPED",
                _ => "AD FAILED"
            };
        }

        private void HandleItemAdded(string id)
        {
            if (itemData != null && string.Equals(itemData.itemId, id, System.StringComparison.OrdinalIgnoreCase))
                Refresh();
        }

        private void HandleItemRemoved(string id)
        {
            if (itemData != null && string.Equals(itemData.itemId, id, System.StringComparison.OrdinalIgnoreCase))
                Refresh();
        }

        private void HandleSelectionChanged(string group, string id)
        {
            if (itemData != null &&
                string.Equals(itemData.selectionGroupId, group, System.StringComparison.OrdinalIgnoreCase))
                Refresh();
        }

        private void HandlePurchaseSuccessful(MN7ItemData item)
        {
            if (item != null && itemData != null &&
                string.Equals(item.itemId, itemData.itemId, System.StringComparison.OrdinalIgnoreCase))
                Refresh();
        }

        private void HandlePurchaseFailed(MN7ItemData item)
        {
            if (item != null && itemData != null &&
                string.Equals(item.itemId, itemData.itemId, System.StringComparison.OrdinalIgnoreCase))
                Refresh();
        }

        private Image FindImage(params string[] names)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                foreach (string targetName in names)
                {
                    if (child.name == targetName)
                        return child.GetComponent<Image>();
                }
            }

            return null;
        }

        private TMP_Text FindText(string targetName)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == targetName)
                    return child.GetComponent<TMP_Text>();
            }

            return null;
        }

        private void ValidateReferences()
        {
            if (background == null) Debug.LogError($"[MN7 Store] {name}: Background reference is missing.");
            if (icon == null) Debug.LogError($"[MN7 Store] {name}: Icon reference is missing.");
            if (nameText == null) Debug.LogError($"[MN7 Store] {name}: Name Text reference is missing.");
            if (descriptionText == null) Debug.LogError($"[MN7 Store] {name}: Description Text reference is missing.");
            if (priceText == null) Debug.LogError($"[MN7 Store] {name}: Price Text reference is missing.");
            if (statusText == null) Debug.LogError($"[MN7 Store] {name}: Status Text reference is missing.");
            if (actionSet == null) Debug.LogError($"[MN7 Store] {name}: Action Set reference is missing.");
        }
    }
}
