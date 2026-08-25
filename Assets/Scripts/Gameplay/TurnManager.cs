using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField]
    private GameState currentState;

    public GameState CurrentState => currentState;

    public PlayerAction PlayerXAction { get; private set; }
    public PlayerAction PlayerYAction { get; private set; }

    public int CollisionCount { get; private set; }

    private void Awake()
    {
        PlayerXAction =
            new PlayerAction(PlayerType.X);

        PlayerYAction =
            new PlayerAction(PlayerType.Y);

        CollisionCount = 0;

        SetState(
            GameState.WaitingForPlayerXPlacement
        );
    }

    public bool CanPlaceCoin(PlayerType player)
    {
        bool canPlace;

        if (player == PlayerType.X)
        {
            canPlace =
                currentState ==
                GameState.WaitingForPlayerXPlacement;
        }
        else
        {
            canPlace =
                currentState ==
                GameState.WaitingForPlayerYPlacement;
        }

        Debug.Log(
            $"CanPlaceCoin({player}) = {canPlace} | " +
            $"State = {currentState}"
        );

        return canPlace;
    }

    public bool CanChooseDirection(PlayerType player)
    {
        bool canChoose;

        if (player == PlayerType.X)
        {
            canChoose =
                currentState ==
                GameState.WaitingForPlayerXDirection;
        }
        else
        {
            canChoose =
                currentState ==
                GameState.WaitingForPlayerYDirection;
        }

        Debug.Log(
            $"CanChooseDirection({player}) = {canChoose} | " +
            $"State = {currentState}"
        );

        return canChoose;
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
            $"GAME STATE: " +
            $"{currentState} → {newState}"
        );

        currentState = newState;
    }

    public void RegisterCollision()
    {
        CollisionCount++;

        Debug.Log(
            $"COLLISION COUNT = {CollisionCount}"
        );
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

        Debug.Log(
            "Waiting for Player X placement."
        );
    }

    public void ResetMatch()
    {
        PlayerXAction.Reset();
        PlayerYAction.Reset();

        CollisionCount = 0;

        SetState(
            GameState.WaitingForPlayerXPlacement
        );
    }
}