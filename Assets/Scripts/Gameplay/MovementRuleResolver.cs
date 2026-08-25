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

        // =================================================
        // 1. Both actively move to the same destination
        // =================================================

        if (xResult.IsMoving &&
            yResult.IsMoving &&
            xResult.destination ==
            yResult.destination)
        {
            resolution.playerXCollision =
                MovementCollisionType.SameDestination;

            resolution.playerYCollision =
                MovementCollisionType.SameDestination;

            return resolution;
        }

        // =================================================
        // 2. Check X
        // =================================================

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

        // =================================================
        // 3. Check Y
        // =================================================

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

        // =================================================
        // 4. Dependency check
        //
        // If one coin is returning/staying at its
        // starting cell, the other cannot enter it.
        // =================================================

        bool xCollides =
            resolution.playerXCollision !=
            MovementCollisionType.None;

        bool yCollides =
            resolution.playerYCollision !=
            MovementCollisionType.None;

        // Y wants X's starting cell,
        // but X is not successfully leaving it.
        if (yResult.IsMoving &&
            yResult.destination ==
            xResult.startPosition &&
            (
                !xResult.IsMoving ||
                xCollides
            ))
        {
            resolution.playerYCollision =
                MovementCollisionType.OccupiedCell;
        }

        // X wants Y's starting cell,
        // but Y is not successfully leaving it.
        if (xResult.IsMoving &&
            xResult.destination ==
            yResult.startPosition &&
            (
                !yResult.IsMoving ||
                yCollides
            ))
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

    // =========================================================
    // X
    // =========================================================

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

        if (cell == null)
        {
            return false;
        }

        if (cell.IsEmpty)
        {
            return false;
        }

        // X is targeting Y's starting position.
        // Allowed only if Y successfully leaves.
        if (xResult.destination ==
            yResult.startPosition)
        {
            return !CanSuccessfullyLeave(
                yResult
            );
        }

        // X is targeting its own starting position.
        if (xResult.destination ==
            xResult.startPosition)
        {
            return false;
        }

        // Any other occupied cell is permanent.
        return true;
    }

    // =========================================================
    // Y
    // =========================================================

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

        if (cell == null)
        {
            return false;
        }

        if (cell.IsEmpty)
        {
            return false;
        }

        // Y is targeting X's starting position.
        // Allowed only if X successfully leaves.
        if (yResult.destination ==
            xResult.startPosition)
        {
            return !CanSuccessfullyLeave(
                xResult
            );
        }

        // Y is targeting its own starting position.
        if (yResult.destination ==
            yResult.startPosition)
        {
            return false;
        }

        // Any other occupied cell is permanent.
        return true;
    }

    private bool CanSuccessfullyLeave(
        MovementResult result)
    {
        return result.IsMoving;
    }
}