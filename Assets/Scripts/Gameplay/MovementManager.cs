using System.Collections;
using UnityEngine;

public class MovementManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private MovementRuleResolver movementRuleResolver;

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private BoardView boardView;

    [SerializeField]
    private GameConfig gameConfig;

    [Header("Timing")]

    [SerializeField]
    private float revealDelay = 2f;

    [SerializeField]
    private float movementDuration = 0.3f;

    [Header("Collision Animation")]

    [SerializeField]
    private float collisionMoveDuration = 0.25f;

    [SerializeField]
    private float collisionPauseDuration = 0.08f;

    [SerializeField]
    private float collisionReturnDuration = 0.16f;
    private bool movementInProgress;
    private GameObject playerXCoin;
    private GameObject playerYCoin;

    public void StartMovementSequence(
    GameObject xCoin,
    GameObject yCoin)
    {
        if (movementInProgress)
        {
            Debug.Log(
                "Movement already in progress."
            );

            return;
        }

        movementInProgress = true;

        playerXCoin = xCoin;
        playerYCoin = yCoin;

        StartCoroutine(
            RevealAndResolve()
        );
    }

    private IEnumerator RevealAndResolve()
    {
        // --------------------------------
        // Reveal
        // --------------------------------

        RevealDirections();

        // --------------------------------
        // Wait 2 seconds
        // --------------------------------

        yield return new WaitForSeconds(
            revealDelay
        );

        // --------------------------------
        // Calculate BOTH destinations
        // --------------------------------

        MovementResolution resolution =
            movementRuleResolver.Resolve(
                turnManager.PlayerXAction,
                turnManager.PlayerYAction
            );

        LogResolution(
            resolution
        );

        // --------------------------------
        // Resolve movement
        // --------------------------------

        if (resolution.AnyCollision)
        {
            yield return StartCoroutine(
                HandleCollision(
                    resolution
                )
            );
        }
        else
        {
            yield return StartCoroutine(
                HandleSuccessfulMovement(
                    resolution
                )
            );
        }
        movementInProgress = false;
    }

    // =========================================================
    // REVEAL
    // =========================================================

    private void RevealDirections()
    {
        turnManager.SetState(
            GameState.RevealingDirections
        );

        Debug.Log(
            $"REVEAL → X: " +
            $"{turnManager.PlayerXAction.direction}"
        );

        Debug.Log(
            $"REVEAL → Y: " +
            $"{turnManager.PlayerYAction.direction}"
        );
    }

    // =========================================================
    // DEBUG
    // =========================================================

    private void LogResolution(
        MovementResolution resolution)
    {
        Debug.Log(
            $"X: " +
            $"{resolution.playerXResult.startPosition}" +
            " → " +
            $"{resolution.playerXResult.destination}" +
            $" | Collision: " +
            $"{resolution.playerXCollision}"
        );

        Debug.Log(
            $"Y: " +
            $"{resolution.playerYResult.startPosition}" +
            " → " +
            $"{resolution.playerYResult.destination}" +
            $" | Collision: " +
            $"{resolution.playerYCollision}"
        );
    }

    // =========================================================
    // SUCCESSFUL MOVEMENT
    // =========================================================

    private IEnumerator HandleSuccessfulMovement(
        MovementResolution resolution)
    {
        turnManager.SetState(
            GameState.Moving
        );

        Vector3 xStart =
            boardView.GetCellWorldPosition(
                resolution.playerXResult.startPosition.x,
                resolution.playerXResult.startPosition.y
            );

        Vector3 xEnd =
            boardView.GetCellWorldPosition(
                resolution.playerXResult.destination.x,
                resolution.playerXResult.destination.y
            );

        Vector3 yStart =
            boardView.GetCellWorldPosition(
                resolution.playerYResult.startPosition.x,
                resolution.playerYResult.startPosition.y
            );

        Vector3 yEnd =
            boardView.GetCellWorldPosition(
                resolution.playerYResult.destination.x,
                resolution.playerYResult.destination.y
            );

        // --------------------------------
        // Animate both simultaneously
        // --------------------------------

        yield return StartCoroutine(
            AnimateTwoCoins(
                xStart,
                xEnd,
                yStart,
                yEnd,
                movementDuration
            )
        );

        // --------------------------------
        // Save their new positions
        // permanently on the board.
        // --------------------------------

        ApplySuccessfulBoardMovement(
            resolution
        );

        Debug.Log(
            "Movement successful. " +
            "New positions saved."
        );

        boardManager.CurrentBoard
            .DebugPrintBoard();

        // --------------------------------
        // Next round
        // --------------------------------

        turnManager.SetState(
            GameState.CheckingResult
        );

        turnManager.StartNextRound();
    }

    private void ApplySuccessfulBoardMovement(
        MovementResolution resolution)
    {
        Board board =
            boardManager.CurrentBoard;

        board.ApplySuccessfulMovement(
            resolution.playerXResult,
            resolution.playerYResult
        );
    }

    // =========================================================
    // COLLISION
    // =========================================================

    private IEnumerator HandleCollision(
    MovementResolution resolution)
    {
        turnManager.RegisterCollision();

        Debug.Log(
            $"COLLISION! Count = " +
            $"{turnManager.CollisionCount}"
        );

        yield return StartCoroutine(
            AnimateMovementResults(resolution)
        );

        // --------------------------------
        // Check collision draw
        // --------------------------------

        if (turnManager.CollisionCount >=
            gameConfig.collisionDrawCount)
        {
            Debug.Log(
                "DRAW — Collision limit reached."
            );

            turnManager.SetState(
                GameState.Draw
            );

            yield break;
        }

        // --------------------------------
        // Apply successful movements.
        //
        // Coins that collided stay at their
        // original positions.
        // Coins that succeeded become permanent.
        // --------------------------------

        ApplyResolvedMovement(resolution);

        boardManager.CurrentBoard
            .DebugPrintBoard();

        turnManager.StartNextRound();
    }

    private void ApplyResolvedMovement(
    MovementResolution resolution)
    {
        MovementResult xResult =
            resolution.playerXResult;

        MovementResult yResult =
            resolution.playerYResult;

        // -------------------------------------------------
        // If a coin collided, make its destination equal
        // to its starting position for board resolution.
        // -------------------------------------------------

        MovementResult xFinal =
            xResult;

        MovementResult yFinal =
            yResult;

        if (resolution.playerXCollision !=
            MovementCollisionType.None)
        {
            xFinal =
                new MovementResult(
                    PlayerType.X,
                    xResult.startPosition,
                    xResult.startPosition,
                    xResult.direction
                );
        }

        if (resolution.playerYCollision !=
            MovementCollisionType.None)
        {
            yFinal =
                new MovementResult(
                    PlayerType.Y,
                    yResult.startPosition,
                    yResult.startPosition,
                    yResult.direction
                );
        }

        boardManager.CurrentBoard
            .ApplySuccessfulMovement(
                xFinal,
                yFinal
            );
    }
    // =========================================================
    // COLLISION ANIMATION
    // =========================================================

    private IEnumerator AnimateMovementResults(
    MovementResolution resolution)
    {
        turnManager.SetState(
            GameState.Moving
        );

        Vector3 xStart =
            boardView.GetCellWorldPosition(
                resolution.playerXResult.startPosition.x,
                resolution.playerXResult.startPosition.y
            );

        Vector3 xTarget =
            boardView.GetCellWorldPosition(
                resolution.playerXResult.destination.x,
                resolution.playerXResult.destination.y
            );

        Vector3 yStart =
            boardView.GetCellWorldPosition(
                resolution.playerYResult.startPosition.x,
                resolution.playerYResult.startPosition.y
            );

        Vector3 yTarget =
            boardView.GetCellWorldPosition(
                resolution.playerYResult.destination.x,
                resolution.playerYResult.destination.y
            );

        // -------------------------------------------------
        // Determine what each coin actually does
        // -------------------------------------------------

        bool xCollides =
            resolution.playerXCollision !=
            MovementCollisionType.None;

        bool yCollides =
            resolution.playerYCollision !=
            MovementCollisionType.None;

        // -------------------------------------------------
        // Phase 1
        //
        // Both coins move toward their targets.
        // -------------------------------------------------

        yield return StartCoroutine(
            AnimateTwoCoins(
                xStart,
                xTarget,
                yStart,
                yTarget,
                movementDuration
            )
        );

        // -------------------------------------------------
        // If there was a collision, pause at impact.
        // -------------------------------------------------

        if (xCollides || yCollides)
        {
            yield return new WaitForSeconds(
                collisionPauseDuration
            );
        }

        // -------------------------------------------------
        // Phase 2
        //
        // ONLY collided coins return.
        //
        // Successful coins remain at destination.
        // -------------------------------------------------

        if (xCollides || yCollides)
        {
            Vector3 xReturn =
                xCollides
                    ? xStart
                    : xTarget;

            Vector3 yReturn =
                yCollides
                    ? yStart
                    : yTarget;

            yield return StartCoroutine(
                AnimateTwoCoins(
                    xTarget,
                    xReturn,
                    yTarget,
                    yReturn,
                    collisionReturnDuration
                )
            );
        }

        // -------------------------------------------------
        // Guarantee final visual positions
        // -------------------------------------------------

        if (playerXCoin != null)
        {
            playerXCoin.transform.position =
                xCollides
                    ? xStart
                    : xTarget;
        }

        if (playerYCoin != null)
        {
            playerYCoin.transform.position =
                yCollides
                    ? yStart
                    : yTarget;
        }
    }

    // =========================================================
    // SHARED ANIMATION
    // =========================================================

    private IEnumerator AnimateTwoCoins(
        Vector3 xStart,
        Vector3 xEnd,
        Vector3 yStart,
        Vector3 yEnd,
        float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / duration
                );

            t = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            if (playerXCoin != null)
            {
                playerXCoin.transform.position =
                    Vector3.Lerp(
                        xStart,
                        xEnd,
                        t
                    );
            }

            if (playerYCoin != null)
            {
                playerYCoin.transform.position =
                    Vector3.Lerp(
                        yStart,
                        yEnd,
                        t
                    );
            }

            yield return null;
        }

        if (playerXCoin != null)
        {
            playerXCoin.transform.position =
                xEnd;
        }

        if (playerYCoin != null)
        {
            playerYCoin.transform.position =
                yEnd;
        }
    }
}