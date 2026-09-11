using UnityEngine;

namespace MN7.Store
{
    public class MN7SaveManager
    {
        private readonly string defaultSaveKey;

        public MN7SaveManager(string defaultSaveKey)
        {
            this.defaultSaveKey = defaultSaveKey?.Trim();
        }

        //==================================================
        // SAVE
        //==================================================

        public void Save(string data)
        {
            Save(defaultSaveKey, data);
        }

        public void Save(string key, string data)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            PlayerPrefs.SetString(key.Trim(), data ?? string.Empty);
            PlayerPrefs.Save();
        }

        //==================================================
        // LOAD
        //==================================================

        public string Load()
        {
            return Load(defaultSaveKey);
        }

        public string Load(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            string normalizedKey = key.Trim();

            if (!PlayerPrefs.HasKey(normalizedKey))
                return null;

            return PlayerPrefs.GetString(normalizedKey);
        }

        //==================================================
        // DELETE
        //==================================================

        public void Delete()
        {
            Delete(defaultSaveKey);
        }

        public void Delete(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            PlayerPrefs.DeleteKey(key.Trim());
            PlayerPrefs.Save();
        }

        //==================================================
        // EXISTS
        //==================================================

        public bool HasSave()
        {
            return HasSave(defaultSaveKey);
        }

        public bool HasSave(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            return PlayerPrefs.HasKey(key.Trim());
        }
    }
}