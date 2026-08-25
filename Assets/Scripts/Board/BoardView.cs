using UnityEngine;

public class BoardView : MonoBehaviour
{
    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private float cellSize = 1f;

    [SerializeField]
    private GameObject cellPrefab;

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

    public Vector3 GetCellWorldPosition(int x, int y)
    {
        float boardWidth = boardManager.CurrentBoard.Width * cellSize;
        float boardHeight = boardManager.CurrentBoard.Height * cellSize;

        float startX = -(boardWidth - cellSize) / 2f;
        float startY = -(boardHeight - cellSize) / 2f;

        return new Vector3(
            startX + x * cellSize,
            startY + y * cellSize,
            0f
        );
    }

    public bool TryGetCellCoordinates(
    Vector3 worldPosition,
    out int x,
    out int y)
    {
        Board board = boardManager.CurrentBoard;

        float boardWidth = board.Width * cellSize;
        float boardHeight = board.Height * cellSize;

        float startX = -(boardWidth - cellSize) / 2f;
        float startY = -(boardHeight - cellSize) / 2f;

        x = Mathf.RoundToInt(
            (worldPosition.x - startX) / cellSize
        );

        y = Mathf.RoundToInt(
            (worldPosition.y - startY) / cellSize
        );

        return board.IsInsideBoard(x, y);
    }
}