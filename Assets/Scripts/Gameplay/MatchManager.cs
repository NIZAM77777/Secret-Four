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
        // 1. Reset Game Result
        // --------------------------------

        gameResultManager.ResetResult();


        // --------------------------------
        // 2. Clear Coin Visuals
        // --------------------------------

        placementManager.ClearAllCoins();


        // --------------------------------
        // 3. Clear Logical Board
        // --------------------------------

        boardManager.CurrentBoard.ClearBoard();


        // --------------------------------
        // 4. Reset Turn System
        // --------------------------------

        turnManager.ResetMatch();


        Debug.Log(
            "New match ready. Player X starts."
        );
    }
}