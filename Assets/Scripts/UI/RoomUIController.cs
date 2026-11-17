using SecretFour.Localization;
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
    [SerializeField] private GameObject roomShareHint;
    [SerializeField] private GameObject roomWaitingHint;


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
    private bool isJoiningRoom;
    private int requestVersion;
    private string createErrorKey, joinErrorKey;
    private void OnEnable() => GameLocalization.Changed += RefreshMessages;
    private void OnDisable() => GameLocalization.Changed -= RefreshMessages;
    private void RefreshMessages()
    {
        if (createErrorKey != null && roomCodeText != null) roomCodeText.text = GameLocalization.Text(createErrorKey);
        if (joinErrorKey != null && joinRoomMessage != null) joinRoomMessage.text = GameLocalization.Text(joinErrorKey);
    }


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
        if (isCreatingRoom || isJoiningRoom)
        {
            return;
        }

        isCreatingRoom = true;
        int request = ++requestVersion;


        // Show Creating Room panel
        if (creatingRoomPanel != null)
        {
            creatingRoomPanel.SetActive(true);
        }


        string joinCode = await relayManager.CreateRelayRoom();
        if (this == null || request != requestVersion) return;


        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogWarning("Failed to create room.");
            ShowRoomError(relayManager.LastError);


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


        createErrorKey = null;
        if (roomShareHint != null) roomShareHint.SetActive(true);
        if (roomWaitingHint != null) roomWaitingHint.SetActive(true);
        roomCodeText.text = joinCode;
        GameAudio.Play("notify");


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

    private void ShowRoomError(string message)
    {
        // Reuse the existing room-code panel so a failed create has a visible message and Back button.
        if (creatingRoomPanel != null) creatingRoomPanel.SetActive(false);
        playInRoomMenuPanel.SetActive(false);
        createRoomPanel.SetActive(true);
        if (roomShareHint != null) roomShareHint.SetActive(false);
        if (roomWaitingHint != null) roomWaitingHint.SetActive(false);
        createErrorKey = message ?? "room.createError";
        GameAudio.Play("error");
        RefreshMessages();
    }

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

        joinRoomButton.interactable = hasRoomCode && !isJoiningRoom;
    }


    private void ShowJoinRoomMessage(string message)
    {
        if (joinRoomMessage == null)
        {
            return;
        }

        joinErrorKey = message ?? "room.joinError";
        GameAudio.Play("error");
        RefreshMessages();
        joinRoomMessage.gameObject.SetActive(true);
    }


    private void ClearJoinRoomMessage()
    {
        if (joinRoomMessage == null)
        {
            return;
        }

        joinErrorKey = null;
        joinRoomMessage.text = "";
        joinRoomMessage.gameObject.SetActive(false);
    }


    // =========================================
    // JOIN ROOM
    // =========================================

    public async void JoinRoom()
    {
        if (isJoiningRoom || isCreatingRoom) return;
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


        isJoiningRoom = true;
        int request = ++requestVersion;


        bool success =
            await relayManager.JoinRelayRoom(
                joinCode
            );


        if (this == null || request != requestVersion) return;
        isJoiningRoom = false;
        if (!success)
        {
            Debug.LogError(
                "Failed to join room."
            );

            ShowJoinRoomMessage(relayManager.LastError ?? "room.joinError");

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
        requestVersion++;
        isCreatingRoom = false;
        await relayManager.CancelRelayRoom();
        if (this == null) return;

        createRoomPanel.SetActive(false);

        playInRoomMenuPanel.SetActive(true);
    }


    // =========================================
    // BACK FROM JOIN ROOM
    // =========================================

    public async void BackFromJoinRoom()
    {
        requestVersion++;
        isJoiningRoom = false;
        await relayManager.CancelRelayRoom();
        if (this == null) return;
        ClearJoinRoomMessage();

        joinRoomPanel.SetActive(false);

        playInRoomMenuPanel.SetActive(true);
    }
}