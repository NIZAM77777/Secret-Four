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
    private string mainMenuSceneName = "MainMenu";


    // =============================================
    // SHOW DISCONNECT
    // =============================================

    public void ShowDisconnect(
        string title,
        string message)
    {
        Debug.Log(
            $"DISCONNECT: {title} | {message}"
        );

        if (disconnectPanel != null)
        {
            disconnectPanel.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (messageText != null)
        {
            messageText.text = message;
        }
    }


    // =============================================
    // RETURN TO MAIN MENU
    // =============================================

    public void ReturnToMainMenu()
    {
        if (NetworkManagerExists())
        {
            Unity.Netcode.NetworkManager.Singleton
                .Shutdown();
        }

        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }


    private bool NetworkManagerExists()
    {
        return Unity.Netcode.NetworkManager.Singleton != null;
    }
}