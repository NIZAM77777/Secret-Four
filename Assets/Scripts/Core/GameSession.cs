using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public GameMode CurrentGameMode
    {
        get;
        private set;
    }

    public string Player1Name
    {
        get;
        private set;
    }

    public string Player2Name
    {
        get;
        private set;
    }

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        LoadSavedPlayerName();

        Player2Name = "Player 2";
    }

    private void LoadSavedPlayerName()
    {
        Player1Name =
            PlayerNameManager.GetPlayerName();
    }

    public void StartBotGame()
    {
        CurrentGameMode =
            GameMode.Bot;

        Player1Name =
            PlayerNameManager.GetPlayerName();

        Player2Name = "Bot";
    }

    public void StartLocalHumanGame(
        string player1Name,
        string player2Name)
    {
        CurrentGameMode =
            GameMode.LocalHuman;

        Player1Name =
            string.IsNullOrWhiteSpace(player1Name)
                ? "Player 1"
                : player1Name.Trim();

        Player2Name =
            string.IsNullOrWhiteSpace(player2Name)
                ? "Player 2"
                : player2Name.Trim();
    }

    public void StartOnlineRoomGame()
    {
        CurrentGameMode =
            GameMode.OnlineRoom;

        Player1Name =
            PlayerNameManager.GetPlayerName();

        Player2Name = "Opponent";
    }
}