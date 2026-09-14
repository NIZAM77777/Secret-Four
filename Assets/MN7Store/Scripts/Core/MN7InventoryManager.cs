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
            public List<string> completedRewardOperations = new();
        }

        [Serializable]
        private class InventorySaveEnvelope
        {
            public int version = MN7StoreSaveVersion.CurrentVersion;
            public InventorySaveData data;
        }

        private const string InventorySuffix = "_INVENTORY";
        private readonly Dictionary<string, int> itemQuantities = new();
        private readonly HashSet<string> completedRewardOperations = new();
        private readonly MN7StoreConfig config;
        private readonly MN7StoreCatalog catalog;
        private readonly MN7SaveManager saveManager;
        private readonly string saveKey;

        public event Action<string> OnItemAdded;
        public event Action<string> OnItemRemoved;
        public event Action<string, int> OnItemQuantityChanged;

        public MN7InventoryManager(
            MN7StoreConfig config,
            MN7SaveManager saveManager,
            MN7StoreCatalog catalog)
        {
            this.config = config;
            this.catalog = catalog;
            this.saveManager = saveManager;
            saveKey = $"{config.GetEffectiveSaveKey()}{InventorySuffix}";
            Load();
            SeedDefaultOwnedItems();
        }

        public bool IsOwned(string itemId) => GetQuantity(itemId) > 0 && GetItem(itemId) != null;

        public int GetQuantity(string itemId) =>
            !string.IsNullOrWhiteSpace(itemId) && itemQuantities.TryGetValue(itemId.Trim(), out int quantity)
                ? quantity
                : 0;

        public bool CanAddItem(string itemId, int quantity = 1)
        {
            if (!IsValidItem(itemId) || quantity <= 0)
                return false;

            MN7ItemData item = GetItem(itemId);
            int current = GetQuantity(itemId);

            if (item.ownershipType == MN7ItemOwnershipType.Permanent)
                return current == 0 && quantity == 1;

            return (long)current + quantity <= int.MaxValue;
        }

        public bool AddItem(string itemId, int quantity = 1, string operationId = null)
        {
            itemId = itemId?.Trim();

            if (!string.IsNullOrWhiteSpace(operationId) && completedRewardOperations.Contains(operationId))
                return true;

            if (!CanAddItem(itemId, quantity))
                return false;

            MN7ItemData item = GetItem(itemId);
            int oldQuantity = GetQuantity(itemId);
            int newQuantity = oldQuantity + quantity;
            itemQuantities[itemId] = newQuantity;

            if (!string.IsNullOrWhiteSpace(operationId))
                completedRewardOperations.Add(operationId);

            Save();
            OnItemQuantityChanged?.Invoke(itemId, newQuantity);

            if (oldQuantity == 0)
                OnItemAdded?.Invoke(itemId);

            Debug.Log($"[MN7 Store] Item added: {item.displayName} x{quantity}");
            return true;
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            itemId = itemId?.Trim();
            if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
                return false;

            int current = GetQuantity(itemId);
            if (current < quantity)
                return false;

            int newQuantity = current - quantity;
            if (newQuantity <= 0)
            {
                itemQuantities.Remove(itemId);
                newQuantity = 0;
                OnItemRemoved?.Invoke(itemId);
            }
            else
            {
                itemQuantities[itemId] = newQuantity;
            }

            Save();
            OnItemQuantityChanged?.Invoke(itemId, newQuantity);
            return true;
        }

        public bool IsRewardOperationCompleted(string operationId) =>
            !string.IsNullOrWhiteSpace(operationId) && completedRewardOperations.Contains(operationId);

        private bool IsValidItem(string itemId) => GetItem(itemId) != null;

        public MN7ItemData GetItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return null;

            return catalog?.GetItem(itemId);
        }

        private void SeedDefaultOwnedItems()
        {
            bool changed = false;
            if (config?.items == null)
                return;

            foreach (MN7ItemData item in config.items)
            {
                if (item == null || !item.defaultOwned || string.IsNullOrWhiteSpace(item.itemId))
                    continue;

                string id = item.itemId.Trim();
                if (GetQuantity(id) > 0)
                    continue;

                if (item.ownershipType == MN7ItemOwnershipType.Permanent)
                {
                    itemQuantities[id] = 1;
                    changed = true;
                }
            }

            if (changed)
                Save();
        }

        private void Save()
        {
            InventorySaveData data = BuildSaveData();
            InventorySaveEnvelope envelope = new()
            {
                version = MN7StoreSaveVersion.CurrentVersion,
                data = data
            };
            saveManager?.Save(saveKey, JsonUtility.ToJson(envelope));
        }

        private InventorySaveData BuildSaveData()
        {
            InventorySaveData data = new();
            foreach (var pair in itemQuantities)
            {
                if (pair.Value > 0)
                {
                    data.items.Add(new InventoryEntry { itemId = pair.Key, quantity = pair.Value });
                }
            }

            foreach (string operationId in completedRewardOperations)
            {
                if (!string.IsNullOrWhiteSpace(operationId))
                    data.completedRewardOperations.Add(operationId);
            }

            return data;
        }

        private void Load()
        {
            itemQuantities.Clear();
            completedRewardOperations.Clear();
            string json = saveManager?.Load(saveKey);
            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                if (TryLoadCurrentEnvelope(json))
                    return;

                InventorySaveData legacyData = JsonUtility.FromJson<InventorySaveData>(json);
                if (legacyData == null || legacyData.items == null)
                    return;

                MN7SaveMigration.LogMigration(0, MN7StoreSaveVersion.CurrentVersion, "inventory");
                ApplySaveData(legacyData);
                Save();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MN7 Store] Failed to load inventory.\n{exception}");
            }
        }

        private bool TryLoadCurrentEnvelope(string json)
        {
            InventorySaveEnvelope envelope = JsonUtility.FromJson<InventorySaveEnvelope>(json);
            if (envelope == null || envelope.data == null)
                return false;

            if (!MN7SaveMigration.IsSupportedVersion(envelope.version))
            {
                Debug.LogError($"[MN7 Store] Unsupported inventory save version: {envelope.version}");
                return true;
            }

            ApplySaveData(envelope.data);
            if (envelope.version < MN7StoreSaveVersion.CurrentVersion)
            {
                MN7SaveMigration.LogMigration(envelope.version, MN7StoreSaveVersion.CurrentVersion, "inventory");
                Save();
            }

            return true;
        }

        private void ApplySaveData(InventorySaveData saveData)
        {
            if (saveData?.items != null)
            {
                foreach (InventoryEntry entry in saveData.items)
                {
                    if (entry == null || !IsValidItem(entry.itemId) || entry.quantity <= 0)
                        continue;

                    MN7ItemData item = GetItem(entry.itemId);
                    string id = entry.itemId.Trim();
                    itemQuantities[id] = item.ownershipType == MN7ItemOwnershipType.Permanent ? 1 : entry.quantity;
                }
            }

            if (saveData?.completedRewardOperations == null)
                return;

            foreach (string operationId in saveData.completedRewardOperations)
            {
                if (!string.IsNullOrWhiteSpace(operationId))
                    completedRewardOperations.Add(operationId);
            }
        }

        internal void ResetToDefaults()
        {
            itemQuantities.Clear();
            completedRewardOperations.Clear();
            SeedDefaultOwnedItems();
            Save();
        }

        internal void DebugPrintInventory()
        {
            if (itemQuantities.Count == 0)
            {
                Debug.Log("[MN7 Store] Inventory is empty.");
                return;
            }

            foreach (var pair in itemQuantities)
                Debug.Log($"[MN7 Store] {pair.Key} x{pair.Value}");
        }
    }
}
