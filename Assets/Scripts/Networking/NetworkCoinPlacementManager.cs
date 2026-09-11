using UnityEngine;
using Unity.Netcode;

public class NetworkCoinPlacementManager : NetworkBehaviour
{
    [SerializeField]
    private PlacementManager placementManager;

    public void RequestPlaceCoin(int x, int y)
    {
        if (!IsSpawned)
        {
            Debug.LogError("NetworkCoinPlacementManager is not spawned.");
            return;
        }

        Debug.Log($"Network placement requested: ({x},{y})");
        RequestPlaceCoinRpc(x, y);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPlaceCoinRpc(int x, int y, RpcParams rpcParams = default)
    {
        if (placementManager == null)
        {
            Debug.LogError("PlacementManager is missing.");
            return;
        }

        PlayerType player = GetCurrentPlayer();
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!IsPlayerAllowed(senderId, player))
        {
            Debug.LogWarning("Player attempted an illegal move.");
            return;
        }

        if (!IsMoveValid(x, y, player))
        {
            Debug.Log("Host rejected placement.");
            return;
        }

        Debug.Log($"HOST accepted placement: {player} → ({x},{y})");
        PlaceCoinRpc(x, y, (int)player);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlaceCoinRpc(int x, int y, int playerValue)
    {
        if (placementManager == null)
        {
            Debug.LogError("PlacementManager is missing.");
            return;
        }

        PlayerType player = (PlayerType)playerValue;
        Debug.Log($"NETWORK MOVE RECEIVED: {player} → ({x},{y})");
        placementManager.ApplyCoinPlacement(x, y, player);
    }

    private PlayerType GetCurrentPlayer()
    {
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();

        if (turnManager == null)
            return PlayerType.X;

        if (turnManager.CurrentState == GameState.WaitingForPlayerXPlacement)
            return PlayerType.X;

        return PlayerType.Y;
    }

    private bool IsMoveValid(int x, int y, PlayerType player)
    {
        BoardManager boardManager = FindFirstObjectByType<BoardManager>();
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();

        if (boardManager == null || turnManager == null)
            return false;

        if (!turnManager.CanPlaceCoin(player))
            return false;

        Board board = boardManager.CurrentBoard;
        if (board == null)
            return false;

        if (!board.IsInsideBoard(x, y))
            return false;

        if (!board.IsEmpty(x, y))
            return false;

        return true;
    }

    private bool IsPlayerAllowed(ulong senderId, PlayerType player)
    {
        if (player == PlayerType.X)
            return senderId == NetworkManager.ServerClientId;

        if (player == PlayerType.Y)
            return senderId != NetworkManager.ServerClientId;

        return false;
    }
}