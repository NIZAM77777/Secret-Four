using System.Collections.Generic;
using UnityEngine;

public class BotMoveAnalyzer : MonoBehaviour
{
    // =====================================================
    // GET VALID DIRECTIONS
    // =====================================================

    public List<MoveDirection> GetValidDirections(
    Board board,
    Vector2Int position,
    Vector2Int otherPlayerPosition)
    {
        List<MoveDirection> directions =
            new List<MoveDirection>();


        // =============================================
        // STAY IS ALWAYS AVAILABLE
        // =============================================

        directions.Add(
            MoveDirection.Stay
        );


        TryAddDirection(
            directions,
            board,
            position,
            otherPlayerPosition,
            MoveDirection.Up
        );

        TryAddDirection(
            directions,
            board,
            position,
            otherPlayerPosition,
            MoveDirection.Down
        );

        TryAddDirection(
            directions,
            board,
            position,
            otherPlayerPosition,
            MoveDirection.Left
        );

        TryAddDirection(
            directions,
            board,
            position,
            otherPlayerPosition,
            MoveDirection.Right
        );


        return directions;
    }


    // =====================================================
    // TRY ADD DIRECTION
    // =====================================================

    private void TryAddDirection(
        List<MoveDirection> directions,
        Board board,
        Vector2Int position,
        Vector2Int otherPlayerPosition,
        MoveDirection direction)
    {
        Vector2Int destination =
            GetDestination(
                position,
                direction
            );


        // Outside board

        if (!board.IsInsideBoard(
            destination.x,
            destination.y))
        {
            return;
        }


        // Empty destination

        if (board.IsEmpty(
            destination.x,
            destination.y))
        {
            directions.Add(direction);

            return;
        }


        // Allow moving toward the other
        // current-round coin position.

        if (destination ==
            otherPlayerPosition)
        {
            directions.Add(direction);
        }
    }


    // =====================================================
    // GET DESTINATION
    // =====================================================

    public Vector2Int GetDestination(
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


            case MoveDirection.Stay:
            default:
                return position;
        }
    }


    // =====================================================
    // CHECK IF POSITION CREATES FOUR
    // =====================================================

    public bool WouldCreateFour(
        Board board,
        Vector2Int position,
        PlayerType player)
    {
        return
            CountLine(
                board,
                position,
                player,
                1,
                0
            ) >= 4 ||

            CountLine(
                board,
                position,
                player,
                0,
                1
            ) >= 4 ||

            CountLine(
                board,
                position,
                player,
                1,
                1
            ) >= 4 ||

            CountLine(
                board,
                position,
                player,
                1,
                -1
            ) >= 4;
    }


    // =====================================================
    // COUNT LINE
    // =====================================================

    private int CountLine(
        Board board,
        Vector2Int position,
        PlayerType player,
        int directionX,
        int directionY)
    {
        int count = 1;


        count += CountInDirection(
            board,
            position,
            player,
            directionX,
            directionY
        );


        count += CountInDirection(
            board,
            position,
            player,
            -directionX,
            -directionY
        );


        return count;
    }


    // =====================================================
    // COUNT IN DIRECTION
    // =====================================================

    private int CountInDirection(
        Board board,
        Vector2Int startPosition,
        PlayerType player,
        int directionX,
        int directionY)
    {
        int count = 0;

        int x =
            startPosition.x + directionX;

        int y =
            startPosition.y + directionY;


        while (board.IsInsideBoard(x, y))
        {
            BoardCell cell =
                board.GetCell(x, y);


            if (cell == null ||
                cell.IsEmpty ||
                cell.Occupant != player)
            {
                break;
            }


            count++;

            x += directionX;
            y += directionY;
        }


        return count;
    }
}