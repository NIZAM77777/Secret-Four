using UnityEngine;

public class DirectionSelector : MonoBehaviour
{
    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private MovementManager movementManager;

    [SerializeField]
    private PlacementManager placementManager;

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

    private void SelectDirection(
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

        PlayerAction action =
            turnManager.GetAction(player);

        action.direction =
            direction;

        action.hasChosenDirection =
            true;

        Debug.Log(
            $"{player} secretly selected {direction}"
        );

        turnManager.DirectionChosen(
            player
        );

        // --------------------------------
        // Both players have selected.
        // Start movement.
        // --------------------------------

        if (turnManager.CurrentState ==
            GameState.RevealingDirections)
        {
            movementManager.StartMovementSequence(
                placementManager.PlayerXCoin,
                placementManager.PlayerYCoin
            );
        }
    }

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