using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7InventoryManager
    {
        [Serializable]
        private class InventoryEntry
        {
            public string itemId;
            public int quantity;
        }

        [Serializable]
        private class InventorySaveData
        {
            public List<InventoryEntry> items = new();
        }

        private readonly Dictionary<string, int> itemQuantities =
            new();

        private readonly MN7StoreConfig config;
        private readonly MN7SaveManager saveManager;

        public event Action<string> OnItemAdded;
        public event Action<string> OnItemRemoved;
        public event Action<string, int> OnItemQuantityChanged;

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

            MN7ItemData item =
                GetItem(itemId);

            if (item == null)
                return false;

            if (item.ownershipType ==
                MN7ItemOwnershipType.Permanent)
            {
                return GetQuantity(itemId) > 0;
            }

            return GetQuantity(itemId) > 0;
        }

        //==================================================
        // QUANTITY
        //==================================================

        public int GetQuantity(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return 0;

            return itemQuantities.TryGetValue(
                itemId,
                out int quantity)
                ? quantity
                : 0;
        }

        //==================================================
        // CAN ADD
        //==================================================

        public bool CanAddItem(
            string itemId,
            int quantity = 1)
        {
            if (!IsValidItem(itemId))
                return false;

            if (quantity <= 0)
                return false;

            MN7ItemData item =
                GetItem(itemId);

            if (item == null)
                return false;

            int current =
                GetQuantity(itemId);

            // Permanent items can only exist once.
            if (item.ownershipType ==
                MN7ItemOwnershipType.Permanent)
            {
                if (current > 0)
                    return false;

                return quantity == 1;
            }

            long result =
                (long)current + quantity;

            return result <= int.MaxValue;
        }

        //==================================================
        // ADD ITEM
        //==================================================

        public bool AddItem(
            string itemId,
            int quantity = 1)
        {
            if (!CanAddItem(
                    itemId,
                    quantity))
            {
                Debug.LogWarning(
                    $"[MN7 Store] Cannot add item: " +
                    $"{itemId} x{quantity}"
                );

                return false;
            }

            MN7ItemData item =
                GetItem(itemId);

            int oldQuantity =
                GetQuantity(itemId);

            int newQuantity =
                oldQuantity + quantity;

            itemQuantities[itemId] =
                newQuantity;

            Save();

            OnItemQuantityChanged?.Invoke(
                itemId,
                newQuantity
            );

            if (oldQuantity == 0)
            {
                OnItemAdded?.Invoke(
                    itemId
                );
            }

            Debug.Log(
                $"[MN7 Store] Item added: " +
                $"{item.displayName} x{quantity}"
            );

            return true;
        }

        //==================================================
        // REMOVE ITEM
        //==================================================

        public bool RemoveItem(
            string itemId,
            int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            if (quantity <= 0)
                return false;

            int current =
                GetQuantity(itemId);

            if (current < quantity)
                return false;

            int newQuantity =
                current - quantity;

            if (newQuantity <= 0)
            {
                itemQuantities.Remove(
                    itemId
                );

                newQuantity = 0;

                OnItemRemoved?.Invoke(
                    itemId
                );
            }
            else
            {
                itemQuantities[itemId] =
                    newQuantity;
            }

            Save();

            OnItemQuantityChanged?.Invoke(
                itemId,
                newQuantity
            );

            Debug.Log(
                $"[MN7 Store] Item removed: " +
                $"{itemId} x{quantity}"
            );

            return true;
        }

        //==================================================
        // VALIDATION
        //==================================================

        private bool IsValidItem(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            foreach (
                MN7ItemData item
                in config.items)
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

        public MN7ItemData GetItem(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            foreach (
                MN7ItemData item
                in config.items)
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
            InventorySaveData saveData =
                new();

            foreach (
                var pair
                in itemQuantities)
            {
                if (pair.Value <= 0)
                    continue;

                saveData.items.Add(
                    new InventoryEntry
                    {
                        itemId = pair.Key,
                        quantity = pair.Value
                    }
                );
            }

            string json =
                JsonUtility.ToJson(
                    saveData
                );

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
            itemQuantities.Clear();

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
                    saveData.items == null)
                {
                    return;
                }

                foreach (
                    InventoryEntry entry
                    in saveData.items)
                {
                    if (entry == null)
                        continue;

                    if (!IsValidItem(
                            entry.itemId))
                    {
                        continue;
                    }

                    if (entry.quantity <= 0)
                        continue;

                    MN7ItemData item =
                        GetItem(entry.itemId);

                    if (item == null)
                        continue;

                    if (item.ownershipType ==
                        MN7ItemOwnershipType.Permanent)
                    {
                        itemQuantities[
                            entry.itemId] = 1;
                    }
                    else
                    {
                        itemQuantities[
                            entry.itemId] =
                                entry.quantity;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[MN7 Store] Failed to load inventory.\n" +
                    $"{exception}"
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
            itemQuantities.Clear();

            Save();
        }

        //==================================================
        // DEBUG
        //==================================================

        public void DebugPrintInventory()
        {
            if (itemQuantities.Count == 0)
            {
                Debug.Log(
                    "[MN7 Store] Inventory is empty."
                );

                return;
            }

            foreach (
                var pair
                in itemQuantities)
            {
                Debug.Log(
                    $"[MN7 Store] " +
                    $"{pair.Key} x{pair.Value}"
                );
            }
        }
    }
}