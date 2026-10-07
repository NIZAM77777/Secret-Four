using System.Collections.Generic;
using UnityEngine;

public class WinConditionChecker : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    public List<Vector2Int> WinningPositions { get; } = new List<Vector2Int>();
    public bool HasSimultaneousWin => SecretFourRules.HasLine(boardManager.CurrentBoard, PlayerType.X) &&
        SecretFourRules.HasLine(boardManager.CurrentBoard, PlayerType.Y);
    public bool HasWinner(out PlayerType winner)
    {
        WinningPositions.Clear();
        winner = PlayerType.X;
        if (HasSimultaneousWin) return false;
        if (SecretFourRules.HasLine(boardManager.CurrentBoard, winner, WinningPositions)) return true;
        winner = PlayerType.Y;
        return SecretFourRules.HasLine(boardManager.CurrentBoard, winner, WinningPositions);
    }
    public bool IsBoardFull() => boardManager.CurrentBoard.IsFull();
}
