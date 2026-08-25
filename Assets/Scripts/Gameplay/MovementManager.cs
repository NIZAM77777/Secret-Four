using System.Collections;
using UnityEngine;

public class MovementManager : MonoBehaviour
{
    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private MovementResolver movementResolver;

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private BoardView boardView;

    [SerializeField]
    private GameObject coinPrefab;

    [SerializeField]
    private CoinConfig coinConfig;

    [SerializeField]
    private Transform coinParent;

    [SerializeField]
    private float revealDelay = 2f;

    [SerializeField]
    private float movementDuration = 0.3f;

    private GameObject playerXCoin;
    private GameObject playerYCoin;

    public void StartMovementSequence(
        GameObject xCoin,
        GameObject yCoin)
    {
        playerXCoin = xCoin;
        playerYCoin = yCoin;

        StartCoroutine(
            RevealAndMove()
        );
    }

    private IEnumerator RevealAndMove()
    {
        Debug.Log(
            $"X Direction: " +
            $"{turnManager.PlayerXAction.direction}"
        );

        Debug.Log(
            $"Y Direction: " +
            $"{turnManager.PlayerYAction.direction}"
        );

        turnManager.SetState(
            GameState.RevealingDirections
        );

        yield return new WaitForSeconds(
            revealDelay
        );

        MovementResult xResult =
            CalculateMovement(
                turnManager.PlayerXAction
            );

        MovementResult yResult =
            CalculateMovement(
                turnManager.PlayerYAction
            );

        Debug.Log(
            $"X: {xResult.startPosition} → " +
            $"{xResult.destination}"
        );

        Debug.Log(
            $"Y: {yResult.startPosition} → " +
            $"{yResult.destination}"
        );

        turnManager.SetState(
            GameState.Moving
        );

        yield return StartCoroutine(
            MoveBothCoins(
                xResult,
                yResult
            )
        );

        turnManager.SetState(
            GameState.CheckingResult
        );
    }

    private MovementResult CalculateMovement(
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

    private IEnumerator MoveBothCoins(
        MovementResult xResult,
        MovementResult yResult)
    {
        Vector3 xStart =
            boardView.GetCellWorldPosition(
                xResult.startPosition.x,
                xResult.startPosition.y
            );

        Vector3 xEnd =
            boardView.GetCellWorldPosition(
                xResult.destination.x,
                xResult.destination.y
            );

        Vector3 yStart =
            boardView.GetCellWorldPosition(
                yResult.startPosition.x,
                yResult.startPosition.y
            );

        Vector3 yEnd =
            boardView.GetCellWorldPosition(
                yResult.destination.x,
                yResult.destination.y
            );

        float elapsed = 0f;

        while (elapsed < movementDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / movementDuration
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