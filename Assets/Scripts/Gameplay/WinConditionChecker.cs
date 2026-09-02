using System.Collections.Generic;
using UnityEngine;

public class WinConditionChecker : MonoBehaviour
{
    [SerializeField]
    private BoardManager boardManager;


    // Stores the winning coin positions
    public List<Vector2Int> WinningPositions
    { get; private set; }
        = new List<Vector2Int>();


    // =====================================================
    // CHECK WINNER
    // =====================================================

    public bool HasWinner(out PlayerType winner)
    {
        Board board =
            boardManager.CurrentBoard;


        // Clear old winning positions

        WinningPositions.Clear();


        // Check X

        if (HasFour(board, PlayerType.X))
        {
            winner = PlayerType.X;

            return true;
        }


        // Check Y

        if (HasFour(board, PlayerType.Y))
        {
            winner = PlayerType.Y;

            return true;
        }


        winner = default;

        return false;
    }


    // =====================================================
    // BOARD FULL
    // =====================================================

    public bool IsBoardFull()
    {
        return boardManager.CurrentBoard.IsFull();
    }


    // =====================================================
    // CHECK FOUR
    // =====================================================

    private bool HasFour(
        Board board,
        PlayerType player)
    {
        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                if (!IsPlayerAt(
                    board,
                    x,
                    y,
                    player))
                {
                    continue;
                }


                // =========================================
                // HORIZONTAL →
                // =========================================

                if (CheckWinningDirection(
                    board,
                    x,
                    y,
                    1,
                    0,
                    player))
                {
                    return true;
                }


                // =========================================
                // VERTICAL ↑
                // =========================================

                if (CheckWinningDirection(
                    board,
                    x,
                    y,
                    0,
                    1,
                    player))
                {
                    return true;
                }


                // =========================================
                // DIAGONAL ↗
                // =========================================

                if (CheckWinningDirection(
                    board,
                    x,
                    y,
                    1,
                    1,
                    player))
                {
                    return true;
                }


                // =========================================
                // DIAGONAL ↘
                // =========================================

                if (CheckWinningDirection(
                    board,
                    x,
                    y,
                    1,
                    -1,
                    player))
                {
                    return true;
                }
            }
        }


        return false;
    }


    // =====================================================
    // CHECK WINNING DIRECTION
    // =====================================================

    private bool CheckWinningDirection(
        Board board,
        int startX,
        int startY,
        int directionX,
        int directionY,
        PlayerType player)
    {
        List<Vector2Int> positions =
            new List<Vector2Int>();


        int x = startX;

        int y = startY;


        // Count connected coins

        while (
            board.IsInsideBoard(x, y) &&
            IsPlayerAt(
                board,
                x,
                y,
                player))
        {
            positions.Add(
                new Vector2Int(x, y)
            );


            x += directionX;

            y += directionY;
        }


        // Need minimum 4

        if (positions.Count < 4)
        {
            return false;
        }


        // Save winning positions

        WinningPositions.Clear();

        WinningPositions.AddRange(
            positions
        );


        return true;
    }


    // =====================================================
    // CHECK PLAYER AT CELL
    // =====================================================

    private bool IsPlayerAt(
        Board board,
        int x,
        int y,
        PlayerType player)
    {
        BoardCell cell =
            board.GetCell(x, y);


        if (cell == null)
        {
            return false;
        }


        if (cell.IsEmpty)
        {
            return false;
        }


        return cell.Occupant == player;
    }
}