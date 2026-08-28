using UnityEngine;
using UnityEngine.UI;

public class DirectionButtonController : MonoBehaviour
{
    [Header("Direction Buttons")]

    [SerializeField]
    private Button upButton;

    [SerializeField]
    private Button downButton;

    [SerializeField]
    private Button leftButton;

    [SerializeField]
    private Button rightButton;

    [SerializeField]
    private Button stayButton;

    [Header("References")]

    [SerializeField]
    private TurnManager turnManager;

    [SerializeField]
    private BoardManager boardManager;


    public void UpdateDirectionButtons(
        PlayerType player)
    {
        PlayerAction action =
            turnManager.GetAction(player);

        Vector2Int position =
            action.position;

        Board board =
            boardManager.CurrentBoard;

        // Check each direction.

        bool canMoveUp =
            board.IsInsideBoard(
                position.x,
                position.y + 1
            );

        bool canMoveDown =
            board.IsInsideBoard(
                position.x,
                position.y - 1
            );

        bool canMoveLeft =
            board.IsInsideBoard(
                position.x - 1,
                position.y
            );

        bool canMoveRight =
            board.IsInsideBoard(
                position.x + 1,
                position.y
            );

        upButton.interactable =
            canMoveUp;

        downButton.interactable =
            canMoveDown;

        leftButton.interactable =
            canMoveLeft;

        rightButton.interactable =
            canMoveRight;

        // Stay is always available.
        stayButton.interactable = true;
    }


    public void ResetButtons()
    {
        upButton.interactable = true;
        downButton.interactable = true;
        leftButton.interactable = true;
        rightButton.interactable = true;
        stayButton.interactable = true;
    }
}