using UnityEngine;

public class MovementResult
{
    public PlayerType playerType;

    public Vector2Int startPosition;
    public Vector2Int destination;

    public MoveDirection direction;

    public bool IsMoving =>
        startPosition != destination;

    public MovementResult(
        PlayerType playerType,
        Vector2Int startPosition,
        Vector2Int destination,
        MoveDirection direction)
    {
        this.playerType = playerType;
        this.startPosition = startPosition;
        this.destination = destination;
        this.direction = direction;
    }
}