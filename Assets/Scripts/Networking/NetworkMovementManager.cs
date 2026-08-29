using UnityEngine;
using Unity.Netcode;

public class NetworkMovementManager : NetworkBehaviour
{
    [Header("References")]

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private MovementManager movementManager;

    [SerializeField]
    private MovementRuleResolver movementRuleResolver;

    [SerializeField]
    private PlacementManager placementManager;


    private bool movementResolved;


    // =====================================================
    // REQUEST MOVEMENT RESOLUTION
    // =====================================================

    public void RequestMovementResolution()
    {
        if (!IsSpawned)
        {
            Debug.LogError(
                "NetworkMovementManager is not spawned."
            );

            return;
        }


        // Only the Host should start resolution.

        if (!IsServer)
        {
            Debug.Log(
                "Only Host can resolve movement."
            );

            return;
        }


        ResolveMovementOnHost();
    }


    // =====================================================
    // HOST RESOLUTION
    // =====================================================

    private void ResolveMovementOnHost()
    {
        if (!IsServer)
        {
            return;
        }


        if (movementResolved)
        {
            Debug.Log(
                "Movement already resolved."
            );

            return;
        }


        if (turnManager == null ||
            movementRuleResolver == null)
        {
            Debug.LogError(
                "Movement references missing."
            );

            return;
        }


        // =============================================
        // MAKE SURE BOTH PLAYERS ARE READY
        // =============================================

        if (!turnManager.PlayerXAction.hasChosenDirection ||
            !turnManager.PlayerYAction.hasChosenDirection)
        {
            Debug.LogWarning(
                "Cannot resolve movement. " +
                "Both directions are not ready."
            );

            return;
        }


        movementResolved = true;


        Debug.Log(
            "HOST RESOLVING MOVEMENT..."
        );


        // =============================================
        // HOST CALCULATES THE RESULT ⭐
        // =============================================

        MovementResolution resolution =
            movementRuleResolver.Resolve(
                turnManager.PlayerXAction,
                turnManager.PlayerYAction
            );


        // =============================================
        // SEND FINAL RESULT TO EVERYONE
        // =============================================

        SendResolutionClientRpc(
            GetMovementResultData(
                resolution.playerXResult
            ),

            GetMovementResultData(
                resolution.playerYResult
            ),

            (int)resolution.playerXCollision,

            (int)resolution.playerYCollision
        );
    }


    // =====================================================
    // RECEIVE FINAL RESULT
    // =====================================================

    [ClientRpc]
    private void SendResolutionClientRpc(
        MovementResultData xData,
        MovementResultData yData,
        int xCollision,
        int yCollision)
    {
        MovementResult xResult =
            CreateMovementResult(xData);

        MovementResult yResult =
            CreateMovementResult(yData);


        MovementResolution resolution =
            new MovementResolution(
                xResult,
                yResult
            );


        resolution.playerXCollision =
            (MovementCollisionType)xCollision;

        resolution.playerYCollision =
            (MovementCollisionType)yCollision;


        Debug.Log(
            "NETWORK MOVEMENT RESULT RECEIVED"
        );


        if (movementManager == null ||
            placementManager == null)
        {
            Debug.LogError(
                "Movement references missing."
            );

            return;
        }


        movementManager.ApplyNetworkResolution(
            resolution,
            placementManager.PlayerXCoin,
            placementManager.PlayerYCoin
        );


        // Reset this after the current
        // resolution is sent.
        //
        // The next round will be able
        // to resolve again.

        movementResolved = false;
    }


    // =====================================================
    // RESULT → NETWORK DATA
    // =====================================================

    private MovementResultData GetMovementResultData(
        MovementResult result)
    {
        return new MovementResultData
        {
            playerType =
                (int)result.playerType,

            startX =
                result.startPosition.x,

            startY =
                result.startPosition.y,

            destinationX =
                result.destination.x,

            destinationY =
                result.destination.y,

            direction =
                (int)result.direction
        };
    }


    // =====================================================
    // NETWORK DATA → RESULT
    // =====================================================

    private MovementResult CreateMovementResult(
        MovementResultData data)
    {
        return new MovementResult(
            (PlayerType)data.playerType,

            new Vector2Int(
                data.startX,
                data.startY
            ),

            new Vector2Int(
                data.destinationX,
                data.destinationY
            ),

            (MoveDirection)data.direction
        );
    }
}