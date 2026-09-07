using System;

namespace MN7.Store
{
    public interface IMN7AdProvider
    {
        bool IsRewardedAdReady(string placementId);

        void ShowRewardedAd(
            string placementId,
            Action<MN7AdResult> onCompleted
        );
    }
}