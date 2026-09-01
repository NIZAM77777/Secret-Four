using System.Collections.Generic;
using UnityEngine;

public class HardBot : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private MovementRuleResolver movementRuleResolver;


    // =====================================================
    // PLANNED DIRECTION
    // =====================================================

    private MoveDirection plannedDirection;

    private bool hasPlannedDirection;


    // =====================================================
    // GET BEST PLACEMENT
    // =====================================================

    public Vector2Int GetBestPlacement(
        Board board,
        List<Vector2Int> emptyCells)
    {
        hasPlannedDirection = false;


        if (emptyCells == null ||
            emptyCells.Count == 0)
        {
            return Vector2Int.zero;
        }


        // =================================================
        // GET PLAYER X CURRENT POSITION
        // =================================================

        Vector2Int playerXPosition =
            turnManager.PlayerXAction.position;


        // =================================================
        // PRIORITY 0
        // STOP PLAYER X IMMEDIATE WINNING MOVEMENT
        // =================================================

        List<Vector2Int> xWinningDestinations =
            GetPlayerXWinningDestinations(
                board,
                playerXPosition
            );


        foreach (Vector2Int winningDestination
            in xWinningDestinations)
        {
            Vector2Int counterPosition;

            MoveDirection counterDirection;


            if (TryFindCounterPosition(
                board,
                emptyCells,
                winningDestination,
                out counterPosition,
                out counterDirection
            ))
            {
                Debug.Log(
                    "HARD BOT → COUNTERING X WINNING MOVE!"
                );

                Debug.Log(
                    "Y Position: " +
                    counterPosition +
                    " | Direction: " +
                    counterDirection
                );


                plannedDirection =
                    counterDirection;

                hasPlannedDirection =
                    true;


                return counterPosition;
            }
        }


        // =================================================
        // PRIORITY 1
        // FIND IMMEDIATE WIN FOR Y
        // =================================================

        foreach (Vector2Int cell in emptyCells)
        {
            List<MoveDirection> directions =
                GetValidDirectionsForPosition(
                    board,
                    cell
                );


            foreach (MoveDirection direction
                in directions)
            {
                if (direction ==
                    MoveDirection.Stay)
                {
                    continue;
                }


                if (DoesYWinWithDirection(
                    board,
                    playerXPosition,
                    cell,
                    direction
                ))
                {
                    Debug.Log(
                        "HARD BOT → IMMEDIATE WIN FOUND!"
                    );


                    plannedDirection =
                        direction;

                    hasPlannedDirection =
                        true;


                    return cell;
                }
            }
        }


        // =================================================
        // PRIORITY 2
        // BLOCK NORMAL PLAYER X FOUR
        // =================================================

        foreach (Vector2Int cell in emptyCells)
        {
            if (WouldCreateFour(
                board,
                cell,
                PlayerType.X
            ))
            {
                Debug.Log(
                    "HARD BOT → BLOCKING X FOUR!"
                );


                List<MoveDirection> directions =
                    GetValidDirectionsForPosition(
                        board,
                        cell
                    );


                plannedDirection =
                    GetBestStrategicDirection(
                        board,
                        playerXPosition,
                        cell,
                        directions
                    );


                hasPlannedDirection =
                    true;


                return cell;
            }
        }


        // =================================================
        // PRIORITY 3
        // FIND BEST STRATEGIC POSITION
        // =================================================

        Vector2Int bestCell =
            GetBestStrategicPosition(
                board,
                emptyCells,
                PlayerType.Y
            );


        List<MoveDirection> directionsForBestCell =
            GetValidDirectionsForPosition(
                board,
                bestCell
            );


        plannedDirection =
            GetBestStrategicDirection(
                board,
                playerXPosition,
                bestCell,
                directionsForBestCell
            );


        hasPlannedDirection =
            true;


        return bestCell;
    }


    // =====================================================
    // GET BEST DIRECTION
    // =====================================================

    public MoveDirection GetBestDirection(
        List<MoveDirection> validDirections)
    {
        // ================================================
        // USE PLANNED DIRECTION
        // ================================================

        if (hasPlannedDirection &&
            validDirections.Contains(
                plannedDirection
            ))
        {
            MoveDirection direction =
                plannedDirection;


            hasPlannedDirection =
                false;


            Debug.Log(
                "HARD BOT → Using planned direction: " +
                direction
            );


            return direction;
        }


        // ================================================
        // FALLBACK
        // ================================================

        return GetReasonableDirection(
            validDirections
        );
    }


    // =====================================================
    // PLAYER X WINNING DESTINATIONS
    // =====================================================

    private List<Vector2Int>
        GetPlayerXWinningDestinations(
            Board board,
            Vector2Int playerXPosition)
    {
        List<Vector2Int> winningDestinations =
            new List<Vector2Int>();


        MoveDirection[] directions =
            GetAllDirections();


        foreach (MoveDirection direction
            in directions)
        {
            if (direction ==
                MoveDirection.Stay)
            {
                continue;
            }


            Vector2Int destination =
                GetDestination(
                    playerXPosition,
                    direction
                );


            // ---------------------------------------------
            // Must be inside board
            // ---------------------------------------------

            if (!board.IsInsideBoard(
                destination.x,
                destination.y
            ))
            {
                continue;
            }


            // ---------------------------------------------
            // Check possible movement
            // ---------------------------------------------

            if (!IsPossibleXDestination(
                board,
                playerXPosition,
                destination
            ))
            {
                continue;
            }


            // ---------------------------------------------
            // Check if destination gives X four
            // ---------------------------------------------

            if (WouldWinAtPosition(
                board,
                destination,
                PlayerType.X
            ))
            {
                winningDestinations.Add(
                    destination
                );


                Debug.Log(
                    "HARD BOT → X WINNING THREAT: " +
                    destination
                );
            }
        }


        return winningDestinations;
    }


    // =====================================================
    // CHECK POSSIBLE X DESTINATION
    // =====================================================

    private bool IsPossibleXDestination(
        Board board,
        Vector2Int start,
        Vector2Int destination)
    {
        if (destination == start)
        {
            return false;
        }


        if (!board.IsInsideBoard(
            destination.x,
            destination.y
        ))
        {
            return false;
        }


        // Empty cell

        if (board.IsEmpty(
            destination.x,
            destination.y
        ))
        {
            return true;
        }


        // X may move toward Y's current-round position
        // because movement is simultaneous.

        if (turnManager != null &&
            destination ==
            turnManager.PlayerYAction.position)
        {
            return true;
        }


        return false;
    }


    // =====================================================
    // FIND COUNTER POSITION
    // =====================================================

    private bool TryFindCounterPosition(
        Board board,
        List<Vector2Int> emptyCells,
        Vector2Int winningDestination,
        out Vector2Int counterPosition,
        out MoveDirection counterDirection)
    {
        foreach (Vector2Int cell in emptyCells)
        {
            foreach (MoveDirection direction
                in GetAllDirections())
            {
                if (direction ==
                    MoveDirection.Stay)
                {
                    continue;
                }


                Vector2Int destination =
                    GetDestination(
                        cell,
                        direction
                    );


                // We want Y to move into
                // the SAME winning destination.

                if (destination !=
                    winningDestination)
                {
                    continue;
                }


                if (!board.IsInsideBoard(
                    destination.x,
                    destination.y
                ))
                {
                    continue;
                }


                counterPosition =
                    cell;

                counterDirection =
                    direction;


                Debug.Log(
                    "HARD BOT → COUNTER POSITION FOUND"
                );


                return true;
            }
        }


        counterPosition =
            Vector2Int.zero;

        counterDirection =
            MoveDirection.Stay;


        return false;
    }


    // =====================================================
    // CHECK Y IMMEDIATE WIN
    // =====================================================

    private bool DoesYWinWithDirection(
        Board board,
        Vector2Int playerXPosition,
        Vector2Int playerYPosition,
        MoveDirection playerYDirection)
    {
        foreach (MoveDirection xDirection
            in GetAllDirections())
        {
            MovementResolution resolution =
                SimulateMovement(
                    playerXPosition,
                    xDirection,
                    playerYPosition,
                    playerYDirection
                );


            if (resolution == null)
            {
                continue;
            }


            // If Y can win against at least one
            // X movement, this is useful.

            if (DoesHardBotWinAfterSimulation(
                board,
                resolution
            ))
            {
                return true;
            }
        }


        return false;
    }


    // =====================================================
    // SIMULATE BOTH PLAYERS
    // =====================================================

    private MovementResolution SimulateMovement(
        Vector2Int playerXPosition,
        MoveDirection playerXDirection,
        Vector2Int playerYPosition,
        MoveDirection playerYDirection)
    {
        if (movementRuleResolver == null)
        {
            Debug.LogError(
                "HARD BOT → MovementRuleResolver missing."
            );

            return null;
        }


        // ================================================
        // TEMPORARY X ACTION
        // ================================================

        PlayerAction xAction =
            new PlayerAction(
                PlayerType.X
            );


        xAction.position =
            playerXPosition;

        xAction.direction =
            playerXDirection;

        xAction.hasPlaced =
            true;

        xAction.hasChosenDirection =
            true;


        // ================================================
        // TEMPORARY Y ACTION
        // ================================================

        PlayerAction yAction =
            new PlayerAction(
                PlayerType.Y
            );


        yAction.position =
            playerYPosition;

        yAction.direction =
            playerYDirection;

        yAction.hasPlaced =
            true;

        yAction.hasChosenDirection =
            true;


        // ================================================
        // USE REAL GAME RULES
        // ================================================

        return movementRuleResolver.Resolve(
            xAction,
            yAction
        );
    }


    // =====================================================
    // DOES X WIN AFTER SIMULATION
    // =====================================================

    private bool DoesPlayerXWinAfterSimulation(
        Board board,
        MovementResolution resolution)
    {
        if (resolution == null)
        {
            return false;
        }


        // ================================================
        // X COLLISION
        // ================================================

        if (resolution.playerXCollision !=
            MovementCollisionType.None)
        {
            return false;
        }


        Vector2Int finalPosition =
            resolution.playerXResult.destination;


        return WouldWinAtPosition(
            board,
            finalPosition,
            PlayerType.X
        );
    }


    // =====================================================
    // DOES Y WIN AFTER SIMULATION
    // =====================================================

    private bool DoesHardBotWinAfterSimulation(
        Board board,
        MovementResolution resolution)
    {
        if (resolution == null)
        {
            return false;
        }


        // ================================================
        // Y COLLISION
        // ================================================

        if (resolution.playerYCollision !=
            MovementCollisionType.None)
        {
            return false;
        }


        Vector2Int finalPosition =
            resolution.playerYResult.destination;


        return WouldWinAtPosition(
            board,
            finalPosition,
            PlayerType.Y
        );
    }


    // =====================================================
    // GET BEST STRATEGIC POSITION
    // =====================================================

    private Vector2Int GetBestStrategicPosition(
        Board board,
        List<Vector2Int> emptyCells,
        PlayerType player)
    {
        Vector2Int bestCell =
            emptyCells[0];


        int bestScore =
            int.MinValue;


        foreach (Vector2Int cell
            in emptyCells)
        {
            int score =
                EvaluatePosition(
                    board,
                    cell,
                    player
                );


            // ================================================
            // ALSO PREFER POSITIONS WITH GOOD MOBILITY
            // ================================================

            int movementScore =
                GetMovementScore(
                    board,
                    cell
                );


            score +=
                movementScore;


            if (score > bestScore)
            {
                bestScore =
                    score;

                bestCell =
                    cell;
            }
        }


        Debug.Log(
            "HARD BOT → Best position: " +
            bestCell +
            " | Score: " +
            bestScore
        );


        return bestCell;
    }


    // =====================================================
    // MOVEMENT SCORE
    // =====================================================

    private int GetMovementScore(
        Board board,
        Vector2Int position)
    {
        int score = 0;


        foreach (MoveDirection direction
            in GetAllDirections())
        {
            if (direction ==
                MoveDirection.Stay)
            {
                continue;
            }


            Vector2Int destination =
                GetDestination(
                    position,
                    direction
                );


            if (!board.IsInsideBoard(
                destination.x,
                destination.y
            ))
            {
                continue;
            }


            if (board.IsEmpty(
                destination.x,
                destination.y
            ))
            {
                score += 3;
            }
        }


        return score;
    }


    // =====================================================
    // EVALUATE POSITION
    // =====================================================

    private int EvaluatePosition(
        Board board,
        Vector2Int position,
        PlayerType player)
    {
        int score = 0;


        // Horizontal

        score += EvaluateLine(
            board,
            position,
            player,
            1,
            0
        );


        // Vertical

        score += EvaluateLine(
            board,
            position,
            player,
            0,
            1
        );


        // Diagonal ↗

        score += EvaluateLine(
            board,
            position,
            player,
            1,
            1
        );


        // Diagonal ↘

        score += EvaluateLine(
            board,
            position,
            player,
            1,
            -1
        );


        // ================================================
        // PREFER CENTER
        // ================================================

        int center =
            board.Width / 2;


        score += Mathf.Max(
            0,
            5 -
            Mathf.Abs(
                position.x - center
            )
        );


        return score;
    }


    // =====================================================
    // EVALUATE LINE
    // =====================================================

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
            return 10000;
        }


        if (count == 3)
        {
            return 500;
        }


        if (count == 2)
        {
            return 50;
        }


        return 5;
    }


    // =====================================================
    // BEST STRATEGIC DIRECTION
    // =====================================================

    private MoveDirection GetBestStrategicDirection(
        Board board,
        Vector2Int playerXPosition,
        Vector2Int playerYPosition,
        List<MoveDirection> directions)
    {
        MoveDirection bestDirection =
            MoveDirection.Stay;


        int bestScore =
            int.MinValue;


        foreach (MoveDirection direction
            in directions)
        {
            int score =
                EvaluateDirection(
                    board,
                    playerXPosition,
                    playerYPosition,
                    direction
                );


            if (score > bestScore)
            {
                bestScore =
                    score;

                bestDirection =
                    direction;
            }
        }


        Debug.Log(
            "HARD BOT → Best direction: " +
            bestDirection +
            " | Score: " +
            bestScore
        );


        return bestDirection;
    }


    // =====================================================
    // EVALUATE DIRECTION AGAINST ALL X MOVES
    // =====================================================

    private int EvaluateDirection(
        Board board,
        Vector2Int playerXPosition,
        Vector2Int playerYPosition,
        MoveDirection playerYDirection)
    {
        int totalScore = 0;


        foreach (MoveDirection playerXDirection
            in GetAllDirections())
        {
            MovementResolution resolution =
                SimulateMovement(
                    playerXPosition,
                    playerXDirection,
                    playerYPosition,
                    playerYDirection
                );


            if (resolution == null)
            {
                continue;
            }


            // ============================================
            // VERY BAD IF X WINS
            // ============================================

            if (DoesPlayerXWinAfterSimulation(
                board,
                resolution
            ))
            {
                totalScore -= 10000;

                continue;
            }


            // ============================================
            // VERY GOOD IF Y WINS
            // ============================================

            if (DoesHardBotWinAfterSimulation(
                board,
                resolution
            ))
            {
                totalScore += 10000;

                continue;
            }


            // ============================================
            // NORMAL POSITION SCORE
            // ============================================

            if (resolution.playerYCollision ==
                MovementCollisionType.None)
            {
                totalScore +=
                    EvaluatePosition(
                        board,
                        resolution.playerYResult.destination,
                        PlayerType.Y
                    );
            }


            // ============================================
            // PREFER STOPPING X
            // ============================================

            if (resolution.playerXCollision !=
                MovementCollisionType.None)
            {
                totalScore += 200;
            }
        }


        return totalScore;
    }


    // =====================================================
    // VALID DIRECTIONS
    // =====================================================

    private List<MoveDirection>
        GetValidDirectionsForPosition(
            Board board,
            Vector2Int position)
    {
        List<MoveDirection> directions =
            new List<MoveDirection>();


        // ================================================
        // CHECK ALL MOVEMENT DIRECTIONS FIRST
        // ================================================

        MoveDirection[] movementDirections =
        {
            MoveDirection.Up,
            MoveDirection.Down,
            MoveDirection.Left,
            MoveDirection.Right
        };


        foreach (MoveDirection direction
            in movementDirections)
        {
            Vector2Int destination =
                GetDestination(
                    position,
                    direction
                );


            if (IsValidDestination(
                board,
                position,
                destination
            ))
            {
                directions.Add(
                    direction
                );
            }
        }


        // ================================================
        // STAY ONLY IF NO MOVEMENT EXISTS
        // ================================================

        if (directions.Count == 0)
        {
            directions.Add(
                MoveDirection.Stay
            );
        }


        return directions;
    }


    // =====================================================
    // VALID DESTINATION
    // =====================================================

    private bool IsValidDestination(
        Board board,
        Vector2Int start,
        Vector2Int destination)
    {
        // Stay

        if (destination == start)
        {
            return false;
        }


        // Outside board

        if (!board.IsInsideBoard(
            destination.x,
            destination.y
        ))
        {
            return false;
        }


        // Empty

        if (board.IsEmpty(
            destination.x,
            destination.y
        ))
        {
            return true;
        }


        // ================================================
        // Allow X current position
        // ================================================

        if (turnManager != null &&
            destination ==
            turnManager.PlayerXAction.position)
        {
            return true;
        }


        return false;
    }


    // =====================================================
    // GET DESTINATION
    // =====================================================

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


            case MoveDirection.Stay:
            default:

                return position;
        }
    }


    // =====================================================
    // GET ALL DIRECTIONS
    // =====================================================

    private MoveDirection[] GetAllDirections()
    {
        return new MoveDirection[]
        {
            MoveDirection.Up,
            MoveDirection.Down,
            MoveDirection.Left,
            MoveDirection.Right,
            MoveDirection.Stay
        };
    }


    // =====================================================
    // WOULD WIN AT POSITION
    // =====================================================

    private bool WouldWinAtPosition(
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


    // =====================================================
    // WOULD CREATE FOUR
    // =====================================================

    private bool WouldCreateFour(
        Board board,
        Vector2Int position,
        PlayerType player)
    {
        return WouldWinAtPosition(
            board,
            position,
            player
        );
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
            startPosition.x +
            directionX;


        int y =
            startPosition.y +
            directionY;


        while (board.IsInsideBoard(
            x,
            y
        ))
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


    // =====================================================
    // REASONABLE FALLBACK
    // =====================================================

    private MoveDirection GetReasonableDirection(
        List<MoveDirection> validDirections)
    {
        if (validDirections == null ||
            validDirections.Count == 0)
        {
            return MoveDirection.Stay;
        }


        List<MoveDirection> movementDirections =
            new List<MoveDirection>();


        foreach (MoveDirection direction
            in validDirections)
        {
            if (direction !=
                MoveDirection.Stay)
            {
                movementDirections.Add(
                    direction
                );
            }
        }


        // Prefer actual movement

        if (movementDirections.Count > 0)
        {
            return movementDirections[
                Random.Range(
                    0,
                    movementDirections.Count
                )
            ];
        }


        // Stay only when nothing else exists

        return MoveDirection.Stay;
    }
}