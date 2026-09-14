using UnityEngine;
using UnityEngine.UI;

namespace MN7.Store
{
    /// <summary>
    /// Optional bridge for 2D games. Assign a Selection Group and either a UI Image
    /// or SpriteRenderer; the currently selected Store item sprite is applied automatically.
    /// For 3D/prefab games, read MN7StoreManager.Instance.Selection.GetSelectedItem(group) instead.
    /// </summary>
    public sealed class MN7SelectedSpriteApplier : MonoBehaviour
    {
        [SerializeField] private string selectionGroupId = "PLAYER";
        [SerializeField] private Image targetImage;
        [SerializeField] private SpriteRenderer targetSpriteRenderer;
        [SerializeField] private bool clearIfNoSelection;

        private void OnEnable()
        {
            Subscribe();
            Apply();
        }

        private void OnDisable()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store?.Selection != null)
                store.Selection.OnSelectionChanged -= HandleSelectionChanged;
        }

        private void Subscribe()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            if (store?.Selection != null)
            {
                store.Selection.OnSelectionChanged -= HandleSelectionChanged;
                store.Selection.OnSelectionChanged += HandleSelectionChanged;
            }
        }

        private void HandleSelectionChanged(string group, string itemId)
        {
            if (string.Equals(group, selectionGroupId?.Trim(), System.StringComparison.OrdinalIgnoreCase))
                Apply();
        }

        public void Apply()
        {
            MN7StoreManager store = MN7StoreManager.Instance;
            Sprite sprite = store?.Selection?.GetSelectedSprite(selectionGroupId);

            if (targetImage != null)
                targetImage.sprite = sprite;
            if (targetSpriteRenderer != null)
                targetSpriteRenderer.sprite = sprite;

            if (clearIfNoSelection)
            {
                if (targetImage != null) targetImage.enabled = sprite != null;
                if (targetSpriteRenderer != null) targetSpriteRenderer.enabled = sprite != null;
            }
        }
    }
}
