using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Runtime lookup catalog built from MN7StoreConfig.
    /// Provides centralized item lookup and preserves the config order.
    /// </summary>
    public sealed class MN7StoreCatalog
    {
        private readonly Dictionary<string, MN7ItemData> items = new();
        private readonly List<MN7ItemData> orderedItems = new();

        public MN7StoreCatalog(MN7StoreConfig config)
        {
            if (config == null || config.items == null)
                return;

            for (int i = 0; i < config.items.Count; i++)
            {
                MN7ItemData item = config.items[i];

                if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                    continue;

                string itemId = item.itemId.Trim();

                if (items.ContainsKey(itemId))
                    continue;

                items.Add(itemId, item);
                orderedItems.Add(item);
            }
        }

        public bool Contains(string itemId)
        {
            return !string.IsNullOrWhiteSpace(itemId) &&
                   items.ContainsKey(itemId.Trim());
        }

        public MN7ItemData GetItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            items.TryGetValue(
                itemId.Trim(),
                out MN7ItemData item
            );

            return item;
        }

        public IReadOnlyList<MN7ItemData> GetItems()
        {
            return orderedItems;
        }
    }
}
