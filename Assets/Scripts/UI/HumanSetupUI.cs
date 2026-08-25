using UnityEngine;
using TMPro;

public class HumanSetupUI : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField player1Input;

    [SerializeField]
    private TMP_InputField player2Input;

    [SerializeField]
    private MainMenuManager mainMenuManager;

    private void OnEnable()
    {
        player1Input.text =
            PlayerNameManager.GetPlayerName();

        player2Input.text =
            "Player 2";
    }

    public void StartGame()
    {
        string player1 =
            player1Input.text;

        string player2 =
            player2Input.text;

        mainMenuManager.StartHumanGame(
            player1,
            player2
        );
    }
}