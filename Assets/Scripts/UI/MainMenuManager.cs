using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Store Buttons")]
    [SerializeField]
    private Button storeButton;

    [SerializeField]
    private Button storeCloseButton;

    [Header("Human Setup")]
    [SerializeField]
    private HumanSetupManager humanSetupManager;

    private void Awake()
    {
        ResolveStorePanel();
        AutoWireStoreButtons();
        CloseStore();
    }

    private void ResolveStorePanel()
    {
        if (storePanel != null)
            return;

        // GameObject.Find does not find inactive StorePanel objects.
        // Search the Main Menu scene hierarchy instead so the panel can start disabled.
        Scene currentScene = gameObject.scene;
        if (!currentScene.IsValid() || !currentScene.isLoaded)
            return;

        foreach (GameObject root in currentScene.GetRootGameObjects())
        {
            Transform found = FindChildRecursive(root.transform, "StorePanel");
            if (found != null)
            {
                storePanel = found.gameObject;
                return;
            }
        }
    }

    private void AutoWireStoreButtons()
    {
        if (storeButton == null)
        {
            GameObject found = GameObject.Find("StoreButton");
            if (found != null)
                storeButton = found.GetComponent<Button>();
        }

        if (storeCloseButton == null && storePanel != null)
        {
            Transform closeTransform = FindChildRecursive(storePanel.transform, "CloseButton");
            if (closeTransform != null)
                storeCloseButton = closeTransform.GetComponent<Button>();
        }

        if (storeButton != null)
        {
            storeButton.onClick.RemoveListener(OpenStore);
            storeButton.onClick.AddListener(OpenStore);
        }
        else
        {
            Debug.LogWarning("[Secret Four] StoreButton was not found or has no Button component.");
        }

        if (storeCloseButton != null)
        {
            storeCloseButton.onClick.RemoveListener(CloseStore);
            storeCloseButton.onClick.AddListener(CloseStore);
        }
        else
        {
            Debug.LogWarning("[Secret Four] StorePanel/CloseButton was not found or has no Button component.");
        }
    }

    private static Transform FindChildRecursive(Transform parent, string targetName)
    {
        if (parent == null)
            return null;

        if (parent.name == targetName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, targetName);
            if (result != null)
                return result;
        }

        return null;
    }

    // =========================
    // STORE
    // =========================

    public void OpenStore()
    {
        ResolveStorePanel();

        if (storePanel == null)
        {
            Debug.LogError("[Secret Four] Cannot open Store: StorePanel is missing.");
            return;
        }

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);

        if (humanSetupPanel != null)
            humanSetupPanel.SetActive(false);

        if (botPanel != null)
            botPanel.SetActive(false);

        if (playInRoomMenuPanel != null)
            playInRoomMenuPanel.SetActive(false);

        storePanel.SetActive(true);
    }

    public void CloseStore()
    {
        ResolveStorePanel();

        if (storePanel != null)
            storePanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

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
        mainMenuPanel.SetActive(false);
        humanSetupPanel.SetActive(true);

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

        GameSession.Instance.StartLocalHumanGame(player1Name, player2Name);
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
        Debug.Log("Play In Room menu opened.");
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

    private void LoadGameScene()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("Game scene name is missing.");
            return;
        }

        if (storePanel != null)
            storePanel.SetActive(false);

        SceneManager.LoadScene(gameSceneName);
    }
}
