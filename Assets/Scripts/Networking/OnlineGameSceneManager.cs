using UnityEngine;
using Unity.Netcode;


public class OnlineGameSceneManager : MonoBehaviour
{
    private void Start()
    {
        CheckOnlineMode();
    }


    private void CheckOnlineMode()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.Log(
                "NetworkManager not found. Normal local game."
            );

            return;
        }


        if (!NetworkManager.Singleton.IsListening)
        {
            Debug.Log(
                "Network is not running. Normal local game."
            );

            return;
        }


        Debug.Log(
            "ONLINE GAME MODE DETECTED!"
        );


        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log(
                "LOCAL PLAYER: PLAYER X (HOST)"
            );
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log(
                "LOCAL PLAYER: PLAYER Y (CLIENT)"
            );
        }
    }
}