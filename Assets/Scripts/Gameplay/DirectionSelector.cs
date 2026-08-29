using UnityEngine;
using Unity.Netcode;

public class DirectionSelector : MonoBehaviour
{
    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private MovementManager movementManager;

    [SerializeField]
    private PlacementManager placementManager;

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
        PlayerType player =
            GetCurrentDirectionPlayer();


        if (!turnManager.CanChooseDirection(player))
        {
            Debug.Log(
                "Direction cannot be selected right now."
            );

            return;
        }


        // =====================================
        // ONLINE MODE
        // =====================================

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


            networkDirectionManager
                .RequestDirection(direction);

            return;
        }


        // =====================================
        // LOCAL MODE
        // =====================================

        ApplyDirection(
            player,
            direction
        );
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