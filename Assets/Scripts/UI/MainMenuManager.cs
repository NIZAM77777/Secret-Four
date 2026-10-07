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

    [SerializeField]
    private GameObject storePanel;

    [SerializeField]
    private GameObject settingsPanel;

    [SerializeField]
    private GameObject howToPlayPanel;

    [Header("Human Setup")]
    [SerializeField]
    private HumanSetupManager humanSetupManager;

    private void Awake()
    {
        CloseAllPanels();
    }

    // =========================
    // PANEL MANAGEMENT
    // =========================

    private void CloseAllPanels()
    {
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (humanSetupPanel != null)
            humanSetupPanel.SetActive(false);

        if (botPanel != null)
            botPanel.SetActive(false);

        if (playInRoomMenuPanel != null)
            playInRoomMenuPanel.SetActive(false);

        if (storePanel != null)
            storePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }

    private void ShowMainMenu() => CloseAllPanels();

    // =========================
    // STORE
    // =========================

    public void OpenStore()
    {
        ShowPanel(storePanel);
    }

    public void CloseStore()
    {
        ShowMainMenu();
    }

    // =========================
    // SETTINGS
    // =========================

    /// <summary>
    /// Opens the Settings panel.
    /// </summary>
    public void OpenSettings()
    {
        ShowPanel(settingsPanel);
    }

    /// <summary>
    /// Closes Settings and returns to Main Menu.
    /// </summary>
    public void CloseSettings()
    {
        ShowMainMenu();

        Debug.Log("[Secret Four] Returned from Settings.");
    }

    // =========================
    // HOW TO PLAY
    // =========================

    /// <summary>
    /// Opens the How To Play panel.
    /// </summary>
    public void OpenHowToPlay()
    {
        ShowPanel(howToPlayPanel);
    }

    /// <summary>
    /// Closes How To Play and returns to Main Menu.
    /// </summary>
    public void CloseHowToPlay()
    {
        ShowMainMenu();

        Debug.Log("[Secret Four] Returned from How To Play.");
    }

    // =========================
    // BOT
    // =========================

    public void OpenBotPanel()
    {
        ShowPanel(botPanel);
    }

    public void BackFromBotPanel()
    {
        botPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void PlayEasyBot()
    {
        StartBotGame(BotDifficulty.Easy);
    }

    public void PlayMediumBot()
    {
        StartBotGame(BotDifficulty.Medium);
    }

    public void PlayHardBot()
    {
        StartBotGame(BotDifficulty.Hard);
    }

    private void StartBotGame(BotDifficulty difficulty)
    {
        if (GameSession.Instance == null)
        {
            Debug.LogError("GameSession is missing.");
            return;
        }

        GameSession.Instance.StartBotGame(difficulty);
        LoadGameScene();
    }

    // =========================
    // LOCAL HUMAN
    // =========================

    public void OpenHumanSetup()
    {
        ShowPanel(humanSetupPanel);

        if (humanSetupManager != null)
            humanSetupManager.ResetPlayerNames();
    }

    public void StartHumanGame(string player1Name, string player2Name)
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
        ShowPanel(playInRoomMenuPanel);
    }

    public void BackFromPlayInRoom()
    {
        playInRoomMenuPanel.SetActive(false);
        mainMenuPanel.SetActive(true);

        Debug.Log("Returned to Main Menu.");
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

    private void ShowPanel(GameObject panel)
    {
        ShowMainMenu();
        if (panel == null) return;
        mainMenuPanel.SetActive(false);
        panel.SetActive(true);
    }

    private void LoadGameScene()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("Game scene name is missing.");
            return;
        }

        if (storePanel != null)
            storePanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);

        SceneManager.LoadScene(gameSceneName);
    }
}