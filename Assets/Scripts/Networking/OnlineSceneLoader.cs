using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class OnlineSceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "Game";

    private bool gameStarting;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogError("OnlineSceneLoader: NetworkManager not found.");
            return;
        }

        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == gameSceneName) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null)
            return;

        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsHost)
            return;

        // The host is already connected; only the second player starts the game.
        if (clientId == networkManager.LocalClientId)
            return;

        // Never start two scene transitions for the same room.
        if (gameStarting)
            return;

        if (!networkManager.IsListening)
        {
            Debug.LogWarning("OnlineSceneLoader: Network is no longer listening.");
            return;
        }

        if (networkManager.SceneManager == null)
        {
            Debug.LogError("OnlineSceneLoader: Network SceneManager is missing.");
            return;
        }

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("OnlineSceneLoader: Game scene name is missing.");
            return;
        }

        gameStarting = true;

        if (GameSession.Instance == null)
        {
            Debug.LogError("OnlineSceneLoader: GameSession is missing.");
            gameStarting = false;
            return;
        }

        Debug.Log("Player 2 connected! Starting Secret Four game...");

        GameSession.Instance.StartOnlineRoomGame();

        // Online player profiles are initialized in the networked Game scene
        // before gameplay begins. The scene itself is started only after the
        // joiner has connected.
        networkManager.SceneManager.LoadScene(
            gameSceneName,
            LoadSceneMode.Single
        );

        // This helper is only needed while starting the network scene.
        Destroy(gameObject);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsHost)
            return;

        if (!gameStarting && clientId != NetworkManager.ServerClientId)
        {
            Debug.Log("Waiting client disconnected. Ready for another player.");
            gameStarting = false;
        }
    }
}
