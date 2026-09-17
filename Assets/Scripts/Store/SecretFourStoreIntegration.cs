using System;
using UnityEngine;
using MN7.Store;

/// <summary>
/// Game-specific bridge between the reusable MN7 Store and Secret Four gameplay visuals.
/// Player skins and board skins use independent selection groups.
/// </summary>
public static class SecretFourStoreIntegration
{
    public const string PlayerXSelectionGroup = "PLAYER";
    public const string PlayerYSelectionGroup = "OPPOSITE";
    public const string BoardSelectionGroup = "BOARD";

    // Compatibility alias for older code that used one generic player group.
    public const string PlayerSelectionGroup = PlayerXSelectionGroup;

    public static string GetSelectionGroup(PlayerType player)
    {
        return player == PlayerType.X ? PlayerXSelectionGroup : PlayerYSelectionGroup;
    }

    public static string GetSelectedPlayerItemId(PlayerType player)
    {
        MN7StoreManager store = MN7StoreManager.Instance;
        if (store?.Selection == null)
            return null;

        return store.Selection.GetSelectedItemId(GetSelectionGroup(player));
    }

    public static string GetSelectedPlayerItemId()
    {
        return GetSelectedPlayerItemId(PlayerType.X);
    }

    public static string GetSelectedBoardItemId()
    {
        MN7StoreManager store = MN7StoreManager.Instance;
        if (store?.Selection == null)
            return null;

        return store.Selection.GetSelectedItemId(BoardSelectionGroup);
    }

    public static Sprite GetSelectedBoardSprite()
    {
        return GetSpriteForItem(GetSelectedBoardItemId());
    }

    public static Sprite GetSpriteForItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return null;

        MN7StoreManager store = MN7StoreManager.Instance;
        MN7ItemData item = store?.Catalog?.GetItem(itemId);

        return item?.icon;
    }

    public static Sprite GetSelectedPlayerSprite(PlayerType player)
    {
        return GetSpriteForItem(GetSelectedPlayerItemId(player));
    }

    public static Sprite GetSelectedPlayerSprite()
    {
        return GetSelectedPlayerSprite(PlayerType.X);
    }

    public static bool IsValidPlayerSkin(string itemId, PlayerType player)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return true;

        MN7StoreManager store = MN7StoreManager.Instance;
        MN7ItemData item = store?.Catalog?.GetItem(itemId);
        if (item == null || !item.selectable)
            return false;

        return string.Equals(
            item.selectionGroupId?.Trim(),
            GetSelectionGroup(player),
            StringComparison.OrdinalIgnoreCase);
    }
}