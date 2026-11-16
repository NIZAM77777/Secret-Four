using System;
using UnityEngine;

namespace MN7.Store
{
    public class MN7AdManager
    {
        private IMN7AdProvider provider;

        public void SetProvider(IMN7AdProvider adProvider)
        {
            provider = adProvider;
            Debug.Log("[MN7 Store] Ad provider registered.");
        }

        public bool IsRewardedAdReady(string placementId)
        {
            if (provider == null)
            {
                Debug.LogWarning("[MN7 Store] No ad provider registered.");
                return false;
            }

            return provider.IsRewardedAdReady(placementId);
        }

        public void ShowRewardedAd(
            string placementId,
            Action<MN7AdResult> onCompleted)
        {
            if (provider == null)
            {
                Debug.LogWarning("[MN7 Store] No ad provider registered.");
                onCompleted?.Invoke(MN7AdResult.NotReady);
                return;
            }

            provider.ShowRewardedAd(placementId, onCompleted);
        }

        public void SetBannerVisible(bool visible)
        {
            provider?.SetBannerVisible(visible);
        }

        public bool TryShowInterstitial(Action onFinished = null)
        {
            if (provider == null)
            {
                Debug.LogWarning("[MN7 Store] No ad provider registered for interstitial.");
                return false;
            }

            return provider.TryShowInterstitial(onFinished);
        }
    }
}
