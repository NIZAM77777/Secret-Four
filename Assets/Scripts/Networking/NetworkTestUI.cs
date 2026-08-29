using UnityEngine;
using Unity.Netcode;

public class NetworkTestUI : MonoBehaviour
{
    public void StartHost()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Network is already running!");
            return;
        }

        bool success = NetworkManager.Singleton.StartHost();

        if (success)
        {
            Debug.Log("HOST STARTED SUCCESSFULLY");
        }
        else
        {
            Debug.LogError("FAILED TO START HOST");
        }
    }

    public void StartClient()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Network is already running!");
            return;
        }

        bool success = NetworkManager.Singleton.StartClient();

        if (success)
        {
            Debug.Log("CLIENT STARTED SUCCESSFULLY");
        }
        else
        {
            Debug.LogError("FAILED TO START CLIENT");
        }
    }
}