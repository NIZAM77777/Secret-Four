using UnityEngine;
using TMPro;


public class RoomUIController : MonoBehaviour
{
    [Header("Panels")]

    [SerializeField]
    private GameObject playInRoomMenuPanel;

    [SerializeField]
    private GameObject createRoomPanel;

    [SerializeField]
    private GameObject joinRoomPanel;


    [Header("Create Room UI")]

    [SerializeField]
    private TMP_Text roomCodeText;


    [Header("Join Room UI")]

    [SerializeField]
    private TMP_InputField roomCodeInput;


    [Header("Managers")]

    [SerializeField]
    private RelayManager relayManager;


    // =========================================
    // CREATE ROOM
    // =========================================

    public async void CreateRoom()
    {
        Debug.Log(
            "Creating Room..."
        );


        string joinCode =
            await relayManager.CreateRelayRoom();


        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError(
                "Failed to create room."
            );

            return;
        }


        playInRoomMenuPanel.SetActive(false);

        createRoomPanel.SetActive(true);


        roomCodeText.text = joinCode;


        Debug.Log(
            "Room Code Displayed: " +
            joinCode
        );
    }


    // =========================================
    // OPEN JOIN ROOM PANEL
    // =========================================

    public void OpenJoinRoom()
    {
        playInRoomMenuPanel.SetActive(false);

        joinRoomPanel.SetActive(true);


        // Clear old room code
        roomCodeInput.text = "";
    }


    // =========================================
    // JOIN ROOM
    // =========================================

    public async void JoinRoom()
    {
        string joinCode =
            roomCodeInput.text;


        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogWarning(
                "Please enter a Room Code."
            );

            return;
        }


        Debug.Log(
            "Attempting to join room..."
        );


        bool success =
            await relayManager.JoinRelayRoom(
                joinCode
            );


        if (!success)
        {
            Debug.LogError(
                "Failed to join room."
            );

            return;
        }


        Debug.Log(
            "Successfully joined the room!"
        );


        // We will decide the next screen/game flow
        // in the next networking phase.
    }


    // =========================================
    // BACK FROM CREATE ROOM
    // =========================================

    public void BackFromCreateRoom()
    {
        createRoomPanel.SetActive(false);

        playInRoomMenuPanel.SetActive(true);
    }


    // =========================================
    // BACK FROM JOIN ROOM
    // =========================================

    public void BackFromJoinRoom()
    {
        joinRoomPanel.SetActive(false);

        playInRoomMenuPanel.SetActive(true);
    }
}