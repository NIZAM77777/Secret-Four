using System;
using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Purchase features used by Secret Four.
    /// Store acquisition is limited to free claims and rewarded ads.
    /// </summary>
    [Serializable]
    public class MN7StoreFeatureConfig
    {
        [Header("Store Features")]
        [Tooltip("Allow items configured as Free to be claimed.")]
        public bool freeItems = true;

        [Tooltip("Enable rewarded-ad unlocks.")]
        public bool adRewards = true;
    }
}
