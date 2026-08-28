using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scenes")]

    [SerializeField]
    private string gameSceneName = "GameScene";

    [Header("Panels")]

    [SerializeField]
    private GameObject mainMenuPanel;

    [SerializeField]
    private GameObject humanSetupPanel;

    [SerializeField]
    private GameObject botPanel;

    [Header("Human Setup")]

    [SerializeField]
    private HumanSetupManager humanSetupManager;

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
        GameSession.Instance.StartBotGame(
            difficulty
        );

        LoadGameScene();
    }


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


    public void PlayInRoom()
    {
        GameSession.Instance.StartOnlineRoomGame();

        Debug.Log(
            "Online Room mode selected."
        );
    }


    public void ExitGame()
    {
        Debug.Log("Exit Game");

        Application.Quit();
    }


    private void LoadGameScene()
    {
        SceneManager.LoadScene(
            gameSceneName
        );
    }
}