using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField]
    private GameState currentState;

    public GameState CurrentState => currentState;

    public PlayerAction PlayerXAction { get; private set; }
    public PlayerAction PlayerYAction { get; private set; }

    private void Awake()
    {
        PlayerXAction =
            new PlayerAction(PlayerType.X);

        PlayerYAction =
            new PlayerAction(PlayerType.Y);

        SetState(
            GameState.WaitingForPlayerXPlacement
        );
    }

    public bool CanPlaceCoin(PlayerType player)
    {
        if (player == PlayerType.X)
        {
            return currentState ==
                GameState.WaitingForPlayerXPlacement;
        }

        return currentState ==
            GameState.WaitingForPlayerYPlacement;
    }

    public bool CanChooseDirection(PlayerType player)
    {
        if (player == PlayerType.X)
        {
            return currentState ==
                GameState.WaitingForPlayerXDirection;
        }

        return currentState ==
            GameState.WaitingForPlayerYDirection;
    }

    public void PlayerPlaced(PlayerType player)
    {
        if (player == PlayerType.X)
        {
            SetState(
                GameState.WaitingForPlayerXDirection
            );
        }
        else
        {
            SetState(
                GameState.WaitingForPlayerYDirection
            );
        }
    }

    public void DirectionChosen(PlayerType player)
    {
        if (player == PlayerType.X)
        {
            SetState(
                GameState.WaitingForPlayerYPlacement
            );
        }
        else
        {
            SetState(
                GameState.RevealingDirections
            );
        }
    }

    public PlayerAction GetAction(PlayerType player)
    {
        return player == PlayerType.X
            ? PlayerXAction
            : PlayerYAction;
    }

    public void SetState(GameState newState)
    {
        Debug.Log(
            $"GAME STATE: {currentState} → {newState}"
        );

        currentState = newState;
    }

    public void StartNextRound()
    {
        PlayerXAction.Reset();
        PlayerYAction.Reset();

        SetState(
            GameState.WaitingForPlayerXPlacement
        );

        Debug.Log(
            "===== NEXT ROUND ====="
        );
    }

    public void ResetMatch()
    {
        PlayerXAction.Reset();
        PlayerYAction.Reset();

        SetState(
            GameState.WaitingForPlayerXPlacement
        );
    }
}