using UnityEngine;

public class MatchPlayerManager : MonoBehaviour
{
    [Header("Player X")]

    [SerializeField]
    private MatchPlayerData playerX;

    [Header("Player Y")]

    [SerializeField]
    private MatchPlayerData playerY;


    public MatchPlayerData PlayerX => playerX;

    public MatchPlayerData PlayerY => playerY;


    private void Awake()
    {
        // Temporary default values.
        // Later these will come from
        // Main Menu / Game Mode.

        if (string.IsNullOrEmpty(playerX.playerName))
        {
            playerX.playerName = "Player 1";
        }

        playerX.playerType = PlayerType.X;

        if (string.IsNullOrEmpty(playerY.playerName))
        {
            playerY.playerName = "Player 2";
        }

        playerY.playerType = PlayerType.Y;
    }


    public string GetPlayerName(
        PlayerType playerType)
    {
        if (playerType == PlayerType.X)
        {
            return playerX.playerName;
        }

        return playerY.playerName;
    }


    public bool IsBot(
        PlayerType playerType)
    {
        if (playerType == PlayerType.X)
        {
            return playerX.isBot;
        }

        return playerY.isBot;
    }
}