using UnityEngine;
using MN7.Store;

/// <summary>
/// Game-specific bridge between the reusable MN7 Store and Secret Four gameplay visuals.
/// The Store remains reusable; this class only defines how Secret Four consumes the selected item.
/// </summary>
public static class SecretFourStoreIntegration
{
    public const string PlayerSelectionGroup = "PLAYER";

    public static string GetSelectedPlayerItemId()
    {
        return MN7StoreManager.Instance?.Selection?.GetSelectedItemId(PlayerSelectionGroup);
    }

    public static Sprite GetSelectedPlayerSprite()
    {
        return GetSpriteForItem(GetSelectedPlayerItemId());
    }

    public static Sprite GetSpriteForItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        MN7StoreManager store = MN7StoreManager.Instance;
        MN7ItemData item = store?.Catalog?.GetItem(itemId);

        if (item == null ||
            !item.selectable ||
            !string.Equals(item.selectionGroupId?.Trim(), PlayerSelectionGroup, System.StringComparison.OrdinalIgnoreCase))
            return null;

        return item.icon;
    }
}