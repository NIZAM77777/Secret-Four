using UnityEngine;

namespace MN7.Store
{
    [CreateAssetMenu(
        fileName = "CurrencyData",
        menuName = "MN7 Store/Currency Data"
    )]
    public class MN7CurrencyData : ScriptableObject
    {
        [Header("Currency")]
        public string currencyId = "soft_currency";

        public string displayName = "Coins";

        [Header("Starting Balance")]
        [Min(0)]
        public int startingAmount = 0;
    }
}