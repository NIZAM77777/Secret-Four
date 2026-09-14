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

        [Header("Acquisition")]
        public MN7PurchaseType purchaseType = MN7PurchaseType.Free;

        [Header("Categories")]
        public List<string> categoryIds = new();

        [Header("Selection")]
        public string selectionGroupId;
        public bool selectable;
        public bool defaultOwned;
        public bool defaultSelected;

        [Header("Sorting")]
        public int sortPriority;
        public int customSortWeight;

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
