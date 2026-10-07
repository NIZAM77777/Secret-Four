#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SecretFourGameLayoutFixer
{
    [MenuItem("Tools/Secret Four/Fit Game Background")]
    private static void FixActiveGameScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Game.unity") return;
        GameObject background = GameObject.Find("GameBackground");
        if (background == null || background.GetComponent<SpriteRenderer>()?.sprite == null) return;
        Undo.RecordObject(background.transform, "Fit Secret Four background");
        var fitter = background.GetComponent<GameBackgroundFitter>();
        if (fitter == null) fitter = Undo.AddComponent<GameBackgroundFitter>(background);
        fitter.Fit();
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
#endif
