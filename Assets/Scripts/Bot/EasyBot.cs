using System.Collections.Generic;
using UnityEngine;

public class EasyBot : MonoBehaviour
{
    // ==========================================
    // CHOOSE POSITION
    // ==========================================

    public Vector2Int ChoosePosition(
        Board board)
    {
        List<Vector2Int> emptyCells =
            GetEmptyCells(board);

        if (emptyCells.Count == 0)
        {
            return Vector2Int.zero;
        }

        return emptyCells[
            Random.Range(
                0,
                emptyCells.Count
            )
        ];
    }


    // ==========================================
    // CHOOSE DIRECTION
    // ==========================================

    public MoveDirection ChooseDirection(
        Board board,
        Vector2Int position,
        Vector2Int playerXPosition)
    {
        List<MoveDirection> validDirections =
            GetValidDirections(
                board,
                position,
                playerXPosition
            );

        // --------------------------------------
        // NO VALID MOVEMENT
        // --------------------------------------

        if (validDirections.Count == 0)
        {
            return MoveDirection.Stay;
        }


        return validDirections[
            Random.Range(
                0,
                validDirections.Count
            )
        ];
    }


    // ==========================================
    // GET EMPTY CELLS
    // ==========================================

    private List<Vector2Int> GetEmptyCells(
        Board board)
    {
        List<Vector2Int> emptyCells =
            new List<Vector2Int>();


        for (int x = 0;
             x < board.Width;
             x++)
        {
            for (int y = 0;
                 y < board.Height;
                 y++)
            {
                if (board.IsEmpty(x, y))
                {
                    emptyCells.Add(
                        new Vector2Int(x, y)
                    );
                }
            }
        }


        return emptyCells;
    }


    // ==========================================
    // GET VALID DIRECTIONS
    // ==========================================

    private List<MoveDirection>
        GetValidDirections(
            Board board,
            Vector2Int position,
            Vector2Int playerXPosition)
    {
        List<MoveDirection> directions =
            new List<MoveDirection>();


        // ======================================
        // ADD ONLY REAL MOVEMENT DIRECTIONS
        // ======================================

        TryAddDirection(
            directions,
            board,
            position,
            playerXPosition,
            MoveDirection.Up
        );


        TryAddDirection(
            directions,
            board,
            position,
            playerXPosition,
            MoveDirection.Down
        );


        TryAddDirection(
            directions,
            board,
            position,
            playerXPosition,
            MoveDirection.Left
        );


        TryAddDirection(
            directions,
            board,
            position,
            playerXPosition,
            MoveDirection.Right
        );


        // IMPORTANT:
        //
        // Stay is NOT added here.
        //
        // Stay will only be returned by
        // ChooseDirection when there are
        // no valid movement directions.


        return directions;
    }


    // ==========================================
    // TRY ADD DIRECTION
    // ==========================================

    private void TryAddDirection(
        List<MoveDirection> directions,
        Board board,
        Vector2Int position,
        Vector2Int playerXPosition,
        MoveDirection direction)
    {
        Vector2Int destination =
            GetDestination(
                position,
                direction
            );


        // ======================================
        // OUTSIDE BOARD
        // ======================================

        if (!board.IsInsideBoard(
            destination.x,
            destination.y))
        {
            return;
        }


        // ======================================
        // EMPTY DESTINATION
        // ======================================

        if (board.IsEmpty(
            destination.x,
            destination.y))
        {
            directions.Add(direction);

            return;
        }


        // ======================================
        // ALLOW PLAYER X NEWLY PLACED POSITION
        // ======================================

        // This allows simultaneous movement.

        if (destination == playerXPosition)
        {
            directions.Add(direction);
        }
    }


    // ==========================================
    // GET DESTINATION
    // ==========================================

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
}