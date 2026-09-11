using System.Text;
using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    [CreateAssetMenu(fileName = "MN7StoreConfig", menuName = "MN7 Store/Store Config")]
    public class MN7StoreConfig : ScriptableObject
    {
        [Header("Secret Four")]
        public string gameId = "SECRET_FOUR";

        [Header("Store Items")]
        public List<MN7ItemData> items = new();

        public string GetEffectiveSaveKey()
        {
            string normalized = NormalizeGameId(gameId);
            return string.IsNullOrWhiteSpace(normalized) ? "MN7_STORE_SECRET_FOUR" : $"MN7_STORE_{normalized}";
        }

        private static string NormalizeGameId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            StringBuilder builder = new StringBuilder();
            foreach (char character in value.Trim())
            {
                if (char.IsLetterOrDigit(character) || character == '_' || character == '-')
                    builder.Append(char.ToUpperInvariant(character));
                else if (char.IsWhiteSpace(character))
                    builder.Append('_');
            }
            return builder.ToString();
        }
    }
}
