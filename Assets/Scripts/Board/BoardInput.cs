using UnityEngine;
using UnityEngine.InputSystem;

public class BoardInput : MonoBehaviour
{
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
        // ---------------------------------
        // FIRST: Block input if paused
        // ---------------------------------

        if (pauseManager != null)
        {
            if (pauseManager.IsPaused() ||
                pauseManager.IsInputBlocked())
            {
                return;
            }
        }

        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        Vector2 screenPosition =
            Mouse.current.position.ReadValue();

        Vector3 worldPosition =
            mainCamera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x,
                    screenPosition.y,
                    -mainCamera.transform.position.z
                )
            );

        worldPosition.z = 0f;

        TrySelectCell(worldPosition);
    }

    private void TrySelectCell(Vector3 worldPosition)
    {
        if (boardView == null ||
            placementManager == null)
        {
            return;
        }

        // Safety check again
        if (pauseManager != null)
        {
            if (pauseManager.IsPaused() ||
                pauseManager.IsInputBlocked())
            {
                return;
            }
        }

        if (!boardView.TryGetCellCoordinates(
            worldPosition,
            out int x,
            out int y))
        {
            return;
        }

        placementManager.TryPlaceCoin(x, y);
    }
}