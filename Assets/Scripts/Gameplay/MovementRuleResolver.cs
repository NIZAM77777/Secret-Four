using UnityEngine;

public class MovementRuleResolver : MonoBehaviour
{
    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private MovementResolver movementResolver;

    public MovementResolution Resolve(
        PlayerAction playerXAction,
        PlayerAction playerYAction)
    {
        MovementResult xResult =
            CalculateResult(playerXAction);

        MovementResult yResult =
            CalculateResult(playerYAction);

        MovementResolution resolution =
            new MovementResolution(
                xResult,
                yResult
            );

        Board board =
            boardManager.CurrentBoard;

        // ==========================================
        // BOTH COINS TARGET SAME DESTINATION
        // Both move visually, then both return.
        // ==========================================

        if (
            xResult.IsMoving &&
            yResult.IsMoving &&
            xResult.destination ==
            yResult.destination
        )
        {
            resolution.playerXCollision =
                MovementCollisionType.SameDestination;

            resolution.playerYCollision =
                MovementCollisionType.SameDestination;

            return resolution;
        }

        // ==========================================
        // CHECK X
        // ==========================================

        if (xResult.IsMoving)
        {
            if (IsBlockedForX(
                board,
                xResult,
                yResult))
            {
                resolution.playerXCollision =
                    MovementCollisionType.OccupiedCell;
            }
        }

        // ==========================================
        // CHECK Y
        // ==========================================

        if (yResult.IsMoving)
        {
            if (IsBlockedForY(
                board,
                xResult,
                yResult))
            {
                resolution.playerYCollision =
                    MovementCollisionType.OccupiedCell;
            }
        }

        // ==========================================
        // DEPENDENCY CHECK
        //
        // If a player cannot leave their starting
        // position, the other player cannot enter it.
        // ==========================================

        bool xBlocked =
            resolution.playerXCollision !=
            MovementCollisionType.None;

        bool yBlocked =
            resolution.playerYCollision !=
            MovementCollisionType.None;

        // Y wants X's position,
        // but X will remain there.
        if (
            yResult.IsMoving &&
            yResult.destination ==
            xResult.startPosition &&
            (
                !xResult.IsMoving ||
                xBlocked
            )
        )
        {
            resolution.playerYCollision =
                MovementCollisionType.OccupiedCell;
        }

        // X wants Y's position,
        // but Y will remain there.
        if (
            xResult.IsMoving &&
            xResult.destination ==
            yResult.startPosition &&
            (
                !yResult.IsMoving ||
                yBlocked
            )
        )
        {
            resolution.playerXCollision =
                MovementCollisionType.OccupiedCell;
        }

        return resolution;
    }

    private MovementResult CalculateResult(
        PlayerAction action)
    {
        Vector2Int destination =
            movementResolver.GetDestination(
                action.position,
                action.direction
            );

        return new MovementResult(
            action.playerType,
            action.position,
            destination,
            action.direction
        );
    }

    private bool IsBlockedForX(
        Board board,
        MovementResult xResult,
        MovementResult yResult)
    {
        BoardCell cell =
            board.GetCell(
                xResult.destination.x,
                xResult.destination.y
            );

        if (cell == null || cell.IsEmpty)
        {
            return false;
        }

        // X targets Y's starting position.
        // Allow it if Y successfully leaves.
        if (
            xResult.destination ==
            yResult.startPosition
        )
        {
            return !yResult.IsMoving;
        }

        // X staying in its own position.
        if (
            xResult.destination ==
            xResult.startPosition
        )
        {
            return false;
        }

        return true;
    }

    private bool IsBlockedForY(
        Board board,
        MovementResult xResult,
        MovementResult yResult)
    {
        BoardCell cell =
            board.GetCell(
                yResult.destination.x,
                yResult.destination.y
            );

        if (cell == null || cell.IsEmpty)
        {
            return false;
        }

        // Y targets X's starting position.
        // Allow it if X successfully leaves.
        if (
            yResult.destination ==
            xResult.startPosition
        )
        {
            return !xResult.IsMoving;
        }

        // Y staying in its own position.
        if (
            yResult.destination ==
            yResult.startPosition
        )
        {
            return false;
        }

        return true;
    }
}