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

    public void PlayWithBot()
    {
        GameSession.Instance.StartBotGame();

        LoadGameScene();
    }

    public void OpenHumanSetup()
    {
        mainMenuPanel.SetActive(false);

        humanSetupPanel.SetActive(true);
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
        Debug.Log(
            "Exit Game"
        );

        Application.Quit();
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene(
            gameSceneName
        );
    }
}