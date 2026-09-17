using UnityEngine;

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

    [Header("Board Background")]
    [Tooltip("Optional sprite drawn behind the complete board. It automatically fits the current board size and scale.")]
    [SerializeField] private Sprite boardBackgroundSprite;

    [Tooltip("Extra world-space padding around the board background on each side.")]
    [SerializeField] private float boardBackgroundPadding = 0.18f;

    [Tooltip("Sorting order for the board background. Keep below cells (normally 0).")]
    [SerializeField] private int boardBackgroundSortingOrder = -1;

    [Tooltip("Optional material for the board background SpriteRenderer.")]
    [SerializeField] private Material boardBackgroundMaterial;

    private GameObject boardBackgroundObject;
    private SpriteRenderer boardBackgroundRenderer;

    public float BoardVisualScale => Mathf.Max(0.01f, boardScale);
    private float EffectiveCellSize => Mathf.Max(0.01f, cellSize * BoardVisualScale);

    private void Start()
    {
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

        ClearBoardView();
        Board board = boardManager.CurrentBoard;

        for (int y = 0; y < board.Height; y++)
        {
            for (int x = 0; x < board.Width; x++)
            {
                Vector3 position = GetCellWorldPosition(x, y);
                GameObject cell = Instantiate(cellPrefab, position, Quaternion.identity, transform);
                cell.name = $"Cell_{x}_{y}";
                cell.transform.localScale *= BoardVisualScale;

                CellView cellView = cell.GetComponent<CellView>();
                if (cellView != null)
                    cellView.Initialize(x, y);
            }
        }

        SetupBoardBackground(board);
    }

    private void ClearBoardView()
    {
        if (boardBackgroundObject != null)
        {
            Destroy(boardBackgroundObject);
            boardBackgroundObject = null;
            boardBackgroundRenderer = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private void SetupBoardBackground(Board board)
    {
        if (boardBackgroundSprite == null)
            return;

        boardBackgroundObject = new GameObject("BoardBackground");
        boardBackgroundObject.transform.SetParent(transform, false);

        boardBackgroundRenderer = boardBackgroundObject.AddComponent<SpriteRenderer>();
        boardBackgroundRenderer.sprite = boardBackgroundSprite;
        boardBackgroundRenderer.sortingOrder = boardBackgroundSortingOrder;
        boardBackgroundRenderer.drawMode = SpriteDrawMode.Sliced;
        boardBackgroundRenderer.size = CalculateBackgroundSize(board);

        if (boardBackgroundMaterial != null)
            boardBackgroundRenderer.sharedMaterial = boardBackgroundMaterial;

        boardBackgroundObject.transform.localPosition = GetBoardCenterWorldPosition(board);
        boardBackgroundObject.transform.localRotation = Quaternion.identity;
        boardBackgroundObject.transform.localScale = Vector3.one;

        // Keep the background behind the cells even if its GameObject was created last.
        boardBackgroundObject.transform.SetAsFirstSibling();
    }

    private Vector2 CalculateBackgroundSize(Board board)
    {
        float spacing = EffectiveCellSize;
        float boardWidth = Mathf.Max(spacing, (board.Width - 1) * spacing);
        float boardHeight = Mathf.Max(spacing, (board.Height - 1) * spacing);

        // Include one cell's visual footprint so the panel surrounds the outer cells,
        // then add user-controlled padding on both sides.
        float cellFootprint = spacing;
        return new Vector2(
            boardWidth + cellFootprint + boardBackgroundPadding * 2f,
            boardHeight + cellFootprint + boardBackgroundPadding * 2f
        );
    }

    private Vector3 GetBoardCenterWorldPosition(Board board)
    {
        float spacing = EffectiveCellSize;
        float boardWidth = (board.Width - 1) * spacing;
        float boardHeight = (board.Height - 1) * spacing;

        return new Vector3(0f, -boardHeight / 2f + boardVerticalOffset + boardHeight / 2f, 0f);
    }

    public Vector3 GetCellWorldPosition(int x, int y)
    {
        Board board = boardManager.CurrentBoard;
        float spacing = EffectiveCellSize;
        float boardWidth = (board.Width - 1) * spacing;
        float boardHeight = (board.Height - 1) * spacing;
        float startX = -boardWidth / 2f;
        float startY = -boardHeight / 2f + boardVerticalOffset;

        return new Vector3(startX + x * spacing, startY + y * spacing, 0f);
    }

    public bool TryGetCellCoordinates(Vector3 worldPosition, out int x, out int y)
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