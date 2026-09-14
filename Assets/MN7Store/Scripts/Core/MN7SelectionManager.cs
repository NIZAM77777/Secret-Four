using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Persists one selected item per selection group. Different groups are independent.
    /// </summary>
    public sealed class MN7SelectionManager
    {
        [Serializable]
        private class SelectionEntry
        {
            public string selectionGroupId;
            public string itemId;
        }

        [Serializable]
        private class SelectionSaveData
        {
            public List<SelectionEntry> selections = new();
        }

        private const string SelectionSuffix = "_SELECTIONS";
        private readonly Dictionary<string, string> selectedByGroup = new(StringComparer.OrdinalIgnoreCase);
        private readonly MN7StoreConfig config;
        private readonly MN7StoreCatalog catalog;
        private readonly MN7SaveManager saveManager;
        private readonly string saveKey;

        public event Action<string, string> OnSelectionChanged;

        public MN7SelectionManager(MN7StoreConfig config, MN7SaveManager saveManager, MN7StoreCatalog catalog)
        {
            this.config = config;
            this.saveManager = saveManager;
            this.catalog = catalog;
            saveKey = $"{config.GetEffectiveSaveKey()}{SelectionSuffix}";
            Load();
            EnsureDefaultSelections();
        }

        public string GetSelectedItemId(string selectionGroupId)
        {
            string normalized = Normalize(selectionGroupId);
            if (string.IsNullOrEmpty(normalized)) return null;
            return selectedByGroup.TryGetValue(normalized, out string itemId) ? itemId : null;
        }

        public MN7ItemData GetSelectedItem(string selectionGroupId)
        {
            string itemId = GetSelectedItemId(selectionGroupId);
            return string.IsNullOrEmpty(itemId) ? null : catalog?.GetItem(itemId);
        }

        public Sprite GetSelectedSprite(string selectionGroupId) => GetSelectedItem(selectionGroupId)?.icon;

        public bool IsSelected(MN7ItemData item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.selectionGroupId)) return false;
            return string.Equals(GetSelectedItemId(item.selectionGroupId), item.itemId, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsSelected(string itemId) => IsSelected(catalog?.GetItem(itemId));

        public bool SelectItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId) || catalog == null) return false;
            MN7ItemData item = catalog.GetItem(itemId);
            if (item == null || !item.selectable || string.IsNullOrWhiteSpace(item.selectionGroupId)) return false;
            if (MN7StoreManager.Instance?.Inventory == null || !MN7StoreManager.Instance.Inventory.IsOwned(item.itemId)) return false;

            string group = Normalize(item.selectionGroupId);
            string previous = GetSelectedItemId(group);
            if (string.Equals(previous, item.itemId, StringComparison.OrdinalIgnoreCase)) return true;

            selectedByGroup[group] = item.itemId.Trim();
            Save();
            OnSelectionChanged?.Invoke(group, item.itemId.Trim());
            return true;
        }

        public void ClearSelection(string selectionGroupId)
        {
            string group = Normalize(selectionGroupId);
            if (string.IsNullOrEmpty(group) || !selectedByGroup.Remove(group)) return;
            Save();
            OnSelectionChanged?.Invoke(group, null);
        }

        /// <summary>Editor diagnostic helper. Clears saved selections and reapplies configured defaults.</summary>
        internal void ResetToDefaults()
        {
            selectedByGroup.Clear();
            Save();
            EnsureDefaultSelections();
        }

        private void EnsureDefaultSelections()
        {
            bool changed = false;
            HashSet<string> groupsWithSelection = new(StringComparer.OrdinalIgnoreCase);

            if (config?.items == null) return;

            foreach (MN7ItemData item in config.items)
            {
                if (item == null || !item.selectable || string.IsNullOrWhiteSpace(item.selectionGroupId)) continue;
                string group = Normalize(item.selectionGroupId);
                if (!string.IsNullOrEmpty(group) && !string.IsNullOrEmpty(GetSelectedItemId(group))) groupsWithSelection.Add(group);
            }

            foreach (MN7ItemData item in config.items)
            {
                if (item == null || !item.selectable || !item.defaultSelected || !item.defaultOwned) continue;
                string group = Normalize(item.selectionGroupId);
                if (string.IsNullOrEmpty(group) || groupsWithSelection.Contains(group)) continue;
                selectedByGroup[group] = item.itemId.Trim();
                groupsWithSelection.Add(group);
                changed = true;
            }

            if (changed) Save();
        }

        private void Save()
        {
            SelectionSaveData data = new();
            foreach (KeyValuePair<string, string> pair in selectedByGroup)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                    data.selections.Add(new SelectionEntry { selectionGroupId = pair.Key, itemId = pair.Value });
            }
            saveManager?.Save(saveKey, JsonUtility.ToJson(data));
        }

        private void Load()
        {
            selectedByGroup.Clear();
            string json = saveManager?.Load(saveKey);
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                SelectionSaveData data = JsonUtility.FromJson<SelectionSaveData>(json);
                if (data?.selections == null) return;

                foreach (SelectionEntry entry in data.selections)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.selectionGroupId) || string.IsNullOrWhiteSpace(entry.itemId)) continue;
                    MN7ItemData item = catalog?.GetItem(entry.itemId);
                    if (item == null || !item.selectable || !string.Equals(Normalize(item.selectionGroupId), Normalize(entry.selectionGroupId), StringComparison.OrdinalIgnoreCase)) continue;
                    selectedByGroup[Normalize(entry.selectionGroupId)] = entry.itemId.Trim();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MN7 Store] Failed to load selections.\n{exception}");
            }
        }

        private static string Normalize(string value) => value?.Trim();
    }
}
