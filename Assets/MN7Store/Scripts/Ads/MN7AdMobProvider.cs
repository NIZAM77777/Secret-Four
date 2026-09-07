using GoogleMobileAds.Api;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7AdMobProvider : IMN7AdProvider
    {
        private readonly Dictionary<string, RewardedAd> rewardedAds =
            new();

        private bool initialized;

        //==================================================
        // INITIALIZE
        //==================================================

        public void Initialize(
            Action<bool> onCompleted = null)
        {
            if (initialized)
            {
                onCompleted?.Invoke(true);
                return;
            }

            MobileAds.Initialize(
                initializationStatus =>
                {
                    initialized = true;

                    Debug.Log(
                        "[MN7 Store] AdMob initialized."
                    );

                    onCompleted?.Invoke(true);
                }
            );
        }

        //==================================================
        // LOAD
        //==================================================

        public void LoadRewardedAd(
            string placementId,
            string adUnitId)
        {
            if (!initialized)
            {
                Debug.LogWarning(
                    "[MN7 Store] AdMob is not initialized."
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(adUnitId))
            {
                Debug.LogError(
                    "[MN7 Store] Rewarded Ad Unit ID is empty."
                );

                return;
            }

            if (rewardedAds.TryGetValue(
                    placementId,
                    out RewardedAd existingAd))
            {
                existingAd.Destroy();
                rewardedAds.Remove(placementId);
            }

            AdRequest adRequest = new();

            RewardedAd.Load(
                adUnitId,
                adRequest,
                (RewardedAd ad, LoadAdError error) =>
                {
                    if (error != null)
                    {
                        Debug.LogWarning(
                            $"[MN7 Store] Rewarded ad failed to load: " +
                            $"{error}"
                        );

                        return;
                    }

                    if (ad == null)
                    {
                        Debug.LogWarning(
                            "[MN7 Store] Rewarded ad returned null."
                        );

                        return;
                    }

                    rewardedAds[placementId] = ad;

                    RegisterEvents(
                        placementId,
                        ad,
                        adUnitId
                    );

                    Debug.Log(
                        $"[MN7 Store] Rewarded ad loaded: " +
                        $"{placementId}"
                    );
                }
            );
        }

        //==================================================
        // READY
        //==================================================

        public bool IsRewardedAdReady(
            string placementId)
        {
            if (!rewardedAds.TryGetValue(
                    placementId,
                    out RewardedAd ad))
            {
                return false;
            }

            return ad != null &&
                   ad.CanShowAd();
        }

        //==================================================
        // SHOW
        //==================================================

        public void ShowRewardedAd(
            string placementId,
            Action<MN7AdResult> onCompleted)
        {
            if (!rewardedAds.TryGetValue(
                    placementId,
                    out RewardedAd ad))
            {
                onCompleted?.Invoke(
                    MN7AdResult.NotReady
                );

                return;
            }

            if (ad == null ||
                !ad.CanShowAd())
            {
                onCompleted?.Invoke(
                    MN7AdResult.NotReady
                );

                return;
            }

            bool rewardEarned = false;

            ad.Show(
                reward =>
                {
                    rewardEarned = true;

                    Debug.Log(
                        $"[MN7 Store] Reward earned. " +
                        $"Type={reward.Type}, " +
                        $"Amount={reward.Amount}"
                    );

                    onCompleted?.Invoke(
                        MN7AdResult.Completed
                    );
                }
            );
        }

        //==================================================
        // EVENTS
        //==================================================

        private void RegisterEvents(
            string placementId,
            RewardedAd ad,
            string adUnitId)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log(
                    $"[MN7 Store] Rewarded ad closed: " +
                    $"{placementId}"
                );

                DestroyAd(placementId);

                LoadRewardedAd(
                    placementId,
                    adUnitId
                );
            };

            ad.OnAdFullScreenContentFailed +=
                (AdError error) =>
                {
                    Debug.LogWarning(
                        $"[MN7 Store] Rewarded ad failed to show: " +
                        $"{error}"
                    );

                    DestroyAd(placementId);

                    LoadRewardedAd(
                        placementId,
                        adUnitId
                    );
                };
        }

        //==================================================
        // DESTROY
        //==================================================

        private void DestroyAd(
            string placementId)
        {
            if (!rewardedAds.TryGetValue(
                    placementId,
                    out RewardedAd ad))
            {
                return;
            }

            if (ad != null)
            {
                ad.Destroy();
            }

            rewardedAds.Remove(
                placementId
            );
        }
    }
}