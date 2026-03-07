using System;
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
    private const string KeystorePathEnv = "ANDROID_KEYSTORE_PATH";
    private const string KeystorePasswordEnv = "ANDROID_KEYSTORE_PASSWORD";
    private const string KeyAliasNameEnv = "ANDROID_KEYALIAS_NAME";
    private const string KeyAliasPasswordEnv = "ANDROID_KEYALIAS_PASSWORD";

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
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        ConfigureAndroidSigning(buildAppBundle);

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

    private static void ConfigureAndroidSigning(bool requireReleaseSigning)
    {
        string keystorePath = GetEnvironmentVariable(KeystorePathEnv);
        string keystorePassword = GetEnvironmentVariable(KeystorePasswordEnv);
        string keyAliasName = GetEnvironmentVariable(KeyAliasNameEnv);
        string keyAliasPassword = GetEnvironmentVariable(KeyAliasPasswordEnv);

        bool hasAnySigningValue =
            !string.IsNullOrEmpty(keystorePath) ||
            !string.IsNullOrEmpty(keystorePassword) ||
            !string.IsNullOrEmpty(keyAliasName) ||
            !string.IsNullOrEmpty(keyAliasPassword);

        if (!hasAnySigningValue)
        {
            PlayerSettings.Android.useCustomKeystore = false;
            if (requireReleaseSigning)
            {
                Debug.LogError("[NeonConnectWords] Android release signing is required. Set ANDROID_KEYSTORE_PATH, ANDROID_KEYSTORE_PASSWORD, ANDROID_KEYALIAS_NAME, and ANDROID_KEYALIAS_PASSWORD.");
                EditorApplication.Exit(1);
            }

            Debug.LogWarning("[NeonConnectWords] No Android signing environment variables found. Continuing with default signing.");
            return;
        }

        if (string.IsNullOrEmpty(keystorePath) ||
            string.IsNullOrEmpty(keystorePassword) ||
            string.IsNullOrEmpty(keyAliasName) ||
            string.IsNullOrEmpty(keyAliasPassword))
        {
            Debug.LogError("[NeonConnectWords] Android signing environment variables are incomplete.");
            EditorApplication.Exit(1);
            return;
        }

        if (!File.Exists(keystorePath))
        {
            Debug.LogError("[NeonConnectWords] Android keystore file was not found at " + keystorePath);
            EditorApplication.Exit(1);
            return;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePassword;
        PlayerSettings.Android.keyaliasName = keyAliasName;
        PlayerSettings.Android.keyaliasPass = keyAliasPassword;

        Debug.Log("[NeonConnectWords] Using custom Android signing keystore at " + keystorePath);
    }

    private static string GetEnvironmentVariable(string variableName)
    {
        return Environment.GetEnvironmentVariable(variableName)?.Trim() ?? string.Empty;
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
