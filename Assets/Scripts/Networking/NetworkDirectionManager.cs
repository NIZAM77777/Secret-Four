using UnityEngine;
using Unity.Netcode;

public class NetworkDirectionManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private NetworkMovementManager networkMovementManager;
    [SerializeField] private BoardManager boardManager;

    public void RequestDirection(MoveDirection direction)
    {
        if (!IsSpawned)
        {
            Debug.LogError("NetworkDirectionManager is not spawned.");
            return;
        }

        RequestDirectionRpc((int)direction);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestDirectionRpc(
        int directionValue,
        RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        if (turnManager == null || boardManager == null)
        {
            Debug.LogError("Network references are missing.");
            return;
        }

        if (!IsValidDirectionValue(directionValue))
        {
            Debug.LogWarning("Invalid direction value received.");
            return;
        }

        PlayerType player = GetCurrentDirectionPlayer();
        MoveDirection direction = (MoveDirection)directionValue;
        ulong senderId = rpcParams.Receive.SenderClientId;

        if (!IsPlayerAllowed(senderId, player))
        {
            Debug.LogWarning("Illegal direction request.");
            return;
        }

        if (NetworkGameState.Instance != null &&
            !NetworkGameState.Instance.CanRequestDirection(player))
        {
            Debug.LogWarning($"HOST rejected direction because the authoritative state is not accepting {player}.");
            return;
        }

        if (NetworkGameState.Instance == null &&
            !turnManager.CanChooseDirection(player))
        {
            Debug.Log("Direction rejected.");
            return;
        }

        if (!IsDirectionValid(player, direction))
        {
            Debug.LogWarning(
                $"HOST rejected invalid direction: {player} → {direction}");
            return;
        }

        PlayerAction action = turnManager.GetAction(player);
        action.direction = direction;
        action.hasChosenDirection = true;

        turnManager.DirectionChosen(player);
        if (player == PlayerType.X && boardManager.CurrentBoard.IsFull())
            turnManager.SetState(GameState.RevealingDirections);

        NetworkGameState state = NetworkGameState.Instance;
        if (state != null)
        {
            // Commit the accepted direction and phase as one authoritative
            // snapshot so the selecting client and remote client consume the
            // exact same direction state.
            state.CommitAuthoritativePhase(
                turnManager.CurrentState,
                turnManager.PlayerXAction,
                turnManager.PlayerYAction);
        }

        Debug.Log($"HOST stored direction: {player} → {direction}");

        if (turnManager.CurrentState == GameState.RevealingDirections)
        {
            if (state != null)
                state.CommitAuthoritativePhase(
                    GameState.RevealingDirections,
                    turnManager.PlayerXAction,
                    turnManager.PlayerYAction);

            if (networkMovementManager != null)
                networkMovementManager.RequestMovementResolution();
            else
                Debug.LogError("NetworkMovementManager is missing.");
        }
    }

    private bool IsValidDirectionValue(int directionValue)
    {
        return directionValue == (int)MoveDirection.Up ||
               directionValue == (int)MoveDirection.Down ||
               directionValue == (int)MoveDirection.Left ||
               directionValue == (int)MoveDirection.Right ||
               directionValue == (int)MoveDirection.Stay;
    }


    private bool IsDirectionValid(PlayerType player, MoveDirection direction)
    {
        PlayerAction action = turnManager.GetAction(player);
        PlayerAction other = turnManager.GetAction(player == PlayerType.X ? PlayerType.Y : PlayerType.X);
        return action.hasPlaced && SecretFourRules.CanChooseDirection(boardManager.CurrentBoard, action.position,
            direction, other.hasPlaced ? other.position : (Vector2Int?)null);
    }





    private PlayerType GetCurrentDirectionPlayer()
    {
        if (NetworkGameState.Instance != null)
        {
            return NetworkGameState.Instance.IsPhase(
                GameState.WaitingForPlayerXDirection)
                ? PlayerType.X
                : PlayerType.Y;
        }

        return turnManager.CurrentState == GameState.WaitingForPlayerXDirection
            ? PlayerType.X
            : PlayerType.Y;
    }

    private bool IsPlayerAllowed(ulong senderId, PlayerType player) =>
        NetworkGameState.Instance != null && NetworkGameState.Instance.IsPlayerAllowed(senderId, player);
}