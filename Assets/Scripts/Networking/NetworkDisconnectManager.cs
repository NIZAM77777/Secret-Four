using UnityEngine;
using Unity.Netcode;

public class NetworkDisconnectManager : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private DisconnectUIManager disconnectUIManager;

    private bool intentionalLeaveRequested;
    private bool localDisconnectHandled;

    public override void OnNetworkSpawn()
    {
        NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.OnTransportFailure += OnLocalTransportFailure;

        Debug.Log("Network Disconnect Manager started.");
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager != null)
        {
            NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.OnTransportFailure -= OnLocalTransportFailure;
        }
    }

    /// <summary>
    /// Marks a deliberate leave before shutting down the local network.
    /// The server tells the other player that this was a normal leave,
    /// instead of treating it as an unknown connection failure.
    /// </summary>
    public void NotifyIntentionalLeave()
    {
        if (!IsSpawned || NetworkManager == null || !NetworkManager.IsListening)
            return;

        intentionalLeaveRequested = true;

        if (IsServer)
        {
            NotifyRemotePlayerOfLeave();
            return;
        }

        RequestIntentionalLeaveServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestIntentionalLeaveServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
            return;

        NotifyRemotePlayerOfLeave(rpcParams.Receive.SenderClientId);
    }

    private void NotifyRemotePlayerOfLeave(ulong leavingClientId = ulong.MaxValue)
    {
        if (!IsServer)
            return;

        if (leavingClientId == ulong.MaxValue)
            leavingClientId = NetworkManager.ServerClientId;

        ShowRemoteLeaveClientRpc(
            leavingClientId,
            new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = GetOtherClientIds(leavingClientId)
                }
            }
        );
    }

    private ulong[] GetOtherClientIds(ulong leavingClientId)
    {
        var clients = NetworkManager.ConnectedClientsIds;

        if (clients == null || clients.Count == 0)
            return new ulong[0];

        var result = new System.Collections.Generic.List<ulong>();

        foreach (ulong clientId in clients)
        {
            if (clientId != leavingClientId)
                result.Add(clientId);
        }

        return result.ToArray();
    }

    [ClientRpc]
    private void ShowRemoteLeaveClientRpc(
        ulong leavingClientId,
        ClientRpcParams clientRpcParams = default)
    {
        if (disconnectUIManager == null || localDisconnectHandled)
            return;

        localDisconnectHandled = true;

        if (leavingClientId == NetworkManager.ServerClientId)
        {
            disconnectUIManager.ShowDisconnect(
                "The host left the game."
            );
        }
        else
        {
            disconnectUIManager.ShowDisconnect(
                "The other player left the game."
            );
        }
    }

    private void OnApplicationQuit()
    {
        // A normal application close is treated as an intentional leave when
        // Unity gives us a chance to notify the remote player. Force-closing
        // the app or killing it from the recent-apps screen may not invoke
        // this callback; those cases correctly fall back to disconnect handling.
        if (NetworkManager != null &&
            NetworkManager.IsListening &&
            !intentionalLeaveRequested)
        {
            NotifyIntentionalLeave();
        }
    }

    private void OnLocalTransportFailure()
    {
        if (disconnectUIManager == null ||
            NetworkManager == null ||
            !NetworkManager.IsListening ||
            intentionalLeaveRequested ||
            localDisconnectHandled)
            return;

        localDisconnectHandled = true;

        disconnectUIManager.ShowDisconnect(
            "Your network connection was lost. Please check your internet connection."
        );
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"CLIENT DISCONNECTED: {clientId}");

        if (disconnectUIManager == null ||
            NetworkManager == null ||
            intentionalLeaveRequested ||
            localDisconnectHandled)
            return;

        // The local client lost its own connection. This covers cases where
        // NGO reports the disconnect without raising OnTransportFailure.
        if (clientId == NetworkManager.LocalClientId)
        {
            localDisconnectHandled = true;

            disconnectUIManager.ShowDisconnect(
                "Your network connection was lost. Please check your internet connection."
            );

            return;
        }

        // Host sees a remote client disappear.
        if (IsServer && clientId != NetworkManager.ServerClientId)
        {
            localDisconnectHandled = true;

            disconnectUIManager.ShowDisconnect(
                "The other player has disconnected from the game."
            );

            return;
        }

        // Client sees the host disappear.
        if (IsClient && clientId == NetworkManager.ServerClientId)
        {
            localDisconnectHandled = true;

            disconnectUIManager.ShowDisconnect(
                "The host has disconnected from the game."
            );
        }
    }
}
