using UnityEngine;

public class CellView : MonoBehaviour
{
    public int X { get; private set; }
    public int Y { get; private set; }

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(int x, int y)
    {
        X = x;
        Y = y;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Applies the selected Store board skin to this cell.
    /// The board item icon is the visual used for every cell.
    /// </summary>
    public void ApplyBoardSprite(Sprite sprite)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"CellView ({X},{Y}): SpriteRenderer is missing.");
            return;
        }

        if (sprite == null)
        {
            Debug.LogWarning($"CellView ({X},{Y}): Board sprite is null; keeping prefab sprite.");
            return;
        }

        spriteRenderer.sprite = sprite;
        spriteRenderer.enabled = true;
    }
}