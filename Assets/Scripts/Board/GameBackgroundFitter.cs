using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GameBackgroundFitter : MonoBehaviour
{
    private Camera gameCamera;
    private SpriteRenderer background;
    private void Awake() { gameCamera = Camera.main; background = GetComponent<SpriteRenderer>(); }
    private void LateUpdate() => Fit();
    public void Fit()
    {
        if (gameCamera == null) gameCamera = Camera.main;
        if (background == null) background = GetComponent<SpriteRenderer>();
        if (gameCamera == null || background.sprite == null || !gameCamera.orthographic) return;
        Vector2 size = background.sprite.bounds.size;
        float height = gameCamera.orthographicSize * 2;
        // Keep the opaque marble interior behind the controls; the frame stays outside the play area.
        float scale = Mathf.Max(height / size.y, height * gameCamera.aspect / size.x) * 1.5f;
        transform.localScale = new Vector3(scale, scale, 1);
        transform.position = new Vector3(gameCamera.transform.position.x, gameCamera.transform.position.y, 0);
    }
}
