using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MN7.Store
{
    /// <summary>
    /// Persistent AdMob lifecycle controller.
    /// - Main Menu: banner visible.
    /// - Active gameplay and splash/other scenes: no banner.
    /// - Result screen: banner visible; an interstitial is attempted every N completed matches.
    /// </summary>
    public class MN7AdMobBootstrap : MonoBehaviour
    {
        private const string CompletedMatchCounterKey = "MN7.SecretFour.AdMob.CompletedMatchesSinceInterstitial";

        public static MN7AdMobBootstrap Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private MN7AdMobConfig adMobConfig;

        private MN7AdMobProvider provider;
        private bool isMainMenuScene;
        private bool isResultScreenVisible;
        private bool isInterstitialShowing;
        private bool initializationStarted;
        private bool subscribedToStoreEvents;
        private int completedMatchesSinceInterstitial;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            completedMatchesSinceInterstitial = Mathf.Max(
                0,
                PlayerPrefs.GetInt(CompletedMatchCounterKey, 0));

            isMainMenuScene = SceneManager.GetActiveScene().name == "Mainmenu";
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private IEnumerator Start()
        {
            // The bootstrap lives in Mainmenu. Wait for the persistent store manager
            // so initialization order between scene objects cannot race.
            while (MN7StoreManager.Instance == null)
                yield return null;

            MN7StoreManager store = MN7StoreManager.Instance;

            if (store.IsReady)
            {
                Initialize(store);
                yield break;
            }

            store.OnStoreInitialized -= HandleStoreInitialized;
            store.OnStoreInitialized += HandleStoreInitialized;
            store.OnStoreInitializationFailed -= HandleStoreInitializationFailed;
            store.OnStoreInitializationFailed += HandleStoreInitializationFailed;
            subscribedToStoreEvents = true;
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;

            MN7StoreManager store = MN7StoreManager.Instance;
            if (store != null && subscribedToStoreEvents)
            {
                store.OnStoreInitialized -= HandleStoreInitialized;
                store.OnStoreInitializationFailed -= HandleStoreInitializationFailed;
            }

            provider?.Dispose();
            Instance = null;
        }

        private void HandleStoreInitialized()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store != null)
                Initialize(store);
        }

        private void HandleStoreInitializationFailed()
        {
            Debug.LogError("[MN7 AdMob] Store initialization failed. Ads were not initialized.");
        }

        private void Initialize(MN7StoreManager store)
        {
            if (initializationStarted || store == null || !store.IsReady)
                return;

            initializationStarted = true;
            store.OnStoreInitialized -= HandleStoreInitialized;
            store.OnStoreInitializationFailed -= HandleStoreInitializationFailed;
            subscribedToStoreEvents = false;

#if UNITY_ANDROID || UNITY_IOS
            if (!ValidateConfig(store, adMobConfig))
            {
                Debug.LogError("[MN7 AdMob] Configuration validation failed. No ads will be requested.");
                return;
            }

            provider = new MN7AdMobProvider(adMobConfig);
            provider.OnInterstitialLoaded += HandleInterstitialLoaded;

            if (store.Ads == null)
            {
                Debug.LogError("[MN7 AdMob] MN7 Ad Manager is missing.");
                provider.Dispose();
                provider = null;
                return;
            }

            store.Ads.SetProvider(provider);
            provider.Initialize();
            UpdateBannerVisibility();
#else
            Debug.Log("[MN7 AdMob] Ads are configured for Android/iOS only.");
#endif
        }

        private static bool ValidateConfig(MN7StoreManager store, MN7AdMobConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[MN7 AdMob] MN7AdMobConfig asset is not assigned.");
                return false;
            }

            if (config.interstitialEveryCompletedMatches < 1)
            {
                Debug.LogError("[MN7 AdMob] Interstitial frequency must be at least one completed match.");
                return false;
            }

#if UNITY_ANDROID
            if (string.IsNullOrWhiteSpace(config.androidBannerAdUnitId) ||
                string.IsNullOrWhiteSpace(config.androidInterstitialAdUnitId))
            {
                Debug.LogError("[MN7 AdMob] Android banner or interstitial Ad Unit ID is missing.");
                return false;
            }
#elif UNITY_IOS
            if (string.IsNullOrWhiteSpace(config.iosBannerAdUnitId) ||
                string.IsNullOrWhiteSpace(config.iosInterstitialAdUnitId))
            {
                Debug.LogError("[MN7 AdMob] iOS banner or interstitial Ad Unit ID is missing.");
                return false;
            }
#endif

            if (config.rewardedPlacements == null || config.rewardedPlacements.Count == 0)
            {
                Debug.LogError("[MN7 AdMob] No rewarded placements are configured.");
                return false;
            }

            HashSet<string> placementIds = new(StringComparer.OrdinalIgnoreCase);

            foreach (MN7AdMobPlacement placement in config.rewardedPlacements)
            {
                if (placement == null)
                {
                    Debug.LogError("[MN7 AdMob] Configuration contains a null rewarded placement.");
                    return false;
                }

                string placementId = placement.placementId?.Trim();
                if (string.IsNullOrWhiteSpace(placementId) || !placementIds.Add(placementId))
                {
                    Debug.LogError($"[MN7 AdMob] Empty or duplicate rewarded placement ID: '{placement.placementId}'.");
                    return false;
                }

#if UNITY_ANDROID
                if (string.IsNullOrWhiteSpace(placement.androidAdUnitId))
                {
                    Debug.LogError($"[MN7 AdMob] Android rewarded Ad Unit ID is missing for '{placementId}'.");
                    return false;
                }
#elif UNITY_IOS
                if (string.IsNullOrWhiteSpace(placement.iosAdUnitId))
                {
                    Debug.LogError($"[MN7 AdMob] iOS rewarded Ad Unit ID is missing for '{placementId}'.");
                    return false;
                }
#endif
            }

            // Every rewarded-ad store item (including each skin/board) must point
            // to one of the placements above. Its requiredAds value remains item-specific.
            if (store.Config?.items != null)
            {
                foreach (MN7ItemData item in store.Config.items)
                {
                    if (item == null || item.purchaseType != MN7PurchaseType.RewardedAd)
                        continue;

                    string placementId = item.adPlacementId?.Trim();
                    if (string.IsNullOrWhiteSpace(placementId) || !placementIds.Contains(placementId))
                    {
                        Debug.LogError(
                            $"[MN7 AdMob] Store item '{item.itemId}' references missing rewarded placement '{placementId}'.");
                        return false;
                    }

                    if (item.requiredAds < 1)
                    {
                        Debug.LogError($"[MN7 AdMob] Store item '{item.itemId}' must require at least one rewarded ad.");
                        return false;
                    }
                }
            }

            return true;
        }

        //==================================================
        // SCENE / BANNER VISIBILITY
        //==================================================

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isMainMenuScene = string.Equals(scene.name, "Mainmenu", StringComparison.OrdinalIgnoreCase);
            isResultScreenVisible = false;
            UpdateBannerVisibility();
        }

        public void OnResultScreenShown()
        {
            isResultScreenVisible = true;
            TryShowPendingInterstitial();
            UpdateBannerVisibility();
        }

        public void OnResultScreenHidden()
        {
            isResultScreenVisible = false;
            UpdateBannerVisibility();
        }

        private void UpdateBannerVisibility()
        {
            bool shouldShowBanner =
                (isMainMenuScene || isResultScreenVisible) &&
                !isInterstitialShowing;

            provider?.SetBannerVisible(shouldShowBanner);
        }

        //==================================================
        // MATCH FREQUENCY
        //==================================================

        /// <summary>
        /// Called once by GameResultManager for each accepted win or draw.
        /// The count persists across scenes and app restarts.
        /// </summary>
        public void RegisterCompletedMatch()
        {
            completedMatchesSinceInterstitial = Mathf.Min(
                int.MaxValue,
                completedMatchesSinceInterstitial + 1);

            PlayerPrefs.SetInt(CompletedMatchCounterKey, completedMatchesSinceInterstitial);
            PlayerPrefs.Save();

            Debug.Log(
                $"[MN7 AdMob] Completed matches since last interstitial: {completedMatchesSinceInterstitial}");
        }

        private void HandleInterstitialLoaded()
        {
            // If an interstitial became ready while a result screen is still open,
            // show it there rather than interrupting active gameplay.
            TryShowPendingInterstitial();
        }

        private void TryShowPendingInterstitial()
        {
            if (!isResultScreenVisible || isInterstitialShowing || provider == null)
                return;

            int frequency = Mathf.Max(1, adMobConfig.interstitialEveryCompletedMatches);
            if (completedMatchesSinceInterstitial < frequency)
                return;

            bool started = provider.TryShowInterstitial(HandleInterstitialFinished);
            if (!started)
            {
                // Keep the match count pending. It is retried only when a later
                // result screen is shown or an interstitial finishes loading.
                return;
            }

            isInterstitialShowing = true;
            completedMatchesSinceInterstitial = 0;
            PlayerPrefs.SetInt(CompletedMatchCounterKey, 0);
            PlayerPrefs.Save();

            UpdateBannerVisibility();
            Debug.Log($"[MN7 AdMob] Interstitial shown after {frequency} completed matches.");
        }

        private void HandleInterstitialFinished()
        {
            isInterstitialShowing = false;
            UpdateBannerVisibility();
        }
    }
}
