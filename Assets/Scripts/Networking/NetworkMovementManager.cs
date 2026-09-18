using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkMovementManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private MovementRuleResolver movementRuleResolver;
    [SerializeField] private PlacementManager placementManager;

    private bool movementResolved;
    private int activeMoveId = -1;
    private readonly HashSet<ulong> presentationCompleteClients = new();

    public bool IsMoveActive => movementResolved;

    public void RequestMovementResolution()
    {
        if (!IsSpawned)
        {
            Debug.LogError("NetworkMovementManager is not spawned.");
            return;
        }

        if (!IsServer)
            return;

        ResolveMovementOnHost();
    }

    private void ResolveMovementOnHost()
    {
        if (!IsServer || movementResolved)
            return;

        if (turnManager == null || movementRuleResolver == null)
        {
            Debug.LogError("Movement references missing.");
            return;
        }

        if (!turnManager.PlayerXAction.hasChosenDirection ||
            !turnManager.PlayerYAction.hasChosenDirection)
        {
            Debug.LogWarning("Both directions are not ready.");
            return;
        }

        movementResolved = true;
        presentationCompleteClients.Clear();

        NetworkGameState state = NetworkGameState.Instance;
        activeMoveId = state != null ? state.BeginMove() : 0;

        MovementResolution resolution =
            movementRuleResolver.Resolve(
                turnManager.PlayerXAction,
                turnManager.PlayerYAction);

        // The resolution is calculated exactly once by the server.
        // Clients only receive this immutable result for presentation.
        SendResolutionClientRpc(
            activeMoveId,
            GetMovementResultData(resolution.playerXResult),
            GetMovementResultData(resolution.playerYResult),
            (int)resolution.playerXCollision,
            (int)resolution.playerYCollision);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendResolutionClientRpc(
        int moveId,
        MovementResultData xData,
        MovementResultData yData,
        int xCollision,
        int yCollision)
    {
        if (moveId != activeMoveId && activeMoveId != -1)
        {
            Debug.Log($"Ignoring stale movement result {moveId}; active move is {activeMoveId}.");
            return;
        }

        activeMoveId = moveId;

        MovementResult xResult = CreateMovementResult(xData);
        MovementResult yResult = CreateMovementResult(yData);

        MovementResolution resolution =
            new MovementResolution(xResult, yResult)
            {
                playerXCollision = (MovementCollisionType)xCollision,
                playerYCollision = (MovementCollisionType)yCollision
            };

        Debug.Log($"NETWORK MOVEMENT RESULT RECEIVED | MoveId = {moveId}");

        if (movementManager == null || placementManager == null)
        {
            Debug.LogError("Movement references missing.");
            return;
        }

        movementManager.ApplyNetworkResolution(
            resolution,
            placementManager.PlayerXCoin,
            placementManager.PlayerYCoin,
            moveId);
    }

    public void ReportPresentationComplete(int moveId)
    {
        if (!IsSpawned || moveId != activeMoveId)
            return;

        if (IsServer)
        {
            MarkPresentationComplete(NetworkManager.ServerClientId, moveId);
            return;
        }

        ReportPresentationCompleteRpc(moveId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ReportPresentationCompleteRpc(
        int moveId,
        RpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        MarkPresentationComplete(
            rpcParams.Receive.SenderClientId,
            moveId);
    }

    private void MarkPresentationComplete(ulong clientId, int moveId)
    {
        if (!IsServer || moveId != activeMoveId || !movementResolved)
            return;

        presentationCompleteClients.Add(clientId);

        int expectedClients = NetworkManager.ConnectedClientsIds.Count;
        if (expectedClients <= 0 || presentationCompleteClients.Count < expectedClients)
            return;

        FinishAuthoritativeMove(moveId);
    }

    private void FinishAuthoritativeMove(int moveId)
    {
        if (!IsServer || moveId != activeMoveId)
            return;

        movementResolved = false;
        presentationCompleteClients.Clear();

        NetworkGameState state = NetworkGameState.Instance;

        if (state != null)
            state.CaptureFromServer();

        GameResultManager resultManager =
            FindFirstObjectByType<GameResultManager>();

        if (resultManager != null && resultManager.HasResult)
        {
            if (state != null)
            {
                if (resultManager.IsDraw)
                    state.SetDraw();
                else
                    state.SetWinner(resultManager.Winner);
            }

            activeMoveId = -1;
            return;
        }

        if (turnManager != null)
            turnManager.StartNextRound();

        if (state != null)
            state.CaptureFromServer();

        activeMoveId = -1;
    }

    public void ResetNetworkMoveState()
    {
        if (!IsServer)
            return;

        movementResolved = false;
        activeMoveId = -1;
        presentationCompleteClients.Clear();
    }

    private MovementResultData GetMovementResultData(MovementResult result)
    {
        return new MovementResultData
        {
            playerType = (int)result.playerType,
            startX = result.startPosition.x,
            startY = result.startPosition.y,
            destinationX = result.destination.x,
            destinationY = result.destination.y,
            direction = (int)result.direction
        };
    }

    private MovementResult CreateMovementResult(MovementResultData data)
    {
        return new MovementResult(
            (PlayerType)data.playerType,
            new Vector2Int(data.startX, data.startY),
            new Vector2Int(data.destinationX, data.destinationY),
            (MoveDirection)data.direction);
    }
}