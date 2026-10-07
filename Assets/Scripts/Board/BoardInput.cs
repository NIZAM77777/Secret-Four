using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BoardInput : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private BoardView boardView;
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private PlacementManager placementManager;
    private readonly List<RaycastResult> hits = new List<RaycastResult>();
    private void Awake() { if (mainCamera == null) mainCamera = Camera.main; }
    private void Update()
    {
        if (Time.timeScale == 0 || (pauseManager != null && (pauseManager.IsPaused() || pauseManager.IsInputBlocked()))) return;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            TrySelectBoardCell(Touchscreen.current.primaryTouch.position.ReadValue());
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            TrySelectBoardCell(Mouse.current.position.ReadValue());
    }
    private void TrySelectBoardCell(Vector2 screenPosition)
    {
        if (mainCamera == null || boardView == null || placementManager == null) return;
        if (EventSystem.current != null)
        {
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition }, hits);
            foreach (RaycastResult hit in hits)
                if (hit.module is GraphicRaycaster) return;
        }
        Vector3 world = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
        if (boardView.TryGetCellCoordinates(world, out int x, out int y)) placementManager.TryPlaceCoin(x, y);
    }
}
