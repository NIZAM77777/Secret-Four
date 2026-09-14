using System;
using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Features used by the Secret Four integration of the reusable Store System.
    /// Secret Four uses rewarded ads as its only purchase method.
    /// </summary>
    [Serializable]
    public class MN7StoreFeatureConfig
    {
        [Header("Store Features")]
        [Tooltip("Allow items configured as Free to be claimed.")]
        public bool freeItems = false;

        [Tooltip("Enable rewarded-ad purchases. This is the only purchase method used by Secret Four.")]
        public bool adRewards = true;

        [HideInInspector]
        public bool coins = false;

        [HideInInspector]
        public bool iap = false;
    }
}
