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

        // --------------------------------
        // Clear visual coins
        // --------------------------------

        placementManager.ClearAllCoins();

        // --------------------------------
        // Clear logical board
        // --------------------------------

        boardManager.CurrentBoard.ClearBoard();

        // --------------------------------
        // Reset result
        // --------------------------------

        gameResultManager.ResetResult();

        // --------------------------------
        // Reset turn / actions / collisions
        // --------------------------------

        turnManager.ResetMatch();

        Debug.Log(
            "New match ready."
        );
    }
}