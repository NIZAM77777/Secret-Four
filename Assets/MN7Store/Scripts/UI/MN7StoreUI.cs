using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7StoreUI : MonoBehaviour
    {
        [Header("Store")]
        [SerializeField]
        private Transform itemContainer;

        [SerializeField]
        private MN7StoreItemUI itemPrefab;

        private readonly List<MN7StoreItemUI> spawnedItems = new();

        private void OnEnable()
        {
            MN7StoreManager store = MN7StoreManager.Instance;

            if (store == null)
                return;

            if (store.IsReady)
            {
                BuildStore();
                return;
            }

            store.OnStoreInitialized -= HandleStoreInitialized;
            store.OnStoreInitialized += HandleStoreInitialized;
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            MN7StoreManager store = MN7StoreManager.Instance;

            if (store != null)
                store.OnStoreInitialized -= HandleStoreInitialized;
        }

        private void HandleStoreInitialized()
        {
            BuildStore();
        }

        //==================================================
        // BUILD
        //==================================================

        public void BuildStore()
        {
            ClearStore();

            MN7StoreManager store = MN7StoreManager.Instance;

            if (store == null || !store.IsReady)
            {
                Debug.LogWarning(
                    "[MN7 Store] Cannot build Store UI before the store is ready."
                );
                return;
            }

            if (itemContainer == null)
            {
                Debug.LogError(
                    "[MN7 Store] Store UI item container is not assigned."
                );
                return;
            }

            if (itemPrefab == null)
            {
                Debug.LogError(
                    "[MN7 Store] Store UI item prefab is not assigned."
                );
                return;
            }

            MN7StoreCatalog catalog = store.Catalog;

            if (catalog == null)
            {
                Debug.LogError(
                    "[MN7 Store] Store catalog is not initialized."
                );
                return;
            }

            IReadOnlyCollection<MN7ItemData> items = catalog.GetItems();

            foreach (MN7ItemData item in items)
            {
                if (item == null)
                    continue;

                MN7StoreItemUI itemUI = Instantiate(
                    itemPrefab,
                    itemContainer
                );

                itemUI.SetItem(item);
                spawnedItems.Add(itemUI);
            }
        }

        //==================================================
        // CLEAR
        //==================================================

        private void ClearStore()
        {
            foreach (MN7StoreItemUI itemUI in spawnedItems)
            {
                if (itemUI != null)
                    Destroy(itemUI.gameObject);
            }

            spawnedItems.Clear();
        }

        //==================================================
        // REFRESH
        //==================================================

        public void RefreshStore()
        {
            for (int i = 0; i < spawnedItems.Count; i++)
            {
                if (spawnedItems[i] != null)
                    spawnedItems[i].Refresh();
            }
        }
    }
}
