using UnityEngine;

public class StoreManager : MonoBehaviour
{
    public static StoreManager Instance { get; private set; }

    [Header("Store Data")]
    [SerializeField] private StoreDatabase database;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (database == null)
        {
            Debug.LogError("StoreManager: StoreDatabase is not assigned.");
            return;
        }

        if (PlayerInventory.Instance == null)
        {
            Debug.LogError("StoreManager: PlayerInventory is missing.");
            return;
        }

        PlayerInventory.Instance.InitializeDefaults(database);
    }

    public StoreItemData GetItem(string itemID)
    {
        if (database == null)
            return null;

        return database.GetItemByID(itemID);
    }

    public bool IsOwned(string itemID)
    {
        if (PlayerInventory.Instance == null)
            return false;

        return PlayerInventory.Instance.IsOwned(itemID);
    }

    public bool UnlockItem(string itemID)
    {
        StoreItemData item = GetItem(itemID);

        if (item == null)
        {
            Debug.LogWarning(
                "StoreManager: Item not found: " + itemID
            );

            return false;
        }

        PlayerInventory.Instance.UnlockItem(itemID);

        return true;
    }

    public bool EquipCoinForPlayerX(string itemID)
    {
        return PlayerInventory.Instance.EquipCoinForPlayerX(itemID);
    }

    public bool EquipCoinForPlayerY(string itemID)
    {
        return PlayerInventory.Instance.EquipCoinForPlayerY(itemID);
    }

    public bool EquipBoard(string itemID)
    {
        return PlayerInventory.Instance.EquipBoard(itemID);
    }

    public string GetSelectedPlayerXCoinID()
    {
        return PlayerInventory.Instance.SelectedPlayerXCoinID;
    }

    public string GetSelectedPlayerYCoinID()
    {
        return PlayerInventory.Instance.SelectedPlayerYCoinID;
    }

    public string GetSelectedBoardID()
    {
        return PlayerInventory.Instance.SelectedBoardID;
    }
}