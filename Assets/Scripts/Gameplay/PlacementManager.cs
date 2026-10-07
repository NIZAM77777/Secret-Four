using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

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

    [SerializeField]
    private NetworkCoinPlacementManager networkCoinPlacementManager;

    public GameObject PlayerXCoin { get; private set; }
    public GameObject PlayerYCoin { get; private set; }

    private readonly Dictionary<Vector2Int, GameObject> coinVisuals = new();

    private void Start()
    {
        if (coinParent == null)
            coinParent = transform;
    }

    public void TryPlaceCoin(int x, int y)
    {
        PlayerType player = IsOnlineGame() && NetworkGameState.Instance != null
            ? (NetworkGameState.Instance.IsLocalPlayerSide(PlayerType.X) ? PlayerType.X : PlayerType.Y)
            : GetCurrentPlacementPlayer();
        if (!turnManager.CanLocalHumanAct(player) || Time.timeScale == 0) return;

        if (IsOnlineGame())
        {
            if (networkCoinPlacementManager == null)
            {
                Debug.LogError("NetworkCoinPlacementManager is missing.");
                return;
            }

            // In online mode the server owns validation. Do not reject the
            // request using a potentially stale client-side board/state cache.
            if (!IsLocalPlayerAllowed(player))
            {
                Debug.Log("It is not your turn.");
                return;
            }

            if (NetworkGameState.Instance != null &&
                !NetworkGameState.Instance.CanRequestPlacement(player))
            {
                Debug.Log("The authoritative match is processing another state.");
                return;
            }

            networkCoinPlacementManager.RequestPlaceCoin(x, y, player);
            return;
        }

        ApplyCoinPlacement(x, y, player);
    }

    /// <summary>
    /// Applies an accepted placement. When a network skin ID is supplied, that exact
    /// player's selected skin is used instead of reading the local player's selection.
    /// </summary>
    public bool ApplyCoinPlacement(int x, int y, PlayerType player, string selectedSkinId = null)
    {
        Debug.Log($"PLACE ATTEMPT ({x},{y}) | Player = {player} | State = {turnManager.CurrentState}");

        bool online = IsOnlineGame();

        if (!online && !turnManager.CanPlaceCoin(player))
        {
            Debug.Log("Coin cannot be placed right now.");
            return false;
        }

        if (online &&
            NetworkGameState.Instance != null &&
            !NetworkGameState.Instance.CanRequestPlacement(player))
        {
            Debug.Log("Coin cannot be applied because the authoritative state rejected it.");
            return false;
        }

        Board board = boardManager.CurrentBoard;
        if (!board.IsInsideBoard(x, y) || !board.IsEmpty(x, y))
            return false;

        if (!board.TryPlace(x, y, player))
            return false;

        CreateCoinVisual(x, y, player, selectedSkinId);

        PlayerAction action = turnManager.GetAction(player);
        action.position = new Vector2Int(x, y);
        action.hasPlaced = true;

        Debug.Log($"{player} placed at ({x}, {y})");
        turnManager.PlayerPlaced(player);
        directionButtonController.RefreshForCurrentTurn();
        return true;
    }

    private void CreateCoinVisual(int x, int y, PlayerType playerType, string selectedSkinId = null)
    {
        Vector3 position = boardView.GetCellWorldPosition(x, y);
        GameObject coin = Instantiate(coinPrefab, position, Quaternion.identity, coinParent);
        coin.name = $"{playerType}_Coin_{x}_{y}";

        if (boardView != null)
            coin.transform.localScale *= boardView.BoardVisualScale;

        Vector2Int boardPosition = new(x, y);
        coinVisuals[boardPosition] = coin;

        CoinView coinView = coin.GetComponent<CoinView>();
        if (coinView != null)
        {
            Sprite sprite = null;

            if (IsOnlineGame())
            {
                // Online room rule: visual identity is determined only by the
                // authoritative network profile or the skin stored on this cell.
                // Never fall back to this device's Store selection.
                string authoritativeSkinId = selectedSkinId;

                if (string.IsNullOrWhiteSpace(authoritativeSkinId) &&
                    OnlineMatchPlayerState.Instance != null)
                {
                    authoritativeSkinId =
                        OnlineMatchPlayerState.Instance.GetSkinId(playerType);
                }

                if (!string.IsNullOrWhiteSpace(authoritativeSkinId))
                    sprite = SecretFourStoreIntegration.GetSpriteForItem(authoritativeSkinId);

                // coinConfig is only a final asset fallback when the authoritative
                // network profile has no usable skin; local Store state is forbidden.
                if (sprite == null && coinConfig != null)
                    sprite = coinConfig.GetSprite(playerType);
            }
            else
            {
                // Preserve existing offline/bot cosmetic behavior.
                if (!string.IsNullOrWhiteSpace(selectedSkinId))
                    sprite = SecretFourStoreIntegration.GetSpriteForItem(selectedSkinId);

                if (sprite == null)
                    sprite = SecretFourStoreIntegration.GetSelectedPlayerSprite(playerType);

                if (sprite == null && coinConfig != null)
                    sprite = coinConfig.GetSprite(playerType);
            }

            coinView.Setup(playerType, sprite);
        }

        if (playerType == PlayerType.X)
            PlayerXCoin = coin;
        else
            PlayerYCoin = coin;
    }

    public GameObject GetCoinAtPosition(Vector2Int position)
    {
        return coinVisuals.TryGetValue(position, out GameObject coin) ? coin : null;
    }

    public void ClearAllCoins()
    {
        foreach (GameObject coin in new HashSet<GameObject>(coinVisuals.Values))
            if (coin != null) Destroy(coin);
        coinVisuals.Clear();
        PlayerXCoin = null;
        PlayerYCoin = null;
    }

    private bool IsOnlineGame()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private bool IsLocalPlayerAllowed(PlayerType player)
    {
        if (NetworkManager.Singleton == null)
            return true;

        if (NetworkManager.Singleton.IsHost && player == PlayerType.X)
            return true;

        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost && player == PlayerType.Y)
            return true;

        return false;
    }

    private PlayerType GetCurrentPlacementPlayer()
    {
        return turnManager.CurrentState == GameState.WaitingForPlayerXPlacement
            ? PlayerType.X
            : PlayerType.Y;
    }

    public void UpdateCoinPosition(Vector2Int oldPosition, Vector2Int newPosition, GameObject coin)
    {
        if (coinVisuals.ContainsKey(oldPosition))
            coinVisuals.Remove(oldPosition);

        coinVisuals[newPosition] = coin;
    }

    public void UpdateCoinPositions(MovementResolution resolution, GameObject xCoin, GameObject yCoin)
    {
        Vector2Int xOld = resolution.playerXResult.startPosition;
        Vector2Int yOld = resolution.playerYResult.startPosition;
        Vector2Int xNew = SecretFourRules.FinalPosition(resolution.playerXResult, resolution.playerXCollision);
        Vector2Int yNew = SecretFourRules.FinalPosition(resolution.playerYResult, resolution.playerYCollision);
        // Remove both old entries before adding either destination, including swaps and following moves.
        if (xCoin != null) coinVisuals.Remove(xOld);
        if (yCoin != null) coinVisuals.Remove(yOld);
        if (xCoin != null) coinVisuals[xNew] = xCoin;
        if (yCoin != null) coinVisuals[yNew] = yCoin;
    }

    public IEnumerable<GameObject> GetAllCoins()
    {
        return coinVisuals.Values;
    }

    /// <summary>
    /// Rebuilds the local visual cache from the authoritative network board state.
    /// This is presentation only; the network state remains the source of truth.
    /// </summary>
    public void EnsureNetworkCoin(Vector2Int position, PlayerType player)
    {
        if (!boardManager.CurrentBoard.IsInsideBoard(position.x, position.y)) return;
        GameObject coin = GetCoinAtPosition(position);
        if (coin == null)
        {
            string skin = OnlineMatchPlayerState.Instance != null ? OnlineMatchPlayerState.Instance.GetSkinId(player) : null;
            CreateCoinVisual(position.x, position.y, player, skin);
            coin = GetCoinAtPosition(position);
        }
        if (player == PlayerType.X) PlayerXCoin = coin;
        else PlayerYCoin = coin;
    }

    public void RebuildFromNetworkState(
        NetworkList<byte> boardCells,
        NetworkList<FixedString64Bytes> skinIds,
        Vector2Int xPosition,
        Vector2Int yPosition,
        bool xHasPlaced,
        bool yHasPlaced)
    {
        if (boardManager == null || boardManager.CurrentBoard == null || boardView == null)
            return;

        ClearAllCoins();

        Board board = boardManager.CurrentBoard;

        for (int x = 0; x < board.Width; x++)
        {
            for (int y = 0; y < board.Height; y++)
            {
                int index = x * board.Height + y;
                if (index < 0 || index >= boardCells.Count)
                    continue;

                PlayerType? player = null;
                if (boardCells[index] == 1)
                    player = PlayerType.X;
                else if (boardCells[index] == 2)
                    player = PlayerType.Y;

                if (!player.HasValue)
                    continue;

                string skinId = string.Empty;
                if (skinIds != null && index < skinIds.Count)
                    skinId = skinIds[index].ToString();

                CreateCoinVisual(
                    x,
                    y,
                    player.Value,
                    string.IsNullOrWhiteSpace(skinId) ? null : skinId);

                GameObject coin = GetCoinAtPosition(new Vector2Int(x, y));

                if (player.Value == PlayerType.X &&
                    xHasPlaced &&
                    xPosition == new Vector2Int(x, y))
                {
                    PlayerXCoin = coin;
                }

                if (player.Value == PlayerType.Y &&
                    yHasPlaced &&
                    yPosition == new Vector2Int(x, y))
                {
                    PlayerYCoin = coin;
                }
            }
        }
        PlayerXCoin = xHasPlaced ? GetCoinAtPosition(xPosition) : null;
        PlayerYCoin = yHasPlaced ? GetCoinAtPosition(yPosition) : null;
    }
}
