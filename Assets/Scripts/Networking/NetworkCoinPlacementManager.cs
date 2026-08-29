using UnityEngine;
using Unity.Netcode;


public class NetworkCoinPlacementManager
    : NetworkBehaviour
{
    [SerializeField]
    private PlacementManager placementManager;


    // =========================================
    // REQUEST FROM LOCAL PLAYER
    // =========================================

    public void RequestPlaceCoin(
        int x,
        int y)
    {
        if (!IsSpawned)
        {
            Debug.LogError(
                "NetworkCoinPlacementManager is not spawned."
            );

            return;
        }


        Debug.Log(
            $"Network placement requested: ({x},{y})"
        );


        RequestPlaceCoinServerRpc(
            x,
            y
        );
    }


    // =========================================
    // HOST VALIDATES
    // =========================================

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlaceCoinServerRpc(
        int x,
        int y,
        ServerRpcParams rpcParams = default)
    {
        if (placementManager == null)
        {
            Debug.LogError(
                "PlacementManager is missing."
            );

            return;
        }


        PlayerType player =
            GetCurrentPlayer();


        // ---------------------------------
        // SECURITY CHECK
        // ---------------------------------

        ulong senderId =
            rpcParams.Receive.SenderClientId;


        if (!IsPlayerAllowed(
            senderId,
            player))
        {
            Debug.LogWarning(
                "Player attempted an illegal move."
            );

            return;
        }


        // ---------------------------------
        // HOST VALIDATES THE MOVE
        // ---------------------------------

        bool valid =
            IsMoveValid(
                x,
                y,
                player
            );


        if (!valid)
        {
            Debug.Log(
                "Host rejected placement."
            );

            return;
        }


        Debug.Log(
            $"HOST accepted placement: " +
            $"{player} → ({x},{y})"
        );


        // ---------------------------------
        // SEND RESULT TO EVERYONE
        // ---------------------------------

        PlaceCoinClientRpc(
            x,
            y,
            (int)player
        );
    }


    // =========================================
    // BOTH PLAYERS APPLY MOVE
    // =========================================

    [ClientRpc]
    private void PlaceCoinClientRpc(
        int x,
        int y,
        int playerValue)
    {
        if (placementManager == null)
        {
            Debug.LogError(
                "PlacementManager is missing."
            );

            return;
        }


        PlayerType player =
            (PlayerType)playerValue;


        Debug.Log(
            $"NETWORK MOVE RECEIVED: " +
            $"{player} → ({x},{y})"
        );


        placementManager.ApplyCoinPlacement(
            x,
            y,
            player
        );
    }


    // =========================================
    // CURRENT PLAYER
    // =========================================

    private PlayerType GetCurrentPlayer()
    {
        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();


        if (turnManager.CurrentState ==
            GameState.WaitingForPlayerXPlacement)
        {
            return PlayerType.X;
        }


        return PlayerType.Y;
    }


    // =========================================
    // VALIDATE MOVE
    // =========================================

    private bool IsMoveValid(
        int x,
        int y,
        PlayerType player)
    {
        BoardManager boardManager =
            FindFirstObjectByType<BoardManager>();


        TurnManager turnManager =
            FindFirstObjectByType<TurnManager>();


        if (boardManager == null ||
            turnManager == null)
        {
            return false;
        }


        if (!turnManager.CanPlaceCoin(player))
        {
            return false;
        }


        Board board =
            boardManager.CurrentBoard;


        if (!board.IsInsideBoard(x, y))
        {
            return false;
        }


        if (!board.IsEmpty(x, y))
        {
            return false;
        }


        return true;
    }


    // =========================================
    // CHECK PLAYER ID
    // =========================================

    private bool IsPlayerAllowed(
        ulong senderId,
        PlayerType player)
    {
        // ---------------------------------
        // HOST = Player X
        // ---------------------------------

        if (player == PlayerType.X)
        {
            return senderId ==
                   NetworkManager.ServerClientId;
        }


        // ---------------------------------
        // CLIENT = Player Y
        // ---------------------------------

        if (player == PlayerType.Y)
        {
            return senderId !=
                   NetworkManager.ServerClientId;
        }


        return false;
    }
}