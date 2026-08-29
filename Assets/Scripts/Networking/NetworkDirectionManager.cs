using UnityEngine;
using Unity.Netcode;


public class NetworkDirectionManager
    : NetworkBehaviour
{
    [SerializeField]
    private DirectionSelector directionSelector;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private NetworkMovementManager networkMovementManager;


    // =========================================
    // LOCAL REQUEST
    // =========================================

    public void RequestDirection(
        MoveDirection direction)
    {
        if (!IsSpawned)
        {
            Debug.LogError(
                "NetworkDirectionManager is not spawned."
            );

            return;
        }


        RequestDirectionServerRpc(
            (int)direction
        );
    }


    // =========================================
    // HOST VALIDATES + STORES SECRETLY
    // =========================================

    [ServerRpc(RequireOwnership = false)]
    private void RequestDirectionServerRpc(
        int directionValue,
        ServerRpcParams rpcParams = default)
    {
        if (turnManager == null)
        {
            Debug.LogError(
                "TurnManager is missing."
            );

            return;
        }


        PlayerType player =
            GetCurrentDirectionPlayer();


        MoveDirection direction =
            (MoveDirection)directionValue;


        ulong senderId =
            rpcParams.Receive.SenderClientId;


        // -------------------------------------
        // PLAYER VALIDATION
        // -------------------------------------

        if (!IsPlayerAllowed(
            senderId,
            player))
        {
            Debug.LogWarning(
                "Illegal direction request."
            );

            return;
        }


        // -------------------------------------
        // TURN VALIDATION
        // -------------------------------------

        if (!turnManager.CanChooseDirection(player))
        {
            Debug.Log(
                "Direction rejected."
            );

            return;
        }


        // -------------------------------------
        // STORE ON HOST ONLY 🔒
        // -------------------------------------

        PlayerAction action =
            turnManager.GetAction(player);


        action.direction =
            direction;


        action.hasChosenDirection =
            true;


        Debug.Log(
            $"HOST secretly stored: " +
            $"{player} → {direction}"
        );


        // Move to next state

        turnManager.DirectionChosen(player);


        // -------------------------------------
        // CHECK IF BOTH READY
        // -------------------------------------

        if (turnManager.CurrentState ==
            GameState.RevealingDirections)
        {
            RevealDirectionsClientRpc(
                (int)turnManager.PlayerXAction.direction,
                (int)turnManager.PlayerYAction.direction
            );
            networkMovementManager.RequestMovementResolution();
        }
        else
        {
            // Inform players that selection
            // was accepted, without revealing
            // the direction.

            DirectionAcceptedClientRpc(
                (int)player
            );
        }
    }


    // =========================================
    // DIRECTION ACCEPTED — SECRET
    // =========================================

    [ClientRpc]
    private void DirectionAcceptedClientRpc(
        int playerValue)
    {
        PlayerType player =
            (PlayerType)playerValue;


        Debug.Log(
            $"{player} direction selected."
        );

        // IMPORTANT:
        // Do not reveal the direction here.
    }


    // =========================================
    // REVEAL BOTH DIRECTIONS ⭐
    // =========================================

    [ClientRpc]
    private void RevealDirectionsClientRpc(
        int playerXDirection,
        int playerYDirection)
    {
        if (directionSelector == null ||
            turnManager == null)
        {
            Debug.LogError(
                "Direction references missing."
            );

            return;
        }


        // -------------------------------------
        // APPLY X
        // -------------------------------------

        PlayerAction xAction =
            turnManager.PlayerXAction;


        xAction.direction =
            (MoveDirection)playerXDirection;


        xAction.hasChosenDirection =
            true;


        // -------------------------------------
        // APPLY Y
        // -------------------------------------

        PlayerAction yAction =
            turnManager.PlayerYAction;


        yAction.direction =
            (MoveDirection)playerYDirection;


        yAction.hasChosenDirection =
            true;


        Debug.Log(
            "DIRECTIONS REVEALED!"
        );


        Debug.Log(
            $"X → {xAction.direction}"
        );


        Debug.Log(
            $"Y → {yAction.direction}"
        );


        // Make sure both clients reach
        // the reveal state

        if (turnManager.CurrentState !=
            GameState.RevealingDirections)
        {
            turnManager.SetState(
                GameState.RevealingDirections
            );
        }
    }


    // =========================================
    // CURRENT PLAYER
    // =========================================

    private PlayerType GetCurrentDirectionPlayer()
    {
        if (turnManager.CurrentState ==
            GameState.WaitingForPlayerXDirection)
        {
            return PlayerType.X;
        }


        return PlayerType.Y;
    }


    // =========================================
    // PLAYER SECURITY
    // =========================================

    private bool IsPlayerAllowed(
        ulong senderId,
        PlayerType player)
    {
        if (player == PlayerType.X)
        {
            return senderId ==
                   NetworkManager.ServerClientId;
        }


        if (player == PlayerType.Y)
        {
            return senderId !=
                   NetworkManager.ServerClientId;
        }


        return false;
    }
}