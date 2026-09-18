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

    /// <summary>
    /// Applies the authoritative network snapshot to the local presentation cache.
    /// This method does not perform a transition or trigger gameplay logic.
    /// </summary>
    public void ApplyAuthoritativeState(
        GameState state,
        Vector2Int xPosition,
        Vector2Int yPosition,
        MoveDirection xDirection,
        MoveDirection yDirection,
        bool xHasPlaced,
        bool yHasPlaced,
        bool xHasChosenDirection,
        bool yHasChosenDirection)
    {
        if (PlayerXAction == null || PlayerYAction == null)
            return;

        PlayerXAction.position = xPosition;
        PlayerXAction.direction = xDirection;
        PlayerXAction.hasPlaced = xHasPlaced;
        PlayerXAction.hasChosenDirection = xHasChosenDirection;

        PlayerYAction.position = yPosition;
        PlayerYAction.direction = yDirection;
        PlayerYAction.hasPlaced = yHasPlaced;
        PlayerYAction.hasChosenDirection = yHasChosenDirection;

        currentState = state;
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