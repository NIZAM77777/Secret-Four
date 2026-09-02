using System.Collections.Generic;
using UnityEngine;

public class MediumBot : MonoBehaviour
{
    private MoveDirection plannedDirection;

    private bool hasPlannedDirection;


    // =================================================
    // CHOOSE POSITION
    // =================================================

    public Vector2Int ChoosePosition(
        Board board,
        Vector2Int playerXPosition)
    {
        hasPlannedDirection = false;


        List<Vector2Int> emptyCells =
            GetEmptyCells(board);


        if (emptyCells.Count == 0)
        {
            return Vector2Int.zero;
        }


        // ==========================================
        // PRIORITY 1
        // TRY TO FIND A WINNING MOVE
        // ==========================================

        foreach (Vector2Int cell in emptyCells)
        {
            List<MoveDirection> directions =
                GetValidDirectionsForPosition(
                    board,
                    cell,
                    playerXPosition
                );


            foreach (MoveDirection direction
                in directions)
            {
                Vector2Int destination =
                    GetDestination(
                        cell,
                        direction
                    );


                if (WouldWinAtPosition(
                    board,
                    destination,
                    PlayerType.Y
                ))
                {
                    Debug.Log(
                        "MEDIUM BOT → WINNING MOVE FOUND!"
                    );


                    plannedDirection =
                        direction;

                    hasPlannedDirection =
                        true;


                    return cell;
                }
            }
        }


        // ==========================================
        // PRIORITY 2
        // BLOCK PLAYER X
        // ==========================================

        foreach (Vector2Int cell in emptyCells)
        {
            if (WouldCreateFour(
                board,
                cell,
                PlayerType.X
            ))
            {
                Debug.Log(
                    "MEDIUM BOT → BLOCKING PLAYER!"
                );


                List<MoveDirection> directions =
                    GetValidDirectionsForPosition(
                        board,
                        cell,
                        playerXPosition
                    );


                plannedDirection =
                    GetBestDirection(
                        board,
                        cell,
                        directions
                    );


                hasPlannedDirection =
                    true;


                return cell;
            }
        }


        // ==========================================
        // PRIORITY 3
        // FIND BEST ATTACKING POSITION
        // ==========================================

        Vector2Int bestPosition =
            GetBestPosition(
                board,
                emptyCells,
                PlayerType.Y
            );


        List<MoveDirection> bestDirections =
            GetValidDirectionsForPosition(
                board,
                bestPosition,
                playerXPosition
            );


        plannedDirection =
            GetBestDirection(
                board,
                bestPosition,
                bestDirections
            );


        hasPlannedDirection = true;


        return bestPosition;
    }


    // =================================================
    // CHOOSE DIRECTION
    // =================================================

    public MoveDirection ChooseDirection(
        Board board,
        Vector2Int position,
        Vector2Int playerXPosition)
    {
        List<MoveDirection> validDirections =
            GetValidDirectionsForPosition(
                board,
                position,
                playerXPosition
            );


        // ==========================================
        // NO MOVEMENT AVAILABLE
        // ==========================================

        if (validDirections.Count == 0)
        {
            hasPlannedDirection = false;

            return MoveDirection.Stay;
        }


        // ==========================================
        // USE PLANNED DIRECTION
        // ==========================================

        if (hasPlannedDirection &&
            validDirections.Contains(
                plannedDirection
            ))
        {
            Debug.Log(
                "MEDIUM BOT → Using planned direction: " +
                plannedDirection
            );


            hasPlannedDirection = false;

            return plannedDirection;
        }


        // ==========================================
        // FALLBACK
        // ==========================================

        return GetBestDirection(
            board,
            position,
            validDirections
        );
    }


    // =================================================
    // GET EMPTY CELLS
    // =================================================

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


    // =================================================
    // GET VALID DIRECTIONS
    // =================================================

    private List<MoveDirection>
     GetValidDirectionsForPosition(
         Board board,
         Vector2Int position,
         Vector2Int playerXPosition)
    {
        List<MoveDirection> directions =
            new List<MoveDirection>();


        // ==========================================
        // STAY IS ALWAYS AVAILABLE
        // ==========================================

        directions.Add(
            MoveDirection.Stay
        );


        // ==========================================
        // VALID MOVEMENT DIRECTIONS
        // ==========================================

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


        return directions;
    }


    // =================================================
    // TRY ADD DIRECTION
    // =================================================

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


        // Outside board

        if (!board.IsInsideBoard(
            destination.x,
            destination.y
        ))
        {
            return;
        }


        // Empty cell

        if (board.IsEmpty(
            destination.x,
            destination.y
        ))
        {
            directions.Add(direction);

            return;
        }


        // Allow simultaneous movement
        // toward Player X position

        if (destination == playerXPosition)
        {
            directions.Add(direction);
        }
    }


    // =================================================
    // GET BEST POSITION
    // =================================================

    private Vector2Int GetBestPosition(
        Board board,
        List<Vector2Int> emptyCells,
        PlayerType player)
    {
        Vector2Int bestCell =
            emptyCells[0];

        int bestScore =
            int.MinValue;


        foreach (Vector2Int cell in emptyCells)
        {
            int score =
                EvaluatePosition(
                    board,
                    cell,
                    player
                );


            if (score > bestScore)
            {
                bestScore =
                    score;

                bestCell =
                    cell;
            }
        }


        return bestCell;
    }


    // =================================================
    // GET BEST DIRECTION
    // =================================================

    private MoveDirection GetBestDirection(
        Board board,
        Vector2Int position,
        List<MoveDirection> directions)
    {
        // No movement available

        if (directions.Count == 0)
        {
            return MoveDirection.Stay;
        }


        MoveDirection bestDirection =
            directions[0];

        int bestScore =
            int.MinValue;


        foreach (MoveDirection direction
            in directions)
        {
            Vector2Int destination =
                GetDestination(
                    position,
                    direction
                );


            int score =
                EvaluatePosition(
                    board,
                    destination,
                    PlayerType.Y
                );


            if (score > bestScore)
            {
                bestScore =
                    score;

                bestDirection =
                    direction;
            }
        }


        return bestDirection;
    }


    // =================================================
    // EVALUATE POSITION
    // =================================================

    private int EvaluatePosition(
        Board board,
        Vector2Int position,
        PlayerType player)
    {
        int score = 0;


        score += EvaluateLine(
            board,
            position,
            player,
            1,
            0
        );


        score += EvaluateLine(
            board,
            position,
            player,
            0,
            1
        );


        score += EvaluateLine(
            board,
            position,
            player,
            1,
            1
        );


        score += EvaluateLine(
            board,
            position,
            player,
            1,
            -1
        );


        // Prefer center

        int centerX =
            board.Width / 2;


        score += Mathf.Max(
            0,
            3 - Mathf.Abs(
                position.x - centerX
            )
        );


        return score;
    }


    // =================================================
    // EVALUATE LINE
    // =================================================

    private int EvaluateLine(
        Board board,
        Vector2Int position,
        PlayerType player,
        int directionX,
        int directionY)
    {
        int count =
            CountLine(
                board,
                position,
                player,
                directionX,
                directionY
            );


        if (count >= 4)
        {
            return 1000;
        }


        if (count == 3)
        {
            return 100;
        }


        if (count == 2)
        {
            return 10;
        }


        return 1;
    }


    // =================================================
    // WOULD CREATE FOUR
    // =================================================

    private bool WouldCreateFour(
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
            ) >= 4

            ||

            CountLine(
                board,
                position,
                player,
                0,
                1
            ) >= 4

            ||

            CountLine(
                board,
                position,
                player,
                1,
                1
            ) >= 4

            ||

            CountLine(
                board,
                position,
                player,
                1,
                -1
            ) >= 4;
    }


    // =================================================
    // WOULD WIN
    // =================================================

    private bool WouldWinAtPosition(
        Board board,
        Vector2Int position,
        PlayerType player)
    {
        return WouldCreateFour(
            board,
            position,
            player
        );
    }


    // =================================================
    // COUNT LINE
    // =================================================

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


    // =================================================
    // COUNT IN DIRECTION
    // =================================================

    private int CountInDirection(
        Board board,
        Vector2Int startPosition,
        PlayerType player,
        int directionX,
        int directionY)
    {
        int count = 0;


        int x =
            startPosition.x +
            directionX;

        int y =
            startPosition.y +
            directionY;


        while (board.IsInsideBoard(x, y))
        {
            BoardCell cell =
                board.GetCell(
                    x,
                    y
                );


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


    // =================================================
    // GET DESTINATION
    // =================================================

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