using UnityEngine;

[CreateAssetMenu(
    fileName = "StoreItem",
    menuName = "Shift Four/Store Item"
)]
public class StoreItemData : ScriptableObject
{
    [Header("Basic Information")]
    public string itemID;
    public string itemName;

    public StoreItemType itemType;

    [Header("Visual")]
    public Sprite icon;

    [Header("Unlock")]
    [Min(0)]
    public int adsRequired = 0;

    [Header("Default")]
    public bool unlockedByDefault = false;
}