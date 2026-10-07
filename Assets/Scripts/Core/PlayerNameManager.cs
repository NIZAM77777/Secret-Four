using UnityEngine;

public static class PlayerNameManager
{
    private const string PlayerNameKey = "PlayerName";

    public static string GetPlayerName()
    {
        return Normalize(PlayerPrefs.GetString(
            PlayerNameKey,
            "Player 1"
        ));
    }

    public static void SavePlayerName(
        string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = "Player 1";
        }

        playerName = Normalize(playerName);

        PlayerPrefs.SetString(
            PlayerNameKey,
            playerName
        );

        PlayerPrefs.Save();

        Debug.Log(
            $"Player name saved: {playerName}"
        );
    }
    public static string Normalize(string value, string fallback = "Player 1")
    {
        string name = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        name = name.Replace("<", "").Replace(">", "").Replace("\n", " ").Replace("\r", " ");
        // Twelve Unicode text elements fit the HUD and the network's 61-byte name payload.
        var elements = System.Globalization.StringInfo.GetTextElementEnumerator(name);
        var result = new System.Text.StringBuilder();
        int count = 0;
        while (elements.MoveNext() && count < 12)
        {
            string element = elements.GetTextElement();
            if (System.Text.Encoding.UTF8.GetByteCount(result.ToString() + element) > 60) break;
            result.Append(element);
            count++;
        }
        return result.Length == 0 ? fallback : result.ToString();
    }
}