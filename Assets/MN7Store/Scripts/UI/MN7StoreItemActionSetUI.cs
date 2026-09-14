using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MN7.Store
{
    /// <summary>
    /// Shared action-button set for a store item. The prefab owns button artwork and text.
    /// </summary>
    public class MN7StoreItemActionSetUI : MonoBehaviour
    {
        [Header("Buy")]
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyLabel;

        [Header("Buy With Ad")]
        [SerializeField] private Button buyWithAdButton;
        [SerializeField] private TMP_Text buyWithAdLabel;

        [Header("Select")]
        [SerializeField] private Button selectButton;
        [SerializeField] private TMP_Text selectLabel;

        [Header("Selected")]
        [SerializeField] private Button selectedButton;
        [SerializeField] private TMP_Text selectedLabel;

        public Button BuyButton => buyButton;
        public Button BuyWithAdButton => buyWithAdButton;
        public Button SelectButton => selectButton;
        public Button SelectedButton => selectedButton;

        public void Bind(UnityAction onBuy, UnityAction onBuyWithAd, UnityAction onSelect)
        {
            BindButton(buyButton, onBuy);
            BindButton(buyWithAdButton, onBuyWithAd);
            BindButton(selectButton, onSelect);
            BindButton(selectedButton, onSelect);
        }

        public void HideAll()
        {
            SetActive(buyButton, false);
            SetActive(buyWithAdButton, false);
            SetActive(selectButton, false);
            SetActive(selectedButton, false);
        }

        public void ShowBuy() => ShowOnly(buyButton);
        public void ShowBuyWithAd() => ShowOnly(buyWithAdButton);
        public void ShowSelect() => ShowOnly(selectButton);
        public void ShowSelected() => ShowOnly(selectedButton);

        public void ShowBuyAndAd()
        {
            SetActive(buyButton, true);
            SetActive(buyWithAdButton, true);
            SetActive(selectButton, false);
            SetActive(selectedButton, false);
        }

        private void ShowOnly(Button target)
        {
            HideAll();
            SetActive(target, true);
        }

        private static void BindButton(Button button, UnityAction callback)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            if (callback != null) button.onClick.AddListener(callback);
        }

        private static void SetActive(Button button, bool active)
        {
            if (button != null) button.gameObject.SetActive(active);
        }
    }
}
