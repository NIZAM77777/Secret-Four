using UnityEngine;

public class Board
{
    private readonly int width;
    private readonly int height;

    private readonly BoardCell[,] cells;

    public int Width => width;
    public int Height => height;

    public Board(int width, int height)
    {
        this.width = width;
        this.height = height;

        cells = new BoardCell[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] = new BoardCell(x, y);
            }
        }
    }

    public bool IsInsideBoard(int x, int y)
    {
        return x >= 0 &&
               x < width &&
               y >= 0 &&
               y < height;
    }

    public BoardCell GetCell(int x, int y)
    {
        if (!IsInsideBoard(x, y))
            return null;

        return cells[x, y];
    }

    public bool IsEmpty(int x, int y)
    {
        BoardCell cell = GetCell(x, y);

        return cell != null && cell.IsEmpty;
    }

    public bool TryPlace(int x, int y, PlayerType player)
    {
        BoardCell cell = GetCell(x, y);

        if (cell == null)
            return false;

        return cell.SetOccupant(player);
    }

    public void ClearCell(int x, int y)
    {
        BoardCell cell = GetCell(x, y);

        if (cell != null)
            cell.Clear();
    }

    public void ClearBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y].Clear();
            }
        }
    }
}