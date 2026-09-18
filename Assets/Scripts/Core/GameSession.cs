using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public GameMode CurrentGameMode
    {
        get;
        private set;
    }

    public BotDifficulty CurrentBotDifficulty
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

    public PlayerType LocalOnlinePlayerType
    {
        get;
        private set;
    } = PlayerType.X;

    public string LocalOnlineSkinId
    {
        get;
        private set;
    }

    public void ApplyOnlineMatchProfile(
        PlayerType localPlayerType,
        string player1Name,
        string player2Name,
        string localSkinId)
    {
        CurrentGameMode = GameMode.OnlineRoom;

        LocalOnlinePlayerType = localPlayerType;

        Player1Name =
            string.IsNullOrWhiteSpace(player1Name)
                ? "Player 1"
                : player1Name.Trim();

        Player2Name =
            string.IsNullOrWhiteSpace(player2Name)
                ? "Player 2"
                : player2Name.Trim();

        LocalOnlineSkinId = localSkinId;
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

    public void StartBotGame(
    BotDifficulty difficulty)
    {
        CurrentGameMode =
            GameMode.Bot;

        CurrentBotDifficulty =
            difficulty;

        Player1Name =
            PlayerNameManager.GetPlayerName();

        Player2Name = "Bot";

        Debug.Log(
            $"Bot Game Started: {difficulty}"
        );
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