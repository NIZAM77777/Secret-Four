using System.Collections.Generic;
using UnityEngine;

/// <summary>Chooses a placement and direction together, without reading the opponent's secret choice.</summary>
public static class BotPlanner
{
    public struct Plan
    {
        public Vector2Int position;
        public MoveDirection direction;
        public bool valid;
    }
    private const double WinScore = 1000000;
    private static readonly Vector2Int[] Axes = { Vector2Int.right, Vector2Int.up, new Vector2Int(1, 1), new Vector2Int(1, -1) };

    public static Plan Choose(Board board, Vector2Int xPosition, BotDifficulty difficulty, Vector2Int? fixedPosition = null)
    {
        var placements = new List<Vector2Int>();
        if (fixedPosition.HasValue) placements.Add(fixedPosition.Value);
        else
            for (int x = 0; x < board.Width; x++)
            for (int y = 0; y < board.Height; y++)
                if (board.IsEmpty(x, y)) placements.Add(new Vector2Int(x, y));
        if (placements.Count == 0) return default;

        if (difficulty == BotDifficulty.Easy)
        {
            Vector2Int position = placements[Random.Range(0, placements.Count)];
            var directions = SecretFourRules.LegalDirections(board, position, xPosition);
            return new Plan { position = position, direction = directions[Random.Range(0, directions.Count)], valid = true };
        }

        // X chose before Y was placed, so its legal possibilities come from that board.
        Board beforeY = board.Copy();
        if (fixedPosition.HasValue) beforeY.ClearCell(fixedPosition.Value.x, fixedPosition.Value.y);
        var xDirections = SecretFourRules.LegalDirections(beforeY, xPosition);
        if (xDirections.Count == 0) xDirections.Add(MoveDirection.Stay);
        Plan best = default;
        double bestScore = double.NegativeInfinity;
        int tied = 0;
        foreach (Vector2Int position in placements)
        {
            Board placed = beforeY.Copy();
            if (!placed.TryPlace(position.x, position.y, PlayerType.Y)) continue;
            foreach (MoveDirection direction in SecretFourRules.LegalDirections(placed, position, xPosition))
            {
                double worst = double.PositiveInfinity;
                double total = 0;
                foreach (MoveDirection xDirection in xDirections)
                {
                    Board simulated = placed.Copy();
                    var xAction = Action(PlayerType.X, xPosition, xDirection);
                    var yAction = Action(PlayerType.Y, position, direction);
                    SecretFourRules.Apply(simulated, SecretFourRules.Resolve(simulated, xAction, yAction));
                    double value = Evaluate(simulated);
                    worst = System.Math.Min(worst, value);
                    total += value;
                }
                double average = total / xDirections.Count;
                // Hard prioritizes its safest outcome; expected value breaks equal safe outcomes.
                double score = difficulty == BotDifficulty.Hard ? worst * 100 + average : average;
                if (score > bestScore + 0.001)
                {
                    bestScore = score;
                    tied = 1;
                    best = new Plan { position = position, direction = direction, valid = true };
                }
                else if (System.Math.Abs(score - bestScore) < 0.001 && Random.Range(0, ++tied) == 0)
                    best = new Plan { position = position, direction = direction, valid = true };
            }
        }
        return best;
    }

    private static PlayerAction Action(PlayerType player, Vector2Int position, MoveDirection direction) =>
        new PlayerAction(player) { position = position, direction = direction, hasPlaced = true, hasChosenDirection = true };

    private static double Evaluate(Board board)
    {
        bool xWin = SecretFourRules.HasLine(board, PlayerType.X);
        bool yWin = SecretFourRules.HasLine(board, PlayerType.Y);
        if (xWin && yWin) return 0;
        if (yWin) return WinScore;
        if (xWin) return -WinScore;
        if (board.IsFull()) return 0;
        double score = 0;
        foreach (Vector2Int axis in Axes)
        for (int x = 0; x < board.Width; x++)
        for (int y = 0; y < board.Height; y++)
        {
            if (!board.IsInsideBoard(x + (board.WinLength - 1) * axis.x, y + (board.WinLength - 1) * axis.y)) continue;
            int xs = 0, ys = 0;
            for (int i = 0; i < board.WinLength; i++)
            {
                PlayerType? occupant = board.GetCell(x + i * axis.x, y + i * axis.y).Occupant;
                if (occupant == PlayerType.X) xs++;
                if (occupant == PlayerType.Y) ys++;
            }
            if (xs == 0 && ys > 0) score += System.Math.Pow(8, System.Math.Min(ys, 5));
            if (ys == 0 && xs > 0) score -= System.Math.Pow(8, System.Math.Min(xs, 5)) * 1.2;
        }
        // Keep non-terminal scores below terminal results, including configurable boards.
        return System.Math.Max(-WinScore / 2, System.Math.Min(WinScore / 2, score));
    }
}
