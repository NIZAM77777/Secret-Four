using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class RelayManager : MonoBehaviour
{
    private const int MaxConnections = 1;
    private int operation;
    public string LastError { get; private set; }

    public async Task<string> CreateRelayRoom()
    {
        int request = ++operation;
        LastError = null;
        try
        {
            NetworkManager network = NetworkManager.Singleton;
            if (network == null || network.IsListening || network.ShutdownInProgress) throw new InvalidOperationException("A room is already active. Leave it before creating another.");
            await UGSManager.EnsureReady();
            if (this == null || request != operation) return null;
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            if (this == null || request != operation) return null;
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            if (this == null || request != operation || network == null) return null;
            network.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            if (!network.StartHost()) throw new InvalidOperationException("Could not start the room. Please try again.");
            return code;
        }
        catch (Exception exception)
        {
            if (this != null && request == operation) LastError = "Could not create a room. Check your connection and try again.";
            Debug.LogWarning("Relay create failed: " + exception.Message);
            return null;
        }
    }
    public async Task<bool> JoinRelayRoom(string joinCode)
    {
        int request = ++operation;
        LastError = null;
        NetworkManager network = NetworkManager.Singleton;
        try
        {
            if (network == null || network.IsListening || network.ShutdownInProgress) throw new InvalidOperationException("A connection is already active.");
            if (string.IsNullOrWhiteSpace(joinCode)) return false;
            await UGSManager.EnsureReady();
            if (this == null || request != operation) return false;
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim().ToUpperInvariant());
            if (this == null || request != operation || network == null) return false;
            network.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            if (!network.StartClient()) throw new InvalidOperationException("Could not start the connection.");
            float deadline = Time.realtimeSinceStartup + 15f;
            while (network != null && network.IsListening && !network.IsConnectedClient && Time.realtimeSinceStartup < deadline)
            {
                if (this == null || request != operation) return false;
                await Task.Delay(50);
            }
            if (network != null && network.IsConnectedClient) return true;
            throw new TimeoutException("The host did not respond.");
        }
        catch (Exception exception)
        {
            if (this != null && request == operation)
            {
                LastError = "Could not join. Check the code, room availability, and your connection.";
                if (network != null && network.IsListening) network.Shutdown(true);
            }
            Debug.LogWarning("Relay join failed: " + exception.Message);
            return false;
        }
    }
    public async Task CancelRelayRoom()
    {
        operation++;
        NetworkManager network = NetworkManager.Singleton;
        if (network == null) return;
        if (network.IsListening) network.Shutdown(true);
        while (network != null && (network.IsListening || network.ShutdownInProgress)) await Task.Yield();
    }
    private void OnDestroy() { operation++; }
}
