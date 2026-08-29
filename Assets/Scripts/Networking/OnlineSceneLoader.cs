using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;


public class OnlineSceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]

    [SerializeField]
    private string gameSceneName = "GameScene";


    private bool gameStarting = false;


    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError(
                "NetworkManager not found."
            );

            return;
        }


        NetworkManager.Singleton.OnClientConnectedCallback +=
            OnClientConnected;
    }


    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -=
                OnClientConnected;
        }
    }


    private void OnClientConnected(
        ulong clientId)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            return;
        }


        // Ignore the Host itself
        if (clientId ==
            NetworkManager.Singleton.LocalClientId)
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


        GameSession.Instance.StartOnlineRoomGame();


        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName,
            LoadSceneMode.Single
        );
    }
}