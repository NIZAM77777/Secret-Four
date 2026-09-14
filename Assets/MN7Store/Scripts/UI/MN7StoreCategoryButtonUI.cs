using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MN7.Store
{
    /// <summary>
    /// Shared category button. The prefab defines the visual design; category data supplies only text and identity.
    /// </summary>
    public class MN7StoreCategoryButtonUI : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject selectionVisual;

        [Header("Selection")]
        [SerializeField] private Color normalColor = new(1f, 1f, 1f, 0.18f);
        [SerializeField] private Color selectedColor = new(1f, 1f, 1f, 0.85f);
        [SerializeField] private Color normalTextColor = Color.white;
        [SerializeField] private Color selectedTextColor = Color.black;
        [SerializeField] private bool useSelectionScale;
        [SerializeField, Min(0.5f)] private float selectedScale = 1.05f;

        private Vector3 baseScale = Vector3.one;
        private MN7StoreCategoryData category;

        public Button Button => button;
        public string CategoryId => category == null ? string.Empty : category.categoryId;

        private void Awake()
        {
            baseScale = transform.localScale;
            ValidateReferences();
        }

        public void Configure(MN7StoreCategoryData data, UnityEngine.Events.UnityAction onClick)
        {
            category = data;

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                if (onClick != null)
                    button.onClick.AddListener(onClick);
            }

            if (nameText != null)
                nameText.text = data == null ? string.Empty : data.displayName;

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
                background.color = selected ? selectedColor : normalColor;

            if (nameText != null)
                nameText.color = selected ? selectedTextColor : normalTextColor;

            if (selectionVisual != null)
                selectionVisual.SetActive(selected);

            transform.localScale = useSelectionScale && selected
                ? baseScale * selectedScale
                : baseScale;

            if (button != null)
                button.interactable = !selected;
        }

        private void ValidateReferences()
        {
            if (button == null) Debug.LogError($"[MN7 Store] {name}: Category Button reference is missing.");
            if (background == null) Debug.LogError($"[MN7 Store] {name}: Category Background reference is missing.");
            if (nameText == null) Debug.LogError($"[MN7 Store] {name}: Name Text reference is missing. Add a NameText child to the prefab.");
            if (selectionVisual == null) Debug.LogWarning($"[MN7 Store] {name}: Selection Visual is optional but not assigned.");
        }
    }
}
