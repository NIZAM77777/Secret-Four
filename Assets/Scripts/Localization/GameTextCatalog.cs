using System;
using TMPro;
using UnityEngine;

namespace SecretFour.Localization
{
    [CreateAssetMenu(menuName = "Secret Four/Text and translations")]
    public class GameTextCatalog : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;
            [TextArea(1, 12)] public string english;
            [TextArea(1, 12)] public string spanish;
            [TextArea(1, 12)] public string french;
            [TextArea(1, 12)] public string arabic;
            public string Get(GameLanguage language) => language switch
            {
                GameLanguage.Spanish => spanish,
                GameLanguage.French => french,
                GameLanguage.Arabic => arabic,
                _ => english
            };
        }
        public TMP_FontAsset latinFont;
        public TMP_FontAsset arabicFont;
        public TMP_FontAsset titleFont;
        public Entry[] entries = Array.Empty<Entry>();
    }
    public enum GameLanguage { English, Spanish, French, Arabic }
}
