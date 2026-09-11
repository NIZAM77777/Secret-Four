using UnityEngine;

namespace MN7.Store
{
    public enum MN7RewardType
    {
        Item
    }

    [System.Serializable]
    public class MN7RewardData
    {
        [Header("Item Reward")]
        public MN7RewardType rewardType = MN7RewardType.Item;
        public string itemId;
        [Min(1)] public int itemQuantity = 1;
    }
}
