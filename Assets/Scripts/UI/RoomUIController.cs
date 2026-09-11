using UnityEngine;
using TMPro;
using UnityEngine.UI;


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

    [SerializeField]
    private Button joinRoomButton;

    [SerializeField]
    private TMP_Text joinRoomMessage;


    [Header("Managers")]

    [SerializeField]
    private RelayManager relayManager;

    [SerializeField]
    private GameObject creatingRoomPanel;

    private bool isCreatingRoom;


    private void Awake()
    {
        if (roomCodeInput != null)
        {
            roomCodeInput.onValueChanged.AddListener(OnRoomCodeChanged);
        }

        ClearJoinRoomMessage();
    }


    private void OnDestroy()
    {
        if (roomCodeInput != null)
        {
            roomCodeInput.onValueChanged.RemoveListener(OnRoomCodeChanged);
        }
    }


    // =========================================
    // CREATE ROOM
    // =========================================

    public async void CreateRoom()
    {
        // Prevent multiple button presses
        if (isCreatingRoom)
        {
            return;
        }

        isCreatingRoom = true;


        // Show Creating Room panel
        if (creatingRoomPanel != null)
        {
            creatingRoomPanel.SetActive(true);
        }


        string joinCode =
            await relayManager.CreateRelayRoom();


        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError(
                "Failed to create room."
            );


            // Hide loading panel
            if (creatingRoomPanel != null)
            {
                creatingRoomPanel.SetActive(false);
            }


            // Allow trying again
            isCreatingRoom = false;

            return;
        }


        // Hide Creating Room panel
        if (creatingRoomPanel != null)
        {
            creatingRoomPanel.SetActive(false);
        }


        playInRoomMenuPanel.SetActive(false);

        createRoomPanel.SetActive(true);


        roomCodeText.text = joinCode;


        isCreatingRoom = false;
    }


    // =========================================
    // OPEN JOIN ROOM PANEL
    // =========================================

    public void OpenJoinRoom()
    {
        playInRoomMenuPanel.SetActive(false);

        joinRoomPanel.SetActive(true);


        // Clear old room code and previous error message
        roomCodeInput.text = "";
        ClearJoinRoomMessage();

        UpdateJoinButtonState();
    }


    // =========================================
    // JOIN ROOM CODE VALIDATION
    // =========================================

    private void OnRoomCodeChanged(string value)
    {
        // A new code means the previous error is no longer relevant.
        ClearJoinRoomMessage();
        UpdateJoinButtonState();
    }


    private void UpdateJoinButtonState()
    {
        if (joinRoomButton == null)
        {
            return;
        }

        // Enable Join only after a non-empty Relay code has been entered.
        bool hasRoomCode =
            roomCodeInput != null &&
            !string.IsNullOrWhiteSpace(roomCodeInput.text);

        joinRoomButton.interactable = hasRoomCode;
    }


    private void ShowJoinRoomMessage(string message)
    {
        if (joinRoomMessage == null)
        {
            return;
        }

        joinRoomMessage.text = message;
        joinRoomMessage.gameObject.SetActive(true);
    }


    private void ClearJoinRoomMessage()
    {
        if (joinRoomMessage == null)
        {
            return;
        }

        joinRoomMessage.text = "";
        joinRoomMessage.gameObject.SetActive(false);
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
            UpdateJoinButtonState();

            Debug.LogWarning(
                "Please enter a Room Code."
            );

            return;
        }


        ClearJoinRoomMessage();

        if (joinRoomButton != null)
        {
            joinRoomButton.interactable = false;
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

            ShowJoinRoomMessage("Incorrect room code.");

            // Keep the button available so the player can correct the code.
            UpdateJoinButtonState();
            return;
        }


        Debug.Log(
            "Successfully joined the room!"
        );


        // Disable while connection/scene transition is being handled.
        if (joinRoomButton != null)
        {
            joinRoomButton.interactable = false;
        }


        // We will decide the next screen/game flow
        // in the next networking phase.
    }


    // =========================================
    // BACK FROM CREATE ROOM
    // =========================================

    public async void BackFromCreateRoom()
    {
        await relayManager.CancelRelayRoom();

        createRoomPanel.SetActive(false);

        playInRoomMenuPanel.SetActive(true);
    }


    // =========================================
    // BACK FROM JOIN ROOM
    // =========================================

    public void BackFromJoinRoom()
    {
        ClearJoinRoomMessage();

        joinRoomPanel.SetActive(false);

        playInRoomMenuPanel.SetActive(true);
    }
}