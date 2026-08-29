using UnityEngine;

public class RoomUIController : MonoBehaviour
{
    [Header("Main Panels")]
    public GameObject playInRoomMenuPanel;
    public GameObject createRoomPanel;
    public GameObject joinRoomPanel;

    // CREATE ROOM button
    public void OpenCreateRoom()
    {
        playInRoomMenuPanel.SetActive(false);
        createRoomPanel.SetActive(true);
    }

    // JOIN ROOM button
    public void OpenJoinRoom()
    {
        playInRoomMenuPanel.SetActive(false);
        joinRoomPanel.SetActive(true);
    }

    // Back from Create Room
    public void BackFromCreateRoom()
    {
        createRoomPanel.SetActive(false);
        playInRoomMenuPanel.SetActive(true);
    }

    // Back from Join Room
    public void BackFromJoinRoom()
    {
        joinRoomPanel.SetActive(false);
        playInRoomMenuPanel.SetActive(true);
    }
}