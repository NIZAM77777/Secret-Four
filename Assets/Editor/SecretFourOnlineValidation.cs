#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class SecretFourOnlineValidation
{
    public static void BuildBatch()
    {
        SecretFourValidation.RunAllChecks();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Mainmenu.unity", "Assets/Scenes/Game.unity" },
            locationPathName = "ValidationBuild/SecretFourValidation.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development,
            extraScriptingDefines = new[] { "SECRET_FOUR_VALIDATION" }
        });
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
#endif
