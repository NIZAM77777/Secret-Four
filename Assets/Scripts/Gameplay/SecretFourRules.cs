using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared simultaneous-move rules for live matches and bot simulations.</summary>
public static class SecretFourRules
{
    public static readonly MoveDirection[] Directions = {
        MoveDirection.Stay, MoveDirection.Up, MoveDirection.Down, MoveDirection.Left, MoveDirection.Right
    };
    public static bool IsDirection(MoveDirection direction) =>
        direction >= MoveDirection.Up && direction <= MoveDirection.Stay;

    public static Vector2Int Destination(Vector2Int start, MoveDirection direction)
    {
        switch (direction)
        {
            case MoveDirection.Up: return start + Vector2Int.up;
            case MoveDirection.Down: return start + Vector2Int.down;
            case MoveDirection.Left: return start + Vector2Int.left;
            case MoveDirection.Right: return start + Vector2Int.right;
            default: return start;
        }
    }
    public static bool CanChooseDirection(Board board, Vector2Int start,
        MoveDirection direction, Vector2Int? otherActiveCoin = null)
    {
        if (board == null || !board.IsInsideBoard(start.x, start.y) || !IsDirection(direction)) return false;
        if (direction == MoveDirection.Stay) return true;
        Vector2Int target = Destination(start, direction);
        return board.IsInsideBoard(target.x, target.y) &&
            (board.IsEmpty(target.x, target.y) || (otherActiveCoin.HasValue && target == otherActiveCoin.Value));
    }
    public static List<MoveDirection> LegalDirections(Board board, Vector2Int start, Vector2Int? otherActiveCoin = null)
    {
        var result = new List<MoveDirection>(5);
        foreach (MoveDirection direction in Directions)
            if (CanChooseDirection(board, start, direction, otherActiveCoin)) result.Add(direction);
        return result;
    }
    public static MovementResolution Resolve(Board board, PlayerAction x, PlayerAction y)
    {
        var xr = Result(board, x);
        var yr = Result(board, y);
        var result = new MovementResolution(xr, yr);
        if (xr.IsMoving && yr.IsMoving && xr.destination == yr.destination)
        {
            result.playerXCollision = MovementCollisionType.SameDestination;
            result.playerYCollision = MovementCollisionType.SameDestination;
            return result;
        }
        bool xb = IsBlocked(board, xr, yr, y.hasPlaced);
        bool yb = IsBlocked(board, yr, xr, x.hasPlaced);
        if (xr.IsMoving && y.hasPlaced && xr.destination == yr.startPosition && (!yr.IsMoving || yb)) xb = true;
        if (yr.IsMoving && x.hasPlaced && yr.destination == xr.startPosition && (!xr.IsMoving || xb)) yb = true;
        if (xb) result.playerXCollision = MovementCollisionType.OccupiedCell;
        if (yb) result.playerYCollision = MovementCollisionType.OccupiedCell;
        return result;
    }
    private static MovementResult Result(Board board, PlayerAction action)
    {
        Vector2Int target = action.hasPlaced ? Destination(action.position, action.direction) : action.position;
        if (!board.IsInsideBoard(target.x, target.y)) target = action.position;
        return new MovementResult(action.playerType, action.position, target, action.direction);
    }
    private static bool IsBlocked(Board board, MovementResult coin, MovementResult other, bool otherPlaced)
    {
        if (!coin.IsMoving || board.IsEmpty(coin.destination.x, coin.destination.y)) return false;
        return !(otherPlaced && other.IsMoving && coin.destination == other.startPosition);
    }
    public static Vector2Int FinalPosition(MovementResult result, MovementCollisionType collision) =>
        collision == MovementCollisionType.None ? result.destination : result.startPosition;
    public static void Apply(Board board, MovementResolution resolution)
    {
        MovementResult x = resolution.playerXResult;
        MovementResult y = resolution.playerYResult;
        board.ApplySuccessfulMovement(
            new MovementResult(PlayerType.X, x.startPosition, FinalPosition(x, resolution.playerXCollision), x.direction),
            new MovementResult(PlayerType.Y, y.startPosition, FinalPosition(y, resolution.playerYCollision), y.direction));
    }
    public static bool HasLine(Board board, PlayerType player, List<Vector2Int> positions = null)
    {
        positions?.Clear();
        for (int x = 0; x < board.Width; x++)
        for (int y = 0; y < board.Height; y++)
        {
            if (board.GetCell(x, y).Occupant != player) continue;
            if (Line(board, player, x, y, 1, 0, positions) || Line(board, player, x, y, 0, 1, positions) ||
                Line(board, player, x, y, 1, 1, positions) || Line(board, player, x, y, 1, -1, positions)) return true;
        }
        return false;
    }
    private static bool Line(Board board, PlayerType player, int x, int y, int dx, int dy, List<Vector2Int> positions)
    {
        int count = 0;
        while (board.IsInsideBoard(x + count * dx, y + count * dy) &&
               board.GetCell(x + count * dx, y + count * dy).Occupant == player) count++;
        if (count < board.WinLength) return false;
        if (positions != null)
            for (int i = 0; i < count; i++) positions.Add(new Vector2Int(x + i * dx, y + i * dy));
        return true;
    }
}
