using System;
using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Optional capabilities exposed by the reusable Store System.
    /// Keep this configuration on the Store Manager so each game can enable only what it uses.
    /// </summary>
    [Serializable]
    public class MN7StoreFeatureConfig
    {
        [Header("Store Features")]
        [Tooltip("Allow items configured as Free to be purchased.")]
        public bool freeItems = true;

        [Tooltip("Allow Rewarded Ad and Currency Or Ad purchases and initialize ad-facing behaviour.")]
        public bool adRewards = true;

        [Tooltip("Enable the store's currency economy. Disable this when the game does not use currency purchases.")]
        public bool coins = true;

        [Tooltip("Enable IAP initialization and IAP item purchases.")]
        public bool iap = false;
    }
}
