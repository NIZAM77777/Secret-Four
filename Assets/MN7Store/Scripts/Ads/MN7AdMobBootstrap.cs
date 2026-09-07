using UnityEngine;

namespace MN7.Store
{
    public class MN7AdMobBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private MN7AdMobConfig adMobConfig;

        private MN7AdMobProvider provider;

        private void Start()
        {
            provider =
                new MN7AdMobProvider();

            provider.Initialize(
                success =>
                {
                    if (!success)
                    {
                        Debug.LogError(
                            "[MN7 Store] AdMob initialization failed."
                        );

                        return;
                    }

                    RegisterProvider();
                    PreloadRewardedAds();
                }
            );
        }

        private void RegisterProvider()
        {
            if (MN7StoreManager.Instance == null)
            {
                Debug.LogError(
                    "[MN7 Store] Store Manager not found."
                );

                return;
            }

            MN7StoreManager.Instance.Ads
                .SetProvider(provider);
        }

        private void PreloadRewardedAds()
        {
            if (adMobConfig == null)
            {
                Debug.LogWarning(
                    "[MN7 Store] AdMob Config not assigned."
                );

                return;
            }

            foreach (
                MN7AdMobPlacement placement
                in adMobConfig.rewardedPlacements)
            {
                if (placement == null)
                    continue;

                string adUnitId =
                    GetPlatformAdUnitId(
                        placement
                    );

                if (string.IsNullOrWhiteSpace(
                        adUnitId))
                {
                    Debug.LogWarning(
                        $"[MN7 Store] No Ad Unit ID for: " +
                        $"{placement.placementId}"
                    );

                    continue;
                }

                provider.LoadRewardedAd(
                    placement.placementId,
                    adUnitId
                );
            }
        }

        private string GetPlatformAdUnitId(
            MN7AdMobPlacement placement)
        {
#if UNITY_ANDROID
            return placement.androidAdUnitId;
#elif UNITY_IOS
            return placement.iosAdUnitId;
#else
            return string.Empty;
#endif
        }
    }
}