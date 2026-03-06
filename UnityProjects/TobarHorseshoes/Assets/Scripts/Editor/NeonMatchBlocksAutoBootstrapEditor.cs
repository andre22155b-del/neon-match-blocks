#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NeonMatchBlocksAutoBootstrapEditor
{
    private const string ScenePath = "Assets/Scenes/NeonMatchBlocks.unity";
    private const string PrefKey = "NeonMatchBlocks.AutoBootstrapDone.v1";

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

        if (EditorPrefs.GetBool(PrefKey, false)) return;

        string absoluteScenePath = Path.Combine(Directory.GetCurrentDirectory(), ScenePath);
        if (File.Exists(absoluteScenePath))
        {
            EditorPrefs.SetBool(PrefKey, true);
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        NeonMatchBlocksAutoSetupEditor.AutoSetupCompleteSceneBatch();
        AssetDatabase.Refresh();
        EditorPrefs.SetBool(PrefKey, true);
        Debug.Log("Neon Match Blocks bootstrap complete. Scene created at " + ScenePath);
    }
}
#endif
