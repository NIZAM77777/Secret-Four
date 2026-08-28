using UnityEngine;

public class MovementResolution
{
    public MovementResult playerXResult;
    public MovementResult playerYResult;

    public MovementCollisionType playerXCollision;
    public MovementCollisionType playerYCollision;

    public bool AnyCollision =>
        playerXCollision != MovementCollisionType.None ||
        playerYCollision != MovementCollisionType.None;

    public MovementResolution(
        MovementResult playerXResult,
        MovementResult playerYResult)
    {
        this.playerXResult = playerXResult;
        this.playerYResult = playerYResult;

        playerXCollision =
            MovementCollisionType.None;

        playerYCollision =
            MovementCollisionType.None;
    }
}