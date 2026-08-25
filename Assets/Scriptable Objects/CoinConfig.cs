using UnityEngine;

[CreateAssetMenu(
    fileName = "CoinConfig",
    menuName = "Shift Four/Coin Config"
)]
public class CoinConfig : ScriptableObject
{
    [Header("Player X")]
    public Sprite playerXSprite;

    [Header("Player Y")]
    public Sprite playerYSprite;

    public Sprite GetSprite(PlayerType playerType)
    {
        return playerType == PlayerType.X
            ? playerXSprite
            : playerYSprite;
    }
}