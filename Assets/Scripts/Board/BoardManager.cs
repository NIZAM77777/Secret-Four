using System.Collections.Generic;
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


    // =====================================================
    // INITIALIZE BOARD
    // =====================================================

    public void InitializeBoard()
    {
        if (gameConfig == null)
        {
            Debug.LogError(
                "BoardManager: GameConfig is not assigned."
            );

            return;
        }

        board = new Board(
            gameConfig.boardWidth,
            gameConfig.boardHeight
        );

        Debug.Log(
            $"Board initialized: " +
            $"{board.Width} x {board.Height}"
        );
    }


    public void ApplySnapshot(
        IList<byte> boardCells,
        IList<Unity.Collections.FixedString64Bytes> skinIds)
    {
        if (board == null || boardCells == null)
            return;

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                int index = x * board.Height + y;
                if (index < 0 || index >= boardCells.Count)
                    continue;

                board.ClearCell(x, y);

                if (boardCells[index] == 1)
                    board.TryPlace(x, y, PlayerType.X);
                else if (boardCells[index] == 2)
                    board.TryPlace(x, y, PlayerType.Y);
            }
        }
    }


    // =====================================================
    // RESET BOARD
    // =====================================================

    public void ResetBoard()
    {
        if (board == null)
        {
            InitializeBoard();
            return;
        }

        board.ClearBoard();

        Debug.Log(
            "Board reset for new match."
        );
    }
}