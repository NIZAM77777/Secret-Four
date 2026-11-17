#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// One-click Play Store settings for Secret Four. Runs once automatically after this script
/// compiles (marker: ProjectSettings/SecretFourPublishSetup.v2.done) and can be re-run from
/// Tools > Secret Four > Apply Publish Settings. Safe to run more than once.
/// Package name and keystore are intentionally NOT touched: they are the developer's choice.
/// </summary>
[InitializeOnLoad]
public static class SecretFourPublishSetup
{
    private const string Marker = "ProjectSettings/SecretFourPublishSetup.v2.done";
    private const string IconPath = "Assets/Art/AppIcon/AppIcon.png";

    // Small on-screen art: 1024 px is plenty on a 1080-wide phone.
    private static readonly string[] SmallArtFolders =
    {
        "Assets/Art/Buttons", "Assets/Art/Icons", "Assets/Art/Coins",
        "Assets/Art/Cases", "Assets/Art/cells", "Assets/Art/ReferenceUI"
    };

    // Full-screen art keeps 2048.
    private static readonly string[] KeepFullSize = { "Assets/Art/ReferenceUI/Marble.png" };

    static SecretFourPublishSetup()
    {
        if (!File.Exists(Marker))
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(Marker)) return;
                Apply();
            };
    }

    [MenuItem("Tools/Secret Four/Apply Publish Settings")]
    public static void Apply()
    {
        // Version
        if (string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "1.0")
            PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.Android.bundleVersionCode = Mathf.Max(1, PlayerSettings.Android.bundleVersionCode);

        // Splash: only the branded in-game splash (SecretFourSplash) is shown.
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.SplashScreen.backgroundColor = Color.black; // matches the black in-game splash

        // Android build output
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
        PlayerSettings.stripEngineCode = true;
        EditorUserBuildSettings.buildAppBundle = true; // Play Store needs .aab

        // Release logs: no stack-trace capture for normal logs/warnings (errors keep theirs).
        PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

        // App icon (replace Assets/Art/AppIcon/AppIcon.png with a designer icon any time)
        AssetDatabase.ImportAsset(IconPath);
        if (AssetImporter.GetAtPath(IconPath) is TextureImporter iconImporter)
        {
            iconImporter.textureType = TextureImporterType.Default;
            iconImporter.mipmapEnabled = false;
            iconImporter.textureCompression = TextureImporterCompression.Uncompressed;
            iconImporter.maxTextureSize = 1024;
            iconImporter.SaveAndReimport();
        }
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon != null)
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

        int textures = OptimizeTextures();

        AssetDatabase.SaveAssets();
        File.WriteAllText(Marker, "Applied by SecretFourPublishSetup. Delete this file to re-run automatically.\n");
        Debug.Log($"[Secret Four] Publish settings applied. Version {PlayerSettings.bundleVersion} ({PlayerSettings.Android.bundleVersionCode}), " +
                  $"Unity splash off, AAB on, icon set, {textures} textures optimized. " +
                  $"Still to do by you: package name ({PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)}) and keystore.");
    }

    private static int OptimizeTextures()
    {
        int changed = 0;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art" });
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == IconPath || !(AssetImporter.GetAtPath(path) is TextureImporter importer))
                    continue;

                bool dirty = false;

                if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    // High quality keeps gold gradients and frame edges clean (ASTC 4x4 on Android).
                    importer.textureCompression = TextureImporterCompression.CompressedHQ;
                    dirty = true;
                }

                if (importer.textureType == TextureImporterType.Sprite && importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false; // UI never needs mipmaps
                    dirty = true;
                }

                bool small = SmallArtFolders.Any(f => path.StartsWith(f + "/")) && !KeepFullSize.Contains(path);
                if (small && importer.maxTextureSize > 1024)
                {
                    importer.maxTextureSize = 1024;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                    changed++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        return changed;
    }
}
#endif
