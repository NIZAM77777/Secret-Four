using System.Collections.Generic;
using UnityEngine;

namespace MN7.Store
{
    [CreateAssetMenu(
        fileName = "MN7StoreConfig",
        menuName = "MN7 Store/Store Config"
    )]
    public class MN7StoreConfig : ScriptableObject
    {
        [Header("Save")]
        public string saveKey = "MN7_STORE_DATA";

        [Header("Currencies")]
        public List<MN7CurrencyData> currencies = new();

        [Header("Store Items")]
        public List<MN7ItemData> items = new();
    }
}