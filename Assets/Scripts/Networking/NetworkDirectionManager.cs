using UnityEngine;
using Unity.Netcode;


public class NetworkDirectionManager
    : NetworkBehaviour
{
    [Header("References")]

    [SerializeField]
    private DirectionSelector directionSelector;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private NetworkMovementManager networkMovementManager;

    [SerializeField]
    private BoardManager boardManager;


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
        if (turnManager == null ||
            boardManager == null)
        {
            Debug.LogError(
                "Network references are missing."
            );

            return;
        }


        // =====================================
        // VALIDATE ENUM VALUE
        // =====================================

        if (!IsValidDirectionValue(
            directionValue
        ))
        {
            Debug.LogWarning(
                "Invalid direction value received."
            );

            return;
        }


        PlayerType player =
            GetCurrentDirectionPlayer();


        MoveDirection direction =
            (MoveDirection)directionValue;


        ulong senderId =
            rpcParams.Receive.SenderClientId;


        // =====================================
        // PLAYER VALIDATION
        // =====================================

        if (!IsPlayerAllowed(
            senderId,
            player
        ))
        {
            Debug.LogWarning(
                "Illegal direction request."
            );

            return;
        }


        // =====================================
        // TURN VALIDATION
        // =====================================

        if (!turnManager.CanChooseDirection(
            player
        ))
        {
            Debug.Log(
                "Direction rejected."
            );

            return;
        }


        // =====================================
        // DIRECTION VALIDATION
        // =====================================

        if (!IsDirectionValid(
            player,
            direction
        ))
        {
            Debug.LogWarning(
                $"HOST rejected invalid direction: " +
                $"{player} → {direction}"
            );

            return;
        }


        // =====================================
        // STORE SECRETLY ON HOST
        // =====================================

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


        // =====================================
        // UPDATE TURN
        // =====================================

        turnManager.DirectionChosen(
            player
        );


        // =====================================
        // BOTH DIRECTIONS READY
        // =====================================

        if (turnManager.CurrentState ==
            GameState.RevealingDirections)
        {
            // ---------------------------------
            // Reveal directions to everyone
            // ---------------------------------

            RevealDirectionsClientRpc(
                (int)turnManager
                    .PlayerXAction
                    .direction,

                (int)turnManager
                    .PlayerYAction
                    .direction
            );


            // ---------------------------------
            // Host resolves movement
            // ---------------------------------

            if (networkMovementManager != null)
            {
                networkMovementManager
                    .RequestMovementResolution();
            }
            else
            {
                Debug.LogError(
                    "NetworkMovementManager is missing."
                );
            }
        }
        else
        {
            // Direction accepted,
            // but keep it secret.

            DirectionAcceptedClientRpc(
                (int)player
            );
        }
    }


    // =========================================
    // VALIDATE DIRECTION ENUM
    // =========================================

    private bool IsValidDirectionValue(
        int directionValue)
    {
        return directionValue ==
                   (int)MoveDirection.Up ||

               directionValue ==
                   (int)MoveDirection.Down ||

               directionValue ==
                   (int)MoveDirection.Left ||

               directionValue ==
                   (int)MoveDirection.Right ||

               directionValue ==
                   (int)MoveDirection.Stay;
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


        // Do NOT reveal direction here.
    }


    // =========================================
    // REVEAL BOTH DIRECTIONS
    // =========================================

    [ClientRpc]
    private void RevealDirectionsClientRpc(
        int playerXDirection,
        int playerYDirection)
    {
        if (turnManager == null)
        {
            Debug.LogError(
                "TurnManager is missing."
            );

            return;
        }


        // =====================================
        // APPLY X DIRECTION
        // =====================================

        PlayerAction xAction =
            turnManager.PlayerXAction;


        xAction.direction =
            (MoveDirection)playerXDirection;


        xAction.hasChosenDirection =
            true;


        // =====================================
        // APPLY Y DIRECTION
        // =====================================

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


        // =====================================
        // SYNCHRONIZE GAME STATE
        // =====================================

        if (turnManager.CurrentState !=
            GameState.RevealingDirections)
        {
            turnManager.SetState(
                GameState.RevealingDirections
            );
        }
    }


    // =========================================
    // VALIDATE DIRECTION
    // =========================================

    private bool IsDirectionValid(
        PlayerType player,
        MoveDirection direction)
    {
        if (boardManager == null)
        {
            Debug.LogError(
                "BoardManager is missing."
            );

            return false;
        }


        Board board =
            boardManager.CurrentBoard;


        if (board == null)
        {
            return false;
        }


        PlayerAction action =
            turnManager.GetAction(
                player
            );


        Vector2Int position =
            action.position;


        // =====================================
        // STAY RULE
        //
        // Stay is ONLY allowed when there is
        // NO valid movement direction.
        // =====================================

        if (direction ==
            MoveDirection.Stay)
        {
            return !HasAnyValidMovement(
                board,
                position,
                player
            );
        }


        // =====================================
        // NORMAL MOVEMENT
        // =====================================

        Vector2Int destination =
            GetDestination(
                position,
                direction
            );


        return CanMoveTo(
            board,
            destination,
            player
        );
    }


    // =========================================
    // CHECK IF ANY VALID MOVEMENT EXISTS
    // =========================================

    private bool HasAnyValidMovement(
        Board board,
        Vector2Int position,
        PlayerType player)
    {
        MoveDirection[] directions =
        {
            MoveDirection.Up,
            MoveDirection.Down,
            MoveDirection.Left,
            MoveDirection.Right
        };


        foreach (
            MoveDirection direction
            in directions
        )
        {
            Vector2Int destination =
                GetDestination(
                    position,
                    direction
                );


            if (CanMoveTo(
                board,
                destination,
                player
            ))
            {
                return true;
            }
        }


        return false;
    }


    // =========================================
    // CHECK DESTINATION
    // =========================================

    private bool CanMoveTo(
        Board board,
        Vector2Int destination,
        PlayerType player)
    {
        // =====================================
        // OUTSIDE BOARD
        // =====================================

        if (!board.IsInsideBoard(
            destination.x,
            destination.y
        ))
        {
            return false;
        }


        // =====================================
        // EMPTY CELL
        // =====================================

        if (board.IsEmpty(
            destination.x,
            destination.y
        ))
        {
            return true;
        }


        // =====================================
        // OTHER PLAYER'S CURRENT ROUND COIN
        //
        // Allow simultaneous movement / swapping.
        // =====================================

        PlayerType otherPlayer =
            player == PlayerType.X
                ? PlayerType.Y
                : PlayerType.X;


        PlayerAction otherAction =
            turnManager.GetAction(
                otherPlayer
            );


        if (otherAction.hasPlaced &&
            destination ==
            otherAction.position)
        {
            return true;
        }


        // =====================================
        // PERMANENT / OCCUPIED CELL
        // =====================================

        return false;
    }


    // =========================================
    // GET DESTINATION
    // =========================================

    private Vector2Int GetDestination(
        Vector2Int position,
        MoveDirection direction)
    {
        switch (direction)
        {
            case MoveDirection.Up:

                return new Vector2Int(
                    position.x,
                    position.y + 1
                );


            case MoveDirection.Down:

                return new Vector2Int(
                    position.x,
                    position.y - 1
                );


            case MoveDirection.Left:

                return new Vector2Int(
                    position.x - 1,
                    position.y
                );


            case MoveDirection.Right:

                return new Vector2Int(
                    position.x + 1,
                    position.y
                );


            default:

                return position;
        }
    }


    // =========================================
    // CURRENT DIRECTION PLAYER
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
        // HOST = PLAYER X

        if (player == PlayerType.X)
        {
            return senderId ==
                   NetworkManager.ServerClientId;
        }


        // CLIENT = PLAYER Y

        if (player == PlayerType.Y)
        {
            return senderId !=
                   NetworkManager.ServerClientId;
        }


        return false;
    }
}