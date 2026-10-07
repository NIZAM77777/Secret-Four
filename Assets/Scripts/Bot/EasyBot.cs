using System.Collections.Generic;
using UnityEngine;

public class EasyBot : MonoBehaviour
{
    public Vector2Int ChoosePosition(Board board)
    {
        var cells = new List<Vector2Int>();
        for (int x = 0; x < board.Width; x++)
        for (int y = 0; y < board.Height; y++)
            if (board.IsEmpty(x, y)) cells.Add(new Vector2Int(x, y));
        return cells.Count == 0 ? new Vector2Int(-1, -1) : cells[Random.Range(0, cells.Count)];
    }
    public MoveDirection ChooseDirection(Board board, Vector2Int position, Vector2Int playerXPosition)
    {
        var directions = SecretFourRules.LegalDirections(board, position, playerXPosition);
        return directions.Count == 0 ? MoveDirection.Stay : directions[Random.Range(0, directions.Count)];
    }
}
