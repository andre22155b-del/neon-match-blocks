using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class NeonConnectWordsiOSBuildEditor
{
    private const string OutputDirectory = "Builds/iOS";

    public static void BuildXcodeProject()
    {
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[NeonConnectWords] No enabled scenes found in Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        Directory.CreateDirectory(OutputDirectory);

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = OutputDirectory,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[NeonConnectWords] iOS Xcode export failed: " + report.summary.result);
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("[NeonConnectWords] iOS Xcode project exported to " + Path.GetFullPath(OutputDirectory));
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
