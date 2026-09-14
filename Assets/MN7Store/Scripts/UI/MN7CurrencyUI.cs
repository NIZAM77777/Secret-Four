using UnityEngine;

namespace MN7.Store
{
    /// <summary>
    /// Secret Four intentionally has no currency economy.
    /// This compatibility component keeps the reusable Store panel prefab free of a missing-script reference
    /// and hides the legacy currency display object when the panel is loaded.
    /// </summary>
    public class MN7CurrencyUI : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.SetActive(false);
        }
    }
}
