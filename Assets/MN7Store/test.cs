using UnityEngine;
using MN7.Store;

public class MN7StoreTest : MonoBehaviour
{
    private void Start()
    {
        int coins =
            MN7StoreManager.Instance.Currency.GetBalance(
                "soft_currency"
            );

        Debug.Log(
            "Current Coins: " + coins
        );

        bool success =
            MN7StoreManager.Instance.Purchases
                .PurchaseWithCurrency(
                    "coin_item"
                );

        Debug.Log(
            "Purchase Result: " + success
        );

        Debug.Log(
            "Remaining Coins: " +
            MN7StoreManager.Instance.Currency
                .GetBalance("soft_currency")
        );
    }
}