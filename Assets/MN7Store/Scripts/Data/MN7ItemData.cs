using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public enum MN7PurchaseType
    {
        Free,
        RewardedAd
    }

    public enum MN7ItemOwnershipType
    {
        Permanent,
        Consumable
    }

    [CreateAssetMenu(
        fileName = "StoreItem",
        menuName = "MN7 Store/Store Item"
    )]
    public class MN7ItemData : ScriptableObject
    {
        [Header("Item")]
        public string itemId = "item_001";
        public string displayName = "New Item";

        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Categories")]
        [Tooltip("IDs of the categories this item belongs to. An item may belong to multiple categories.")]
        public List<string> categoryIds = new();

        [Header("Selection")]
        [Tooltip("Items with the same Selection Group ID share one selection slot. Leave empty for non-selectable items.")]
        public string selectionGroupId;
        [Tooltip("Whether this item can be selected/equipped after ownership.")]
        public bool selectable;
        [Tooltip("When true, this item starts owned on a brand-new save.")]
        public bool defaultOwned;
        [Tooltip("When true and no saved selection exists for this group, this item becomes the initial selection.")]
        public bool defaultSelected;

        [Header("Sorting")]
        [Tooltip("Lower values appear first within the same ownership/selection state.")]
        public int sortPriority;
        [Tooltip("Higher values take priority when the Store's sorting mode gives manual priority more weight.")]
        public int customSortWeight;

        [Header("Purchase")]
        [Tooltip("Secret Four uses rewarded ads as the purchase method. Free items are optional.")]
        public MN7PurchaseType purchaseType = MN7PurchaseType.RewardedAd;

        [Header("Rewarded Ads")]
        [Min(1)]
        public int requiredAds = 1;
        public string adPlacementId = "store_reward";

        [Header("Rewards")]
        public List<MN7RewardData> rewards = new();

        [Header("Ownership")]
        public MN7ItemOwnershipType ownershipType = MN7ItemOwnershipType.Permanent;
    }
}
