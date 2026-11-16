using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;

namespace MN7.Store
{
    /// <summary>
    /// Google Mobile Ads provider for Secret Four.
    /// Supports rewarded skin unlocks, a bottom banner, and result-screen interstitials.
    /// All SDK callbacks that touch Unity/game state are marshalled to the Unity main thread.
    /// </summary>
    public class MN7AdMobProvider : IMN7AdProvider
    {
        private readonly MN7AdMobConfig config;
        private readonly Dictionary<string, RewardedAd> rewardedAds = new();
        private readonly HashSet<string> loadingRewardedPlacements = new();

        private BannerView bannerView;
        private InterstitialAd interstitialAd;

        private bool initialized;
        private bool bannerRequestedVisible;
        private bool interstitialLoadInProgress;

        public event Action OnInterstitialLoaded;

        public MN7AdMobProvider(MN7AdMobConfig config)
        {
            this.config = config;
        }

        public void Initialize()
        {
            if (initialized)
                return;

            if (config == null)
            {
                Debug.LogError("[MN7 AdMob] Configuration asset is missing.");
                return;
            }

            MobileAds.Initialize(status =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (status == null)
                    {
                        Debug.LogError("[MN7 AdMob] SDK initialization returned no status.");
                        return;
                    }

                    initialized = true;
                    Debug.Log("[MN7 AdMob] Google Mobile Ads SDK initialized.");

                    LoadAllRewardedAds();
                    LoadInterstitialAd();

                    if (bannerRequestedVisible)
                        EnsureBannerCreated();
                });
            });
        }

        //==================================================
        // BANNER
        //==================================================

        public void SetBannerVisible(bool visible)
        {
            bannerRequestedVisible = visible;

            if (!initialized)
                return;

            if (!visible)
            {
                bannerView?.Hide();
                return;
            }

            EnsureBannerCreated();

            if (bannerView != null)
                bannerView.Show();
        }

        private void EnsureBannerCreated()
        {
            if (bannerView != null || !initialized)
                return;

            string adUnitId = GetBannerAdUnitId();
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogError("[MN7 AdMob] Banner Ad Unit ID is not configured for this platform.");
                return;
            }

            bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);

            bannerView.OnBannerAdLoaded += () =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (bannerRequestedVisible)
                        bannerView?.Show();
                    else
                        bannerView?.Hide();

                    Debug.Log("[MN7 AdMob] Banner ad loaded.");
                });
            };

            bannerView.OnBannerAdLoadFailed += error =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.LogWarning($"[MN7 AdMob] Banner ad failed to load: {error}");
                });
            };

            bannerView.LoadAd(new AdRequest());
            Debug.Log("[MN7 AdMob] Banner ad requested.");
        }

        private string GetBannerAdUnitId()
        {
#if UNITY_ANDROID
            return config?.androidBannerAdUnitId?.Trim();
#elif UNITY_IOS
            return config?.iosBannerAdUnitId?.Trim();
#else
            return null;
#endif
        }

        //==================================================
        // INTERSTITIAL
        //==================================================

        private void LoadInterstitialAd()
        {
            if (!initialized || interstitialLoadInProgress)
                return;

            if (interstitialAd != null)
            {
                if (interstitialAd.CanShowAd())
                    return;

                interstitialAd.Destroy();
                interstitialAd = null;
            }

            string adUnitId = GetInterstitialAdUnitId();
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogError("[MN7 AdMob] Interstitial Ad Unit ID is not configured for this platform.");
                return;
            }

            interstitialLoadInProgress = true;

            InterstitialAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    interstitialLoadInProgress = false;

                    if (error != null || ad == null)
                    {
                        Debug.LogWarning($"[MN7 AdMob] Interstitial failed to load: {error}");
                        return;
                    }

                    interstitialAd = ad;
                    Debug.Log("[MN7 AdMob] Interstitial ad loaded.");
                    OnInterstitialLoaded?.Invoke();
                });
            });
        }

        /// <summary>
        /// Shows an already-loaded interstitial. Returns false when one is not ready;
        /// callers should retry only at a later natural result-screen transition.
        /// </summary>
        public bool TryShowInterstitial(Action onFinished)
        {
            if (!initialized)
                return false;

            if (interstitialAd == null || !interstitialAd.CanShowAd())
            {
                LoadInterstitialAd();
                return false;
            }

            InterstitialAd adToShow = interstitialAd;
            interstitialAd = null;
            bool finishHandled = false;

            void Finish()
            {
                if (finishHandled)
                    return;

                finishHandled = true;
                adToShow.Destroy();
                onFinished?.Invoke();
                LoadInterstitialAd();
            }

            adToShow.OnAdFullScreenContentClosed += () =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(Finish);
            };

            adToShow.OnAdFullScreenContentFailed += error =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.LogWarning($"[MN7 AdMob] Interstitial failed to show: {error}");
                    Finish();
                });
            };

            try
            {
                Debug.Log("[MN7 AdMob] Showing result-screen interstitial.");
                adToShow.Show();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MN7 AdMob] Interstitial show exception: {exception}");
                Finish();
                return false;
            }
        }

        private string GetInterstitialAdUnitId()
        {
#if UNITY_ANDROID
            return config?.androidInterstitialAdUnitId?.Trim();
#elif UNITY_IOS
            return config?.iosInterstitialAdUnitId?.Trim();
#else
            return null;
#endif
        }

        //==================================================
        // REWARDED ADS
        //==================================================

        private void LoadAllRewardedAds()
        {
            if (config?.rewardedPlacements == null)
                return;

            HashSet<string> seenPlacements = new(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < config.rewardedPlacements.Count; i++)
            {
                MN7AdMobPlacement placement = config.rewardedPlacements[i];
                if (placement == null)
                {
                    Debug.LogError($"[MN7 AdMob] Rewarded placement at index {i} is null.");
                    continue;
                }

                string placementId = placement.placementId?.Trim();
                if (string.IsNullOrWhiteSpace(placementId) || !seenPlacements.Add(placementId))
                {
                    Debug.LogError($"[MN7 AdMob] Empty or duplicate rewarded placement ID at index {i}.");
                    continue;
                }

                LoadRewardedAd(placementId);
            }
        }

        private void LoadRewardedAd(string placementId)
        {
            placementId = placementId?.Trim();

            if (!initialized ||
                string.IsNullOrWhiteSpace(placementId) ||
                loadingRewardedPlacements.Contains(placementId))
                return;

            if (rewardedAds.TryGetValue(placementId, out RewardedAd existingAd))
            {
                if (existingAd != null && existingAd.CanShowAd())
                    return;

                existingAd?.Destroy();
                rewardedAds.Remove(placementId);
            }

            string adUnitId = GetRewardedAdUnitId(placementId);
            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogError($"[MN7 AdMob] No rewarded Ad Unit ID configured for placement '{placementId}'.");
                return;
            }

            loadingRewardedPlacements.Add(placementId);

            RewardedAd.Load(adUnitId, new AdRequest(), (ad, error) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    loadingRewardedPlacements.Remove(placementId);

                    if (error != null || ad == null)
                    {
                        Debug.LogWarning($"[MN7 AdMob] Rewarded ad load failed for '{placementId}': {error}");
                        return;
                    }

                    rewardedAds[placementId] = ad;
                    Debug.Log($"[MN7 AdMob] Rewarded ad loaded: {placementId}");
                });
            });
        }

        public bool IsRewardedAdReady(string placementId)
        {
            placementId = placementId?.Trim();

            return !string.IsNullOrWhiteSpace(placementId) &&
                   rewardedAds.TryGetValue(placementId, out RewardedAd ad) &&
                   ad != null &&
                   ad.CanShowAd();
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
                ad.Destroy();
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
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.Log($"[MN7 AdMob] Rewarded ad closed: {placementId}");

                    if (rewardedAds.TryGetValue(placementId, out RewardedAd current) && current == ad)
                        rewardedAds.Remove(placementId);

                    ad.Destroy();
                    LoadRewardedAd(placementId);

                    if (!rewardEarned)
                        Complete(MN7AdResult.Skipped);
                });
            };

            ad.OnAdFullScreenContentFailed += error =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    Debug.LogWarning($"[MN7 AdMob] Rewarded ad failed to show: {placementId} - {error}");

                    if (rewardedAds.TryGetValue(placementId, out RewardedAd current) && current == ad)
                        rewardedAds.Remove(placementId);

                    ad.Destroy();
                    LoadRewardedAd(placementId);
                    Complete(MN7AdResult.Failed);
                });
            };

            ad.Show(reward =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    rewardEarned = true;
                    Debug.Log($"[MN7 AdMob] Reward earned for placement: {placementId}");
                    Complete(MN7AdResult.Completed);
                });
            });
        }

        private string GetRewardedAdUnitId(string placementId)
        {
            if (config?.rewardedPlacements == null)
                return null;

            foreach (MN7AdMobPlacement placement in config.rewardedPlacements)
            {
                if (placement == null ||
                    !string.Equals(
                        placement.placementId?.Trim(),
                        placementId,
                        StringComparison.OrdinalIgnoreCase))
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

        //==================================================
        // CLEANUP
        //==================================================

        public void Dispose()
        {
            if (bannerView != null)
            {
                bannerView.Destroy();
                bannerView = null;
            }

            if (interstitialAd != null)
            {
                interstitialAd.Destroy();
                interstitialAd = null;
            }

            foreach (RewardedAd ad in rewardedAds.Values)
                ad?.Destroy();

            rewardedAds.Clear();
            loadingRewardedPlacements.Clear();
        }
    }
}
