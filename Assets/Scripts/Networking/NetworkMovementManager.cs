using UnityEngine;
using Unity.Netcode;

public class NetworkMovementManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private MovementManager movementManager;
    [SerializeField] private MovementRuleResolver movementRuleResolver;
    [SerializeField] private PlacementManager placementManager;

    [Header("Reveal Timing")]
    [SerializeField] private float revealDelay = 2f;

    private bool movementResolved;
    private int activeMoveId = -1;
    private int lastReceivedMatchId = -1;
    private int lastReceivedMoveId = -1;

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
            (turnManager.PlayerYAction.hasPlaced && !turnManager.PlayerYAction.hasChosenDirection))
        {
            Debug.LogWarning("Both directions are not ready.");
            return;
        }

        movementResolved = true;

        NetworkGameState state = NetworkGameState.Instance;
        activeMoveId = state != null ? state.BeginMove() : 0;

        if (state != null)
            state.CommitAuthoritativePhase(
                GameState.RevealingDirections,
                turnManager.PlayerXAction,
                turnManager.PlayerYAction);

        MovementResolution resolution =
            movementRuleResolver.Resolve(
                turnManager.PlayerXAction,
                turnManager.PlayerYAction);

        // Keep RevealingDirections authoritative for the full reveal window.
        // Previously the server changed the network phase to Moving immediately,
        // so clients could skip the reveal state and display inconsistent
        // direction/movement presentation.
        StartCoroutine(CommitMovingPhaseAfterReveal());

        // The resolution is calculated exactly once by the server.
        // Clients only receive this immutable result for presentation.
        SendResolutionClientRpc(
            state != null ? state.MatchId.Value : 0,
            activeMoveId,
            GetMovementResultData(resolution.playerXResult),
            GetMovementResultData(resolution.playerYResult),
            (int)resolution.playerXCollision,
            (int)resolution.playerYCollision);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SendResolutionClientRpc(
        int matchId, int moveId,
        MovementResultData xData,
        MovementResultData yData,
        int xCollision,
        int yCollision)
    {
        if (NetworkGameState.Instance != null && matchId < NetworkGameState.Instance.MatchId.Value) return;
        if (matchId < lastReceivedMatchId || (matchId == lastReceivedMatchId && moveId <= lastReceivedMoveId)) return;
        lastReceivedMatchId = matchId;
        lastReceivedMoveId = moveId;
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

        if (!IsServer)
        {
            placementManager.EnsureNetworkCoin(xResult.startPosition, PlayerType.X);
            if (yResult.startPosition.x >= 0) placementManager.EnsureNetworkCoin(yResult.startPosition, PlayerType.Y);
            turnManager.PlayerXAction.direction = xResult.direction;
            turnManager.PlayerYAction.direction = yResult.direction;
        }

        movementManager.ApplyNetworkResolution(
            resolution,
            placementManager.PlayerXCoin,
            placementManager.PlayerYCoin,
            moveId);
    }

    private System.Collections.IEnumerator CommitMovingPhaseAfterReveal()
    {
        yield return new WaitForSeconds(revealDelay);

        if (!IsServer || !movementResolved || NetworkGameState.Instance == null)
            yield break;

        NetworkGameState.Instance.CommitAuthoritativePhase(
            GameState.Moving,
            turnManager.PlayerXAction,
            turnManager.PlayerYAction);
    }

    public void CompleteAuthoritativeMove(int moveId)
    {
        if (!IsServer || moveId != activeMoveId)
            return;

        movementResolved = false;
        activeMoveId = -1;
    }

    public void ResetNetworkMoveState()
    {
        StopAllCoroutines();
        movementResolved = false;
        activeMoveId = -1;
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