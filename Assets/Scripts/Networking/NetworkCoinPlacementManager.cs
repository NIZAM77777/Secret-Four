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

        if (NetworkGameState.Instance != null &&
            !NetworkGameState.Instance.CanRequestPlacement(player))
        {
            Debug.Log("Local placement request ignored: authoritative state is not accepting this player.");
            return;
        }

        // Online cosmetics come only from the authoritative match profile.
        // Never read the local Store selection when sending a move for a room.
        OnlineMatchPlayerState profile = OnlineMatchPlayerState.Instance;
        if (profile == null || !profile.IsMatchReady())
        {
            Debug.Log("Online placement ignored: player profile is not ready.");
            return;
        }

        // The network role is immutable: Host = X, Joiner = Y.
        if (!profile.IsLocalSide(player))
        {
            Debug.Log("Online placement ignored: requested role is not the local network role.");
            return;
        }

        string selectedSkinId = profile.GetSkinId(player);
        Debug.Log($"Network placement requested: ({x},{y}), player = {player}, authoritative skin = {selectedSkinId}");
        RequestPlaceCoinRpc(x, y, (int)player, selectedSkinId ?? string.Empty);
    }

    public void RequestPlaceCoin(int x, int y)
    {
        PlayerType player = GetCurrentPlayer();
        RequestPlaceCoin(x, y, player);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestPlaceCoinRpc(
        int x,
        int y,
        int requestedPlayerValue,
        string selectedSkinId,
        RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

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

        if (NetworkGameState.Instance != null &&
            !NetworkGameState.Instance.CanRequestPlacement(player))
        {
            Debug.LogWarning($"HOST rejected placement because authoritative state is not accepting {player}.");
            return;
        }

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

        if (!placementManager.ApplyCoinPlacement(
            x,
            y,
            player,
            selectedSkinId))
        {
            Debug.LogWarning("HOST accepted placement could not be applied.");
            return;
        }

        if (NetworkGameState.Instance != null)
        {
            NetworkGameState.Instance.CapturePlacementSkin(
                x,
                y,
                selectedSkinId);

            NetworkGameState.Instance.CaptureFromServer();
        }
    }

    private PlayerType GetCurrentPlayer()
    {
        if (NetworkGameState.Instance != null)
        {
            return NetworkGameState.Instance.IsPhase(
                GameState.WaitingForPlayerXPlacement)
                ? PlayerType.X
                : PlayerType.Y;
        }

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

        if (NetworkGameState.Instance != null)
        {
            if (!NetworkGameState.Instance.CanRequestPlacement(player))
                return false;
        }
        else if (!turnManager.CanPlaceCoin(player))
        {
            return false;
        }

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