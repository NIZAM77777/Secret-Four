using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class MovementManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private GameResultManager gameResultManager;

    [SerializeField]
    private WinConditionChecker winConditionChecker;

    [SerializeField]
    private MovementRuleResolver movementRuleResolver;

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private BoardView boardView;

    [SerializeField]
    private NetworkMovementManager networkMovementManager;

    [SerializeField]
    private NetworkGameResultManager networkGameResultManager;

    [SerializeField]
    private PlacementManager placementManager;


    [Header("Timing")]

    [SerializeField]
    private float revealDelay = 2f;

    [SerializeField]
    private float movementDuration = 0.3f;

    [SerializeField]
    private float blockedPauseDuration = 0.08f;

    [SerializeField]
    private float returnDuration = 0.16f;


    private bool movementInProgress;

    private GameObject playerXCoin;
    private GameObject playerYCoin;
    private int activeNetworkMoveId = -1;


    // =====================================================
    // START MOVEMENT
    // =====================================================

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


        // =============================================
        // ONLINE MODE
        // =============================================

        if (IsOnlineGame())
        {
            if (networkMovementManager == null)
            {
                Debug.LogError(
                    "NetworkMovementManager is missing."
                );

                return;
            }


            networkMovementManager
                .RequestMovementResolution();

            return;
        }


        // =============================================
        // LOCAL MODE
        // =============================================

        playerXCoin = xCoin;
        playerYCoin = yCoin;
        activeNetworkMoveId = moveId;

        movementInProgress = true;

        StartCoroutine(
            RevealAndResolveLocal()
        );
    }


    // =====================================================
    // LOCAL GAME MOVEMENT
    // =====================================================

    private IEnumerator RevealAndResolveLocal()
    {
        RevealDirections();

        yield return new WaitForSeconds(
            revealDelay
        );


        MovementResolution resolution =
            movementRuleResolver.Resolve(
                turnManager.PlayerXAction,
                turnManager.PlayerYAction
            );


        LogResolution(resolution);


        yield return StartCoroutine(
            ApplyResolutionSequence(
                resolution
            )
        );


        movementInProgress = false;
    }


    // =====================================================
    // NETWORK APPLY MOVEMENT
    // =====================================================

    public void ApplyNetworkResolution(
        MovementResolution resolution,
        GameObject xCoin,
        GameObject yCoin,
        int moveId = -1)
    {
        if (movementInProgress)
        {
            Debug.Log(
                "Movement already in progress."
            );

            return;
        }


        playerXCoin = xCoin;
        playerYCoin = yCoin;

        movementInProgress = true;


        StartCoroutine(
            ApplyNetworkResolutionCoroutine(
                resolution
            )
        );
    }


    private IEnumerator ApplyNetworkResolutionCoroutine(
        MovementResolution resolution)
    {
        turnManager.SetState(
            GameState.RevealingDirections
        );


        Debug.Log(
            "NETWORK MOVEMENT REVEAL"
        );


        Debug.Log(
            $"X → {turnManager.PlayerXAction.direction}"
        );


        Debug.Log(
            $"Y → {turnManager.PlayerYAction.direction}"
        );


        yield return new WaitForSeconds(
            revealDelay
        );


        LogResolution(resolution);


        yield return StartCoroutine(
            ApplyResolutionSequence(
                resolution
            )
        );


        movementInProgress = false;
    }


    // =====================================================
    // APPLY RESOLUTION
    // =====================================================

    private IEnumerator ApplyResolutionSequence(
        MovementResolution resolution)
    {
        if (resolution.AnyCollision)
        {
            yield return StartCoroutine(
                HandleBlockedMovement(
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
    }


    // =====================================================
    // REVEAL
    // =====================================================

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


    // =====================================================
    // BLOCKED MOVEMENT
    // =====================================================

    private IEnumerator HandleBlockedMovement(
        MovementResolution resolution)
    {
        turnManager.SetState(
            GameState.Moving
        );


        yield return StartCoroutine(
            AnimateMovementResults(
                resolution
            )
        );


        ApplyResolvedMovement(
            resolution
        );

        UpdateCoinVisualPositions(
    resolution
);


        boardManager.CurrentBoard
            .DebugPrintBoard();


        turnManager.SetState(
            GameState.CheckingResult
        );


        if (CheckGameResult())
        {
            if (IsOnlineGame())
                ReportNetworkPresentationComplete();
            yield break;
        }

        if (IsOnlineGame())
        {
            ReportNetworkPresentationComplete();
            yield break;
        }

        StartNextRoundCorrectly();
    }


    // =====================================================
    // SUCCESSFUL MOVEMENT
    // =====================================================

    private IEnumerator HandleSuccessfulMovement(
        MovementResolution resolution)
    {
        turnManager.SetState(
            GameState.Moving
        );


        Vector3 xStart =
            GetWorldPosition(
                resolution.playerXResult.startPosition
            );


        Vector3 xEnd =
            GetWorldPosition(
                resolution.playerXResult.destination
            );


        Vector3 yStart =
            GetWorldPosition(
                resolution.playerYResult.startPosition
            );


        Vector3 yEnd =
            GetWorldPosition(
                resolution.playerYResult.destination
            );


        yield return StartCoroutine(
            AnimateTwoCoins(
                xStart,
                xEnd,
                yStart,
                yEnd,
                movementDuration
            )
        );


        ApplySuccessfulBoardMovement(
            resolution
        );

        UpdateCoinVisualPositions(
    resolution
);


        boardManager.CurrentBoard
            .DebugPrintBoard();


        turnManager.SetState(
            GameState.CheckingResult
        );


        if (CheckGameResult())
        {
            if (IsOnlineGame())
                ReportNetworkPresentationComplete();
            yield break;
        }

        if (IsOnlineGame())
        {
            ReportNetworkPresentationComplete();
            yield break;
        }

        StartNextRoundCorrectly();
    }


    // =====================================================
    // APPLY SUCCESSFUL BOARD MOVEMENT
    // =====================================================

    private void ApplySuccessfulBoardMovement(
     MovementResolution resolution)
    {
        // =============================================
        // UPDATE BOARD DATA
        // =============================================

        boardManager.CurrentBoard
            .ApplySuccessfulMovement(
                resolution.playerXResult,
                resolution.playerYResult
            );


        // =============================================
        // UPDATE COIN VISUAL POSITIONS
        // =============================================

        if (placementManager == null)
        {
            Debug.LogError(
                "PlacementManager is missing."
            );

            return;
        }


        // ---------------------------------------------
        // PLAYER X
        // ---------------------------------------------

        placementManager.UpdateCoinPosition(
            resolution.playerXResult.startPosition,
            resolution.playerXResult.destination,
            playerXCoin
        );


        // ---------------------------------------------
        // PLAYER Y
        // ---------------------------------------------

        placementManager.UpdateCoinPosition(
            resolution.playerYResult.startPosition,
            resolution.playerYResult.destination,
            playerYCoin
        );
    }


    // =====================================================
    // APPLY BLOCKED RESULT
    // =====================================================

    private void ApplyResolvedMovement(
     MovementResolution resolution)
    {
        MovementResult xResult =
            resolution.playerXResult;

        MovementResult yResult =
            resolution.playerYResult;


        MovementResult xFinal =
            xResult;

        MovementResult yFinal =
            yResult;


        // =============================================
        // PLAYER X BLOCKED
        // =============================================

        if (
            resolution.playerXCollision !=
            MovementCollisionType.None
        )
        {
            xFinal =
                new MovementResult(
                    PlayerType.X,
                    xResult.startPosition,
                    xResult.startPosition,
                    xResult.direction
                );
        }


        // =============================================
        // PLAYER Y BLOCKED
        // =============================================

        if (
            resolution.playerYCollision !=
            MovementCollisionType.None
        )
        {
            yFinal =
                new MovementResult(
                    PlayerType.Y,
                    yResult.startPosition,
                    yResult.startPosition,
                    yResult.direction
                );
        }


        // =============================================
        // UPDATE BOARD
        // =============================================

        boardManager.CurrentBoard
            .ApplySuccessfulMovement(
                xFinal,
                yFinal
            );


        // =============================================
        // UPDATE VISUAL POSITION DATABASE
        // =============================================

        if (placementManager != null)
        {
            placementManager.UpdateCoinPosition(
                xFinal.startPosition,
                xFinal.destination,
                playerXCoin
            );


            placementManager.UpdateCoinPosition(
                yFinal.startPosition,
                yFinal.destination,
                playerYCoin
            );
        }
    }

    // =====================================================
    // UPDATE COIN VISUAL POSITIONS
    // =====================================================

    private void UpdateCoinVisualPositions(
        MovementResolution resolution)
    {
        PlacementManager placementManager =
            FindFirstObjectByType<PlacementManager>();


        if (placementManager == null)
        {
            Debug.LogError(
                "PlacementManager not found."
            );

            return;
        }


        // =============================================
        // PLAYER X
        // =============================================

        MovementResult xResult =
            resolution.playerXResult;


        Vector2Int xFinalPosition =
            xResult.destination;


        if (
            resolution.playerXCollision !=
            MovementCollisionType.None
        )
        {
            xFinalPosition =
                xResult.startPosition;
        }


        placementManager.UpdateCoinPosition(
            xResult.startPosition,
            xFinalPosition,
            playerXCoin
        );


        // =============================================
        // PLAYER Y
        // =============================================

        MovementResult yResult =
            resolution.playerYResult;


        Vector2Int yFinalPosition =
            yResult.destination;


        if (
            resolution.playerYCollision !=
            MovementCollisionType.None
        )
        {
            yFinalPosition =
                yResult.startPosition;
        }


        placementManager.UpdateCoinPosition(
            yResult.startPosition,
            yFinalPosition,
            playerYCoin
        );
    }


    // =====================================================
    // ANIMATE BLOCKED RESULTS
    // =====================================================

    private IEnumerator AnimateMovementResults(
        MovementResolution resolution)
    {
        Vector3 xStart =
            GetWorldPosition(
                resolution.playerXResult.startPosition
            );

        Vector3 xTarget =
            GetWorldPosition(
                resolution.playerXResult.destination
            );

        Vector3 yStart =
            GetWorldPosition(
                resolution.playerYResult.startPosition
            );

        Vector3 yTarget =
            GetWorldPosition(
                resolution.playerYResult.destination
            );


        bool xBlocked =
            resolution.playerXCollision !=
            MovementCollisionType.None;

        bool yBlocked =
            resolution.playerYCollision !=
            MovementCollisionType.None;


        yield return StartCoroutine(
            AnimateTwoCoins(
                xStart,
                xTarget,
                yStart,
                yTarget,
                movementDuration
            )
        );


        yield return new WaitForSeconds(
            blockedPauseDuration
        );


        Vector3 xFinal =
            xBlocked
                ? xStart
                : xTarget;

        Vector3 yFinal =
            yBlocked
                ? yStart
                : yTarget;


        yield return StartCoroutine(
            AnimateTwoCoins(
                xTarget,
                xFinal,
                yTarget,
                yFinal,
                returnDuration
            )
        );


        if (playerXCoin != null)
        {
            playerXCoin.transform.position =
                xFinal;
        }


        if (playerYCoin != null)
        {
            playerYCoin.transform.position =
                yFinal;
        }
    }


    // =====================================================
    // ANIMATE TWO COINS
    // =====================================================

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


    // =====================================================
    // WORLD POSITION
    // =====================================================

    private Vector3 GetWorldPosition(
        Vector2Int position)
    {
        return boardView.GetCellWorldPosition(
            position.x,
            position.y
        );
    }


    // =====================================================
    // GAME RESULT
    // =====================================================

    private bool CheckGameResult()
    {
        // =============================================
        // ONLINE GAME
        // =============================================

        if (IsOnlineGame())
        {
            if (networkGameResultManager == null)
            {
                Debug.LogError(
                    "NetworkGameResultManager is missing."
                );

                return false;
            }


            // Only Host decides the result.

            if (NetworkManager.Singleton.IsServer)
            {
                return networkGameResultManager
                    .CheckGameResultOnHost();
            }


            // Client does not independently
            // decide WIN or DRAW.

            return false;
        }


        // =============================================
        // LOCAL GAME
        // =============================================

        if (
            winConditionChecker.HasWinner(
                out PlayerType winner
            )
        )
        {
            gameResultManager.SetWinner(
                winner
            );

            return true;
        }


        if (winConditionChecker.IsBoardFull())
        {
            gameResultManager.SetDraw();

            return true;
        }


        return false;
    }


    private void ReportNetworkPresentationComplete()
    {
        if (!IsOnlineGame() ||
            networkMovementManager == null ||
            activeNetworkMoveId < 0)
        {
            return;
        }

        networkMovementManager.ReportPresentationComplete(
            activeNetworkMoveId);

        activeNetworkMoveId = -1;
    }

    public void ResetNetworkMovementState()
    {
        StopAllCoroutines();
        movementInProgress = false;
        playerXCoin = null;
        playerYCoin = null;
        activeNetworkMoveId = -1;
    }


    // =====================================================
    // ONLINE CHECK
    // =====================================================

    private bool IsOnlineGame()
    {
        return NetworkManager.Singleton != null &&
               NetworkManager.Singleton.IsListening;
    }


    // =====================================================
    // DEBUG
    // =====================================================

    private void LogResolution(
        MovementResolution resolution)
    {
        Debug.Log(
            $"X: " +
            $"{resolution.playerXResult.startPosition}" +
            $" → " +
            $"{resolution.playerXResult.destination}" +
            $" | Blocked: " +
            $"{resolution.playerXCollision}"
        );


        Debug.Log(
            $"Y: " +
            $"{resolution.playerYResult.startPosition}" +
            $" → " +
            $"{resolution.playerYResult.destination}" +
            $" | Blocked: " +
            $"{resolution.playerYCollision}"
        );
    }

    private void StartNextRoundCorrectly()
    {
        // =============================================
        // ONLINE GAME
        // =============================================

        if (IsOnlineGame())
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }


            if (networkGameResultManager != null)
            {
                networkGameResultManager
                    .StartNextRoundOnHost();

                return;
            }
        }


        // =============================================
        // LOCAL GAME
        // =============================================

        turnManager.StartNextRound();
    }
}