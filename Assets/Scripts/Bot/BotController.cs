using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotController : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private PlacementManager placementManager;

    [SerializeField]
    private DirectionSelector directionSelector;

    [Header("Bot Timing")]

    [SerializeField]
    private float thinkingDelay = 0.8f;

    private MoveDirection plannedMediumDirection;

    private bool hasPlannedMediumDirection;

    private bool botThinking;

    private MoveDirection plannedHardDirection;

    private bool hasPlannedHardDirection;

    private void Update()
    {
        if (GameSession.Instance == null)
            return;

        // Only work in Bot mode
        if (GameSession.Instance.CurrentGameMode !=
            GameMode.Bot)
        {
            return;
        }

        BotDifficulty difficulty =
    GameSession.Instance.CurrentBotDifficulty;

        if (difficulty != BotDifficulty.Easy &&
            difficulty != BotDifficulty.Medium && difficulty != BotDifficulty.Hard)
        {
            return;
        }

        if (botThinking)
            return;

        if (turnManager.CurrentState ==
            GameState.WaitingForPlayerYPlacement)
        {
            StartCoroutine(
                BotPlaceCoin()
            );
        }
    }


    // ==========================================
    // BOT PLACE COIN
    // ==========================================

    private IEnumerator BotPlaceCoin()
    {
        botThinking = true;

        yield return new WaitForSeconds(
            thinkingDelay
        );

        if (turnManager.CurrentState !=
            GameState.WaitingForPlayerYPlacement)
        {
            botThinking = false;
            yield break;
        }

        Board board =
            boardManager.CurrentBoard;

        List<Vector2Int> emptyCells =
            GetEmptyCells(board);

        if (emptyCells.Count == 0)
        {
            botThinking = false;
            yield break;
        }

        Vector2Int selectedCell;

        BotDifficulty difficulty =
            GameSession.Instance.CurrentBotDifficulty;

        if (difficulty == BotDifficulty.Hard)
        {
            selectedCell =
                GetHardPlacement(
                    board,
                    emptyCells
                );
        }
        else if (difficulty == BotDifficulty.Medium)
        {
            selectedCell =
                GetMediumPlacement(
                    board,
                    emptyCells
                );
        }
        else
        {
            selectedCell =
                emptyCells[
                    Random.Range(
                        0,
                        emptyCells.Count
                    )
                ];
        }

        Debug.Log(
            $"BOT ({difficulty}) chooses position: " +
            $"({selectedCell.x}, {selectedCell.y})"
        );

        placementManager.TryPlaceCoin(
            selectedCell.x,
            selectedCell.y
        );

        yield return new WaitForSeconds(
            thinkingDelay
        );

        StartCoroutine(
            BotChooseDirection()
        );
    }


    // ==========================================
    // BOT CHOOSE DIRECTION
    // ==========================================

    private IEnumerator BotChooseDirection()
    {
        if (turnManager.CurrentState !=
            GameState.WaitingForPlayerYDirection)
        {
            botThinking = false;
            yield break;
        }

        List<MoveDirection> validDirections =
            GetValidDirections();

        if (validDirections.Count == 0)
        {
            botThinking = false;
            yield break;
        }

        MoveDirection selectedDirection;

        BotDifficulty difficulty =
            GameSession.Instance.CurrentBotDifficulty;

        if (difficulty == BotDifficulty.Hard)
        {
            selectedDirection =
                GetHardDirection(
                    validDirections
                );
        }
        else if (difficulty == BotDifficulty.Medium)
        {
            selectedDirection =
                GetMediumDirection(
                    validDirections
                );
        }
        else
        {
            selectedDirection =
                validDirections[
                    Random.Range(
                        0,
                        validDirections.Count
                    )
                ];
        }

        Debug.Log(
            $"BOT ({difficulty}) chooses direction: " +
            $"{selectedDirection}"
        );

        directionSelector.SelectDirection(
            selectedDirection
        );

        botThinking = false;
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
        GetValidDirections()
    {
        List<MoveDirection> directions =
            new List<MoveDirection>();

        Board board =
            boardManager.CurrentBoard;

        PlayerAction action =
            turnManager.PlayerYAction;

        Vector2Int position =
            action.position;

        // Stay is always valid
        directions.Add(
            MoveDirection.Stay
        );

        // Up
        if (CanMoveTo(
            board,
            new Vector2Int(
                position.x,
                position.y + 1
            )))
        {
            directions.Add(
                MoveDirection.Up
            );
        }

        // Down
        if (CanMoveTo(
            board,
            new Vector2Int(
                position.x,
                position.y - 1
            )))
        {
            directions.Add(
                MoveDirection.Down
            );
        }

        // Left
        if (CanMoveTo(
            board,
            new Vector2Int(
                position.x - 1,
                position.y
            )))
        {
            directions.Add(
                MoveDirection.Left
            );
        }

        // Right
        if (CanMoveTo(
            board,
            new Vector2Int(
                position.x + 1,
                position.y
            )))
        {
            directions.Add(
                MoveDirection.Right
            );
        }

        return directions;
    }


    // ==========================================
    // CHECK VALID DESTINATION
    // ==========================================

    private bool CanMoveTo(
        Board board,
        Vector2Int destination)
    {
        // Outside board = invalid
        if (!board.IsInsideBoard(
            destination.x,
            destination.y))
        {
            return false;
        }

        // Empty cell = valid
        if (board.IsEmpty(
            destination.x,
            destination.y))
        {
            return true;
        }

        // Allow movement toward the newly
        // placed X coin for simultaneous logic

        if (destination ==
            turnManager.PlayerXAction.position)
        {
            return true;
        }

        return false;
    }

    // =================================================
    // MEDIUM BOT PLACEMENT
    // =================================================

    private Vector2Int GetMediumPlacement(
    Board board,
    List<Vector2Int> emptyCells)
    {
        hasPlannedMediumDirection = false;


        // ==========================================
        // 1. TRY TO WIN
        // ==========================================

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
                Vector2Int destination =
                    GetDestination(
                        cell,
                        direction
                    );

                if (!board.IsInsideBoard(
                    destination.x,
                    destination.y))
                {
                    continue;
                }

                // Destination must be empty
                // unless it is the starting position.

                if (destination != cell &&
                    !board.IsEmpty(
                        destination.x,
                        destination.y))
                {
                    continue;
                }

                if (WouldWinAtPosition(
                    board,
                    destination,
                    PlayerType.Y))
                {
                    Debug.Log(
                        "MEDIUM BOT → WINNING MOVE FOUND!"
                    );

                    plannedMediumDirection =
                        direction;

                    hasPlannedMediumDirection =
                        true;

                    return cell;
                }
            }
        }


        // ==========================================
        // 2. BLOCK PLAYER X
        // ==========================================

        foreach (Vector2Int cell in emptyCells)
        {
            if (WouldCreateFour(
                board,
                cell,
                PlayerType.X))
            {
                Debug.Log(
                    "MEDIUM BOT → BLOCKING PLAYER!"
                );

                plannedMediumDirection =
                    MoveDirection.Stay;

                hasPlannedMediumDirection =
                    true;

                return cell;
            }
        }


        // ==========================================
        // 3. REASONABLE / RANDOM
        // ==========================================

        Vector2Int randomPosition =
            GetReasonableRandomPosition(
                board,
                emptyCells
            );

        List<MoveDirection> randomDirections =
            GetValidDirectionsForPosition(
                board,
                randomPosition
            );

        plannedMediumDirection =
            GetReasonableDirection(
                randomDirections
            );

        hasPlannedMediumDirection = true;

        return randomPosition;
    }

    private Vector2Int GetReasonableRandomPosition(
    Board board,
    List<Vector2Int> emptyCells)
    {
        int centerX =
            board.Width / 2;

        List<Vector2Int> reasonableCells =
            new List<Vector2Int>();


        // ==========================================
        // Prefer center and near-center positions
        // ==========================================

        foreach (Vector2Int cell in emptyCells)
        {
            int distanceFromCenter =
                Mathf.Abs(
                    cell.x - centerX
                );

            // Center ± 2 columns
            if (distanceFromCenter <= 2)
            {
                reasonableCells.Add(cell);
            }
        }


        // ==========================================
        // If we found reasonable positions,
        // choose randomly from them.
        // ==========================================

        if (reasonableCells.Count > 0)
        {
            return reasonableCells[
                Random.Range(
                    0,
                    reasonableCells.Count
                )
            ];
        }


        // ==========================================
        // Complete random fallback
        // ==========================================

        return emptyCells[
            Random.Range(
                0,
                emptyCells.Count
            )
        ];
    }

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

        while (
            board.IsInsideBoard(x, y))
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

    private MoveDirection GetMediumDirection(
    List<MoveDirection> validDirections)
    {
        // ==========================================
        // Use the direction planned together
        // with the placement.
        // ==========================================

        if (hasPlannedMediumDirection &&
            validDirections.Contains(
                plannedMediumDirection))
        {
            Debug.Log(
                "MEDIUM BOT → Using planned direction: " +
                plannedMediumDirection
            );

            hasPlannedMediumDirection = false;

            return plannedMediumDirection;
        }


        // ==========================================
        // Fallback
        // ==========================================

        return GetReasonableDirection(
            validDirections
        );
    }
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
    private MoveDirection GetReasonableDirection(
    List<MoveDirection> validDirections)
    {
        List<MoveDirection> movementDirections =
            new List<MoveDirection>();


        foreach (MoveDirection direction
            in validDirections)
        {
            if (direction != MoveDirection.Stay)
            {
                movementDirections.Add(
                    direction
                );
            }
        }


        // Prefer actual movement over Stay

        if (movementDirections.Count > 0)
        {
            return movementDirections[
                Random.Range(
                    0,
                    movementDirections.Count
                )
            ];
        }


        return MoveDirection.Stay;
    }

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

    private List<MoveDirection>
    GetValidDirectionsForPosition(
        Board board,
        Vector2Int position)
    {
        List<MoveDirection> directions =
            new List<MoveDirection>();

        // Stay
        directions.Add(
            MoveDirection.Stay
        );

        // Up
        if (CanMoveTo(
            board,
            new Vector2Int(
                position.x,
                position.y + 1
            )))
        {
            directions.Add(
                MoveDirection.Up
            );
        }

        // Down
        if (CanMoveTo(
            board,
            new Vector2Int(
                position.x,
                position.y - 1
            )))
        {
            directions.Add(
                MoveDirection.Down
            );
        }

        // Left
        if (CanMoveTo(
            board,
            new Vector2Int(
                position.x - 1,
                position.y
            )))
        {
            directions.Add(
                MoveDirection.Left
            );
        }

        // Right
        if (CanMoveTo(
            board,
            new Vector2Int(
                position.x + 1,
                position.y
            )))
        {
            directions.Add(
                MoveDirection.Right
            );
        }

        return directions;
    }

    // =================================================
    // HARD BOT PLACEMENT
    // =================================================

    private Vector2Int GetHardPlacement(
        Board board,
        List<Vector2Int> emptyCells)
    {
        hasPlannedHardDirection = false;


        // =============================================
        // PRIORITY 1
        // FIND IMMEDIATE WIN
        // =============================================

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
                Vector2Int destination =
                    GetDestination(
                        cell,
                        direction
                    );

                if (!IsValidHardDestination(
                    board,
                    cell,
                    destination))
                {
                    continue;
                }

                if (WouldWinAtPosition(
                    board,
                    destination,
                    PlayerType.Y))
                {
                    Debug.Log(
                        "HARD BOT → IMMEDIATE WIN FOUND!"
                    );

                    plannedHardDirection =
                        direction;

                    hasPlannedHardDirection = true;

                    return cell;
                }
            }
        }


        // =============================================
        // PRIORITY 2
        // BLOCK PLAYER IMMEDIATE WIN
        // =============================================

        Vector2Int xPosition =
            turnManager.PlayerXAction.position;

        foreach (Vector2Int cell in emptyCells)
        {
            // If this cell can stop X's winning area
            if (WouldCreateFour(
                board,
                cell,
                PlayerType.X))
            {
                Debug.Log(
                    "HARD BOT → BLOCKING IMMEDIATE WIN!"
                );

                plannedHardDirection =
                    MoveDirection.Stay;

                hasPlannedHardDirection = true;

                return cell;
            }
        }


        // =============================================
        // PRIORITY 3
        // CREATE STRONG LINE
        // =============================================

        Vector2Int bestCell =
            GetBestStrategicPosition(
                board,
                emptyCells,
                PlayerType.Y
            );

        List<MoveDirection> bestDirections =
            GetValidDirectionsForPosition(
                board,
                bestCell
            );

        plannedHardDirection =
            GetBestStrategicDirection(
                board,
                bestCell,
                bestDirections
            );

        hasPlannedHardDirection = true;

        return bestCell;
    }

    // =================================================
    // HARD DESTINATION VALIDATION
    // =================================================

    private bool IsValidHardDestination(
        Board board,
        Vector2Int start,
        Vector2Int destination)
    {
        // Stay is valid
        if (destination == start)
        {
            return true;
        }

        // Must be inside board
        if (!board.IsInsideBoard(
            destination.x,
            destination.y))
        {
            return false;
        }

        // Empty destination
        if (board.IsEmpty(
            destination.x,
            destination.y))
        {
            return true;
        }

        // X's newly placed position may be involved
        // in simultaneous movement.
        if (destination ==
            turnManager.PlayerXAction.position)
        {
            return true;
        }

        return false;
    }

    // =================================================
    // FIND BEST STRATEGIC POSITION
    // =================================================

    private Vector2Int GetBestStrategicPosition(
        Board board,
        List<Vector2Int> emptyCells,
        PlayerType player)
    {
        Vector2Int bestCell =
            emptyCells[0];

        int bestScore = int.MinValue;


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
                bestScore = score;
                bestCell = cell;
            }
        }


        Debug.Log(
            $"HARD BOT → Best position: " +
            $"{bestCell} | Score: {bestScore}"
        );

        return bestCell;
    }

    // =================================================
    // POSITION EVALUATION
    // =================================================

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


        // Prefer center slightly

        int center =
            board.Width / 2;

        score +=
            Mathf.Max(
                0,
                3 -
                Mathf.Abs(
                    position.x - center
                )
            );


        return score;
    }

    private int EvaluateLine(
    Board board,
    Vector2Int position,
    PlayerType player,
    int directionX,
    int directionY)
    {
        int count = CountLine(
            board,
            position,
            player,
            directionX,
            directionY
        );

        // Strong scoring

        if (count >= 4)
        {
            return 10000;
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
    // HARD BOT DIRECTION
    // =================================================

    private MoveDirection GetHardDirection(
        List<MoveDirection> validDirections)
    {
        if (hasPlannedHardDirection &&
            validDirections.Contains(
                plannedHardDirection))
        {
            MoveDirection direction =
                plannedHardDirection;

            hasPlannedHardDirection = false;

            Debug.Log(
                "HARD BOT → Planned direction: " +
                direction
            );

            return direction;
        }


        // Safety fallback

        return GetReasonableDirection(
            validDirections
        );
    }

    private MoveDirection GetBestStrategicDirection(
    Board board,
    Vector2Int position,
    List<MoveDirection> directions)
    {
        MoveDirection bestDirection =
            MoveDirection.Stay;

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

            if (!IsValidHardDestination(
                board,
                position,
                destination))
            {
                continue;
            }

            int score =
                EvaluatePosition(
                    board,
                    destination,
                    PlayerType.Y
                );

            if (score > bestScore)
            {
                bestScore = score;

                bestDirection =
                    direction;
            }
        }


        Debug.Log(
            $"HARD BOT → Best direction: " +
            $"{bestDirection}"
        );

        return bestDirection;
    }
}