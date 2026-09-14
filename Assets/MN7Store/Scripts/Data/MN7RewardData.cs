using UnityEngine;

namespace MN7.Store
{
    [System.Serializable]
    public class MN7RewardData
    {
        [Header("Item Reward")]
        public string itemId;

        [Min(1)]
        public int itemQuantity = 1;
    }
}
