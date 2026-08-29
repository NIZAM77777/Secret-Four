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
        }
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
                "The other player has left the game."
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
                "The host connection was lost."
            );
        }
    }
}