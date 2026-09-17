using UnityEngine;
using UnityEngine.UI;

public class CellView : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }

    private SpriteRenderer spriteRenderer;
    private Image image;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        image = GetComponentInChildren<Image>(true);
    }

    public void Initialize(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Applies the visual supplied by the Secret Four Store board selection.
    /// No gameplay/default board sprite is selected here; the Store is the source of truth.
    /// </summary>
    public void ApplyStoreSprite(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogWarning($"CellView ({X},{Y}): selected Store board sprite is missing.");
            return;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            return;
        }

        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = true;
            return;
        }

        Debug.LogError($"CellView ({X},{Y}): Cell prefab has no SpriteRenderer or UI Image for Store board visuals.");
    }
}
