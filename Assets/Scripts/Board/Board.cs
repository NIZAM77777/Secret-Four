using UnityEngine;

public class Board
{
    private readonly int width;
    private readonly int height;

    private readonly BoardCell[,] cells;

    public int Width => width;
    public int Height => height;

    public Board(
        int width,
        int height)
    {
        this.width = width;
        this.height = height;

        cells =
            new BoardCell[
                width,
                height
            ];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cells[x, y] =
                    new BoardCell(x, y);
            }
        }
    }

    public bool IsInsideBoard(
        int x,
        int y)
    {
        return x >= 0 &&
               x < width &&
               y >= 0 &&
               y < height;
    }

    public BoardCell GetCell(
        int x,
        int y)
    {
        if (!IsInsideBoard(x, y))
            return null;

        return cells[x, y];
    }

    public bool IsEmpty(
        int x,
        int y)
    {
        BoardCell cell =
            GetCell(x, y);

        return cell != null &&
               cell.IsEmpty;
    }

    public bool TryPlace(
        int x,
        int y,
        PlayerType player)
    {
        BoardCell cell =
            GetCell(x, y);

        if (cell == null)
            return false;

        return cell.SetOccupant(player);
    }

    public void ClearCell(
        int x,
        int y)
    {
        BoardCell cell =
            GetCell(x, y);

        if (cell != null)
        {
            cell.Clear();
        }
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

    // -------------------------------------------------
    // Move BOTH successful coins simultaneously.
    // -------------------------------------------------

    public void ApplySuccessfulMovement(
        MovementResult xResult,
        MovementResult yResult)
    {
        BoardCell xStart =
            GetCell(
                xResult.startPosition.x,
                xResult.startPosition.y
            );

        BoardCell yStart =
            GetCell(
                yResult.startPosition.x,
                yResult.startPosition.y
            );

        // Remove both current positions first.
        //
        // This prevents one movement from affecting
        // the other movement.

        if (xResult.IsMoving &&
            xStart != null &&
            xStart.Occupant == PlayerType.X)
        {
            xStart.Clear();
        }

        if (yResult.IsMoving &&
            yStart != null &&
            yStart.Occupant == PlayerType.Y)
        {
            yStart.Clear();
        }

        // -------------------------------------------------
        // Place successful X
        // -------------------------------------------------

        if (xResult.IsMoving)
        {
            BoardCell destination =
                GetCell(
                    xResult.destination.x,
                    xResult.destination.y
                );

            if (destination != null &&
                destination.IsEmpty)
            {
                destination.SetOccupant(
                    PlayerType.X
                );
            }
        }

        // -------------------------------------------------
        // Place successful Y
        // -------------------------------------------------

        if (yResult.IsMoving)
        {
            BoardCell destination =
                GetCell(
                    yResult.destination.x,
                    yResult.destination.y
                );

            if (destination != null &&
                destination.IsEmpty)
            {
                destination.SetOccupant(
                    PlayerType.Y
                );
            }
        }
    }

    public bool IsFull()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (cells[x, y].IsEmpty)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void DebugPrintBoard()
    {
        for (int y = height - 1; y >= 0; y--)
        {
            string row = "";

            for (int x = 0; x < width; x++)
            {
                BoardCell cell =
                    cells[x, y];

                if (cell.IsEmpty)
                {
                    row += ". ";
                }
                else if (
                    cell.Occupant ==
                    PlayerType.X)
                {
                    row += "X ";
                }
                else
                {
                    row += "Y ";
                }
            }

            Debug.Log(row);
        }
    }
}