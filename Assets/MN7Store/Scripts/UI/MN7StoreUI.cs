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

        [Header("Items")]
        [SerializeField]
        private List<MN7ItemData> items = new();

        private readonly List<MN7StoreItemUI> spawnedItems =
            new();

        //==================================================
        // UNITY
        //==================================================

        private void Start()
        {
            BuildStore();
        }

        //==================================================
        // BUILD
        //==================================================

        public void BuildStore()
        {
            ClearStore();

            foreach (MN7ItemData item in items)
            {
                if (item == null)
                    continue;

                if (itemPrefab == null)
                    continue;

                MN7StoreItemUI itemUI =
                    Instantiate(
                        itemPrefab,
                        itemContainer
                    );

                itemUI.SetItem(item);

                spawnedItems.Add(
                    itemUI
                );
            }
        }

        //==================================================
        // CLEAR
        //==================================================

        private void ClearStore()
        {
            foreach (
                MN7StoreItemUI itemUI
                in spawnedItems)
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
            foreach (
                MN7StoreItemUI itemUI
                in spawnedItems)
            {
                if (itemUI != null)
                    itemUI.Refresh();
            }
        }
    }
}