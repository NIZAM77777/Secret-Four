using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;


public class OnlineSceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]

    [SerializeField]
    private string gameSceneName = "GameScene";


    private bool gameStarting;


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {
        NetworkManager networkManager =
            NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogError(
                "OnlineSceneLoader: NetworkManager not found."
            );

            return;
        }

        networkManager.OnClientConnectedCallback +=
            OnClientConnected;

        networkManager.OnClientDisconnectCallback +=
            OnClientDisconnected;
    }


    private void OnDestroy()
    {
        NetworkManager networkManager =
            NetworkManager.Singleton;

        if (networkManager == null)
        {
            return;
        }

        networkManager.OnClientConnectedCallback -=
            OnClientConnected;

        networkManager.OnClientDisconnectCallback -=
            OnClientDisconnected;
    }


    private void OnClientConnected(
        ulong clientId)
    {
        NetworkManager networkManager =
            NetworkManager.Singleton;

        if (networkManager == null ||
            !networkManager.IsHost)
        {
            return;
        }

        // Ignore the Host itself.
        if (clientId == networkManager.LocalClientId)
        {
            return;
        }

        if (gameStarting)
        {
            return;
        }

        gameStarting = true;

        Debug.Log(
            "Player 2 connected!"
        );

        Debug.Log(
            "Loading GameScene for both players..."
        );

        if (GameSession.Instance != null)
        {
            GameSession.Instance.StartOnlineRoomGame();
        }
        else
        {
            Debug.LogError(
                "OnlineSceneLoader: GameSession is missing."
            );

            gameStarting = false;
            return;
        }

        networkManager.SceneManager.LoadScene(
            gameSceneName,
            LoadSceneMode.Single
        );

        // This loader only exists to start the networked
        // scene transition. A fresh instance is created
        // with the next Main Menu scene, preventing stale
        // callbacks/state from surviving into another room.
        Destroy(gameObject);
    }


    private void OnClientDisconnected(
        ulong clientId)
    {
        NetworkManager networkManager =
            NetworkManager.Singleton;

        if (networkManager == null ||
            !networkManager.IsHost)
        {
            return;
        }

        // If the waiting client leaves before the game
        // starts, allow the host to create another room.
        if (!gameStarting &&
            clientId != NetworkManager.ServerClientId)
        {
            Debug.Log(
                "Waiting client disconnected. Online scene loader is ready again."
            );
        }
    }
}