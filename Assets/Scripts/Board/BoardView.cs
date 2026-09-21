using System.Collections;
using UnityEngine;
using MN7.Store;

public class BoardView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private float cellSize = 1f;

    [Header("Mobile Board Layout")]
    [Tooltip("Scales the complete board without changing the logical board coordinates.")]
    [SerializeField] private float boardScale = 0.86f;

    [Tooltip("Moves the complete board upward in world space so it clears the direction controls.")]
    [SerializeField] private float boardVerticalOffset = 0.9f;

    [SerializeField] private GameObject cellPrefab;

    public float BoardVisualScale => Mathf.Max(0.01f, boardScale);

    private float EffectiveCellSize => Mathf.Max(0.01f, cellSize * BoardVisualScale);

    private void Start()
    {
        CreateBoardView();
        BindStore();
    }

    private void OnDestroy()
    {
        MN7StoreManager store = MN7StoreManager.Instance;
        if (store != null)
            store.OnStoreInitialized -= HandleStoreInitialized;

        if (store?.Selection != null)
            store.Selection.OnSelectionChanged -= HandleSelectionChanged;

    }

    private void OnEnable()
    {
        OnlineMatchPlayerState.OnAnyProfileChanged += HandleOnlineProfileChanged;
    }

    private void BindStore()
    {
        MN7StoreManager store = MN7StoreManager.Instance;
        if (store == null)
        {
            StartCoroutine(WaitForStore());
            return;
        }

        store.OnStoreInitialized -= HandleStoreInitialized;
        store.OnStoreInitialized += HandleStoreInitialized;

        if (store.IsInitialized)
            HandleStoreInitialized();
        else if (store.Selection != null)
            SubscribeToSelection(store);
    }

    private IEnumerator WaitForStore()
    {
        while (this != null && MN7StoreManager.Instance == null)
            yield return null;

        if (this != null)
            BindStore();
    }

    private void HandleStoreInitialized()
    {
        MN7StoreManager store = MN7StoreManager.Instance;
        if (store == null)
            return;

        SubscribeToSelection(store);
        ApplySelectedBoardSkin();
    }

    private void SubscribeToSelection(MN7StoreManager store)
    {
        if (store.Selection == null)
            return;

        store.Selection.OnSelectionChanged -= HandleSelectionChanged;
        store.Selection.OnSelectionChanged += HandleSelectionChanged;
    }

    private void HandleSelectionChanged(string group, string itemId)
    {
        if (string.Equals(group, SecretFourStoreIntegration.BoardSelectionGroup, System.StringComparison.OrdinalIgnoreCase))
            ApplySelectedBoardSkin();
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

        ClearBoardView();

        Board board = boardManager.CurrentBoard;
        if (board == null)
        {
            Debug.LogError("BoardView: Current Board is null.");
            return;
        }

        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                Vector3 position = GetCellWorldPosition(x, y);

                GameObject cell = Instantiate(
                    cellPrefab,
                    position,
                    Quaternion.identity,
                    transform
                );

                cell.name = $"Cell_{x}_{y}";
                cell.transform.localScale *= BoardVisualScale;

                CellView cellView = cell.GetComponent<CellView>();
                if (cellView != null)
                    cellView.Initialize(x, y);
            }
        }

        // Apply immediately when the store is already ready. If it is not ready,
        // HandleStoreInitialized will apply the selected board after initialization.
        ApplySelectedBoardSkin();
    }

    private void ClearBoardView()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    /// <summary>
    /// Board appearance is local presentation.
    /// Each device uses its own selected BOARD item, including OnlineRoom.
    /// Player coin skins are synchronized separately through the network profile.
    /// </summary>
    public void ApplySelectedBoardSkin()
    {
        Sprite selectedBoardSprite =
            SecretFourStoreIntegration.GetSelectedBoardSprite();

        if (selectedBoardSprite == null)
            return;

        CellView[] cells = GetComponentsInChildren<CellView>(true);
        foreach (CellView cell in cells)
            cell.ApplyBoardSprite(selectedBoardSprite);
    }

    public Vector3 GetCellWorldPosition(int x, int y)
    {
        Board board = boardManager.CurrentBoard;
        float spacing = EffectiveCellSize;

        float boardWidth = (board.Width - 1) * spacing;
        float boardHeight = (board.Height - 1) * spacing;

        float startX = -boardWidth / 2f;
        float startY = -boardHeight / 2f + boardVerticalOffset;

        return new Vector3(
            startX + x * spacing,
            startY + y * spacing,
            0f
        );
    }

    public bool TryGetCellCoordinates(
        Vector3 worldPosition,
        out int x,
        out int y)
    {
        Board board = boardManager.CurrentBoard;
        float spacing = EffectiveCellSize;

        float boardWidth = (board.Width - 1) * spacing;
        float boardHeight = (board.Height - 1) * spacing;

        float startX = -boardWidth / 2f;
        float startY = -boardHeight / 2f + boardVerticalOffset;

        x = Mathf.RoundToInt((worldPosition.x - startX) / spacing);
        y = Mathf.RoundToInt((worldPosition.y - startY) / spacing);

        return board.IsInsideBoard(x, y);
    }
}