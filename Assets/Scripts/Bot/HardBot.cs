using System.Collections.Generic;
using UnityEngine;

public class HardBot : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private TurnManager turnManager;
    private BotPlanner.Plan plan;
    public Vector2Int GetBestPlacement(Board board, List<Vector2Int> emptyCells)
    {
        plan = BotPlanner.Choose(board, turnManager.PlayerXAction.position, BotDifficulty.Hard);
        return plan.valid ? plan.position : new Vector2Int(-1, -1);
    }
    public MoveDirection GetBestDirection(List<MoveDirection> validDirections)
    {
        if (!plan.valid || !validDirections.Contains(plan.direction))
            plan = BotPlanner.Choose(boardManager.CurrentBoard, turnManager.PlayerXAction.position,
                BotDifficulty.Hard, turnManager.PlayerYAction.position);
        MoveDirection direction = plan.valid ? plan.direction : MoveDirection.Stay;
        plan = default;
        return direction;
    }
}
