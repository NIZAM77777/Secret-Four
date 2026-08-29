using UnityEngine;
using Unity.Netcode;


public class OnlinePlayerManager : MonoBehaviour
{
    public static OnlinePlayerManager Instance;


    public enum OnlinePlayerType
    {
        None,
        PlayerX,
        PlayerY
    }


    public OnlinePlayerType LocalPlayerType
    {
        get;
        private set;
    }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void Start()
    {
        InvokeRepeating(
            nameof(CheckPlayerAssignment),
            0.5f,
            0.5f
        );
    }


    private void CheckPlayerAssignment()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }


        if (!NetworkManager.Singleton.IsListening)
        {
            return;
        }


        if (NetworkManager.Singleton.IsHost)
        {
            if (LocalPlayerType !=
                OnlinePlayerType.PlayerX)
            {
                LocalPlayerType =
                    OnlinePlayerType.PlayerX;

                Debug.Log(
                    "ONLINE PLAYER ASSIGNMENT: HOST → PLAYER X"
                );
            }
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            if (LocalPlayerType !=
                OnlinePlayerType.PlayerY)
            {
                LocalPlayerType =
                    OnlinePlayerType.PlayerY;

                Debug.Log(
                    "ONLINE PLAYER ASSIGNMENT: CLIENT → PLAYER Y"
                );
            }
        }
    }


    public bool IsLocalPlayerX()
    {
        return LocalPlayerType ==
               OnlinePlayerType.PlayerX;
    }


    public bool IsLocalPlayerY()
    {
        return LocalPlayerType ==
               OnlinePlayerType.PlayerY;
    }
}