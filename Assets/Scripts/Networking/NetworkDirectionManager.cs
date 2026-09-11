using UnityEngine;
using Unity.Netcode;

public class NetworkDirectionManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private DirectionSelector directionSelector;
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
    private void RequestDirectionRpc(int directionValue, RpcParams rpcParams = default)
    {
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

        if (!turnManager.CanChooseDirection(player))
        {
            Debug.Log("Direction rejected.");
            return;
        }

        if (!IsDirectionValid(player, direction))
        {
            Debug.LogWarning($"HOST rejected invalid direction: {player} → {direction}");
            return;
        }

        PlayerAction action = turnManager.GetAction(player);
        action.direction = direction;
        action.hasChosenDirection = true;

        Debug.Log($"HOST secretly stored: {player} → {direction}");
        turnManager.DirectionChosen(player);

        if (turnManager.CurrentState == GameState.RevealingDirections)
        {
            RevealDirectionsRpc(
                (int)turnManager.PlayerXAction.direction,
                (int)turnManager.PlayerYAction.direction
            );

            if (networkMovementManager != null)
                networkMovementManager.RequestMovementResolution();
            else
                Debug.LogError("NetworkMovementManager is missing.");
        }
        else
        {
            DirectionAcceptedRpc((int)player);
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

    [Rpc(SendTo.ClientsAndHost)]
    private void DirectionAcceptedRpc(int playerValue)
    {
        PlayerType player = (PlayerType)playerValue;
        Debug.Log($"{player} direction selected.");
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RevealDirectionsRpc(int playerXDirection, int playerYDirection)
    {
        if (turnManager == null)
        {
            Debug.LogError("TurnManager is missing.");
            return;
        }

        PlayerAction xAction = turnManager.PlayerXAction;
        xAction.direction = (MoveDirection)playerXDirection;
        xAction.hasChosenDirection = true;

        PlayerAction yAction = turnManager.PlayerYAction;
        yAction.direction = (MoveDirection)playerYDirection;
        yAction.hasChosenDirection = true;

        Debug.Log("DIRECTIONS REVEALED!");
        Debug.Log($"X → {xAction.direction}");
        Debug.Log($"Y → {yAction.direction}");

        if (turnManager.CurrentState != GameState.RevealingDirections)
            turnManager.SetState(GameState.RevealingDirections);
    }

    private bool IsDirectionValid(PlayerType player, MoveDirection direction)
    {
        if (boardManager == null)
        {
            Debug.LogError("BoardManager is missing.");
            return false;
        }

        Board board = boardManager.CurrentBoard;
        if (board == null)
            return false;

        PlayerAction action = turnManager.GetAction(player);
        Vector2Int position = action.position;

        if (direction == MoveDirection.Stay)
            return !HasAnyValidMovement(board, position, player);

        Vector2Int destination = GetDestination(position, direction);
        return CanMoveTo(board, destination, player);
    }

    private bool HasAnyValidMovement(Board board, Vector2Int position, PlayerType player)
    {
        MoveDirection[] directions =
        {
            MoveDirection.Up,
            MoveDirection.Down,
            MoveDirection.Left,
            MoveDirection.Right
        };

        foreach (MoveDirection direction in directions)
        {
            Vector2Int destination = GetDestination(position, direction);
            if (CanMoveTo(board, destination, player))
                return true;
        }

        return false;
    }

    private bool CanMoveTo(Board board, Vector2Int destination, PlayerType player)
    {
        if (!board.IsInsideBoard(destination.x, destination.y))
            return false;

        if (board.IsEmpty(destination.x, destination.y))
            return true;

        PlayerType otherPlayer = player == PlayerType.X ? PlayerType.Y : PlayerType.X;
        PlayerAction otherAction = turnManager.GetAction(otherPlayer);

        if (otherAction.hasPlaced && destination == otherAction.position)
            return true;

        return false;
    }

    private Vector2Int GetDestination(Vector2Int position, MoveDirection direction)
    {
        switch (direction)
        {
            case MoveDirection.Up:
                return new Vector2Int(position.x, position.y + 1);
            case MoveDirection.Down:
                return new Vector2Int(position.x, position.y - 1);
            case MoveDirection.Left:
                return new Vector2Int(position.x - 1, position.y);
            case MoveDirection.Right:
                return new Vector2Int(position.x + 1, position.y);
            default:
                return position;
        }
    }

    private PlayerType GetCurrentDirectionPlayer()
    {
        if (turnManager.CurrentState == GameState.WaitingForPlayerXDirection)
            return PlayerType.X;

        return PlayerType.Y;
    }

    private bool IsPlayerAllowed(ulong senderId, PlayerType player)
    {
        if (player == PlayerType.X)
            return senderId == NetworkManager.ServerClientId;

        if (player == PlayerType.Y)
            return senderId != NetworkManager.ServerClientId;

        return false;
    }
}