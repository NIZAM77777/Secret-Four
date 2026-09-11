using System.Text;
using UnityEngine;

namespace MN7.Store
{
    [CreateAssetMenu(
        fileName = "MN7StoreConfig",
        menuName = "MN7 Store/Store Config"
    )]
    public class MN7StoreConfig : ScriptableObject
    {
        [Header("Game Identity")]
        [Tooltip("Unique ID for the game using this store. Example: CONNECT_FOUR")]
        public string gameId;

        [Header("Save")]
        [Tooltip("Legacy/fallback save key. When Game ID is set, the store derives an isolated key from it.")]
        public string saveKey = "MN7_STORE_DATA";

        [Header("Currencies")]
        public System.Collections.Generic.List<MN7CurrencyData> currencies = new();

        [Header("Store Items")]
        public System.Collections.Generic.List<MN7ItemData> items = new();

        /// <summary>
        /// Returns the save namespace used by the store.
        /// A Game ID is preferred so the same MN7 Store package can be reused
        /// safely across multiple games without sharing PlayerPrefs data.
        /// </summary>
        public string GetEffectiveSaveKey()
        {
            if (!string.IsNullOrWhiteSpace(gameId))
            {
                string normalized = NormalizeGameId(gameId);

                if (!string.IsNullOrWhiteSpace(normalized))
                    return $"MN7_STORE_{normalized}";
            }

            return saveKey?.Trim();
        }

        private static string NormalizeGameId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            StringBuilder builder = new StringBuilder();

            foreach (char character in value.Trim())
            {
                if (char.IsLetterOrDigit(character) ||
                    character == '_' ||
                    character == '-')
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
                else if (char.IsWhiteSpace(character))
                {
                    builder.Append('_');
                }
            }

            return builder.ToString();
        }
    }
}