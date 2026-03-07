using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public static class NeonConnectWordsAndroidBuildEditor
{
    private const string OutputDirectory = "Builds/Android";
    private const string OutputApkName = "NeonConnectWords.apk";
    private const string OutputAabName = "NeonConnectWords.aab";

    public static void BuildApk()
    {
        BuildAndroidPlayer(false, OutputApkName);
    }

    public static void BuildAab()
    {
        BuildAndroidPlayer(true, OutputAabName);
    }

    private static void BuildAndroidPlayer(bool buildAppBundle, string outputName)
    {
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[NeonConnectWords] No enabled scenes found in Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory(OutputDirectory);

        EditorUserBuildSettings.buildAppBundle = buildAppBundle;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(OutputDirectory, outputName),
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[NeonConnectWords] Android build failed: " + report.summary.result);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[NeonConnectWords] Android build created at " + Path.GetFullPath(options.locationPathName));
        EditorApplication.Exit(0);
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        int enabledCount = 0;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].enabled)
            {
                enabledCount++;
            }
        }

        string[] enabledScenes = new string[enabledCount];
        int index = 0;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].enabled)
            {
                enabledScenes[index++] = scenes[i].path;
            }
        }

        return enabledScenes;
    }
}
