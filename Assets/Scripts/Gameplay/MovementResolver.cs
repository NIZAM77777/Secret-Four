using UnityEngine;

public class MovementResolver : MonoBehaviour
{
    [SerializeField]
    private BoardManager boardManager;

    public Vector2Int GetDestination(
        Vector2Int startPosition,
        MoveDirection direction)
    {
        Vector2Int offset =
            GetDirectionOffset(direction);

        Vector2Int destination =
            startPosition + offset;

        Board board =
            boardManager.CurrentBoard;

        // Outside board:
        // coin stays in original position.
        if (!board.IsInsideBoard(
            destination.x,
            destination.y))
        {
            return startPosition;
        }

        return destination;
    }

    private Vector2Int GetDirectionOffset(
        MoveDirection direction)
    {
        switch (direction)
        {
            case MoveDirection.Up:
                return new Vector2Int(0, 1);

            case MoveDirection.Down:
                return new Vector2Int(0, -1);

            case MoveDirection.Left:
                return new Vector2Int(-1, 0);

            case MoveDirection.Right:
                return new Vector2Int(1, 0);

            case MoveDirection.Stay:
                return Vector2Int.zero;

            default:
                return Vector2Int.zero;
        }
    }
}