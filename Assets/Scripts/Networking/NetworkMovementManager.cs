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

        NetworkGameState state = NetworkGameState.Instance;
        activeMoveId = state != null ? state.BeginMove() : 0;

        if (state != null)
            state.CommitAuthoritativePhase(
                GameState.Moving,
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
        if (!IsServer)
            return;

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