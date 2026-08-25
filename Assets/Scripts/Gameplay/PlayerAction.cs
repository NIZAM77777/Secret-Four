using UnityEngine;

[System.Serializable]
public class PlayerAction
{
    public PlayerType playerType;

    public Vector2Int position;

    public MoveDirection direction;

    public bool hasPlaced;
    public bool hasChosenDirection;

    public PlayerAction(PlayerType playerType)
    {
        this.playerType = playerType;

        position = new Vector2Int(-1, -1);

        direction = MoveDirection.Up;

        hasPlaced = false;
        hasChosenDirection = false;
    }

    public void Reset()
    {
        position = new Vector2Int(-1, -1);

        direction = MoveDirection.Up;

        hasPlaced = false;
        hasChosenDirection = false;
    }
}