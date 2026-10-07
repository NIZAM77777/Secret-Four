using UnityEngine;

public class MovementRuleResolver : MonoBehaviour
{
    [SerializeField] private BoardManager boardManager;
    public MovementResolution Resolve(PlayerAction playerXAction, PlayerAction playerYAction) =>
        SecretFourRules.Resolve(boardManager.CurrentBoard, playerXAction, playerYAction);
}
