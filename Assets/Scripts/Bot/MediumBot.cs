using UnityEngine;

public class MediumBot : MonoBehaviour
{
    private BotPlanner.Plan plan;
    public Vector2Int ChoosePosition(Board board, Vector2Int playerXPosition)
    {
        plan = BotPlanner.Choose(board, playerXPosition, BotDifficulty.Medium);
        return plan.valid ? plan.position : new Vector2Int(-1, -1);
    }
    public MoveDirection ChooseDirection(Board board, Vector2Int position, Vector2Int playerXPosition)
    {
        if (!plan.valid || plan.position != position || !SecretFourRules.CanChooseDirection(board, position, plan.direction, playerXPosition))
            plan = BotPlanner.Choose(board, playerXPosition, BotDifficulty.Medium, position);
        MoveDirection direction = plan.valid ? plan.direction : MoveDirection.Stay;
        plan = default;
        return direction;
    }
}
