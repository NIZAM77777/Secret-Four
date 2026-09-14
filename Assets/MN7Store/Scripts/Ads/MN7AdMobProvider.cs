using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

namespace MN7.Store
{
    public class MN7AdMobProvider : IMN7AdProvider
    {
        private readonly MN7AdMobConfig config;

        private readonly Dictionary<string, RewardedAd> rewardedAds =
            new();

        private readonly HashSet<string> loadingPlacements =
            new();

        private bool initialized;

        public MN7AdMobProvider(MN7AdMobConfig config)
        {
            this.config = config;
        }

        public void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            MobileAds.Initialize(
                initializationStatus =>
                {
                    Debug.Log("[MN7 Store] AdMob initialized.");
                    LoadAllRewardedAds();
                }
            );
        }

        private void LoadAllRewardedAds()
        {
            if (config == null)
            {
                Debug.LogError("[MN7 Store] AdMob config is null.");
                return;
            }

            if (config.rewardedPlacements == null)
                return;

            HashSet<string> seenPlacements = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < config.rewardedPlacements.Count; i++)
            {
                MN7AdMobPlacement placement = config.rewardedPlacements[i];
                if (placement == null)
                {
                    Debug.LogError($"[MN7 Store] AdMob rewarded placement at index {i} is null.");
                    continue;
                }

                string placementId = placement.placementId?.Trim();
                if (string.IsNullOrWhiteSpace(placementId))
                {
                    Debug.LogError($"[MN7 Store] AdMob rewarded placement at index {i} has an empty Placement ID.");
                    continue;
                }

                if (!seenPlacements.Add(placementId))
                {
                    Debug.LogError($"[MN7 Store] Duplicate AdMob rewarded Placement ID: {placementId}");
                    continue;
                }

                LoadRewardedAd(placementId);
            }
        }

        private void LoadRewardedAd(string placementId)
        {
            placementId = placementId?.Trim();
            if (string.IsNullOrWhiteSpace(placementId))
                return;

            if (loadingPlacements.Contains(placementId))
                return;

            if (rewardedAds.TryGetValue(placementId, out RewardedAd existingAd))
            {
                if (existingAd != null && existingAd.CanShowAd())
                    return;

                rewardedAds.Remove(placementId);
            }

            string adUnitId = GetAdUnitId(placementId);
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogError($"[MN7 Store] No AdMob ad unit ID configured for placement: {placementId}");
                return;
            }

            loadingPlacements.Add(placementId);

            RewardedAd.Load(
                adUnitId,
                new AdRequest(),
                (RewardedAd ad, LoadAdError error) =>
                {
                    loadingPlacements.Remove(placementId);

                    if (error != null || ad == null)
                    {
                        Debug.LogError($"[MN7 Store] Rewarded ad load failed: {placementId} - {error}");
                        return;
                    }

                    rewardedAds[placementId] = ad;
                    Debug.Log($"[MN7 Store] Rewarded ad loaded: {placementId}");
                }
            );
        }

        public bool IsRewardedAdReady(string placementId)
        {
            placementId = placementId?.Trim();
            if (string.IsNullOrWhiteSpace(placementId))
                return false;

            if (!rewardedAds.TryGetValue(placementId, out RewardedAd ad) || ad == null)
                return false;

            return ad.CanShowAd();
        }

        public void ShowRewardedAd(string placementId, Action<MN7AdResult> onCompleted)
        {
            placementId = placementId?.Trim();
            if (string.IsNullOrWhiteSpace(placementId))
            {
                onCompleted?.Invoke(MN7AdResult.Failed);
                return;
            }

            if (!rewardedAds.TryGetValue(placementId, out RewardedAd ad) || ad == null)
            {
                LoadRewardedAd(placementId);
                onCompleted?.Invoke(MN7AdResult.NotReady);
                return;
            }

            if (!ad.CanShowAd())
            {
                rewardedAds.Remove(placementId);
                LoadRewardedAd(placementId);
                onCompleted?.Invoke(MN7AdResult.NotReady);
                return;
            }

            bool rewardEarned = false;
            bool callbackSent = false;

            void Complete(MN7AdResult result)
            {
                if (callbackSent)
                    return;

                callbackSent = true;
                onCompleted?.Invoke(result);
            }

            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log($"[MN7 Store] Rewarded ad closed: {placementId}");
                rewardedAds.Remove(placementId);
                LoadRewardedAd(placementId);

                if (!rewardEarned)
                    Complete(MN7AdResult.Skipped);
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError($"[MN7 Store] Rewarded ad fullscreen failed: {placementId} - {error}");
                rewardedAds.Remove(placementId);
                LoadRewardedAd(placementId);
                Complete(MN7AdResult.Failed);
            };

            ad.Show(
                reward =>
                {
                    rewardEarned = true;
                    Debug.Log($"[MN7 Store] Rewarded ad completed: {placementId}");
                    Complete(MN7AdResult.Completed);
                }
            );
        }

        private string GetAdUnitId(string placementId)
        {
            if (config == null || config.rewardedPlacements == null)
                return null;

            for (int i = 0; i < config.rewardedPlacements.Count; i++)
            {
                MN7AdMobPlacement placement = config.rewardedPlacements[i];
                if (placement == null)
                    continue;

                if (!string.Equals(placement.placementId?.Trim(), placementId, StringComparison.OrdinalIgnoreCase))
                    continue;

#if UNITY_ANDROID
                return placement.androidAdUnitId?.Trim();
#elif UNITY_IOS
                return placement.iosAdUnitId?.Trim();
#else
                return null;
#endif
            }

            return null;
        }
    }
}
