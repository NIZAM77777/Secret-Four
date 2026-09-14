using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    private const string StoreSceneName = "MN7StoreSystem";
    private const string StorePanelName = "StorePanel";
    private const string StoreButtonName = "StoreButton";

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

    private Button storeButton;
    private AsyncOperation storeSceneLoadOperation;

    private void Awake()
    {
        AutoWireStoreButton();
    }

    private void OnDestroy()
    {
        if (storeButton != null)
            storeButton.onClick.RemoveListener(OpenStore);
    }

    private void AutoWireStoreButton()
    {
        GameObject storeButtonObject = GameObject.Find(StoreButtonName);
        if (storeButtonObject == null)
        {
            Debug.LogWarning("[Secret Four] StoreButton was not found in the Main Menu.");
            return;
        }

        storeButton = storeButtonObject.GetComponent<Button>();
        if (storeButton == null)
        {
            Debug.LogError("[Secret Four] StoreButton exists but has no Button component.");
            return;
        }

        storeButton.onClick.RemoveListener(OpenStore);
        storeButton.onClick.AddListener(OpenStore);
    }

    // =========================
    // STORE
    // =========================

    public void OpenStore()
    {
        Scene storeScene = SceneManager.GetSceneByName(StoreSceneName);
        if (storeScene.IsValid() && storeScene.isLoaded)
        {
            SetStorePanelVisible(storeScene, true);
            return;
        }

        if (storeSceneLoadOperation != null && !storeSceneLoadOperation.isDone)
            return;

        storeSceneLoadOperation = SceneManager.LoadSceneAsync(
            StoreSceneName,
            LoadSceneMode.Additive
        );

        storeSceneLoadOperation.completed += _ =>
        {
            storeSceneLoadOperation = null;

            Scene loadedStoreScene = SceneManager.GetSceneByName(StoreSceneName);
            RemoveStoreSceneEventSystems(loadedStoreScene);
            SetStorePanelVisible(loadedStoreScene, true);
        };
    }

    public void CloseStore()
    {
        Scene storeScene = SceneManager.GetSceneByName(StoreSceneName);
        if (storeScene.IsValid() && storeScene.isLoaded)
            SetStorePanelVisible(storeScene, false);
    }

    private static void SetStorePanelVisible(Scene storeScene, bool visible)
    {
        if (!storeScene.IsValid() || !storeScene.isLoaded)
            return;

        foreach (GameObject root in storeScene.GetRootGameObjects())
        {
            Transform panel = FindChildRecursive(root.transform, StorePanelName);
            if (panel != null)
            {
                panel.gameObject.SetActive(visible);
                return;
            }
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

    private static void RemoveStoreSceneEventSystems(Scene storeScene)
    {
        if (!storeScene.IsValid() || !storeScene.isLoaded)
            return;

        foreach (GameObject root in storeScene.GetRootGameObjects())
        {
            EventSystem[] eventSystems = root.GetComponentsInChildren<EventSystem>(true);
            foreach (EventSystem eventSystem in eventSystems)
            {
                if (eventSystem != null)
                    Destroy(eventSystem.gameObject);
            }
        }
    }

    private void UnloadStoreSceneBeforeGame()
    {
        Scene storeScene = SceneManager.GetSceneByName(StoreSceneName);
        if (!storeScene.IsValid() || !storeScene.isLoaded)
        {
            LoadGameSceneImmediately();
            return;
        }

        StartCoroutine(UnloadStoreAndLoadGame(storeScene));
    }

    private IEnumerator UnloadStoreAndLoadGame(Scene storeScene)
    {
        AsyncOperation unload = SceneManager.UnloadSceneAsync(storeScene);
        if (unload != null)
            yield return unload;

        LoadGameSceneImmediately();
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

        UnloadStoreSceneBeforeGame();
    }

    private void LoadGameSceneImmediately()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("Game scene name is missing.");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }
}
