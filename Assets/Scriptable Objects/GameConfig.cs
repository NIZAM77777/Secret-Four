using UnityEngine;

[CreateAssetMenu(
    fileName = "GameConfig",
    menuName = "Shift Four/Game Config"
)]
public class GameConfig : ScriptableObject
{
    [Header("Board")]
    [Min(1)]
    public int boardWidth = 7;

    [Min(1)]
    public int boardHeight = 6;

    [Min(1)]
    public int winLength = 4;

    [Header("Movement")]
    [Min(0f)]
    public float movementRevealDelay = 2f;
}