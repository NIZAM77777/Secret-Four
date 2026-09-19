using UnityEngine;
using UnityEngine.UI;

public class DirectionButtonController : MonoBehaviour
{
    [Header("Direction Buttons")]

    [SerializeField]
    private Button upButton;

    [SerializeField]
    private Button downButton;

    [SerializeField]
    private Button leftButton;

    [SerializeField]
    private Button rightButton;

    [SerializeField]
    private Button stayButton;

    [Header("References")]

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private BoardManager boardManager;


    public void RefreshForCurrentTurn()
    {
        if (turnManager == null)
            return;

        if (Network.Netcode.NetworkManager.Singleton != null &&
            Network.Netcode.NetworkManager.Singleton.IsListening &&
            GameSession.Instance != null &&
            GameSession.Instance.CurrentGameMode == GameMode.OnlineRoom)
        {
            GameState state =
                NetworkGameState.Instance != null
                    ? NetworkGameState.Instance.GetAuthoritativeState()
                    : turnManager.CurrentState;

            PlayerType localPlayer =
                Network.Netcode.NetworkManager.Singleton.IsHost
                    ? PlayerType.X
                    : PlayerType.Y;

            if (OnlineMatchPlayerState.Instance != null &&
                OnlineMatchPlayerState.Instance.IsClientAssigned(
                    Network.Netcode.NetworkManager.Singleton.LocalClientId))
            {
                localPlayer =
                    OnlineMatchPlayerState.Instance.IsLocalSide(PlayerType.X)
                        ? PlayerType.X
                        : PlayerType.Y;
            }

            bool localDirectionTurn =
                (localPlayer == PlayerType.X &&
                 state == GameState.WaitingForPlayerXDirection) ||
                (localPlayer == PlayerType.Y &&
                 state == GameState.WaitingForPlayerYDirection);

            if (!localDirectionTurn)
            {
                SetAllInteractable(false);
                return;
            }

            UpdateDirectionButtons(localPlayer);
            return;
        }

        PlayerType player =
            turnManager.CurrentState == GameState.WaitingForPlayerXDirection
                ? PlayerType.X
                : PlayerType.Y;

        if (turnManager.CanChooseDirection(player))
            UpdateDirectionButtons(player);
        else
            SetAllInteractable(false);
    }

    private void SetAllInteractable(bool interactable)
    {
        if (upButton != null)
            upButton.interactable = interactable;
        if (downButton != null)
            downButton.interactable = interactable;
        if (leftButton != null)
            leftButton.interactable = interactable;
        if (rightButton != null)
            rightButton.interactable = interactable;
        if (stayButton != null)
            stayButton.interactable = interactable;
    }

    public void UpdateDirectionButtons(
    PlayerType player)
    {
        PlayerAction action =
            turnManager.GetAction(player);

        Vector2Int position =
            action.position;


        // =====================================
        // CHECK ALL DIRECTIONS
        // =====================================

        bool canMoveUp =
            CanMoveToDestination(
                player,
                new Vector2Int(
                    position.x,
                    position.y + 1
                )
            );

        bool canMoveDown =
            CanMoveToDestination(
                player,
                new Vector2Int(
                    position.x,
                    position.y - 1
                )
            );

        bool canMoveLeft =
            CanMoveToDestination(
                player,
                new Vector2Int(
                    position.x - 1,
                    position.y
                )
            );

        bool canMoveRight =
            CanMoveToDestination(
                player,
                new Vector2Int(
                    position.x + 1,
                    position.y
                )
            );


        // =====================================
        // UPDATE DIRECTION BUTTONS
        // =====================================

        if (upButton != null)
            upButton.interactable = canMoveUp;

        if (downButton != null)
            downButton.interactable = canMoveDown;

        if (leftButton != null)
            leftButton.interactable = canMoveLeft;

        if (rightButton != null)
            rightButton.interactable = canMoveRight;


        // Stay is always a valid direction.

        if (stayButton != null)
        {
            stayButton.interactable = true;
        }
    }


    private void UpdateButton(
        Button button,
        PlayerType currentPlayer,
        Vector2Int destination)
    {
        if (button == null)
            return;

        button.interactable =
            CanMoveToDestination(
                currentPlayer,
                destination
            );
    }


    private bool CanMoveToDestination(
        PlayerType currentPlayer,
        Vector2Int destination)
    {
        Board board =
            boardManager.CurrentBoard;

        // =====================================
        // 1. Outside board
        // =====================================

        if (!board.IsInsideBoard(
            destination.x,
            destination.y))
        {
            return false;
        }


        // =====================================
        // 2. Empty cell
        // =====================================

        if (board.IsEmpty(
            destination.x,
            destination.y))
        {
            return true;
        }


        // =====================================
        // 3. Check the OTHER current-round coin
        // =====================================

        PlayerType otherPlayer =
            currentPlayer == PlayerType.X
                ? PlayerType.Y
                : PlayerType.X;

        PlayerAction otherAction =
            turnManager.GetAction(otherPlayer);


        // If the occupied cell belongs to the
        // coin placed during THIS round,
        // allow movement.
        //
        // This allows:
        // - swapping
        // - simultaneous movement
        // - same destination logic
        //
        if (otherAction.hasPlaced &&
            destination ==
            otherAction.position)
        {
            return true;
        }


        // =====================================
        // 4. Any other occupied cell
        // =====================================
        //
        // This is a permanent / previous coin.
        // Do not allow selecting this direction.

        return false;
    }


    public void ResetButtons()
    {
        if (upButton != null)
            upButton.interactable = true;

        if (downButton != null)
            downButton.interactable = true;

        if (leftButton != null)
            leftButton.interactable = true;

        if (rightButton != null)
            rightButton.interactable = true;

        if (stayButton != null)
            stayButton.interactable = true;
    }
}