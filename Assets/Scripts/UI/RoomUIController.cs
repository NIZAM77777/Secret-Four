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


        // Clear old room code
        roomCodeInput.text = "";

        UpdateJoinButtonState();
    }


    // =========================================
    // JOIN ROOM CODE VALIDATION
    // =========================================

    private void OnRoomCodeChanged(string value)
    {
        UpdateJoinButtonState();
    }


    private void UpdateJoinButtonState()
    {
        if (joinRoomButton == null)
        {
            return;
        }

        // Enable Join only after a valid-looking Relay code has been entered.
        // Relay join codes are non-empty and are normalized before joining.
        bool hasRoomCode =
            roomCodeInput != null &&
            !string.IsNullOrWhiteSpace(roomCodeInput.text);

        joinRoomButton.interactable = hasRoomCode;
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
        joinRoomPanel.SetActive(false);

        playInRoomMenuPanel.SetActive(true);
    }
}