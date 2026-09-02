using System.Collections;
using UnityEngine;

public class GameResultManager : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private GameOverUI gameOverUI;

    [SerializeField]
    private WinGlowManager winGlowManager;


    [Header("Result Timing")]

    [SerializeField]
    private float resultDelay = 1.5f;


    public PlayerType Winner { get; private set; }

    public bool HasResult { get; private set; }

    public bool IsDraw { get; private set; }


    // =====================================================
    // SET WINNER
    // =====================================================

    public void SetWinner(
    PlayerType player)
    {
        if (HasResult)
        {
            return;
        }


        Winner = player;

        HasResult = true;

        IsDraw = false;


        turnManager.SetState(
            GameState.Won
        );


        Debug.Log(
            $"GAME OVER → {player} WINS!"
        );


        // =============================================
        // GLOW WINNING COINS ⭐
        // =============================================

        if (winGlowManager != null)
        {
            winGlowManager.GlowWinningCoins();
        }


        StartCoroutine(
            ShowWinnerAfterDelay(
                player
            )
        );
    }


    // =====================================================
    // SET DRAW
    // =====================================================

    public void SetDraw()
    {
        if (HasResult)
        {
            return;
        }


        HasResult = true;

        IsDraw = true;


        turnManager.SetState(
            GameState.Draw
        );


        Debug.Log(
            "GAME OVER → DRAW!"
        );


        StartCoroutine(
            ShowDrawAfterDelay()
        );
    }


    // =====================================================
    // SHOW WINNER AFTER DELAY
    // =====================================================

    private IEnumerator ShowWinnerAfterDelay(
        PlayerType player)
    {
        yield return new WaitForSeconds(
            resultDelay
        );


        gameOverUI.ShowWinner(
            player
        );
    }


    // =====================================================
    // SHOW DRAW AFTER DELAY
    // =====================================================

    private IEnumerator ShowDrawAfterDelay()
    {
        yield return new WaitForSeconds(
            resultDelay
        );


        gameOverUI.ShowDraw();
    }


    // =====================================================
    // RESET RESULT
    // =====================================================

    public void ResetResult()
    {
        StopAllCoroutines();


        Winner = default;

        HasResult = false;

        IsDraw = false;


        // =============================================
        // STOP WINNING GLOW
        // =============================================

        if (winGlowManager != null)
        {
            winGlowManager.StopAllGlows();
        }


        gameOverUI.Hide();


        Debug.Log(
            "Game result reset."
        );
    }
}