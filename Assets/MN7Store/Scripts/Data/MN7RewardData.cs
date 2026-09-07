using UnityEngine;

namespace MN7.Store
{
    public enum MN7RewardType
    {
        Currency,
        Item
    }

    [System.Serializable]
    public class MN7RewardData
    {
        [Header("Reward")]
        public MN7RewardType rewardType;

        [Header("Currency Reward")]
        public string currencyId;

        [Min(1)]
        public int currencyAmount = 1;

        [Header("Item Reward")]
        public string itemId;

        [Min(1)]
        public int itemQuantity = 1;
    }
}