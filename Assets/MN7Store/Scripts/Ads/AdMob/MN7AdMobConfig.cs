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
        [Header("Banner")]
        public string androidBannerAdUnitId;
        public string iosBannerAdUnitId;

        [Header("Interstitial")]
        public string androidInterstitialAdUnitId;
        public string iosInterstitialAdUnitId;

        [Min(1)]
        [Tooltip("Show one interstitial after this many completed matches. It is displayed only at the result screen and only when an ad is ready.")]
        public int interstitialEveryCompletedMatches = 2;

        [Header("Rewarded Ad Placements")]
        public List<MN7AdMobPlacement> rewardedPlacements = new();
    }
}
