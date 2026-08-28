using UnityEngine;

public class GamePlayerInfo : MonoBehaviour
{
    public string GetPlayerName(PlayerType player)
    {
        if (GameSession.Instance == null)
        {
            Debug.LogWarning(
                "GameSession not found."
            );

            return player == PlayerType.X
                ? "Player 1"
                : "Player 2";
        }

        if (player == PlayerType.X)
        {
            return GameSession.Instance.Player1Name;
        }

        return GameSession.Instance.Player2Name;
    }

    public bool IsBot(PlayerType player)
    {
        if (GameSession.Instance == null)
        {
            return false;
        }

        return GameSession.Instance.CurrentGameMode ==
                   GameMode.Bot
               &&
               player == PlayerType.Y;
    }
}