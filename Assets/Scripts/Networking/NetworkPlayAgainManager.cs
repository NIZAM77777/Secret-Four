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

    private bool hostReady;
    private bool clientReady;

    public override void OnNetworkSpawn()
    {
        hostReady = false;
        clientReady = false;

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

        Debug.Log($"PLAYER {NetworkManager.LocalClientId} REQUESTED PLAY AGAIN");
        RequestPlayAgainRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPlayAgainRpc(RpcParams rpcParams = default)
    {
        if (!IsServer || NetworkManager == null)
            return;

        ulong playerId = rpcParams.Receive.SenderClientId;

        // Only the two players in this match may request a rematch.
        if (!NetworkManager.ConnectedClientsIds.Contains(playerId))
        {
            Debug.LogWarning($"Rejected play-again request from unknown client {playerId}.");
            return;
        }

        // A rematch is only valid after the current match has finished.
        if (gameResultManager == null || !gameResultManager.HasResult)
        {
            Debug.LogWarning("Rejected play-again request because the match is still active.");
            return;
        }

        if (playerId == NetworkManager.ServerClientId)
        {
            hostReady = true;
            Debug.Log("HOST IS READY FOR PLAY AGAIN");
        }
        else
        {
            clientReady = true;
            Debug.Log("CLIENT IS READY FOR PLAY AGAIN");
        }

        UpdateReadyStatusRpc(hostReady, clientReady);
        CheckBothPlayersReady();
    }

    private void CheckBothPlayersReady()
    {
        if (!IsServer || !hostReady || !clientReady)
            return;

        if (gameResultManager == null || !gameResultManager.HasResult)
            return;

        Debug.Log("BOTH PLAYERS READY!");

        hostReady = false;
        clientReady = false;

        RestartMatchRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateReadyStatusRpc(bool hostIsReady, bool clientIsReady)
    {
        Debug.Log($"PLAY AGAIN STATUS → HOST: {hostIsReady} | CLIENT: {clientIsReady}");
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RestartMatchRpc()
    {
        Debug.Log("NETWORK → NEW MATCH STARTING");
        ResetCompleteMatch();
    }

    private void ResetCompleteMatch()
    {
        if (gameResultManager != null)
            gameResultManager.ResetResult();

        if (boardManager != null)
            boardManager.ResetBoard();

        if (placementManager != null)
            placementManager.ClearAllCoins();

        if (turnManager != null)
            turnManager.ResetMatch();

        if (directionButtonController != null)
            directionButtonController.ResetButtons();

        Debug.Log("===== NEW MATCH READY =====");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer || NetworkManager == null)
            return;

        // Clear stale rematch readiness if the second player leaves.
        if (clientId != NetworkManager.ServerClientId)
        {
            clientReady = false;
            Debug.Log("CLIENT DISCONNECTED. PLAY AGAIN READY STATE CLEARED.");
        }
    }
}
