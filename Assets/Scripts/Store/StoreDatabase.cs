using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "StoreDatabase",
    menuName = "Shift Four/Store Database"
)]
public class StoreDatabase : ScriptableObject
{
    [Header("All Store Items")]
    public List<StoreItemData> items = new List<StoreItemData>();

    public StoreItemData GetItemByID(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
            return null;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                continue;

            if (items[i].itemID == itemID)
                return items[i];
        }

        return null;
    }

    public List<StoreItemData> GetItemsByType(StoreItemType type)
    {
        List<StoreItemData> result = new List<StoreItemData>();

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
                continue;

            if (items[i].itemType == type)
                result.Add(items[i]);
        }

        return result;
    }
}