using UnityEngine;
using Unity.Netcode;

public class NetworkDisconnectManager
    : NetworkBehaviour
{
    [Header("References")]

    [SerializeField]
    private DisconnectUIManager disconnectUIManager;


    // =====================================================
    // NETWORK START
    // =====================================================

    public override void OnNetworkSpawn()
    {
        NetworkManager.OnClientDisconnectCallback +=
            OnClientDisconnected;

        // Handles a local transport/network failure on this device. This is
        // different from the remote disconnect callback: the player whose
        // connection actually failed should see a local "connection lost"
        // message, while the remaining player sees the opponent message.
        NetworkManager.OnTransportFailure +=
            OnLocalTransportFailure;


        Debug.Log(
            "Network Disconnect Manager started."
        );
    }


    // =====================================================
    // NETWORK STOP
    // =====================================================

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -=
                OnClientDisconnected;

            NetworkManager.OnTransportFailure -=
                OnLocalTransportFailure;
        }
    }


    // =====================================================
    // LOCAL TRANSPORT FAILURE
    // =====================================================

    private void OnLocalTransportFailure()
    {
        if (disconnectUIManager == null)
            return;

        disconnectUIManager.ShowDisconnect(
            "CONNECTION LOST",
            "Your network connection was lost."
        );
    }


    // =====================================================
    // CLIENT DISCONNECTED
    // =====================================================

    private void OnClientDisconnected(
        ulong clientId)
    {
        Debug.Log(
            $"CLIENT DISCONNECTED: {clientId}"
        );


        if (disconnectUIManager == null)
        {
            Debug.LogError(
                "DisconnectUIManager missing."
            );

            return;
        }


        // =============================================
        // HOST DETECTS CLIENT LEAVING
        // =============================================

        if (IsServer &&
            clientId != NetworkManager.ServerClientId)
        {
            disconnectUIManager.ShowDisconnect(
                "PLAYER DISCONNECTED",
                "The other player has disconnected."
            );

            return;
        }


        // =============================================
        // CLIENT DETECTS HOST DISCONNECTING
        // =============================================

        if (IsClient &&
            clientId == NetworkManager.ServerClientId)
        {
            disconnectUIManager.ShowDisconnect(
                "HOST DISCONNECTED",
                "The host has disconnected from the game."
            );
        }
    }
}