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
        }

        [Serializable]
        private class AdProgressSaveData
        {
            public List<AdProgressEntry> entries = new();
        }

        private readonly Dictionary<string, int> progress =
            new();

        private readonly MN7SaveManager saveManager;

        private const string SaveKey =
            "MN7_STORE_AD_PROGRESS";

        public MN7AdPurchaseProgress(
            MN7SaveManager saveManager)
        {
            this.saveManager = saveManager;

            Load();
        }

        //==================================================
        // GET
        //==================================================

        public int GetProgress(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return 0;

            return progress.TryGetValue(
                itemId,
                out int value
            )
                ? value
                : 0;
        }

        //==================================================
        // ADD
        //==================================================

        public void AddProgress(
            string itemId,
            int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            if (amount <= 0)
                return;

            int current =
                GetProgress(itemId);

            progress[itemId] =
                current + amount;

            Save();
        }

        //==================================================
        // SET
        //==================================================

        public void SetProgress(
            string itemId,
            int value)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            progress[itemId] =
                Mathf.Max(0, value);

            Save();
        }

        //==================================================
        // RESET
        //==================================================

        public void ResetProgress(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            progress.Remove(itemId);

            Save();
        }

        //==================================================
        // SAVE
        //==================================================

        private void Save()
        {
            AdProgressSaveData saveData = new();

            foreach (var pair in progress)
            {
                saveData.entries.Add(
                    new AdProgressEntry
                    {
                        itemId = pair.Key,
                        progress = pair.Value
                    }
                );
            }

            string json =
                JsonUtility.ToJson(saveData);

            saveManager.Save(
                SaveKey,
                json
            );
        }

        //==================================================
        // LOAD
        //==================================================

        private void Load()
        {
            string json =
                saveManager.Load(SaveKey);

            if (string.IsNullOrEmpty(json))
                return;

            try
            {
                AdProgressSaveData saveData =
                    JsonUtility.FromJson<AdProgressSaveData>(
                        json
                    );

                if (saveData == null ||
                    saveData.entries == null)
                {
                    return;
                }

                foreach (AdProgressEntry entry
                         in saveData.entries)
                {
                    if (string.IsNullOrWhiteSpace(
                            entry.itemId))
                    {
                        continue;
                    }

                    progress[entry.itemId] =
                        Mathf.Max(
                            0,
                            entry.progress
                        );
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[MN7 Store] Failed to load ad progress.\n{exception}"
                );
            }
        }
    }
}