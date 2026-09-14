using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DisconnectUIManager : MonoBehaviour
{
    [Header("Disconnect Panel")]

    [SerializeField]
    private GameObject disconnectPanel;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text messageText;

    [Header("Scene")]

    [SerializeField]
    private string mainMenuSceneName = "Mainmenu";

    private bool returningToMainMenu;


    public void ShowDisconnect(
        string title,
        string message)
    {
        Debug.Log(
            $"DISCONNECT: {title} | {message}"
        );

        if (disconnectPanel != null)
            disconnectPanel.SetActive(true);

        if (titleText != null)
            titleText.text = title;

        if (messageText != null)
            messageText.text = message;
    }


    public void ReturnToMainMenu()
    {
        if (returningToMainMenu)
            return;

        returningToMainMenu = true;
        StartCoroutine(ReturnToMainMenuAfterShutdown());
    }


    private IEnumerator ReturnToMainMenuAfterShutdown()
    {
        Unity.Netcode.NetworkManager networkManager =
            Unity.Netcode.NetworkManager.Singleton;

        if (networkManager != null)
        {
            if (networkManager.IsListening ||
                networkManager.ShutdownInProgress)
            {
                networkManager.Shutdown(true);

                while (Unity.Netcode.NetworkManager.Singleton != null &&
                       (Unity.Netcode.NetworkManager.Singleton.IsListening ||
                        Unity.Netcode.NetworkManager.Singleton.ShutdownInProgress))
                {
                    yield return null;
                }
            }

            if (networkManager != null)
            {
                Destroy(networkManager.gameObject);
            }
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}