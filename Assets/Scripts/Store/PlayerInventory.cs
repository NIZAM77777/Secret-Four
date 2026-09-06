using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private const string OwnedItemsKey = "SF_OwnedItems";

    private const string SelectedPlayerXCoinKey = "SF_SelectedPlayerXCoin";
    private const string SelectedPlayerYCoinKey = "SF_SelectedPlayerYCoin";
    private const string SelectedBoardKey = "SF_SelectedBoard";

    private HashSet<string> ownedItems = new HashSet<string>();

    public string SelectedPlayerXCoinID { get; private set; }
    public string SelectedPlayerYCoinID { get; private set; }
    public string SelectedBoardID { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        LoadInventory();
    }

    public void InitializeDefaults(StoreDatabase database)
    {
        if (database == null)
        {
            Debug.LogError("PlayerInventory: StoreDatabase is missing.");
            return;
        }

        bool changed = false;

        for (int i = 0; i < database.items.Count; i++)
        {
            StoreItemData item = database.items[i];

            if (item == null)
                continue;

            if (!item.unlockedByDefault)
                continue;

            if (!ownedItems.Contains(item.itemID))
            {
                ownedItems.Add(item.itemID);
                changed = true;
            }
        }

        if (string.IsNullOrEmpty(SelectedPlayerXCoinID))
        {
            StoreItemData defaultX = FindFirstDefaultItem(
                database,
                StoreItemType.Coin
            );

            if (defaultX != null)
            {
                SelectedPlayerXCoinID = defaultX.itemID;
                changed = true;
            }
        }

        if (string.IsNullOrEmpty(SelectedPlayerYCoinID))
        {
            StoreItemData defaultY = FindFirstDefaultItem(
                database,
                StoreItemType.Coin
            );

            if (defaultY != null)
            {
                SelectedPlayerYCoinID = defaultY.itemID;
                changed = true;
            }
        }

        if (string.IsNullOrEmpty(SelectedBoardID))
        {
            StoreItemData defaultBoard = FindFirstDefaultItem(
                database,
                StoreItemType.Board
            );

            if (defaultBoard != null)
            {
                SelectedBoardID = defaultBoard.itemID;
                changed = true;
            }
        }

        if (changed)
            SaveInventory();
    }

    private StoreItemData FindFirstDefaultItem(
        StoreDatabase database,
        StoreItemType type
    )
    {
        for (int i = 0; i < database.items.Count; i++)
        {
            StoreItemData item = database.items[i];

            if (item == null)
                continue;

            if (item.itemType != type)
                continue;

            if (!item.unlockedByDefault)
                continue;

            return item;
        }

        return null;
    }

    public bool IsOwned(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
            return false;

        return ownedItems.Contains(itemID);
    }

    public void UnlockItem(string itemID)
    {
        if (string.IsNullOrEmpty(itemID))
            return;

        if (ownedItems.Add(itemID))
        {
            SaveInventory();
        }
    }

    public bool EquipCoinForPlayerX(string itemID)
    {
        if (!IsOwned(itemID))
            return false;

        SelectedPlayerXCoinID = itemID;

        SaveInventory();

        return true;
    }

    public bool EquipCoinForPlayerY(string itemID)
    {
        if (!IsOwned(itemID))
            return false;

        SelectedPlayerYCoinID = itemID;

        SaveInventory();

        return true;
    }

    public bool EquipBoard(string itemID)
    {
        if (!IsOwned(itemID))
            return false;

        SelectedBoardID = itemID;

        SaveInventory();

        return true;
    }

    private void SaveInventory()
    {
        PlayerPrefs.SetString(
            OwnedItemsKey,
            string.Join(",", ownedItems)
        );

        PlayerPrefs.SetString(
            SelectedPlayerXCoinKey,
            SelectedPlayerXCoinID ?? ""
        );

        PlayerPrefs.SetString(
            SelectedPlayerYCoinKey,
            SelectedPlayerYCoinID ?? ""
        );

        PlayerPrefs.SetString(
            SelectedBoardKey,
            SelectedBoardID ?? ""
        );

        PlayerPrefs.Save();
    }

    private void LoadInventory()
    {
        ownedItems.Clear();

        string ownedData = PlayerPrefs.GetString(
            OwnedItemsKey,
            ""
        );

        if (!string.IsNullOrEmpty(ownedData))
        {
            string[] ids = ownedData.Split(',');

            for (int i = 0; i < ids.Length; i++)
            {
                if (!string.IsNullOrEmpty(ids[i]))
                    ownedItems.Add(ids[i]);
            }
        }

        SelectedPlayerXCoinID = PlayerPrefs.GetString(
            SelectedPlayerXCoinKey,
            ""
        );

        SelectedPlayerYCoinID = PlayerPrefs.GetString(
            SelectedPlayerYCoinKey,
            ""
        );

        SelectedBoardID = PlayerPrefs.GetString(
            SelectedBoardKey,
            ""
        );
    }

    public void ResetInventory()
    {
        ownedItems.Clear();

        SelectedPlayerXCoinID = "";
        SelectedPlayerYCoinID = "";
        SelectedBoardID = "";

        PlayerPrefs.DeleteKey(OwnedItemsKey);
        PlayerPrefs.DeleteKey(SelectedPlayerXCoinKey);
        PlayerPrefs.DeleteKey(SelectedPlayerYCoinKey);
        PlayerPrefs.DeleteKey(SelectedBoardKey);

        PlayerPrefs.Save();
    }
}