using UnityEngine;
using Unity.Netcode;

public class DirectionSelector : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private MovementManager movementManager;

    [SerializeField]
    private PlacementManager placementManager;

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private NetworkDirectionManager networkDirectionManager;


    // =========================================
    // BUTTON FUNCTIONS
    // =========================================

    public void SelectUp()
    {
        SelectDirection(MoveDirection.Up);
    }

    public void SelectDown()
    {
        SelectDirection(MoveDirection.Down);
    }

    public void SelectLeft()
    {
        SelectDirection(MoveDirection.Left);
    }

    public void SelectRight()
    {
        SelectDirection(MoveDirection.Right);
    }

    public void SelectStay()
    {
        SelectDirection(MoveDirection.Stay);
    }


    // =========================================
    // MAIN DIRECTION SELECTION
    // =========================================

    public void SelectDirection(
        MoveDirection direction)
    {
        PlayerType player = GetCurrentDirectionPlayer();

        // Online input is a request only. The server is the sole authority
        // for the current turn and direction validity.
        if (IsOnlineGame())
        {
            if (networkDirectionManager == null)
            {
                Debug.LogError(
                    "NetworkDirectionManager is missing."
                );

                return;
            }

            if (!IsLocalPlayerAllowed(player))
            {
                Debug.Log(
                    "It is not your direction turn."
                );

                return;
            }

            if (NetworkGameState.Instance != null &&
                !NetworkGameState.Instance.CanRequestDirection(player))
            {
                Debug.Log(
                    "The authoritative match is processing another state."
                );

                return;
            }

            if (!IsValidDirectionValue(direction))
            {
                Debug.LogWarning(
                    $"Invalid direction request: {direction}"
                );

                return;
            }

            networkDirectionManager.RequestDirection(direction);
            return;
        }

        if (!turnManager.CanChooseDirection(player))
        {
            Debug.Log(
                "Direction cannot be selected right now."
            );

            return;
        }

        if (!IsDirectionValid(player, direction))
        {
            Debug.Log(
                $"{direction} is not valid."
            );

            return;
        }

        ApplyDirection(player, direction);
    }

    private bool IsValidDirectionValue(MoveDirection direction)
    {
        return direction == MoveDirection.Up ||
               direction == MoveDirection.Down ||
               direction == MoveDirection.Left ||
               direction == MoveDirection.Right ||
               direction == MoveDirection.Stay;
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
                "DirectionSelector: BoardManager is missing."
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
            turnManager.GetAction(player);


        Vector2Int position =
            action.position;


        // =====================================
        // CHECK STAY
        // =====================================

        if (direction == MoveDirection.Stay)
        {
            return true;
        }


        // =====================================
        // CHECK NORMAL MOVEMENT
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
    // CHECK DESTINATION
    // =========================================

    private bool CanMoveTo(
    Board board,
    Vector2Int destination,
    PlayerType player)
    {
        // =====================================
        // Outside board
        // =====================================

        if (!board.IsInsideBoard(
            destination.x,
            destination.y
        ))
        {
            return false;
        }


        // =====================================
        // Empty cell
        // =====================================

        if (board.IsEmpty(
            destination.x,
            destination.y
        ))
        {
            return true;
        }


        // =====================================
        // SIMULTANEOUS MOVEMENT
        // =====================================

        PlayerType otherPlayer =
            player == PlayerType.X
                ? PlayerType.Y
                : PlayerType.X;


        PlayerAction otherAction =
            turnManager.GetAction(
                otherPlayer
            );


        // Only allow movement toward the other
        // player's coin placed during THIS round.

        if (otherAction.hasPlaced &&
            destination == otherAction.position)
        {
            return true;
        }


        // =====================================
        // OCCUPIED CELL
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
    // APPLY DIRECTION
    // =========================================

    public void ApplyDirection(
        PlayerType player,
        MoveDirection direction)
    {
        if (!turnManager.CanChooseDirection(player))
        {
            Debug.Log(
                "Direction rejected."
            );

            return;
        }


        PlayerAction action =
            turnManager.GetAction(player);


        action.direction =
            direction;


        action.hasChosenDirection =
            true;


        Debug.Log(
            $"{player} selected {direction}"
        );


        turnManager.DirectionChosen(
            player
        );


        // -------------------------------------
        // BOTH DIRECTIONS READY
        // -------------------------------------

        if (turnManager.CurrentState ==
            GameState.RevealingDirections)
        {
            movementManager.StartMovementSequence(
                placementManager.PlayerXCoin,
                placementManager.PlayerYCoin
            );
        }
    }


    // =========================================
    // ONLINE CHECK
    // =========================================

    private bool IsOnlineGame()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening;
    }


    // =========================================
    // PLAYER PERMISSION
    // =========================================

    private bool IsLocalPlayerAllowed(
        PlayerType player)
    {
        if (NetworkManager.Singleton == null)
        {
            return true;
        }


        // HOST = PLAYER X

        if (NetworkManager.Singleton.IsHost &&
            player == PlayerType.X)
        {
            return true;
        }


        // CLIENT = PLAYER Y

        if (NetworkManager.Singleton.IsClient &&
            !NetworkManager.Singleton.IsHost &&
            player == PlayerType.Y)
        {
            return true;
        }


        return false;
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
}