using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public enum MN7PurchaseType
    {
        Free,
        Currency,
        RewardedAd,
        CurrencyOrAd,
        IAP
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

        [Header("Purchase")]
        public MN7PurchaseType purchaseType =
            MN7PurchaseType.Currency;

        [Header("Currency Purchase")]
        public string currencyId = "soft_currency";

        [Min(0)]
        public int currencyCost = 100;

        [Header("Rewarded Ads")]
        [Min(1)]
        public int requiredAds = 1;

        public string adPlacementId =
            "store_reward";

        [Header("IAP")]
        public string iapProductId;

        [Header("Rewards")]
        public List<MN7RewardData> rewards =
            new();

        [Header("Ownership")]
        public MN7ItemOwnershipType ownershipType =
    MN7ItemOwnershipType.Permanent;
    }

    
}