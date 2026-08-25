using UnityEngine;

public class GameResultManager : MonoBehaviour
{
    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private GameOverUI gameOverUI;

    public PlayerType Winner { get; private set; }

    public bool HasResult { get; private set; }

    public bool IsDraw { get; private set; }

    public void SetWinner(
        PlayerType player)
    {
        Winner = player;

        HasResult = true;
        IsDraw = false;

        turnManager.SetState(
            GameState.Won
        );

        Debug.Log(
            $"GAME OVER → {player} WINS!"
        );

        gameOverUI.ShowWinner(
            player
        );
    }

    public void SetDraw()
    {
        HasResult = true;
        IsDraw = true;

        turnManager.SetState(
            GameState.Draw
        );

        Debug.Log(
            "GAME OVER → DRAW!"
        );

        gameOverUI.ShowDraw();
    }

    public void ResetResult()
    {
        Winner = default;

        HasResult = false;
        IsDraw = false;

        gameOverUI.Hide();
    }
}