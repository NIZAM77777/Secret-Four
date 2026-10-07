using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class DirectionButtonController : MonoBehaviour
{
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button stayButton;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private BoardManager boardManager;

    private void Update() => RefreshForCurrentTurn();
    public void RefreshForCurrentTurn()
    {
        if (turnManager == null || boardManager == null || Time.timeScale == 0) { SetAllInteractable(false); return; }
        GameState state = turnManager.CurrentState;
        PlayerType player = state == GameState.WaitingForPlayerXDirection ? PlayerType.X : PlayerType.Y;
        if (!turnManager.CanChooseDirection(player) || !turnManager.CanLocalHumanAct(player)) { SetAllInteractable(false); return; }
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkGameState.Instance == null || !NetworkGameState.Instance.CanRequestDirection(player) ||
                !NetworkGameState.Instance.IsLocalPlayerSide(player)) { SetAllInteractable(false); return; }
        }
        UpdateDirectionButtons(player);
    }
    public void UpdateDirectionButtons(PlayerType player)
    {
        PlayerAction action = turnManager.GetAction(player);
        PlayerAction other = turnManager.GetAction(player == PlayerType.X ? PlayerType.Y : PlayerType.X);
        Vector2Int? otherPosition = other.hasPlaced ? other.position : (Vector2Int?)null;
        Board board = boardManager.CurrentBoard;
        Set(upButton, MoveDirection.Up); Set(downButton, MoveDirection.Down);
        Set(leftButton, MoveDirection.Left); Set(rightButton, MoveDirection.Right); Set(stayButton, MoveDirection.Stay);
        void Set(Button button, MoveDirection direction)
        {
            if (button != null) button.interactable = action.hasPlaced && turnManager.CanLocalHumanAct(player) &&
                SecretFourRules.CanChooseDirection(board, action.position, direction, otherPosition);
        }
    }
    private void SetAllInteractable(bool value)
    {
        if (upButton != null) upButton.interactable = value;
        if (downButton != null) downButton.interactable = value;
        if (leftButton != null) leftButton.interactable = value;
        if (rightButton != null) rightButton.interactable = value;
        if (stayButton != null) stayButton.interactable = value;
    }
    public void ResetButtons() => RefreshForCurrentTurn();
}
