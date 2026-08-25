using UnityEngine;

public static class PlayerNameManager
{
    private const string PlayerNameKey = "PlayerName";

    public static string GetPlayerName()
    {
        return PlayerPrefs.GetString(
            PlayerNameKey,
            "Player 1"
        );
    }

    public static void SavePlayerName(
        string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Player 1";
        }

        playerName =
            playerName.Trim();

        PlayerPrefs.SetString(
            PlayerNameKey,
            playerName
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"Player name saved: {playerName}"
        );
    }
}