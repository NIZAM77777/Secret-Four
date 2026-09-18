using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Resolves the local online player side from the authoritative host/joiner
/// assignment. Host is always X/LEFT, joining client is always Y/RIGHT.
/// </summary>
public class OnlinePlayerManager : MonoBehaviour
{
    public static OnlinePlayerManager Instance;

    public enum OnlinePlayerType
    {
        None,
        PlayerX,
        PlayerY
    }

    public OnlinePlayerType LocalPlayerType { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        ResolveAssignment();
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        ResolveAssignment();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        ResolveAssignment();
    }

    private void ResolveAssignment()
    {
        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
        {
            LocalPlayerType = OnlinePlayerType.None;
            return;
        }

        if (OnlineMatchPlayerState.Instance != null &&
            OnlineMatchPlayerState.Instance.IsClientAssigned(
                networkManager.LocalClientId))
        {
            PlayerType side =
                OnlineMatchPlayerState.Instance.GetSideForClient(
                    networkManager.LocalClientId);

            LocalPlayerType =
                side == PlayerType.X
                    ? OnlinePlayerType.PlayerX
                    : OnlinePlayerType.PlayerY;

            return;
        }

        // During the tiny interval before the profile NetworkObject is ready,
        // NGO host/client role remains the deterministic fallback.
        LocalPlayerType =
            networkManager.IsHost
                ? OnlinePlayerType.PlayerX
                : networkManager.IsClient
                    ? OnlinePlayerType.PlayerY
                    : OnlinePlayerType.None;
    }

    public bool IsLocalPlayerX()
    {
        ResolveAssignment();
        return LocalPlayerType == OnlinePlayerType.PlayerX;
    }

    public bool IsLocalPlayerY()
    {
        ResolveAssignment();
        return LocalPlayerType == OnlinePlayerType.PlayerY;
    }
}