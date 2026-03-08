#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NeonMatchBlocksAutoBootstrapEditor
{
    private const string ScenePath = "Assets/Scenes/NeonMatchBlocks.unity";
    private const string PrefKey = "NeonMatchBlocks.AutoBootstrapVersion";
    private const string BootstrapVersion = "visual-pass-v1";

    static NeonMatchBlocksAutoBootstrapEditor()
    {
        EditorApplication.delayCall += TryBootstrap;
    }

    private static void TryBootstrap()
    {
        if (Application.isBatchMode) return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryBootstrap;
            return;
        }

        if (EditorPrefs.GetString(PrefKey, string.Empty) == BootstrapVersion) return;

        string absoluteScenePath = Path.Combine(Directory.GetCurrentDirectory(), ScenePath);
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        NeonMatchBlocksAutoSetupEditor.AutoSetupCompleteSceneBatch();
        AssetDatabase.Refresh();
        EditorPrefs.SetString(PrefKey, BootstrapVersion);
        Debug.Log("Neon Match Blocks bootstrap complete. Scene refreshed at " + ScenePath);
    }
}
#endif
