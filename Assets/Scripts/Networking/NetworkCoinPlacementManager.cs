using UnityEngine;
using Unity.Netcode;

public class NetworkCoinPlacementManager : NetworkBehaviour
{
    [SerializeField]
    private PlacementManager placementManager;

    public void RequestPlaceCoin(int x, int y, PlayerType player)
    {
        if (!IsSpawned)
        {
            Debug.LogError("NetworkCoinPlacementManager is not spawned.");
            return;
        }

        string selectedSkinId = SecretFourStoreIntegration.GetSelectedPlayerItemId(player);
        Debug.Log($"Network placement requested: ({x},{y}), player = {player}, skin = {selectedSkinId}");
        RequestPlaceCoinRpc(x, y, (int)player, selectedSkinId ?? string.Empty);
    }

    // Compatibility overload for any older callers. The placement manager now supplies
    // the explicit player so X and Y can use different selection groups.
    public void RequestPlaceCoin(int x, int y)
    {
        PlayerType player = GetCurrentPlayer();
        RequestPlaceCoin(x, y, player);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPlaceCoinRpc(int x, int y, int requestedPlayerValue, string selectedSkinId, RpcParams rpcParams = default)
    {
        if (placementManager == null)
        {
            Debug.LogError("PlacementManager is missing.");
            return;
        }

        PlayerType player = (PlayerType)requestedPlayerValue;
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!IsPlayerAllowed(senderId, player))
        {
            Debug.LogWarning("Player attempted an illegal move.");
            return;
        }

        // Do not allow a client to submit a skin belonging to the other player's
        // selection group. This keeps X/Y cosmetics independent online as well.
        if (!SecretFourStoreIntegration.IsValidPlayerSkin(selectedSkinId, player))
        {
            Debug.LogWarning($"Rejected invalid {player} skin: {selectedSkinId}");
            return;
        }

        if (!IsMoveValid(x, y, player))
        {
            Debug.Log("Host rejected placement.");
            return;
        }

        Debug.Log($"HOST accepted placement: {player} → ({x},{y}), skin = {selectedSkinId}");
        PlaceCoinRpc(x, y, (int)player, selectedSkinId);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlaceCoinRpc(int x, int y, int playerValue, string selectedSkinId)
    {
        if (placementManager == null)
        {
            Debug.LogError("PlacementManager is missing.");
            return;
        }

        PlayerType player = (PlayerType)playerValue;
        Debug.Log($"NETWORK MOVE RECEIVED: {player} → ({x},{y}), skin = {selectedSkinId}");
        placementManager.ApplyCoinPlacement(x, y, player, selectedSkinId);
    }

    private PlayerType GetCurrentPlayer()
    {
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager == null)
            return PlayerType.X;

        return turnManager.CurrentState == GameState.WaitingForPlayerXPlacement
            ? PlayerType.X
            : PlayerType.Y;
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
        if (board == null || !board.IsInsideBoard(x, y) || !board.IsEmpty(x, y))
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
