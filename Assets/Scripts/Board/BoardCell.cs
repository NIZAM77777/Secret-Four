using UnityEngine;

public class BoardCell
{
    public int X { get; }
    public int Y { get; }

    public PlayerType? Occupant { get; private set; }

    public bool IsEmpty => !Occupant.HasValue;

    public BoardCell(int x, int y)
    {
        X = x;
        Y = y;
        Occupant = null;
    }

    public bool SetOccupant(PlayerType player)
    {
        if (!IsEmpty)
            return false;

        Occupant = player;
        return true;
    }

    public void Clear()
    {
        Occupant = null;
    }
}