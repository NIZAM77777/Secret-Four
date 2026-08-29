using UnityEngine;
using Unity.Netcode;

public class NetworkPlayAgainManager
    : NetworkBehaviour
{
    [Header("References")]

    [SerializeField]
    private GameResultManager gameResultManager;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private PlacementManager placementManager;

    [SerializeField]
    private DirectionButtonController directionButtonController;


    // =====================================================
    // READY STATUS
    // =====================================================

    private bool hostReady;

    private bool clientReady;


    // =====================================================
    // PLAY AGAIN BUTTON
    // =====================================================

    public void RequestPlayAgain()
    {
        if (!IsSpawned)
        {
            Debug.LogWarning(
                "NetworkPlayAgainManager is not spawned."
            );

            return;
        }

        Debug.Log(
            $"PLAYER {NetworkManager.LocalClientId} " +
            $"REQUESTED PLAY AGAIN"
        );

        RequestPlayAgainServerRpc();
    }


    // =====================================================
    // PLAYER REQUEST → HOST
    // =====================================================

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlayAgainServerRpc(
        ServerRpcParams rpcParams = default)
    {
        ulong playerId =
            rpcParams.Receive.SenderClientId;


        // =============================================
        // HOST READY
        // =============================================

        if (playerId ==
            NetworkManager.ServerClientId)
        {
            hostReady = true;

            Debug.Log(
                "HOST IS READY FOR PLAY AGAIN"
            );
        }


        // =============================================
        // CLIENT READY
        // =============================================

        else
        {
            clientReady = true;

            Debug.Log(
                "CLIENT IS READY FOR PLAY AGAIN"
            );
        }


        // Send status to everyone.

        UpdateReadyStatusClientRpc(
            hostReady,
            clientReady
        );


        // Check if both players pressed Play Again.

        CheckBothPlayersReady();
    }


    // =====================================================
    // CHECK BOTH PLAYERS
    // =====================================================

    private void CheckBothPlayersReady()
    {
        if (!IsServer)
        {
            return;
        }


        if (!hostReady || !clientReady)
        {
            return;
        }


        Debug.Log(
            "BOTH PLAYERS READY!"
        );


        // Reset ready flags BEFORE starting
        // the new match.

        hostReady = false;

        clientReady = false;


        // Tell both players to reset.

        RestartMatchClientRpc();
    }


    // =====================================================
    // READY STATUS
    // =====================================================

    [ClientRpc]
    private void UpdateReadyStatusClientRpc(
        bool hostIsReady,
        bool clientIsReady)
    {
        Debug.Log(
            $"PLAY AGAIN STATUS → " +
            $"HOST: {hostIsReady} | " +
            $"CLIENT: {clientIsReady}"
        );
    }


    // =====================================================
    // RESTART MATCH
    // =====================================================

    [ClientRpc]
    private void RestartMatchClientRpc()
    {
        Debug.Log(
            "NETWORK → NEW MATCH STARTING"
        );

        ResetCompleteMatch();
    }


    // =====================================================
    // RESET COMPLETE MATCH
    // =====================================================

    private void ResetCompleteMatch()
    {
        Debug.Log(
            "===== RESETTING MATCH ====="
        );


        // ---------------------------------------------
        // 1. Reset game result
        // ---------------------------------------------

        if (gameResultManager != null)
        {
            gameResultManager.ResetResult();
        }


        // ---------------------------------------------
        // 2. Clear board data
        // ---------------------------------------------

        if (boardManager != null)
        {
            boardManager.ResetBoard();
        }


        // ---------------------------------------------
        // 3. Remove old coin visuals
        // ---------------------------------------------

        if (placementManager != null)
        {
            placementManager.ClearAllCoins();
        }


        // ---------------------------------------------
        // 4. Reset player actions + turn state
        // ---------------------------------------------

        if (turnManager != null)
        {
            turnManager.ResetMatch();
        }


        // ---------------------------------------------
        // 5. Reset direction buttons
        // ---------------------------------------------

        if (directionButtonController != null)
        {
            directionButtonController.ResetButtons();
        }


        Debug.Log(
            "===== NEW MATCH READY ====="
        );
    }
}