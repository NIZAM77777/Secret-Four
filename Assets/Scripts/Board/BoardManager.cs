using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [SerializeField]
    private GameConfig gameConfig;

    private Board board;

    public Board CurrentBoard => board;

    private void Awake()
    {
        InitializeBoard();
    }

    public void InitializeBoard()
    {
        if (gameConfig == null)
        {
            Debug.LogError("BoardManager: GameConfig is not assigned.");
            return;
        }

        board = new Board(
            gameConfig.boardWidth,
            gameConfig.boardHeight
        );

        Debug.Log(
            $"Board initialized: {board.Width} x {board.Height}"
        );
    }

}