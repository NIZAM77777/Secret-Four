using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SecretFour.Localization
{
    public static class GameLocalization
    {
        public const string PreferenceKey = "SecretFour.Language";
        private static GameTextCatalog catalog;
        private static Dictionary<string, GameTextCatalog.Entry> entries;
        private static GameLanguage language;
        public static event Action Changed;
        public static GameTextCatalog Catalog { get { EnsureLoaded(); return catalog; } }
        public static GameLanguage Language { get { EnsureLoaded(); return language; } }
        public static bool IsRTL => Language == GameLanguage.Arabic;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { catalog = null; entries = null; Changed = null; }
        private static void EnsureLoaded()
        {
            if (entries != null) return;
            catalog = Resources.Load<GameTextCatalog>("Localization/GameTextCatalog");
            entries = new Dictionary<string, GameTextCatalog.Entry>(StringComparer.Ordinal);
            if (catalog != null)
                foreach (var entry in catalog.entries)
                    if (!string.IsNullOrEmpty(entry.key)) entries[entry.key] = entry;
            language = (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, 3);
        }
        public static void SetLanguage(GameLanguage value)
        {
            EnsureLoaded();
            if (!Enum.IsDefined(typeof(GameLanguage), value) || language == value) return;
            language = value;
            PlayerPrefs.SetInt(PreferenceKey, (int)value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        public static string Text(string key, string fallback = null)
        {
            EnsureLoaded();
            if (key != null && entries.TryGetValue(key, out var entry))
            {
                string value = entry.Get(language);
                return string.IsNullOrEmpty(value) ? entry.english : value;
            }
            return fallback ?? key ?? string.Empty;
        }
        public static string Format(string key, params object[] values) =>
            string.Format(CultureInfo.InvariantCulture, Text(key), values);
        public static string DisplayName(string name) => name switch
        {
            "Player 1" => Text("player.one"),
            "Player 2" => Text("player.two"),
            "Bot" => Text("player.bot"),
            _ => name
        };
        public static bool ContainsArabic(string text)
        {
            if (text == null) return false;
            foreach (char c in text)
                if ((c >= '؀' && c <= 'ۿ') || (c >= 'ﭐ' && c <= '﻿')) return true;
            return false;
        }
    }
}
