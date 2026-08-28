using UnityEngine;

public class MatchManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private PlacementManager placementManager;

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private GameResultManager gameResultManager;

    public void StartNewMatch()
    {
        Debug.Log(
            "========== NEW MATCH =========="
        );

        placementManager.ClearAllCoins();

        boardManager.CurrentBoard.ClearBoard();

        gameResultManager.ResetResult();

        turnManager.ResetMatch();

        Debug.Log(
            "New match ready."
        );
    }
}