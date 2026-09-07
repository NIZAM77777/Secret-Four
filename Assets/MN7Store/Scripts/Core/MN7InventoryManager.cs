using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7InventoryManager
    {
        [Serializable]
        private class InventorySaveData
        {
            public List<string> ownedItems = new();
        }

        private readonly HashSet<string> ownedItems = new();

        private readonly MN7StoreConfig config;
        private readonly MN7SaveManager saveManager;

        public event Action<string> OnItemAdded;
        public event Action<string> OnItemRemoved;

        public MN7InventoryManager(
            MN7StoreConfig config,
            MN7SaveManager saveManager)
        {
            this.config = config;
            this.saveManager = saveManager;

            Load();
        }

        //==================================================
        // OWNERSHIP
        //==================================================

        public bool IsOwned(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            return ownedItems.Contains(itemId);
        }

        //==================================================
        // ADD ITEM
        //==================================================

        public bool AddItem(string itemId)
        {
            if (!IsValidItem(itemId))
            {
                Debug.LogWarning(
                    $"[MN7 Store] Cannot add unknown item: {itemId}"
                );

                return false;
            }

            if (ownedItems.Contains(itemId))
            {
                Debug.Log(
                    $"[MN7 Store] Item already owned: {itemId}"
                );

                return false;
            }

            ownedItems.Add(itemId);

            Save();

            OnItemAdded?.Invoke(itemId);

            Debug.Log(
                $"[MN7 Store] Item added to inventory: {itemId}"
            );

            return true;
        }

        //==================================================
        // REMOVE ITEM
        //==================================================

        public bool RemoveItem(string itemId)
        {
            if (!ownedItems.Contains(itemId))
                return false;

            ownedItems.Remove(itemId);

            Save();

            OnItemRemoved?.Invoke(itemId);

            Debug.Log(
                $"[MN7 Store] Item removed from inventory: {itemId}"
            );

            return true;
        }

        //==================================================
        // VALIDATION
        //==================================================

        private bool IsValidItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            foreach (MN7ItemData item in config.items)
            {
                if (item == null)
                    continue;

                if (item.itemId == itemId)
                    return true;
            }

            return false;
        }

        //==================================================
        // GET ITEM
        //==================================================

        public MN7ItemData GetItem(string itemId)
        {
            foreach (MN7ItemData item in config.items)
            {
                if (item == null)
                    continue;

                if (item.itemId == itemId)
                    return item;
            }

            return null;
        }

        //==================================================
        // SAVE
        //==================================================

        private void Save()
        {
            InventorySaveData saveData = new();

            foreach (string itemId in ownedItems)
            {
                saveData.ownedItems.Add(itemId);
            }

            string json =
                JsonUtility.ToJson(saveData);

            saveManager.Save(
                GetInventorySaveKey(),
                json
            );
        }

        //==================================================
        // LOAD
        //==================================================

        private void Load()
        {
            string json =
                saveManager.Load(
                    GetInventorySaveKey()
                );

            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                InventorySaveData saveData =
                    JsonUtility.FromJson<InventorySaveData>(
                        json
                    );

                if (saveData == null ||
                    saveData.ownedItems == null)
                {
                    return;
                }

                foreach (string itemId in saveData.ownedItems)
                {
                    if (IsValidItem(itemId))
                    {
                        ownedItems.Add(itemId);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[MN7 Store] Failed to load inventory.\n{exception}"
                );
            }
        }

        //==================================================
        // SAVE KEY
        //==================================================

        private string GetInventorySaveKey()
        {
            return "MN7_STORE_INVENTORY";
        }

        //==================================================
        // RESET
        //==================================================

        public void ClearInventory()
        {
            ownedItems.Clear();

            Save();
        }

        //==================================================
        // DEBUG
        //==================================================

        public void DebugPrintInventory()
        {
            if (ownedItems.Count == 0)
            {
                Debug.Log(
                    "[MN7 Store] Inventory is empty."
                );

                return;
            }

            foreach (string itemId in ownedItems)
            {
                Debug.Log(
                    $"[MN7 Store] Owned: {itemId}"
                );
            }
        }
    }
}