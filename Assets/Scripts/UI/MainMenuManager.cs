using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scenes")]

    [SerializeField]
    private string gameSceneName = "Game";


    [Header("Panels")]

    [SerializeField]
    private GameObject mainMenuPanel;

    [SerializeField]
    private GameObject humanSetupPanel;

    [SerializeField]
    private GameObject botPanel;

    [SerializeField]
    private GameObject playInRoomMenuPanel;


    [Header("Human Setup")]

    [SerializeField]
    private HumanSetupManager humanSetupManager;


    // =========================
    // BOT
    // =========================

    public void OpenBotPanel()
    {
        mainMenuPanel.SetActive(false);

        humanSetupPanel.SetActive(false);

        botPanel.SetActive(true);
    }


    public void BackFromBotPanel()
    {
        botPanel.SetActive(false);

        mainMenuPanel.SetActive(true);
    }


    public void PlayEasyBot()
    {
        StartBotGame(
            BotDifficulty.Easy
        );
    }


    public void PlayMediumBot()
    {
        StartBotGame(
            BotDifficulty.Medium
        );
    }


    public void PlayHardBot()
    {
        StartBotGame(
            BotDifficulty.Hard
        );
    }


    private void StartBotGame(
        BotDifficulty difficulty)
    {
        if (GameSession.Instance == null)
        {
            Debug.LogError("GameSession is missing.");
            return;
        }

        GameSession.Instance.StartBotGame(
            difficulty
        );

        LoadGameScene();
    }


    // =========================
    // LOCAL HUMAN
    // =========================

    public void OpenHumanSetup()
    {
        mainMenuPanel.SetActive(false);

        humanSetupPanel.SetActive(true);

        if (humanSetupManager != null)
        {
            humanSetupManager.ResetPlayerNames();
        }
    }


    public void StartHumanGame(
        string player1Name,
        string player2Name)
    {
        if (GameSession.Instance == null)
        {
            Debug.LogError("GameSession is missing.");
            return;
        }

        GameSession.Instance.StartLocalHumanGame(
            player1Name,
            player2Name
        );

        LoadGameScene();
    }


    public void BackToMainMenu()
    {
        humanSetupPanel.SetActive(false);

        mainMenuPanel.SetActive(true);
    }


    // =========================
    // PLAY IN ROOM
    // =========================

    public void PlayInRoom()
    {
        mainMenuPanel.SetActive(false);

        playInRoomMenuPanel.SetActive(true);

        Debug.Log(
            "Play In Room menu opened."
        );
    }


    public void BackFromPlayInRoom()
    {
        playInRoomMenuPanel.SetActive(false);

        mainMenuPanel.SetActive(true);

        Debug.Log(
            "Returned to Main Menu."
        );
    }


    // =========================
    // EXIT
    // =========================

    public void ExitGame()
    {
        Debug.Log("Exit Game");

        Application.Quit();
    }


    // =========================
    // SCENE LOADING
    // =========================

    private void LoadGameScene()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("Game scene name is missing.");
            return;
        }

        SceneManager.LoadScene(
            gameSceneName
        );
    }
}