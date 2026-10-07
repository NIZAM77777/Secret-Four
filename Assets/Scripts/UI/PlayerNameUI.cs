using UnityEngine;
using TMPro;

public class PlayerNameUI : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField playerNameInput;

    private void Start()
    {
        playerNameInput.characterLimit = 12;
        LoadName();
    }

    public void SaveName()
    {
        PlayerNameManager.SavePlayerName(
            playerNameInput.text
        );

        playerNameInput.text =
            PlayerNameManager.GetPlayerName();
    }

    private void LoadName()
    {
        playerNameInput.text =
            PlayerNameManager.GetPlayerName();
    }
}