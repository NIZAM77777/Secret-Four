using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7AdMobBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private MN7AdMobConfig adMobConfig;
        private MN7AdMobProvider provider;

        private void Start()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store == null)
            {
                Debug.LogError("[MN7 Store] AdMob bootstrap could not find MN7StoreManager.");
                return;
            }

            if (!store.IsReady)
            {
                store.OnStoreInitialized -= HandleStoreInitialized;
                store.OnStoreInitialized += HandleStoreInitialized;
                return;
            }

            Initialize(store);
        }

        private void OnDestroy()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store != null)
                store.OnStoreInitialized -= HandleStoreInitialized;
        }

        private void HandleStoreInitialized()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store != null)
                Initialize(store);
        }

        private void Initialize(MN7StoreManager store)
        {
            store.OnStoreInitialized -= HandleStoreInitialized;

#if !UNITY_ANDROID && !UNITY_IOS
            Debug.Log("[MN7 Store] Rewarded ads require an Android or iOS build target.");
            return;
#else
            if (adMobConfig == null)
            {
                Debug.LogError("[MN7 Store] AdMob Config is not assigned while Rewarded Ads are enabled.");
                return;
            }

            if (!ValidateConfig(store, adMobConfig))
                return;

            provider = new MN7AdMobProvider(adMobConfig);
            if (!RegisterProvider(store))
                return;

            provider.Initialize();
#endif
        }

        private static bool ValidateConfig(MN7StoreManager store, MN7AdMobConfig config)
        {
            if (config.rewardedPlacements == null || config.rewardedPlacements.Count == 0)
            {
                Debug.LogError("[MN7 Store] AdMob Config has no rewarded placements.");
                return false;
            }

            HashSet<string> placementIds = new(StringComparer.OrdinalIgnoreCase);
            foreach (MN7AdMobPlacement placement in config.rewardedPlacements)
            {
                if (placement == null)
                {
                    Debug.LogError("[MN7 Store] AdMob Config contains a null rewarded placement.");
                    return false;
                }

                string placementId = placement.placementId?.Trim();
                if (string.IsNullOrWhiteSpace(placementId) || !placementIds.Add(placementId))
                {
                    Debug.LogError($"[MN7 Store] AdMob Config has an empty or duplicate Placement ID: '{placement.placementId}'.");
                    return false;
                }

#if UNITY_ANDROID
                if (string.IsNullOrWhiteSpace(placement.androidAdUnitId))
                {
                    Debug.LogError($"[MN7 Store] AdMob placement '{placementId}' has no Android Ad Unit ID.");
                    return false;
                }
#elif UNITY_IOS
                if (string.IsNullOrWhiteSpace(placement.iosAdUnitId))
                {
                    Debug.LogError($"[MN7 Store] AdMob placement '{placementId}' has no iOS Ad Unit ID.");
                    return false;
                }
#endif
            }

            if (store.Config?.items != null)
            {
                foreach (MN7ItemData item in store.Config.items)
                {
                    if (item == null || item.purchaseType != MN7PurchaseType.RewardedAd)
                        continue;

                    string placementId = item.adPlacementId?.Trim();
                    if (string.IsNullOrWhiteSpace(placementId))
                    {
                        Debug.LogError($"[MN7 Store] Rewarded-ad item '{item.itemId}' has no Placement ID.");
                        return false;
                    }

                    bool found = false;
                    foreach (MN7AdMobPlacement placement in config.rewardedPlacements)
                    {
                        if (placement != null &&
                            string.Equals(placement.placementId?.Trim(), placementId, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        Debug.LogError($"[MN7 Store] Store item '{item.itemId}' references rewarded-ad Placement ID '{placementId}', but that placement is missing from AdMob Config.");
                        return false;
                    }
                }
            }

            return true;
        }

        private bool RegisterProvider(MN7StoreManager store)
        {
            if (store.Ads == null)
            {
                Debug.LogError("[MN7 Store] Ad Manager is unavailable. Ensure Store initialization succeeded.");
                return false;
            }

            store.Ads.SetProvider(provider);
            Debug.Log("[MN7 Store] AdMob provider registered.");
            return true;
        }
    }
}
