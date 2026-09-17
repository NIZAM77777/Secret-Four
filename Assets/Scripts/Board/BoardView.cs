using System.Collections;
using UnityEngine;
using MN7.Store;

public class BoardView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private GameObject cellPrefab;

    [Header("Board Position")]
    [Tooltip("Moves the entire board view vertically. Positive values move the board upward.")]
    [SerializeField] private float boardVerticalOffset = 0.7f;

    [Header("Store Board Selection")]
    [Tooltip("Optional selection group used by Secret Four Store board items. If unavailable, the Cell prefab sprite is used.")]
    [SerializeField] private string boardSelectionGroup = "BOARD";

    private bool boardCreated;
    private Coroutine initializationCoroutine;

    private void OnEnable()
    {
        SubscribeToStore();
        initializationCoroutine = StartCoroutine(InitializeWhenReady());
    }

    private void OnDisable()
    {
        UnsubscribeFromStore();

        if (initializationCoroutine != null)
        {
            StopCoroutine(initializationCoroutine);
            initializationCoroutine = null;
        }
    }

    private void SubscribeToStore()
    {
        MN7StoreManager store = MN7StoreManager.Instance;
        if (store != null)
            store.OnStoreInitialized += HandleStoreInitialized;
    }

    private void UnsubscribeFromStore()
    {
        MN7StoreManager store = MN7StoreManager.Instance;
        if (store != null)
        {
            store.OnStoreInitialized -= HandleStoreInitialized;
            if (store.Selection != null)
                store.Selection.OnSelectionChanged -= HandleSelectionChanged;
        }
    }

    private IEnumerator InitializeWhenReady()
    {
        // The board must not depend on the Store existing. If Store is present,
        // wait for it so it can optionally provide a BOARD selection. Otherwise
        // immediately use the Cell prefab's own sprite.
        while (true)
        {
            MN7StoreManager store = MN7StoreManager.Instance;

            if (store == null)
            {
                CreateBoardView();
                initializationCoroutine = null;
                yield break;
            }

            if (store.State == MN7StoreState.Failed)
            {
                Debug.LogWarning("BoardView: MN7 Store failed to initialize. Using the Cell prefab sprite as the board visual.");
                CreateBoardView();
                initializationCoroutine = null;
                yield break;
            }

            if (!store.IsInitialized || store.Selection == null || store.Catalog == null)
            {
                yield return null;
                continue;
            }

            store.Selection.OnSelectionChanged -= HandleSelectionChanged;
            store.Selection.OnSelectionChanged += HandleSelectionChanged;

            CreateBoardView();
            initializationCoroutine = null;
            yield break;
        }
    }

    private void HandleStoreInitialized()
    {
        MN7StoreManager store = MN7StoreManager.Instance;
        if (store?.Selection == null)
            return;

        store.Selection.OnSelectionChanged -= HandleSelectionChanged;
        store.Selection.OnSelectionChanged += HandleSelectionChanged;

        CreateBoardView();
    }

    private void HandleSelectionChanged(string group, string itemId)
    {
        if (!string.Equals(group, boardSelectionGroup, System.StringComparison.OrdinalIgnoreCase))
            return;

        CreateBoardView();
    }

    private void CreateBoardView()
    {
        if (boardManager == null)
        {
            Debug.LogError("BoardView: BoardManager is not assigned.");
            return;
        }

        if (cellPrefab == null)
        {
            Debug.LogError("BoardView: Cell Prefab is not assigned.");
            return;
        }

        Board board = boardManager.CurrentBoard;
        if (board == null)
        {
            Debug.LogError("BoardView: Current Board is null.");
            return;
        }

        // A BOARD Store item is optional. If none exists, use the sprite already
        // assigned to the Cell prefab so the board can never become invisible.
        Sprite selectedCellSprite = GetSelectedCellSprite();
        Sprite defaultCellSprite = GetDefaultCellSprite();
        Sprite cellSprite = selectedCellSprite != null ? selectedCellSprite : defaultCellSprite;

        if (cellSprite == null)
        {
            Debug.LogError("BoardView: Neither a Store board sprite nor a default Cell prefab sprite is available.");
            return;
        }

        ClearBoardView();

        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                GameObject cell = Instantiate(
                    cellPrefab,
                    GetCellWorldPosition(x, y),
                    Quaternion.identity,
                    transform
                );

                cell.name = $"Cell_{x}_{y}";

                CellView cellView = cell.GetComponent<CellView>();
                if (cellView != null)
                {
                    cellView.Initialize(x, y);
                    cellView.ApplyStoreSprite(cellSprite);
                }
                else
                {
                    ApplySpriteToCellObject(cell, cellSprite);
                }
            }
        }

        boardCreated = true;
    }

    private Sprite GetSelectedCellSprite()
    {
        MN7StoreManager store = MN7StoreManager.Instance;
        if (store == null || !store.IsInitialized || store.Selection == null || store.Catalog == null)
            return null;

        string selectedItemId = store.Selection.GetSelectedItemId(boardSelectionGroup);
        if (string.IsNullOrWhiteSpace(selectedItemId))
            return null;

        MN7ItemData selectedItem = store.Catalog.GetItem(selectedItemId);
        return selectedItem?.icon;
    }

    private Sprite GetDefaultCellSprite()
    {
        if (cellPrefab == null)
            return null;

        SpriteRenderer spriteRenderer = cellPrefab.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer != null && spriteRenderer.sprite != null)
            return spriteRenderer.sprite;

        UnityEngine.UI.Image image = cellPrefab.GetComponentInChildren<UnityEngine.UI.Image>(true);
        return image != null ? image.sprite : null;
    }

    private void ApplySpriteToCellObject(GameObject cell, Sprite sprite)
    {
        if (cell == null || sprite == null)
            return;

        SpriteRenderer spriteRenderer = cell.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.enabled = true;
            return;
        }

        UnityEngine.UI.Image image = cell.GetComponentInChildren<UnityEngine.UI.Image>(true);
        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = true;
            return;
        }

        Debug.LogError("BoardView: Cell prefab has no SpriteRenderer or UI Image for board visuals.");
    }

    private void ClearBoardView()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    public Vector3 GetCellWorldPosition(int x, int y)
    {
        Board board = boardManager.CurrentBoard;

        float boardWidth = (board.Width - 1) * cellSize;
        float boardHeight = (board.Height - 1) * cellSize;

        float startX = -boardWidth / 2f;
        float startY = -boardHeight / 2f;

        return new Vector3(
            startX + x * cellSize,
            startY + y * cellSize + boardVerticalOffset,
            0f
        );
    }

    public bool TryGetCellCoordinates(
        Vector3 worldPosition,
        out int x,
        out int y)
    {
        Board board = boardManager.CurrentBoard;

        float boardWidth = (board.Width - 1) * cellSize;
        float boardHeight = (board.Height - 1) * cellSize;

        float startX = -boardWidth / 2f;
        float startY = -boardHeight / 2f + boardVerticalOffset;

        x = Mathf.RoundToInt(
            (worldPosition.x - startX) / cellSize
        );

        y = Mathf.RoundToInt(
            (worldPosition.y - startY) / cellSize
        );

        return board.IsInsideBoard(x, y);
    }
}
