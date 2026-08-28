using UnityEngine;
using TMPro;

public class HumanSetupManager : MonoBehaviour
{
    [Header("Input Fields")]

    [SerializeField]
    private TMP_InputField player1Input;

    [SerializeField]
    private TMP_InputField player2Input;

    [Header("Main Menu Manager")]

    [SerializeField]
    private MainMenuManager mainMenuManager;


    public void ResetPlayerNames()
    {
        player1Input.text = "Player 1";

        player2Input.text = "Player 2";
    }


    // Called by START GAME button
    public void StartGame()
    {
        string player1Name =
            GetPlayer1Name();

        string player2Name =
            GetPlayer2Name();

        mainMenuManager.StartHumanGame(
            player1Name,
            player2Name
        );
    }


    // Called by BACK button
    public void BackToMenu()
    {
        mainMenuManager.BackToMainMenu();
    }


    private string GetPlayer1Name()
    {
        if (string.IsNullOrWhiteSpace(
            player1Input.text))
        {
            return "Player 1";
        }

        return player1Input.text.Trim();
    }


    private string GetPlayer2Name()
    {
        if (string.IsNullOrWhiteSpace(
            player2Input.text))
        {
            return "Player 2";
        }

        return player2Input.text.Trim();
    }
}