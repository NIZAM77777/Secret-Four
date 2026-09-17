#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SecretFourGameLayoutFixer
{
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string BackgroundName = "GameBackground";
    private const string BackgroundAssetPath = "Assets/Art/bg.png";

    static SecretFourGameLayoutFixer()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += FixActiveGameScene;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == GameScenePath)
            EditorApplication.delayCall += FixActiveGameScene;
    }

    private static void FixActiveGameScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != GameScenePath)
            return;

        Camera gameCamera = Object.FindFirstObjectByType<Camera>();
        if (gameCamera == null)
            return;

        Sprite backgroundSprite = LoadBackgroundSprite();
        if (backgroundSprite == null)
        {
            Debug.LogWarning("Secret Four layout fixer: Assets/Art/bg.png could not be loaded.");
            return;
        }

        GameObject background = GameObject.Find(BackgroundName);
        if (background == null)
        {
            background = new GameObject(BackgroundName);
            Undo.RegisterCreatedObjectUndo(background, "Create Secret Four Game Background");
        }

        SpriteRenderer renderer = background.GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = Undo.AddComponent<SpriteRenderer>(background);

        renderer.sprite = backgroundSprite;
        renderer.sortingLayerID = 0;
        renderer.sortingOrder = -100;
        renderer.color = Color.white;
        renderer.drawMode = SpriteDrawMode.Simple;

        background.transform.position = new Vector3(
            gameCamera.transform.position.x,
            gameCamera.transform.position.y,
            0f
        );
        background.transform.rotation = Quaternion.identity;

        float cameraHeight = gameCamera.orthographic
            ? gameCamera.orthographicSize * 2f
            : 12f;
        float cameraWidth = cameraHeight * gameCamera.aspect;

        Vector2 spriteSize = backgroundSprite.bounds.size;
        float scaleX = cameraWidth / Mathf.Max(0.001f, spriteSize.x);
        float scaleY = cameraHeight / Mathf.Max(0.001f, spriteSize.y);
        float scale = Mathf.Max(scaleX, scaleY) * 1.02f;

        background.transform.localScale = new Vector3(scale, scale, 1f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Sprite LoadBackgroundSprite()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(BackgroundAssetPath);
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite && sprite.name == "bg_0")
                return sprite;
        }

        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        return null;
    }
}
#endif
