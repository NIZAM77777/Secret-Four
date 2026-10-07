using UnityEngine;
using Unity.Netcode;

public class DirectionSelector : MonoBehaviour
{
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private PlacementManager placementManager;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private NetworkDirectionManager networkDirectionManager;
    public void SelectUp() => SelectDirection(MoveDirection.Up);
    public void SelectDown() => SelectDirection(MoveDirection.Down);
    public void SelectLeft() => SelectDirection(MoveDirection.Left);
    public void SelectRight() => SelectDirection(MoveDirection.Right);
    public void SelectStay() => SelectDirection(MoveDirection.Stay);

    public void SelectDirection(MoveDirection direction)
    {
        if (Time.timeScale == 0) return;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkGameState state = NetworkGameState.Instance;
            if (state == null || !SecretFourRules.IsDirection(direction)) return;
            PlayerType side = state.IsLocalPlayerSide(PlayerType.X) ? PlayerType.X : PlayerType.Y;
            if (state.CanRequestDirection(side)) networkDirectionManager.RequestDirection(direction);
            return;
        }
        PlayerType player = turnManager.CurrentState == GameState.WaitingForPlayerXDirection ? PlayerType.X : PlayerType.Y;
        if (turnManager.CanLocalHumanAct(player) && turnManager.CanChooseDirection(player))
            ApplyDirection(player, direction);
    }

    // Used by the local bot after it has planned its own turn.
    public void ApplyDirection(PlayerType player, MoveDirection direction)
    {
        if (!turnManager.CanChooseDirection(player)) return;
        PlayerAction action = turnManager.GetAction(player);
        PlayerAction other = turnManager.GetAction(player == PlayerType.X ? PlayerType.Y : PlayerType.X);
        if (!action.hasPlaced || !SecretFourRules.CanChooseDirection(boardManager.CurrentBoard, action.position,
            direction, other.hasPlaced ? other.position : (Vector2Int?)null)) return;
        action.direction = direction;
        action.hasChosenDirection = true;
        turnManager.DirectionChosen(player);
        if (player == PlayerType.X && boardManager.CurrentBoard.IsFull()) turnManager.SetState(GameState.RevealingDirections);
        if (turnManager.CurrentState == GameState.RevealingDirections)
            movementManager.StartMovementSequence(placementManager.PlayerXCoin,
                turnManager.PlayerYAction.hasPlaced ? placementManager.PlayerYCoin : null);
    }
}
