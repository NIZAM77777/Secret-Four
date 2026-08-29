using System;
using System.Threading.Tasks;

using UnityEngine;

using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
//using Unity.Services.Relay.Utilities;


public class RelayManager : MonoBehaviour
{
    [Header("Relay Settings")]

    [SerializeField]
    private int maxConnections = 1;


    // =========================================
    // CREATE ROOM
    // =========================================

    public async Task<string> CreateRelayRoom()
    {
        try
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning(
                    "Network is already running."
                );

                return null;
            }


            if (UnityServices.State !=
                ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }


            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }


            Debug.Log(
                "Creating Relay room..."
            );


            Allocation allocation =
                await RelayService.Instance
                    .CreateAllocationAsync(
                        maxConnections
                    );


            UnityTransport transport =
                NetworkManager.Singleton
                    .GetComponent<UnityTransport>();


            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(
                    allocation,
                    "dtls"
                )
            );


            string joinCode =
                await RelayService.Instance
                    .GetJoinCodeAsync(
                        allocation.AllocationId
                    );


            bool success =
                NetworkManager.Singleton
                    .StartHost();


            if (!success)
            {
                Debug.LogError(
                    "Failed to start Host."
                );

                return null;
            }


            Debug.Log(
                "Relay Room Created!"
            );

            Debug.Log(
                "ROOM CODE: " + joinCode
            );


            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Relay Create Error: " +
                e.Message
            );

            return null;
        }
    }


    // =========================================
    // JOIN ROOM
    // =========================================

    public async Task<bool> JoinRelayRoom(
        string joinCode)
    {
        try
        {
            if (NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning(
                    "Network is already running."
                );

                return false;
            }


            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogError(
                    "Room code is empty."
                );

                return false;
            }


            // Clean the room code
            joinCode = joinCode
                .Trim()
                .ToUpper();


            if (UnityServices.State !=
                ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }


            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance
                    .SignInAnonymouslyAsync();
            }


            Debug.Log(
                "Joining Relay Room: " +
                joinCode
            );


            JoinAllocation joinAllocation =
                await RelayService.Instance
                    .JoinAllocationAsync(
                        joinCode
                    );


            UnityTransport transport =
                NetworkManager.Singleton
                    .GetComponent<UnityTransport>();


            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(
                    joinAllocation,
                    "dtls"
                )
            );


            bool success =
                NetworkManager.Singleton
                    .StartClient();


            if (!success)
            {
                Debug.LogError(
                    "Failed to start Client."
                );

                return false;
            }


            Debug.Log(
                "CLIENT STARTED SUCCESSFULLY!"
            );

            Debug.Log(
                "Joined Room: " +
                joinCode
            );


            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Relay Join Error: " +
                e.Message
            );

            return false;
        }
    }
}