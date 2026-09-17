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
    /// The Cell prefab's serialized/default sprite is never used as the board design.
    /// </summary>
    public void ApplyStoreSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (image != null)
            image.enabled = false;

        if (sprite == null)
        {
            Debug.LogWarning($"CellView ({X},{Y}): selected Store board sprite is missing.");
            return;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = true;
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
