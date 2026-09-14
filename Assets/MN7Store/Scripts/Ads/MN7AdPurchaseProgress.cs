using System;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    public class MN7AdPurchaseProgress
    {
        [Serializable]
        private class AdProgressEntry
        {
            public string itemId;
            public int progress;
            public long updatedAtUnixSeconds;
        }

        [Serializable]
        private class AdProgressSaveData
        {
            public List<AdProgressEntry> entries = new();
        }

        [Serializable]
        private class AdProgressSaveEnvelope
        {
            public int version = MN7StoreSaveVersion.CurrentVersion;
            public AdProgressSaveData data;
        }

        private readonly Dictionary<string, int> progress = new();
        private readonly Dictionary<string, long> updatedAtUnixSeconds = new();
        private readonly MN7SaveManager saveManager;
        private readonly string saveKey;

        public MN7AdPurchaseProgress(
            MN7SaveManager saveManager,
            string storeSaveKey)
        {
            this.saveManager = saveManager;

            if (string.IsNullOrWhiteSpace(storeSaveKey))
            {
                throw new ArgumentException(
                    "Store save key cannot be empty.",
                    nameof(storeSaveKey)
                );
            }

            saveKey = $"{storeSaveKey}_AD_PROGRESS";
            Load();
        }

        public int GetProgress(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return 0;

            return progress.TryGetValue(itemId.Trim(), out int value)
                ? value
                : 0;
        }

        /// <summary>
        /// Returns the last persisted time at which this item's ad progress changed.
        /// A value of zero means the save predates progress timestamps.
        /// </summary>
        public long GetProgressUpdatedAtUnixSeconds(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return 0;

            return updatedAtUnixSeconds.TryGetValue(itemId.Trim(), out long value)
                ? value
                : 0;
        }

        public bool AddProgress(string itemId, int amount = 1)
        {
            itemId = itemId?.Trim();

            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
                return false;

            int current = GetProgress(itemId);
            long result = (long)current + amount;

            if (result > int.MaxValue)
            {
                Debug.LogError($"[MN7 Store] Ad progress overflow: {itemId}");
                return false;
            }

            progress[itemId] = (int)result;
            updatedAtUnixSeconds[itemId] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Save();
            return true;
        }

        public void ResetAll()
        {
            progress.Clear();
            updatedAtUnixSeconds.Clear();
            Save();
            Debug.Log("[MN7 Store] All ad progress reset.");
        }

        public void SetProgress(string itemId, int value)
        {
            itemId = itemId?.Trim();

            if (string.IsNullOrWhiteSpace(itemId))
                return;

            progress[itemId] = Mathf.Max(0, value);
            updatedAtUnixSeconds[itemId] = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            Save();
        }

        public void ResetProgress(string itemId)
        {
            itemId = itemId?.Trim();

            if (string.IsNullOrWhiteSpace(itemId))
                return;

            progress.Remove(itemId);
            updatedAtUnixSeconds.Remove(itemId);
            Save();
        }

        private void Save()
        {
            AdProgressSaveData data = BuildSaveData();
            AdProgressSaveEnvelope envelope = new()
            {
                version = MN7StoreSaveVersion.CurrentVersion,
                data = data
            };

            saveManager.Save(saveKey, JsonUtility.ToJson(envelope));
        }

        private AdProgressSaveData BuildSaveData()
        {
            AdProgressSaveData data = new();

            foreach (var pair in progress)
            {
                data.entries.Add(
                    new AdProgressEntry
                    {
                        itemId = pair.Key,
                        progress = Mathf.Max(0, pair.Value),
                        updatedAtUnixSeconds = GetProgressUpdatedAtUnixSeconds(pair.Key)
                    }
                );
            }

            return data;
        }

        private void Load()
        {
            progress.Clear();
            updatedAtUnixSeconds.Clear();

            string json = saveManager.Load(saveKey);

            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                if (TryLoadCurrentEnvelope(json))
                    return;

                // Version 0/legacy format: the payload itself was AdProgressSaveData.
                AdProgressSaveData legacyData =
                    JsonUtility.FromJson<AdProgressSaveData>(json);

                if (legacyData == null || legacyData.entries == null)
                    return;

                MN7SaveMigration.LogMigration(0, MN7StoreSaveVersion.CurrentVersion, "ad progress");
                ApplySaveData(legacyData);
                Save();
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[MN7 Store] Failed to load ad progress.\n{exception}"
                );
            }
        }

        private bool TryLoadCurrentEnvelope(string json)
        {
            AdProgressSaveEnvelope envelope =
                JsonUtility.FromJson<AdProgressSaveEnvelope>(json);

            if (envelope == null || envelope.data == null)
                return false;

            if (!MN7SaveMigration.IsSupportedVersion(envelope.version))
            {
                Debug.LogError(
                    $"[MN7 Store] Unsupported ad progress save version: {envelope.version}"
                );
                return true;
            }

            ApplySaveData(envelope.data);

            if (envelope.version < MN7StoreSaveVersion.CurrentVersion)
            {
                MN7SaveMigration.LogMigration(
                    envelope.version,
                    MN7StoreSaveVersion.CurrentVersion,
                    "ad progress"
                );
                Save();
            }

            return true;
        }

        private void ApplySaveData(AdProgressSaveData saveData)
        {
            if (saveData.entries == null)
                return;

            foreach (AdProgressEntry entry in saveData.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.itemId))
                    continue;

                string itemId = entry.itemId.Trim();
                progress[itemId] = Mathf.Max(0, entry.progress);
                updatedAtUnixSeconds[itemId] = Math.Max(0, entry.updatedAtUnixSeconds);
            }
        }
    }
}
