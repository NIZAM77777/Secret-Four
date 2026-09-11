using TMPro;
using UnityEngine;

namespace MN7.Store
{
    public class MN7CurrencyUI : MonoBehaviour
    {
        [Header("Currency")]
        [SerializeField]
        private string currencyId = "soft_currency";

        [Header("UI")]
        [SerializeField]
        private TMP_Text amountText;

        //==================================================
        // UNITY
        //==================================================

        private void OnEnable()
        {
            if (MN7StoreManager.Instance == null)
                return;

            if (MN7StoreManager.Instance.Currency == null)
                return;

            MN7StoreManager.Instance.Currency
                .OnCurrencyChanged
                += HandleCurrencyChanged;

            Refresh();
        }

        private void OnDisable()
        {
            if (MN7StoreManager.Instance == null)
                return;

            if (MN7StoreManager.Instance.Currency == null)
                return;

            MN7StoreManager.Instance.Currency
                .OnCurrencyChanged
                -= HandleCurrencyChanged;
        }

        //==================================================
        // REFRESH
        //==================================================

        private void Refresh()
        {
            int amount =
                MN7StoreManager.Instance.Currency
                    .GetBalance(currencyId);

            UpdateText(amount);
        }

        //==================================================
        // EVENT
        //==================================================

        private void HandleCurrencyChanged(
            string changedCurrencyId,
            int newBalance)
        {
            if (changedCurrencyId != currencyId)
                return;

            UpdateText(newBalance);
        }

        //==================================================
        // UI
        //==================================================

        private void UpdateText(int amount)
        {
            if (amountText == null)
                return;

            amountText.text =
                amount.ToString();
        }
    }
}