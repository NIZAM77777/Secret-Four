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


    [Header("Bot Difficulty Scripts")]

    [SerializeField]
    private EasyBot easyBot;

    [SerializeField]
    private MediumBot mediumBot;

    [SerializeField]
    private HardBot hardBot;


    [Header("Bot Timing")]

    [SerializeField]
    private float thinkingDelay = 0.8f;


    // =====================================================
    // VARIABLES
    // =====================================================

    private bool botThinking;

    private Vector2Int botSelectedPosition;


    // =====================================================
    // UPDATE
    // =====================================================

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


        if (turnManager == null)
            return;


        // Prevent multiple bot coroutines

        if (botThinking)
            return;


        // Bot placement turn

        if (turnManager.CurrentState ==
            GameState.WaitingForPlayerYPlacement)
        {
            StartCoroutine(
                BotPlaceCoin()
            );
        }
    }


    // =====================================================
    // BOT PLACE COIN
    // =====================================================

    private IEnumerator BotPlaceCoin()
    {
        botThinking = true;


        // Thinking delay

        yield return new WaitForSeconds(
            thinkingDelay
        );


        // Safety check

        if (turnManager.CurrentState !=
            GameState.WaitingForPlayerYPlacement)
        {
            botThinking = false;

            yield break;
        }


        Board board =
            boardManager.CurrentBoard;


        if (board == null)
        {
            Debug.LogError(
                "BotController: Board is missing."
            );

            botThinking = false;

            yield break;
        }


        // Get bot placement

        botSelectedPosition =
            GetBotPlacement(
                board
            );


        Debug.Log(
            $"BOT chooses position: " +
            $"({botSelectedPosition.x}, " +
            $"{botSelectedPosition.y})"
        );


        // Place coin

        placementManager.TryPlaceCoin(
            botSelectedPosition.x,
            botSelectedPosition.y
        );


        // Wait before direction

        yield return new WaitForSeconds(
            thinkingDelay
        );


        // Choose direction

        StartCoroutine(
            BotChooseDirection()
        );
    }


    // =====================================================
    // BOT CHOOSE DIRECTION
    // =====================================================

    private IEnumerator BotChooseDirection()
    {
        if (turnManager.CurrentState !=
            GameState.WaitingForPlayerYDirection)
        {
            botThinking = false;

            yield break;
        }


        Board board =
            boardManager.CurrentBoard;


        Vector2Int playerXPosition =
            turnManager.PlayerXAction.position;


        // Get bot direction

        MoveDirection selectedDirection =
            GetBotDirection(
                board,
                botSelectedPosition,
                playerXPosition
            );


        Debug.Log(
            $"BOT chooses direction: " +
            $"{selectedDirection}"
        );


        // Select direction

        directionSelector.SelectDirection(
            selectedDirection
        );


        botThinking = false;

        yield return null;
    }


    // =====================================================
    // GET BOT PLACEMENT
    // =====================================================

    private Vector2Int GetBotPlacement(
        Board board)
    {
        BotDifficulty difficulty =
            GameSession.Instance.CurrentBotDifficulty;


        Vector2Int playerXPosition =
            turnManager.PlayerXAction.position;


        switch (difficulty)
        {
            // =============================================
            // EASY
            // =============================================

            case BotDifficulty.Easy:

                if (easyBot != null)
                {
                    return easyBot.ChoosePosition(
                        board
                    );
                }

                break;


            // =============================================
            // MEDIUM
            // =============================================

            case BotDifficulty.Medium:

                if (mediumBot != null)
                {
                    return mediumBot.ChoosePosition(
                        board,
                        playerXPosition
                    );
                }

                break;


            // =============================================
            // HARD
            // =============================================

            case BotDifficulty.Hard:

                if (hardBot != null)
                {
                    List<Vector2Int> emptyCells =
                        GetEmptyCells(board);

                    return hardBot.GetBestPlacement(
                        board,
                        emptyCells
                    );
                }

                break;
        }


        Debug.LogWarning(
            "BotController: Bot difficulty script missing."
        );


        return GetFallbackEmptyCell(
            board
        );
    }


    // =====================================================
    // GET BOT DIRECTION
    // =====================================================

    private MoveDirection GetBotDirection(
        Board board,
        Vector2Int botPosition,
        Vector2Int playerXPosition)
    {
        BotDifficulty difficulty =
            GameSession.Instance.CurrentBotDifficulty;


        switch (difficulty)
        {
            // =============================================
            // EASY
            // =============================================

            case BotDifficulty.Easy:

                if (easyBot != null)
                {
                    return easyBot.ChooseDirection(
                        board,
                        botPosition,
                        playerXPosition
                    );
                }

                break;


            // =============================================
            // MEDIUM
            // =============================================

            case BotDifficulty.Medium:

                if (mediumBot != null)
                {
                    return mediumBot.ChooseDirection(
                        board,
                        botPosition,
                        playerXPosition
                    );
                }

                break;


            // =============================================
            // HARD
            // =============================================

            case BotDifficulty.Hard:

                if (hardBot != null)
                {
                    List<MoveDirection> validDirections =
                        GetValidDirections(
                            board,
                            botPosition,
                            playerXPosition
                        );

                    return hardBot.GetBestDirection(
                        validDirections
                    );
                }

                break;
        }


        return MoveDirection.Stay;
    }


    // =====================================================
    // GET EMPTY CELLS
    // =====================================================

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


    // =====================================================
    // GET VALID DIRECTIONS
    // =====================================================

    private List<MoveDirection> GetValidDirections(
        Board board,
        Vector2Int position,
        Vector2Int playerXPosition)
    {
        List<MoveDirection> directions =
            new List<MoveDirection>();


        // Stay is always valid

        directions.Add(
            MoveDirection.Stay
        );


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


    // =====================================================
    // TRY ADD DIRECTION
    // =====================================================

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


        // Allow movement toward X's newly
        // placed coin because movement is simultaneous

        if (destination == playerXPosition)
        {
            directions.Add(direction);
        }
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
    // FALLBACK EMPTY CELL
    // =====================================================

    private Vector2Int GetFallbackEmptyCell(
        Board board)
    {
        List<Vector2Int> emptyCells =
            GetEmptyCells(board);


        if (emptyCells.Count > 0)
        {
            return emptyCells[
                Random.Range(
                    0,
                    emptyCells.Count
                )
            ];
        }


        return Vector2Int.zero;
    }
}