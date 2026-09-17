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
    [Tooltip("Selection group used by Secret Four Store board items.")]
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
        // The Store lives across scenes. Wait until it has built its catalog/selection
        // data before creating cells so the board never falls back to the Cell prefab sprite.
        while (true)
        {
            MN7StoreManager store = MN7StoreManager.Instance;

            if (store == null)
            {
                yield return null;
                continue;
            }

            if (store.State == MN7StoreState.Failed)
            {
                Debug.LogError("BoardView: MN7 Store failed to initialize. Board cells cannot be created because Store visuals are required.");
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

        Sprite selectedCellSprite = GetSelectedCellSprite();
        if (selectedCellSprite == null)
        {
            ClearBoardView();
            boardCreated = false;
            Debug.LogError(
                $"BoardView: No selected Store board sprite found for selection group '{boardSelectionGroup}'. " +
                "The board will remain empty because Secret Four uses Store board sprites only."
            );
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
                    cellView.ApplyStoreSprite(selectedCellSprite);
                }
                else
                {
                    ApplySpriteToCellObject(cell, selectedCellSprite);
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

    private void ApplySpriteToCellObject(GameObject cell, Sprite sprite)
    {
        if (cell == null || sprite == null)
            return;

        SpriteRenderer spriteRenderer = cell.GetComponentInChildren<SpriteRenderer>(true);
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            return;
        }

        UnityEngine.UI.Image image = cell.GetComponentInChildren<UnityEngine.UI.Image>(true);
        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = true;
            return;
        }

        Debug.LogError("BoardView: Cell prefab has no SpriteRenderer or UI Image for Store board visuals.");
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
