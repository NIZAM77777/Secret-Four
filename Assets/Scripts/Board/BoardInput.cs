using UnityEngine;
using UnityEngine.InputSystem;

public class BoardInput : MonoBehaviour
{
    [Header("References")]

    [SerializeField]
    private Camera mainCamera;

    [SerializeField]
    private BoardView boardView;

    [SerializeField]
    private PauseManager pauseManager;

    [SerializeField]
    private PlacementManager placementManager;


    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }


    private void Update()
    {
        // ==========================================
        // BLOCK INPUT WHEN PAUSED
        // ==========================================

        if (pauseManager != null)
        {
            if (
                pauseManager.IsPaused() ||
                pauseManager.IsInputBlocked()
            )
            {
                return;
            }
        }


        // ==========================================
        // ANDROID TOUCH
        // ==========================================

        if (
            Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press
                .wasPressedThisFrame
        )
        {
            Vector2 touchPosition =
                Touchscreen.current.primaryTouch.position
                    .ReadValue();

            TrySelectBoardCell(
                touchPosition
            );

            return;
        }


        // ==========================================
        // UNITY EDITOR MOUSE
        // ==========================================

#if UNITY_EDITOR

        if (
            Mouse.current != null &&
            Mouse.current.leftButton
                .wasPressedThisFrame
        )
        {
            Vector2 mousePosition =
                Mouse.current.position
                    .ReadValue();

            TrySelectBoardCell(
                mousePosition
            );
        }

#endif
    }


    // ==========================================
    // SELECT BOARD CELL
    // ==========================================

    private void TrySelectBoardCell(
        Vector2 screenPosition)
    {
        if (
            mainCamera == null ||
            boardView == null ||
            placementManager == null
        )
        {
            Debug.LogError(
                "BoardInput: Missing references."
            );

            return;
        }


        // ==========================================
        // SAFETY CHECK
        // ==========================================

        if (pauseManager != null)
        {
            if (
                pauseManager.IsPaused() ||
                pauseManager.IsInputBlocked()
            )
            {
                return;
            }
        }


        // ==========================================
        // SCREEN → WORLD
        // ==========================================

        Vector3 worldPosition =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    -mainCamera.transform.position.z
                )
            );

        worldPosition.z = 0f;


        // ==========================================
        // WORLD → BOARD CELL
        // ==========================================

        if (
            !boardView.TryGetCellCoordinates(
                worldPosition,
                out int x,
                out int y
            )
        )
        {
            return;
        }

        Debug.Log(
            $"BOARD TOUCH → Cell ({x}, {y})"
        );


        // ==========================================
        // PLACE COIN
        // ==========================================

        placementManager.TryPlaceCoin(
            x,
            y
        );
    }
}