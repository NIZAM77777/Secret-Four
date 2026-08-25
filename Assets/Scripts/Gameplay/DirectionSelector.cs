using UnityEngine;

public class DirectionSelector : MonoBehaviour
{
    [SerializeField]
    private TurnManager turnManager;

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

    private void SelectDirection(MoveDirection direction)
    {
        PlayerType player = GetCurrentDirectionPlayer();

        if (!turnManager.CanChooseDirection(player))
        {
            Debug.Log(
                "Direction cannot be selected right now."
            );

            return;
        }

        PlayerAction action =
            turnManager.GetAction(player);

        action.direction = direction;
        action.hasChosenDirection = true;

        Debug.Log(
            $"{player} secretly selected {direction}"
        );

        turnManager.DirectionChosen(player);
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