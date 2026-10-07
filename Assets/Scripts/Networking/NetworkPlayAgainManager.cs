using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class NetworkPlayAgainManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private GameResultManager gameResultManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private DirectionButtonController directionButtonController;
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private NetworkMovementManager networkMovementManager;

    public override void OnNetworkSpawn()
    {
        if (IsServer && NetworkGameState.Instance != null)
            NetworkGameState.Instance.SetRematchMask(0);

        if (NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null)
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    public void RequestPlayAgain()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning("NetworkPlayAgainManager is not spawned.");
            return;
        }

        RequestPlayAgainRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPlayAgainRpc(RpcParams rpcParams = default)
    {
        if (!IsServer || NetworkManager == null)
            return;

        ulong playerId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.ConnectedClientsIds.Contains(playerId))
        {
            Debug.LogWarning($"Rejected play-again request from unknown client {playerId}.");
            return;
        }

        NetworkGameState state = NetworkGameState.Instance;
        if (state == null)
        {
            Debug.LogError("NetworkGameState is missing.");
            return;
        }

        if (!state.IsCurrentMatchOver())
        {
            Debug.LogWarning("Rejected play-again request because the match is still active.");
            return;
        }

        byte mask = state.RematchReadyMask.Value;
        if (playerId == NetworkManager.ServerClientId)
            mask = (byte)(mask | 1);
        else
            mask = (byte)(mask | 2);

        state.SetRematchMask(mask);
        UpdateReadyStatusRpc(mask);

        if (mask == 3)
            StartAuthoritativeRematch();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateReadyStatusRpc(byte mask)
    {
        if (mask == 1 || mask == 2)
        {
            GameOverUI ui = FindFirstObjectByType<GameOverUI>();
            if (ui != null)
                ui.SetWaitingForOpponent((mask & (IsHost ? 1 : 2)) != 0);
        }
    }

    private void StartAuthoritativeRematch()
    {
        if (!IsServer)
            return;

        if (gameResultManager != null)
            gameResultManager.ResetResult();

        if (movementManager != null)
            movementManager.ResetNetworkMovementState();

        if (networkMovementManager != null)
            networkMovementManager.ResetNetworkMoveState();

        if (boardManager != null)
            boardManager.ResetBoard();

        if (placementManager != null)
            placementManager.ClearAllCoins();

        if (turnManager != null)
            turnManager.ResetMatch();

        if (directionButtonController != null)
            directionButtonController.ResetButtons();

        NetworkGameState.Instance.ResetServerMatch();

        Debug.Log("===== AUTHORITATIVE ONLINE REMATCH STARTED =====");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer || NetworkManager == null)
            return;

        if (clientId != NetworkManager.ServerClientId)
        {
            NetworkGameState.Instance?.SetRematchMask(0);
            Debug.Log("CLIENT DISCONNECTED. REMATCH READY STATE CLEARED.");
        }
    }
}