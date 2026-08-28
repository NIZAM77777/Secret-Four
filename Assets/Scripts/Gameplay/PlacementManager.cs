using UnityEngine;

public class PlacementManager : MonoBehaviour
{
    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private BoardView boardView;

    [SerializeField]
    private CoinConfig coinConfig;

    [SerializeField]
    private DirectionButtonController directionButtonController;

    [SerializeField]
    private GameObject coinPrefab;

    [SerializeField]
    private Transform coinParent;

    [SerializeField]
    private TurnManager turnManager;

    public GameObject PlayerXCoin { get; private set; }

    public GameObject PlayerYCoin { get; private set; }

    private void Start()
    {
        if (coinParent == null)
        {
            coinParent = transform;
        }
    }

    public void TryPlaceCoin(int x, int y)
    {
        Debug.Log(
    $"PLACE ATTEMPT ({x},{y}) | " +
    $"State = {turnManager.CurrentState}"
);
        PlayerType player =
            GetCurrentPlacementPlayer();

        if (!turnManager.CanPlaceCoin(player))
        {
            Debug.Log(
                "Coin cannot be placed right now."
            );

            return;
        }

        Board board =
            boardManager.CurrentBoard;

        if (!board.IsInsideBoard(x, y))
        {
            Debug.Log(
                "Position is outside the board."
            );

            return;
        }

        if (!board.IsEmpty(x, y))
        {
            Debug.Log(
                $"CELL REJECTED ({x},{y}) — " +
                $"already occupied."
            );

            return;
        }

        bool placed =
            board.TryPlace(
                x,
                y,
                player
            );

        if (!placed)
            return;

        CreateCoinVisual(
            x,
            y,
            player
        );

        PlayerAction action =
            turnManager.GetAction(player);

        action.position =
            new Vector2Int(x, y);

        action.hasPlaced = true;

        Debug.Log(
            $"{player} placed at ({x}, {y})"
        );

        turnManager.PlayerPlaced(player);

        directionButtonController.UpdateDirectionButtons(
            player
        );
    }

    private PlayerType GetCurrentPlacementPlayer()
    {
        if (turnManager.CurrentState ==
            GameState.WaitingForPlayerXPlacement)
        {
            return PlayerType.X;
        }

        return PlayerType.Y;
    }

    private void CreateCoinVisual(
        int x,
        int y,
        PlayerType playerType)
    {
        Vector3 position =
            boardView.GetCellWorldPosition(
                x,
                y
            );

        GameObject coin =
            Instantiate(
                coinPrefab,
                position,
                Quaternion.identity,
                coinParent
            );

        coin.name =
            $"{playerType}_Coin_{x}_{y}";

        CoinView coinView =
            coin.GetComponent<CoinView>();

        if (coinView != null)
        {
            Sprite sprite =
                coinConfig.GetSprite(playerType);

            coinView.Setup(
                playerType,
                sprite
            );
        }

        if (playerType == PlayerType.X)
        {
            PlayerXCoin = coin;
        }
        else
        {
            PlayerYCoin = coin;
        }
    }

    public void ClearAllCoins()
    {
        if (coinParent == null)
            return;

        for (int i = coinParent.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                coinParent.GetChild(i).gameObject
            );
        }

        PlayerXCoin = null;
        PlayerYCoin = null;
    }
}