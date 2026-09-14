using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    [Serializable]
    public class MN7AdMobPlacement
    {
        public string placementId;

        [Header("Android")]
        public string androidAdUnitId;

        [Header("iOS")]
        public string iosAdUnitId;
    }

    [CreateAssetMenu(
        fileName = "MN7AdMobConfig",
        menuName = "MN7 Store/AdMob Config"
    )]
    public class MN7AdMobConfig : ScriptableObject
    {
        [Header("AdMob Placements")]
        public List<MN7AdMobPlacement> rewardedPlacements = new();
    }
}
