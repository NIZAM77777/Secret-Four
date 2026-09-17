using UnityEngine;

public class BoardView : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private float cellSize = 1f;

    [Header("Mobile Board Layout")]
    [Tooltip("Scales the complete board without changing the logical board coordinates.")]
    [SerializeField]
    private float boardScale = 0.86f;

    [Tooltip("Moves the complete board upward in world space so it clears the direction controls.")]
    [SerializeField]
    private float boardVerticalOffset = 0.9f;

    [SerializeField]
    private GameObject cellPrefab;


    private void Start()
    {
        CreateBoardView();
    }


    private float EffectiveCellSize =>
        Mathf.Max(0.01f, cellSize * boardScale);


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

                GameObject cell = Instantiate(
                    cellPrefab,
                    position,
                    Quaternion.identity,
                    transform
                );

                cell.name = $"Cell_{x}_{y}";

                CellView cellView = cell.GetComponent<CellView>();
                if (cellView != null)
                {
                    cellView.Initialize(x, y);
                }
            }
        }
    }


    private void ClearBoardView()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
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