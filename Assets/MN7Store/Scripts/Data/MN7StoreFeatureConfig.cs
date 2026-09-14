using System;
using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Features used by the Secret Four integration of the reusable Store System.
    /// Secret Four supports free claims and rewarded ads only.
    /// </summary>
    [Serializable]
    public class MN7StoreFeatureConfig
    {
        [Header("Store Features")]
        [Tooltip("Allow items configured as Free to be claimed.")]
        public bool freeItems = true;

        [Tooltip("Enable rewarded-ad purchases. This is the only paid unlock method used by Secret Four.")]
        public bool adRewards = true;
    }
}