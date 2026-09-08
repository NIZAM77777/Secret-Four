using UnityEngine;

namespace MN7.Store
{
    public class MN7AdMobBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField]
        private MN7AdMobConfig adMobConfig;

        private MN7AdMobProvider provider;

        //==================================================
        // UNITY
        //==================================================

        private void Start()
        {
            if (adMobConfig == null)
            {
                Debug.LogError(
                    "[MN7 Store] AdMob Config is not assigned."
                );

                return;
            }

            if (MN7StoreManager.Instance == null)
            {
                Debug.LogError(
                    "[MN7 Store] Store Manager not found."
                );

                return;
            }

            provider =
                new MN7AdMobProvider(
                    adMobConfig
                );

            RegisterProvider();

            provider.Initialize();
        }

        //==================================================
        // REGISTER
        //==================================================

        private void RegisterProvider()
        {
            if (MN7StoreManager.Instance == null)
            {
                Debug.LogError(
                    "[MN7 Store] Store Manager not found."
                );

                return;
            }

            if (MN7StoreManager.Instance.Ads == null)
            {
                Debug.LogError(
                    "[MN7 Store] Ad Manager not found."
                );

                return;
            }

            MN7StoreManager.Instance.Ads
                .SetProvider(provider);

            Debug.Log(
                "[MN7 Store] AdMob provider registered."
            );
        }
    }
}