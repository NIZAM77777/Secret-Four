using UnityEngine;

public class BoardView : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private BoardManager boardManager;

    [SerializeField]
    private float cellSize = 1f;

    [SerializeField]
    private GameObject cellPrefab;


    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        CreateBoardView();
    }


    // =====================================================
    // CREATE BOARD VIEW
    // =====================================================

    private void CreateBoardView()
    {
        if (boardManager == null)
        {
            Debug.LogError(
                "BoardView: BoardManager is not assigned."
            );

            return;
        }

        if (cellPrefab == null)
        {
            Debug.LogError(
                "BoardView: Cell Prefab is not assigned."
            );

            return;
        }


        // =============================================
        // CLEAR OLD CELLS
        // =============================================

        ClearBoardView();


        Board board =
            boardManager.CurrentBoard;


        // =============================================
        // CREATE CELLS
        // =============================================

        for (int y = 0;
             y < board.Height;
             y++)
        {
            for (int x = 0;
                 x < board.Width;
                 x++)
            {
                Vector3 position =
                    GetCellWorldPosition(x, y);

                GameObject cell =
                    Instantiate(
                        cellPrefab,
                        position,
                        Quaternion.identity,
                        transform
                    );

                cell.name =
                    $"Cell_{x}_{y}";

                CellView cellView =
                    cell.GetComponent<CellView>();

                if (cellView != null)
                {
                    cellView.Initialize(
                        x,
                        y
                    );
                }
            }
        }
    }


    // =====================================================
    // CLEAR BOARD VIEW
    // =====================================================

    private void ClearBoardView()
    {
        for (int i = transform.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                transform.GetChild(i).gameObject
            );
        }
    }


    // =====================================================
    // GET CELL WORLD POSITION
    // =====================================================

    public Vector3 GetCellWorldPosition(
        int x,
        int y)
    {
        Board board =
            boardManager.CurrentBoard;

        float boardWidth =
            (board.Width - 1) *
            cellSize;

        float boardHeight =
            (board.Height - 1) *
            cellSize;

        float startX =
            -boardWidth / 2f;

        float startY =
            -boardHeight / 2f;

        return new Vector3(
            startX +
            x * cellSize,

            startY +
            y * cellSize,

            0f
        );
    }


    // =====================================================
    // WORLD → BOARD COORDINATES
    // =====================================================

    public bool TryGetCellCoordinates(
        Vector3 worldPosition,
        out int x,
        out int y)
    {
        Board board =
            boardManager.CurrentBoard;

        float boardWidth =
            (board.Width - 1) *
            cellSize;

        float boardHeight =
            (board.Height - 1) *
            cellSize;

        float startX =
            -boardWidth / 2f;

        float startY =
            -boardHeight / 2f;


        x = Mathf.RoundToInt(
            (worldPosition.x - startX) /
            cellSize
        );

        y = Mathf.RoundToInt(
            (worldPosition.y - startY) /
            cellSize
        );

        return board.IsInsideBoard(x, y);
    }
}