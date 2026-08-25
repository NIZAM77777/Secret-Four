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
        PlayerXAction = new PlayerAction(PlayerType.X);
        PlayerYAction = new PlayerAction(PlayerType.Y);

        currentState = GameState.WaitingForPlayerXPlacement;
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
            currentState =
                GameState.WaitingForPlayerXDirection;
        }
        else
        {
            currentState =
                GameState.WaitingForPlayerYDirection;
        }
    }

    public void DirectionChosen(PlayerType player)
    {
        if (player == PlayerType.X)
        {
            currentState =
                GameState.WaitingForPlayerYPlacement;
        }
        else
        {
            currentState =
                GameState.RevealingDirections;
        }
    }

    public PlayerAction GetAction(PlayerType player)
    {
        return player == PlayerType.X
            ? PlayerXAction
            : PlayerYAction;
    }

    public void ResetActions()
    {
        PlayerXAction.Reset();
        PlayerYAction.Reset();

        currentState =
            GameState.WaitingForPlayerXPlacement;
    }

    public void SetState(GameState newState)
    {
        currentState = newState;
    }
}