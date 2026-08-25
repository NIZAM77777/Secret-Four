using UnityEngine;

public class WinConditionChecker : MonoBehaviour
{
    [SerializeField]
    private BoardManager boardManager;

    public bool HasWinner(out PlayerType winner)
    {
        Board board =
            boardManager.CurrentBoard;

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

    public bool IsBoardFull()
    {
        return boardManager.CurrentBoard.IsFull();
    }

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

                // Horizontal
                if (CountDirection(
                    board,
                    x,
                    y,
                    1,
                    0,
                    player) >= 4)
                {
                    return true;
                }

                // Vertical
                if (CountDirection(
                    board,
                    x,
                    y,
                    0,
                    1,
                    player) >= 4)
                {
                    return true;
                }

                // Diagonal ↗
                if (CountDirection(
                    board,
                    x,
                    y,
                    1,
                    1,
                    player) >= 4)
                {
                    return true;
                }

                // Diagonal ↘
                if (CountDirection(
                    board,
                    x,
                    y,
                    1,
                    -1,
                    player) >= 4)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private int CountDirection(
        Board board,
        int startX,
        int startY,
        int directionX,
        int directionY,
        PlayerType player)
    {
        int count = 0;

        int x = startX;
        int y = startY;

        while (
            board.IsInsideBoard(x, y) &&
            IsPlayerAt(
                board,
                x,
                y,
                player))
        {
            count++;

            x += directionX;
            y += directionY;
        }

        return count;
    }

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