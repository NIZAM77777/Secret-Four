using UnityEngine;

namespace MN7.Store
{
    public class MN7SaveManager
    {
        private readonly string defaultSaveKey;

        public MN7SaveManager(string defaultSaveKey)
        {
            this.defaultSaveKey = defaultSaveKey;
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

            PlayerPrefs.SetString(key, data);
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

            if (!PlayerPrefs.HasKey(key))
                return null;

            return PlayerPrefs.GetString(key);
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

            PlayerPrefs.DeleteKey(key);
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

            return PlayerPrefs.HasKey(key);
        }
    }
}