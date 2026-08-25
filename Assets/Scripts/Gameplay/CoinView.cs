using UnityEngine;

public class CoinView : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    public PlayerType PlayerType { get; private set; }

    public void Setup(PlayerType playerType, Sprite sprite)
    {
        PlayerType = playerType;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }
}