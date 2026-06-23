#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Object = UnityEngine.Object;

public static class NeonFieldGoalAutoSetupEditor
{
    private const string RootFolder = "Assets/NeonFieldGoal";
    private const string MaterialsFolder = RootFolder + "/Materials";
    private const string PrefabsFolder = RootFolder + "/Prefabs";
    private const string ScenesFolder = "Assets/Scenes";
    private const string ScenePath = ScenesFolder + "/NeonFieldGoal.unity";
    private const string ConfigPath = "Assets/Settings/NeonFieldGoalConfig.asset";
    private const string VolumeProfilePath = "Assets/Settings/NeonFieldGoalVolumeProfile.asset";
    private const string FootballPrefabPath = PrefabsFolder + "/FootballProjectile.prefab";

    private const string FloorMaterialPath = MaterialsFolder + "/FieldFloor.mat";
    private const string FieldStripeMaterialPath = MaterialsFolder + "/FieldStripe.mat";
    private const string FieldMarkingMaterialPath = MaterialsFolder + "/FieldMarking.mat";
    private const string FieldGridMaterialPath = MaterialsFolder + "/FieldGrid.mat";
    private const string LaneMaterialPath = MaterialsFolder + "/LaneGlow.mat";
    private const string GoalMaterialPath = MaterialsFolder + "/GoalGlow.mat";
    private const string TrajectoryMaterialPath = MaterialsFolder + "/TrajectoryGuide.mat";
    private const string BackdropMaterialPath = MaterialsFolder + "/Backdrop.mat";
    private const string AccentPinkMaterialPath = MaterialsFolder + "/AccentPink.mat";
    private const string RefereeDarkMaterialPath = MaterialsFolder + "/RefereeDark.mat";
    private const string RefereeLightMaterialPath = MaterialsFolder + "/RefereeLight.mat";
    private const string FootballMaterialPath = MaterialsFolder + "/Football.mat";
    private const string FootballSeamMaterialPath = MaterialsFolder + "/FootballSeam.mat";
    private const string FootballAccentMaterialPath = MaterialsFolder + "/FootballAccent.mat";

    private struct GameplayRigRefs
    {
        public KickerLaneMover laneMover;
        public Transform ballSpawnPoint;
        public Transform spawnParent;
        public Camera gameplayCamera;
    }

    private struct RefereeStubRefs
    {
        public Transform anchor;
        public Transform root;
        public Transform leftArmPivot;
        public Transform rightArmPivot;
    }

    private struct DefenseBlockerStubRefs
    {
        public Transform root;
        public Transform bodyVisual;
        public Transform leftArmPivot;
        public Transform rightArmPivot;
        public Transform hitbox;
        public FieldGoalDefenseHitbox hitboxComponent;
    }

    private struct GoalSceneRefs
    {
        public GoalDetector goalDetector;
        public FieldGoalMovingGoalController movingGoalController;
        public FieldGoalDefenseController defenseController;
        public Renderer[] goalRenderers;
        public Renderer[] endZoneRenderers;
        public Light[] crowdLights;
        public RefereeStubRefs[] refereeStubs;
    }

    private struct UiRefs
    {
        public NeonFieldGoalUI ui;
        public KickInputController kickInput;
    }

    [MenuItem("Tools/Neon FieldGoal/Auto Setup Scene")]
    public static void AutoSetupScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        AutoSetupCore(showDialog: true);
    }

    [MenuItem("Tools/Neon FieldGoal/Auto Setup Scene Silent %#g")]
    public static void AutoSetupSceneSilent()
    {
        AutoSetupCore(showDialog: false);
    }

    public static void AutoSetupSceneBatch()
    {
        AutoSetupCore(showDialog: false);
    }

    private static void AutoSetupCore(bool showDialog)
    {
        EnsureFolder(ScenesFolder);
        EnsureFolder(RootFolder);
        EnsureFolder(MaterialsFolder);
        EnsureFolder(PrefabsFolder);
        AssetDatabase.Refresh();
        ImportTmpEssentialsSilent();

        Material floorMaterial = LoadOrCreateLitMaterial(
            FloorMaterialPath,
            new Color(0.005f, 0.06f, 0.025f),
            new Color(0.08f, 0.52f, 0.18f) * 2.6f,
            0.12f,
            0.92f);
        Material fieldStripeMaterial = LoadOrCreateLitMaterial(
            FieldStripeMaterialPath,
            new Color(0.015f, 0.12f, 0.05f),
            new Color(0.14f, 0.76f, 0.28f) * 2.25f,
            0.08f,
            0.94f);
        Material fieldMarkingMaterial = LoadOrCreateLitMaterial(
            FieldMarkingMaterialPath,
            new Color(0.84f, 1f, 0.92f),
            new Color(0.74f, 1f, 0.92f) * 7.2f,
            0.02f,
            0.97f);
        Material fieldGridMaterial = LoadOrCreateLitMaterial(
            FieldGridMaterialPath,
            new Color(0.06f, 0.22f, 0.12f),
            new Color(0.34f, 1f, 0.62f) * 5.4f,
            0.01f,
            0.95f);
        Material laneMaterial = LoadOrCreateLitMaterial(
            LaneMaterialPath,
            new Color(0.05f, 0.2f, 0.11f),
            new Color(0.28f, 1f, 0.5f) * 5.6f,
            0.03f,
            0.96f);
        Material goalMaterial = LoadOrCreateLitMaterial(
            GoalMaterialPath,
            new Color(0.18f, 0.22f, 0.06f),
            new Color(1f, 0.96f, 0.36f) * 6.8f,
            0.02f,
            0.98f);
        Material trajectoryMaterial = LoadOrCreateLitMaterial(
            TrajectoryMaterialPath,
            new Color(0.08f, 0.18f, 0.12f),
            new Color(0.42f, 1f, 0.74f) * 5.2f,
            0f,
            0.84f);
        Material backdropMaterial = LoadOrCreateLitMaterial(
            BackdropMaterialPath,
            new Color(0.02f, 0.03f, 0.1f),
            new Color(0.1f, 0.18f, 0.42f) * 2.5f,
            0.04f,
            0.82f);
        Material accentPinkMaterial = LoadOrCreateLitMaterial(
            AccentPinkMaterialPath,
            new Color(0.18f, 0.07f, 0.22f),
            new Color(1f, 0.34f, 0.9f) * 5.1f,
            0.04f,
            0.9f);
        Material refereeDarkMaterial = LoadOrCreateLitMaterial(
            RefereeDarkMaterialPath,
            new Color(0.06f, 0.07f, 0.1f),
            new Color(0.04f, 0.05f, 0.08f) * 0.4f,
            0.08f,
            0.58f);
        Material refereeLightMaterial = LoadOrCreateLitMaterial(
            RefereeLightMaterialPath,
            new Color(0.85f, 0.88f, 0.94f),
            new Color(0.1f, 0.12f, 0.16f) * 0.25f,
            0.02f,
            0.66f);
        Material footballMaterial = LoadOrCreateLitMaterial(
            FootballMaterialPath,
            new Color(0.11f, 0.08f, 0.06f),
            new Color(0.22f, 0.14f, 0.08f) * 1.35f,
            0.05f,
            0.92f);
        Material footballSeamMaterial = LoadOrCreateLitMaterial(
            FootballSeamMaterialPath,
            new Color(0.78f, 1f, 0.88f),
            new Color(0.38f, 1f, 0.74f) * 3.6f,
            0.02f,
            0.95f);
        Material footballAccentMaterial = LoadOrCreateLitMaterial(
            FootballAccentMaterialPath,
            new Color(1f, 0.4f, 0.86f),
            new Color(1f, 0.36f, 0.92f) * 3.6f,
            0.02f,
            0.94f);
        VolumeProfile neonVolumeProfile = LoadOrCreateNeonFieldGoalVolumeProfile();

        NeonFieldGoalConfig config = LoadOrCreateConfig();
        FootballProjectile footballPrefab = LoadOrCreateFootballPrefab(config, footballMaterial, footballSeamMaterial, footballAccentMaterial);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(0.05f, 0.09f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.02f, 0.1f, 0.08f);
        RenderSettings.fogDensity = 0.0125f;

        GameObject worldRoot = new GameObject("NeonFieldGoalRoot");

        GameplayRigRefs gameplayRig = BuildGameplayRig(worldRoot.transform, config, laneMaterial);
        CreateSceneLightRig(worldRoot.transform);
        CreatePostProcessingRig(worldRoot.transform, neonVolumeProfile);
        GoalSceneRefs goalScene = BuildFieldAndGoal(
            worldRoot.transform,
            config,
            floorMaterial,
            fieldStripeMaterial,
            fieldMarkingMaterial,
            fieldGridMaterial,
            laneMaterial,
            goalMaterial,
            backdropMaterial,
            accentPinkMaterial,
            refereeDarkMaterial,
            refereeLightMaterial);
        UiRefs uiRefs = BuildUi();
        KickAimGuide kickAimGuide = CreateKickAimGuide(worldRoot.transform, trajectoryMaterial);
        kickAimGuide.ApplyConfig(config);

        GameObject managers = new GameObject("Managers");
        FieldGoalGameManager gameManager = managers.AddComponent<FieldGoalGameManager>();
        GoalPresentationController goalPresentation = managers.AddComponent<GoalPresentationController>();
        RefereePresentationController refereePresentation = managers.AddComponent<RefereePresentationController>();

        goalPresentation.config = config;
        goalPresentation.ui = uiRefs.ui;
        goalPresentation.goalRenderers = goalScene.goalRenderers;
        goalPresentation.endZoneRenderers = goalScene.endZoneRenderers;
        goalPresentation.crowdLights = goalScene.crowdLights;

        refereePresentation.config = config;
        refereePresentation.refereeAnchors = new[]
        {
            goalScene.refereeStubs[0].anchor,
            goalScene.refereeStubs[1].anchor
        };
        refereePresentation.refereeRoots = new[]
        {
            goalScene.refereeStubs[0].root,
            goalScene.refereeStubs[1].root
        };
        refereePresentation.leftArmPivots = new[]
        {
            goalScene.refereeStubs[0].leftArmPivot,
            goalScene.refereeStubs[1].leftArmPivot
        };
        refereePresentation.rightArmPivots = new[]
        {
            goalScene.refereeStubs[0].rightArmPivot,
            goalScene.refereeStubs[1].rightArmPivot
        };

        gameManager.config = config;
        gameManager.ui = uiRefs.ui;
        gameManager.kickerLaneMover = gameplayRig.laneMover;
        gameManager.kickInputController = uiRefs.kickInput;
        gameManager.goalDetector = goalScene.goalDetector;
        gameManager.goalPresentationController = goalPresentation;
        gameManager.refereePresentationController = refereePresentation;
        gameManager.movingGoalController = goalScene.movingGoalController;
        gameManager.defenseController = goalScene.defenseController;
        gameManager.kickAimGuide = kickAimGuide;
        gameManager.cameraJuiceController = gameplayRig.gameplayCamera.GetComponent<FieldGoalCameraJuiceController>();
        gameManager.ballSpawnPoint = gameplayRig.ballSpawnPoint;
        gameManager.spawnParent = gameplayRig.spawnParent;
        gameManager.footballPrefab = footballPrefab;

        uiRefs.ui.kickInputController = uiRefs.kickInput;

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
        EnableSceneInBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Neon FieldGoal",
                "Auto setup complete.\n\nScene created: Assets/Scenes/NeonFieldGoal.unity\nPress Play to kick field goals with placeholder art and futuristic fallback audio.",
                "OK");
        }
    }

    [MenuItem("Tools/Neon FieldGoal/Import TMP Essentials (Silent)")]
    public static void ImportTmpEssentialsSilent()
    {
        TMP_PackageResourceImporter.ImportResources(importEssentials: true, importExamples: false, interactive: false);
        AssetDatabase.Refresh();
        CloseTmpImporterWindows();
    }

    private static NeonFieldGoalConfig LoadOrCreateConfig()
    {
        NeonFieldGoalConfig config = AssetDatabase.LoadAssetAtPath<NeonFieldGoalConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        if (UpgradeConfig(config))
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        return config;
    }

    private static bool UpgradeConfig(NeonFieldGoalConfig config)
    {
        if (config == null)
        {
            return false;
        }

        bool changed = false;

        if (config.moverAcceleration <= 0f)
        {
            config.moverAcceleration = 18f;
            changed = true;
        }

        if (config.clutchModeRoundDurationSeconds <= 0f)
        {
            config.clutchModeRoundDurationSeconds = 35f;
            changed = true;
        }

        if (config.clutchWindowSeconds <= 0f)
        {
            config.clutchWindowSeconds = 10f;
            changed = true;
        }

        if (config.moverDeceleration <= 0f)
        {
            config.moverDeceleration = 24f;
            changed = true;
        }

        if (config.moverLeanAngle <= 0f)
        {
            config.moverLeanAngle = 8f;
            changed = true;
        }

        if (config.moverLeanSharpness <= 0f)
        {
            config.moverLeanSharpness = 10f;
            changed = true;
        }

        if (config.kickReleaseDebounceSeconds < 0f)
        {
            config.kickReleaseDebounceSeconds = 0.1f;
            changed = true;
        }

        if (config.cameraFirstPersonFov <= 0f)
        {
            config.cameraFirstPersonFov = 68f;
            changed = true;
        }

        if (config.cameraFirstPersonEyeHeight <= 0f)
        {
            config.cameraFirstPersonEyeHeight = 1.72f;
            changed = true;
        }

        if (config.cameraFirstPersonBackOffset <= 0f)
        {
            config.cameraFirstPersonBackOffset = 1.35f;
            changed = true;
        }

        if (config.cameraFirstPersonLookHeight <= 0f)
        {
            config.cameraFirstPersonLookHeight = 3.15f;
            changed = true;
        }

        if (config.cameraIdleSwayDistance <= 0f)
        {
            config.cameraIdleSwayDistance = 0.018f;
            changed = true;
        }

        if (config.cameraIdleSwaySpeed <= 0f)
        {
            config.cameraIdleSwaySpeed = 1.45f;
            changed = true;
        }

        if (config.cameraIdleRollAngle <= 0f)
        {
            config.cameraIdleRollAngle = 0.3f;
            changed = true;
        }

        if (config.cameraBallFollowDistance <= 0f)
        {
            config.cameraBallFollowDistance = 5.2f;
            changed = true;
        }

        if (config.cameraBallFollowHeight <= 0f)
        {
            config.cameraBallFollowHeight = 1.85f;
            changed = true;
        }

        if (config.cameraBallFollowLookAhead <= 0f)
        {
            config.cameraBallFollowLookAhead = 1.45f;
            changed = true;
        }

        if (config.cameraBallFollowSharpness <= 0f)
        {
            config.cameraBallFollowSharpness = 4.8f;
            changed = true;
        }

        if (config.cameraDistanceFovBoost <= 0f)
        {
            config.cameraDistanceFovBoost = 4.2f;
            changed = true;
        }

        bool trajectoryDefaultsMissing = config.trajectorySampleCount <= 0 || config.trajectorySampleStep <= 0f;
        if (config.trajectorySampleCount <= 0)
        {
            config.trajectorySampleCount = 18;
            changed = true;
        }

        if (config.trajectorySampleStep <= 0f)
        {
            config.trajectorySampleStep = 0.08f;
            changed = true;
        }

        if (trajectoryDefaultsMissing && Mathf.Approximately(config.aimAssistStrength, 0f))
        {
            config.aimAssistStrength = 0.22f;
            changed = true;
        }

        if (config.kickPowerExponent <= 0f)
        {
            config.kickPowerExponent = 0.82f;
            changed = true;
        }

        if (config.kickReleaseDuration <= 0f)
        {
            config.kickReleaseDuration = 0.08f;
            changed = true;
        }

        if (config.kickReleaseEase <= 0f)
        {
            config.kickReleaseEase = 1.45f;
            changed = true;
        }

        if (config.releaseGravityScale <= 0f)
        {
            config.releaseGravityScale = 0.72f;
            changed = true;
        }

        if (config.risingGravityScale <= 0f)
        {
            config.risingGravityScale = 0.94f;
            changed = true;
        }

        if (config.fallingGravityScale <= 0f)
        {
            config.fallingGravityScale = 1.08f;
            changed = true;
        }

        if (config.movingGoalStartYardLine <= 0)
        {
            config.movingGoalStartYardLine = 50;
            changed = true;
        }

        if (config.movingGoalFullChallengeYardLine < config.movingGoalStartYardLine)
        {
            config.movingGoalFullChallengeYardLine = Mathf.Max(config.movingGoalStartYardLine, 65);
            changed = true;
        }

        if (config.movingGoalBaseSideOffset <= 0f)
        {
            config.movingGoalBaseSideOffset = 0.34f;
            changed = true;
        }

        if (config.movingGoalExtraSideOffset < 0f)
        {
            config.movingGoalExtraSideOffset = 0.18f;
            changed = true;
        }

        if (config.movingGoalBaseSpeed <= 0f)
        {
            config.movingGoalBaseSpeed = 0.32f;
            changed = true;
        }

        if (config.movingGoalExtraSpeed < 0f)
        {
            config.movingGoalExtraSpeed = 0.18f;
            changed = true;
        }

        if (config.movingGoalSharpness <= 0f)
        {
            config.movingGoalSharpness = 5.5f;
            changed = true;
        }

        if (config.defenseBaseBlockerCount <= 0)
        {
            config.defenseBaseBlockerCount = 3;
            changed = true;
        }

        if (config.defenseMaxBlockerCount < config.defenseBaseBlockerCount)
        {
            config.defenseMaxBlockerCount = Mathf.Max(config.defenseBaseBlockerCount, 5);
            changed = true;
        }

        if (config.defenseExtraBlockerStartYardLine <= 0)
        {
            config.defenseExtraBlockerStartYardLine = 35;
            changed = true;
        }

        if (config.defenseMaxBlockerYardLine < config.defenseExtraBlockerStartYardLine)
        {
            config.defenseMaxBlockerYardLine = Mathf.Max(config.defenseExtraBlockerStartYardLine, 50);
            changed = true;
        }

        if (config.defenseLineForwardOffset <= 0f)
        {
            config.defenseLineForwardOffset = 4.75f;
            changed = true;
        }

        if (config.defenseBlockerSpacing <= 0f)
        {
            config.defenseBlockerSpacing = 1.6f;
            changed = true;
        }

        if (config.defenseBarWidth <= 0f)
        {
            config.defenseBarWidth = 1.18f;
            changed = true;
        }

        if (config.defenseBarDepth <= 0f)
        {
            config.defenseBarDepth = 0.48f;
            changed = true;
        }

        if (config.defenseBarBaseHeight <= 0f)
        {
            config.defenseBarBaseHeight = 1.08f;
            changed = true;
        }

        if (config.defenseBarExtraHeight < 0f)
        {
            config.defenseBarExtraHeight = 0.62f;
            changed = true;
        }

        if (config.defenseBarBaseTravel < 0f)
        {
            config.defenseBarBaseTravel = 0.34f;
            changed = true;
        }

        if (config.defenseBarExtraTravel < 0f)
        {
            config.defenseBarExtraTravel = 0.46f;
            changed = true;
        }

        if (config.defenseBarBasePulseSpeed <= 0f)
        {
            config.defenseBarBasePulseSpeed = 1.05f;
            changed = true;
        }

        if (config.defenseBarExtraPulseSpeed < 0f)
        {
            config.defenseBarExtraPulseSpeed = 0.62f;
            changed = true;
        }

        if (config.defenseBarKickSurge < 0f)
        {
            config.defenseBarKickSurge = 0.28f;
            changed = true;
        }

        if (config.defenseJumpDuration <= 0f)
        {
            config.defenseJumpDuration = 0.54f;
            changed = true;
        }

        if (config.defenseReactionDelay < 0f)
        {
            config.defenseReactionDelay = 0.05f;
            changed = true;
        }

        if (config.defenseStaggerDelay < 0f)
        {
            config.defenseStaggerDelay = 0.03f;
            changed = true;
        }

        if (config.windStartYardLine <= 0)
        {
            config.windStartYardLine = 35;
            changed = true;
        }

        if (config.windMaxYardLine < config.windStartYardLine)
        {
            config.windMaxYardLine = Mathf.Max(config.windStartYardLine, 60);
            changed = true;
        }

        if (config.windBaseAcceleration < 0f)
        {
            config.windBaseAcceleration = 0.42f;
            changed = true;
        }

        if (config.windExtraAcceleration < 0f)
        {
            config.windExtraAcceleration = 1.02f;
            changed = true;
        }

        if (config.perfectKickWindResistance < 0f)
        {
            config.perfectKickWindResistance = 0.18f;
            changed = true;
        }

        if (config.perfectKickCenter <= 0f)
        {
            config.perfectKickCenter = 0.84f;
            changed = true;
        }

        if (config.perfectKickWindow <= 0f)
        {
            config.perfectKickWindow = 0.12f;
            changed = true;
        }

        if (config.perfectKickImpulseBonus <= 0f)
        {
            config.perfectKickImpulseBonus = 0.42f;
            changed = true;
        }

        if (config.kickGlowBoost <= 0f)
        {
            config.kickGlowBoost = 0.7f;
            changed = true;
        }

        if (config.kickGlowDuration <= 0f)
        {
            config.kickGlowDuration = 0.16f;
            changed = true;
        }

        if (config.crowdBoostMultiplier <= 0f)
        {
            config.crowdBoostMultiplier = 2.25f;
            changed = true;
        }

        if (config.finalDriveCrowdBoostMultiplier <= 0f)
        {
            config.finalDriveCrowdBoostMultiplier = 1.95f;
            changed = true;
        }

        if (config.cameraKickShake <= 0f)
        {
            config.cameraKickShake = 0.14f;
            changed = true;
        }

        if (config.cameraGoalShake <= 0f)
        {
            config.cameraGoalShake = 0.26f;
            changed = true;
        }

        if (config.cameraMissShake <= 0f)
        {
            config.cameraMissShake = 0.1f;
            changed = true;
        }

        if (config.cameraFinalDriveShake <= 0f)
        {
            config.cameraFinalDriveShake = 0.16f;
            changed = true;
        }

        if (config.cameraShakeAngle <= 0f)
        {
            config.cameraShakeAngle = 1.65f;
            changed = true;
        }

        if (config.cameraShakeDistance <= 0f)
        {
            config.cameraShakeDistance = 0.12f;
            changed = true;
        }

        if (config.cameraKickbackDistance <= 0f)
        {
            config.cameraKickbackDistance = 0.22f;
            changed = true;
        }

        if (config.cameraKickFovBoost <= 0f)
        {
            config.cameraKickFovBoost = 1.35f;
            changed = true;
        }

        if (config.cameraGoalFovBoost <= 0f)
        {
            config.cameraGoalFovBoost = 4.1f;
            changed = true;
        }

        if (config.cameraClutchFovBoost <= 0f)
        {
            config.cameraClutchFovBoost = 1.6f;
            changed = true;
        }

        if (config.cameraClutchRollAngle <= 0f)
        {
            config.cameraClutchRollAngle = 0.7f;
            changed = true;
        }

        if (config.cameraShakeDecay <= 0f)
        {
            config.cameraShakeDecay = 2.8f;
            changed = true;
        }

        if (config.cameraPositionSharpness <= 0f)
        {
            config.cameraPositionSharpness = 11f;
            changed = true;
        }

        if (config.cameraRotationSharpness <= 0f)
        {
            config.cameraRotationSharpness = 9f;
            changed = true;
        }

        if (config.cameraFovSharpness <= 0f)
        {
            config.cameraFovSharpness = 8f;
            changed = true;
        }

        if (config.perfectKickSlowMoScale <= 0f)
        {
            config.perfectKickSlowMoScale = 0.86f;
            changed = true;
        }

        if (config.perfectKickSlowMoDuration <= 0f)
        {
            config.perfectKickSlowMoDuration = 0.08f;
            changed = true;
        }

        if (config.perfectGoalSlowMoScale <= 0f)
        {
            config.perfectGoalSlowMoScale = 0.72f;
            changed = true;
        }

        if (config.perfectGoalSlowMoDuration <= 0f)
        {
            config.perfectGoalSlowMoDuration = 0.12f;
            changed = true;
        }

        if (config.nearMissSlowMoScale <= 0f)
        {
            config.nearMissSlowMoScale = 0.9f;
            changed = true;
        }

        if (config.nearMissSlowMoDuration <= 0f)
        {
            config.nearMissSlowMoDuration = 0.06f;
            changed = true;
        }

        if (config.pointsPerGoal <= 0)
        {
            config.pointsPerGoal = 3;
            changed = true;
        }

        if (config.longBombStartYardLine <= 0)
        {
            config.longBombStartYardLine = 50;
            changed = true;
        }

        if (config.longBombPointsPerGoal <= 0)
        {
            config.longBombPointsPerGoal = 5;
            changed = true;
        }

        if (config.startingYardLine <= 0)
        {
            config.startingYardLine = 20;
            changed = true;
        }

        if (config.yardsPerGoalStep <= 0)
        {
            config.yardsPerGoalStep = 5;
            changed = true;
        }

        if (config.worldUnitsPerYard <= 0f)
        {
            config.worldUnitsPerYard = 0.1f;
            changed = true;
        }

        if (config.perfectKickBonusPoints < 0)
        {
            config.perfectKickBonusPoints = 2;
            changed = true;
        }

        if (config.clutchBonusPoints < 0)
        {
            config.clutchBonusPoints = 1;
            changed = true;
        }

        if (config.clutchPerfectBonusPoints < 0)
        {
            config.clutchPerfectBonusPoints = 2;
            changed = true;
        }

        if (config.makesPerMultiplierStep <= 0)
        {
            config.makesPerMultiplierStep = 2;
            changed = true;
        }

        if (config.maxScoreMultiplier <= 0)
        {
            config.maxScoreMultiplier = 5;
            changed = true;
        }

        if (config.clutchMinimumMultiplier <= 0)
        {
            config.clutchMinimumMultiplier = 2;
            changed = true;
        }

        return changed;
    }

    private static FootballProjectile LoadOrCreateFootballPrefab(
        NeonFieldGoalConfig config,
        Material footballMaterial,
        Material footballSeamMaterial,
        Material footballAccentMaterial)
    {
        GameObject temp = new GameObject("FootballProjectile");
        Rigidbody rb = temp.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        SphereCollider collider = temp.AddComponent<SphereCollider>();
        collider.radius = 0.19f;

        FootballProjectile projectile = temp.AddComponent<FootballProjectile>();

        GameObject visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(temp.transform, false);

        GameObject ballVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballVisual.name = "BallVisual";
        ballVisual.transform.SetParent(visualRoot.transform, false);
        ballVisual.transform.localPosition = Vector3.zero;
        ballVisual.transform.localScale = new Vector3(0.34f, 0.22f, 0.22f);
        DestroyCollider(ballVisual);
        ballVisual.GetComponent<MeshRenderer>().sharedMaterial = footballMaterial;

        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "DataSeamTop",
            ballVisual.transform,
            new Vector3(0f, 0.085f, 0f),
            new Vector3(0.3f, 0.024f, 0.035f),
            footballSeamMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "DataSeamBottom",
            ballVisual.transform,
            new Vector3(0f, -0.085f, 0f),
            new Vector3(0.3f, 0.018f, 0.03f),
            footballAccentMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cylinder,
            "CoreBand",
            ballVisual.transform,
            Vector3.zero,
            Quaternion.Euler(0f, 0f, 90f),
            new Vector3(0.15f, 0.022f, 0.15f),
            footballSeamMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cylinder,
            "TailBandFront",
            ballVisual.transform,
            new Vector3(0.12f, 0f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            new Vector3(0.1f, 0.015f, 0.1f),
            footballAccentMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cylinder,
            "TailBandBack",
            ballVisual.transform,
            new Vector3(-0.12f, 0f, 0f),
            Quaternion.Euler(0f, 0f, 90f),
            new Vector3(0.1f, 0.015f, 0.1f),
            footballAccentMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Sphere,
            "NoseGlowFront",
            ballVisual.transform,
            new Vector3(0.155f, 0f, 0f),
            new Vector3(0.05f, 0.07f, 0.07f),
            footballAccentMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Sphere,
            "NoseGlowBack",
            ballVisual.transform,
            new Vector3(-0.155f, 0f, 0f),
            new Vector3(0.05f, 0.07f, 0.07f),
            footballAccentMaterial);

        Material lacesMaterial = LoadOrCreateLitMaterial(
            MaterialsFolder + "/FootballLaces.mat",
            new Color(0.94f, 1f, 0.96f),
            new Color(0.38f, 1f, 0.76f) * 2.3f,
            0f,
            0.72f);
        for (int laceIndex = -2; laceIndex <= 2; laceIndex++)
        {
            CreatePrimitiveVisual(
                PrimitiveType.Cube,
                "Lace_" + (laceIndex + 3),
                ballVisual.transform,
                new Vector3(laceIndex * 0.045f, 0.102f, 0f),
                new Vector3(0.018f, 0.05f, 0.035f),
                lacesMaterial);
        }

        TrailRenderer trail = temp.AddComponent<TrailRenderer>();
        trail.time = 0.16f;
        trail.startWidth = 0.16f;
        trail.endWidth = 0.03f;
        trail.minVertexDistance = 0.05f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.alignment = LineAlignment.View;
        trail.sharedMaterial = footballAccentMaterial;
        Gradient trailColor = new Gradient();
        trailColor.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.34f, 1f, 0.58f), 0f),
                new GradientColorKey(new Color(1f, 0.42f, 0.9f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.28f, 0f),
                new GradientAlphaKey(0.04f, 1f)
            });
        trail.colorGradient = trailColor;

        projectile.visualRoot = visualRoot.transform;
        projectile.visualMeshFilter = ballVisual.GetComponent<MeshFilter>();
        projectile.visualMeshRenderer = ballVisual.GetComponent<MeshRenderer>();
        projectile.ApplyConfig(config);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, FootballPrefabPath);
        Object.DestroyImmediate(temp);
        AssetDatabase.SaveAssets();

        FootballProjectile result = prefab != null ? prefab.GetComponent<FootballProjectile>() : null;
        if (config != null)
        {
            if (config.footballMaterial == null)
            {
                config.footballMaterial = footballMaterial;
            }

            if (config.footballMesh == null && result != null && result.visualMeshFilter != null)
            {
                config.footballMesh = result.visualMeshFilter.sharedMesh;
            }

            EditorUtility.SetDirty(config);
        }

        return result;
    }

    private static GameplayRigRefs BuildGameplayRig(Transform parent, NeonFieldGoalConfig config, Material laneMaterial)
    {
        GameplayRigRefs refs = new GameplayRigRefs();

        GameObject moverGo = new GameObject("KickerRig");
        moverGo.transform.SetParent(parent, false);
        moverGo.transform.position = Vector3.zero;

        refs.laneMover = moverGo.AddComponent<KickerLaneMover>();
        refs.laneMover.moverRoot = moverGo.transform;

        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "KickTee",
            moverGo.transform,
            new Vector3(0f, 0.08f, 0.5f),
            new Vector3(0.32f, 0.08f, 0.54f),
            laneMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "KickPlate",
            moverGo.transform,
            new Vector3(0f, 0.02f, 0.6f),
            new Vector3(0.82f, 0.02f, 1.1f),
            laneMaterial);

        refs.ballSpawnPoint = new GameObject("BallSpawnPoint").transform;
        refs.ballSpawnPoint.SetParent(moverGo.transform, false);
        refs.ballSpawnPoint.localPosition = new Vector3(0f, 0.58f, 0.52f);
        refs.ballSpawnPoint.localRotation = Quaternion.identity;

        refs.spawnParent = new GameObject("SpawnedFootballs").transform;
        refs.spawnParent.SetParent(parent, false);

        GameObject cameraGo = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener));
        cameraGo.tag = "MainCamera";
        cameraGo.transform.SetParent(moverGo.transform, false);
        Vector3 firstPersonCameraPosition = FieldGoalCameraMath.GetFirstPersonLocalPosition(config, refs.ballSpawnPoint.localPosition);
        cameraGo.transform.localPosition = firstPersonCameraPosition;
        cameraGo.transform.localRotation = FieldGoalCameraMath.GetFirstPersonLookRotation(
            config,
            firstPersonCameraPosition,
            config.goalDistance);

        refs.gameplayCamera = cameraGo.GetComponent<Camera>();
        refs.gameplayCamera.orthographic = false;
        refs.gameplayCamera.fieldOfView = FieldGoalCameraMath.GetFirstPersonFov(config);
        refs.gameplayCamera.clearFlags = CameraClearFlags.SolidColor;
        refs.gameplayCamera.backgroundColor = new Color(0.03f, 0.04f, 0.08f);
        refs.gameplayCamera.nearClipPlane = 0.05f;
        refs.gameplayCamera.farClipPlane = 120f;
        refs.gameplayCamera.allowHDR = true;
        refs.gameplayCamera.allowMSAA = true;

        UniversalAdditionalCameraData cameraData = cameraGo.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;

        FieldGoalCameraJuiceController cameraJuice = cameraGo.AddComponent<FieldGoalCameraJuiceController>();
        cameraJuice.targetCamera = refs.gameplayCamera;

        return refs;
    }

    private static GoalSceneRefs BuildFieldAndGoal(
        Transform parent,
        NeonFieldGoalConfig config,
        Material floorMaterial,
        Material fieldStripeMaterial,
        Material fieldMarkingMaterial,
        Material fieldGridMaterial,
        Material laneMaterial,
        Material goalMaterial,
        Material backdropMaterial,
        Material accentPinkMaterial,
        Material refereeDarkMaterial,
        Material refereeLightMaterial)
    {
        GoalSceneRefs refs = new GoalSceneRefs();
        float rearFieldPadding = Mathf.Max(8f, Mathf.Max(1, config.startingYardLine) * Mathf.Max(0.05f, config.worldUnitsPerYard) + 4f);
        float laneStartZ = -rearFieldPadding;
        float laneEndZ = config.goalDistance * 0.84f;
        float laneLength = laneEndZ - laneStartZ;
        float laneCenterZ = (laneStartZ + laneEndZ) * 0.5f;
        float fieldStartZ = -rearFieldPadding - 4f;
        float fieldEndZ = config.goalDistance + 20f;
        float fieldLength = fieldEndZ - fieldStartZ;
        float fieldCenterZ = (fieldStartZ + fieldEndZ) * 0.5f;
        const float fieldOverlayY = -0.045f;
        const float fieldLineY = -0.038f;

        List<Renderer> goalRenderers = new List<Renderer>();
        List<Renderer> endZoneRenderers = new List<Renderer>();
        List<Light> crowdLights = new List<Light>();
        List<RefereeStubRefs> refereeStubs = new List<RefereeStubRefs>();
        List<DefenseBlockerStubRefs> defenseBlockers = new List<DefenseBlockerStubRefs>();

        GameObject env = new GameObject("Environment");
        env.transform.SetParent(parent, false);

        CreateSceneBlock(
            "FieldBase",
            env.transform,
            new Vector3(0f, -0.56f, fieldCenterZ),
            new Vector3(22f, 1f, fieldLength),
            floorMaterial,
            true);

        float stripeLength = 4.2f;
        int stripeCount = Mathf.CeilToInt(fieldLength / stripeLength);
        for (int stripeIndex = 0; stripeIndex < stripeCount; stripeIndex++)
        {
            float stripeStartZ = fieldStartZ + stripeIndex * stripeLength;
            float remainingLength = Mathf.Min(stripeLength, fieldEndZ - stripeStartZ);
            if (remainingLength <= 0f)
            {
                continue;
            }

            float stripeCenterZ = stripeStartZ + remainingLength * 0.5f;
            CreateSceneBlock(
                "FieldStripe_" + stripeIndex,
                env.transform,
                new Vector3(0f, fieldOverlayY, stripeCenterZ),
                new Vector3(22f, 0.02f, Mathf.Max(0.1f, remainingLength - 0.06f)),
                stripeIndex % 2 == 0 ? fieldStripeMaterial : floorMaterial,
                false);
        }

        float scanlineSpacing = 0.72f;
        int scanlineCount = Mathf.CeilToInt(fieldLength / scanlineSpacing);
        for (int scanlineIndex = 0; scanlineIndex <= scanlineCount; scanlineIndex++)
        {
            float scanlineZ = fieldStartZ + scanlineIndex * scanlineSpacing;
            Material scanlineMaterial = scanlineIndex % 6 == 0
                ? fieldGridMaterial
                : scanlineIndex % 2 == 0
                    ? fieldStripeMaterial
                    : floorMaterial;
            CreateSceneBlock(
                "FieldScanline_" + scanlineIndex,
                env.transform,
                new Vector3(0f, fieldOverlayY + 0.003f, scanlineZ),
                new Vector3(18.6f - (scanlineIndex % 3) * 0.5f, 0.006f, 0.018f),
                scanlineMaterial,
                false);
        }

        for (int vectorIndex = -11; vectorIndex <= 11; vectorIndex++)
        {
            float vectorX = vectorIndex * 0.86f;
            CreateSceneBlock(
                "FieldVectorColumn_" + (vectorIndex + 11),
                env.transform,
                new Vector3(vectorX, fieldOverlayY + 0.002f, fieldCenterZ),
                new Vector3(vectorIndex % 4 == 0 ? 0.03f : 0.018f, 0.007f, fieldLength - 0.8f),
                Mathf.Abs(vectorIndex) % 4 == 0 ? fieldGridMaterial : laneMaterial,
                false);
        }

        int pixelRows = Mathf.CeilToInt(fieldLength / 2.6f);
        for (int pixelRow = 0; pixelRow <= pixelRows; pixelRow++)
        {
            float pixelZ = fieldStartZ + pixelRow * 2.6f;
            for (int pixelColumn = -6; pixelColumn <= 6; pixelColumn++)
            {
                if (((pixelColumn + pixelRow) & 1) != 0)
                {
                    continue;
                }

                float pixelX = pixelColumn * 1.45f;
                CreateSceneBlock(
                    "GrassPixel_" + pixelRow + "_" + (pixelColumn + 6),
                    env.transform,
                    new Vector3(pixelX, fieldOverlayY + 0.005f, pixelZ),
                    new Vector3(0.22f, 0.008f, 0.22f),
                    pixelRow % 3 == 0 ? fieldGridMaterial : fieldStripeMaterial,
                    false);
            }
        }

        Renderer laneMain = CreateSceneBlock(
            "LaneGlow",
            env.transform,
            new Vector3(0f, fieldOverlayY, laneCenterZ),
            new Vector3(config.laneHalfWidth * 2.35f, 0.04f, laneLength),
            laneMaterial,
            false).GetComponent<Renderer>();
        endZoneRenderers.Add(laneMain);
        endZoneRenderers.Add(CreateSceneBlock(
            "LaneEdgeLeft",
            env.transform,
            new Vector3(-config.laneHalfWidth - 0.55f, fieldOverlayY, laneCenterZ),
            new Vector3(0.12f, 0.05f, laneLength + 0.4f),
            accentPinkMaterial,
            false).GetComponent<Renderer>());
        endZoneRenderers.Add(CreateSceneBlock(
            "LaneEdgeRight",
            env.transform,
            new Vector3(config.laneHalfWidth + 0.55f, fieldOverlayY, laneCenterZ),
            new Vector3(0.12f, 0.05f, laneLength + 0.4f),
            accentPinkMaterial,
            false).GetComponent<Renderer>());
        endZoneRenderers.Add(CreateSceneBlock(
            "EndZoneGlow",
            env.transform,
            new Vector3(0f, fieldOverlayY, config.goalDistance + 2.35f),
            new Vector3(8.8f, 0.04f, 4.8f),
            accentPinkMaterial,
            false).GetComponent<Renderer>());
        endZoneRenderers.Add(CreateSceneBlock(
            "GoalReflectionBand",
            env.transform,
            new Vector3(0f, fieldOverlayY + 0.006f, config.goalDistance - 0.9f),
            new Vector3(8.2f, 0.012f, 0.52f),
            fieldMarkingMaterial,
            false).GetComponent<Renderer>());
        endZoneRenderers.Add(CreateSceneBlock(
            "GoalReflectionCore",
            env.transform,
            new Vector3(0f, fieldOverlayY + 0.009f, config.goalDistance - 0.9f),
            new Vector3(3.6f, 0.014f, 0.18f),
            goalMaterial,
            false).GetComponent<Renderer>());
        endZoneRenderers.Add(CreateSceneBlock(
            "SidelineRailLeft",
            env.transform,
            new Vector3(-10.2f, 0.2f, fieldCenterZ),
            new Vector3(0.14f, 0.18f, fieldLength - 2f),
            laneMaterial,
            false).GetComponent<Renderer>());
        endZoneRenderers.Add(CreateSceneBlock(
            "SidelineRailRight",
            env.transform,
            new Vector3(10.2f, 0.2f, fieldCenterZ),
            new Vector3(0.14f, 0.18f, fieldLength - 2f),
            accentPinkMaterial,
            false).GetComponent<Renderer>());
        endZoneRenderers.Add(CreateSceneBlock(
            "SidelineGlowLeft",
            env.transform,
            new Vector3(-9.7f, fieldOverlayY, fieldCenterZ),
            new Vector3(0.3f, 0.03f, fieldLength - 1f),
            fieldMarkingMaterial,
            false).GetComponent<Renderer>());
        endZoneRenderers.Add(CreateSceneBlock(
            "SidelineGlowRight",
            env.transform,
            new Vector3(9.7f, fieldOverlayY, fieldCenterZ),
            new Vector3(0.3f, 0.03f, fieldLength - 1f),
            laneMaterial,
            false).GetComponent<Renderer>());

        float tunnelFrameZ = laneStartZ + 1.8f;
        CreateSceneBlock(
            "TunnelFrameLeft",
            env.transform,
            new Vector3(-7.4f, 3.2f, tunnelFrameZ),
            new Vector3(0.22f, 5.8f, 0.22f),
            fieldGridMaterial,
            false);
        CreateSceneBlock(
            "TunnelFrameRight",
            env.transform,
            new Vector3(7.4f, 3.2f, tunnelFrameZ),
            new Vector3(0.22f, 5.8f, 0.22f),
            fieldGridMaterial,
            false);
        CreateSceneBlock(
            "TunnelFrameTop",
            env.transform,
            new Vector3(0f, 5.95f, tunnelFrameZ),
            new Vector3(15.2f, 0.18f, 0.18f),
            accentPinkMaterial,
            false);
        CreateSceneBlock(
            "TunnelRailLeft",
            env.transform,
            new Vector3(-6.2f, 0.48f, tunnelFrameZ + 1.8f),
            new Vector3(0.14f, 0.14f, 3.8f),
            laneMaterial,
            false);
        CreateSceneBlock(
            "TunnelRailRight",
            env.transform,
            new Vector3(6.2f, 0.48f, tunnelFrameZ + 1.8f),
            new Vector3(0.14f, 0.14f, 3.8f),
            accentPinkMaterial,
            false);

        for (int chevronIndex = 0; chevronIndex < 6; chevronIndex++)
        {
            float chevronZ = laneStartZ + 3.2f + chevronIndex * 4.3f;
            float chevronX = config.laneHalfWidth + 0.64f + chevronIndex * 0.04f;
            CreatePrimitiveVisual(
                PrimitiveType.Cube,
                "LaneChevronLeft_" + chevronIndex,
                env.transform,
                new Vector3(-chevronX, fieldOverlayY + 0.026f, chevronZ),
                Quaternion.Euler(0f, -26f, 0f),
                new Vector3(0.12f, 0.045f, 0.72f),
                fieldMarkingMaterial);
            CreatePrimitiveVisual(
                PrimitiveType.Cube,
                "LaneChevronRight_" + chevronIndex,
                env.transform,
                new Vector3(chevronX, fieldOverlayY + 0.026f, chevronZ),
                Quaternion.Euler(0f, 26f, 0f),
                new Vector3(0.12f, 0.045f, 0.72f),
                fieldMarkingMaterial);
        }

        CreateSceneBlock(
            "GrandstandLeftBase",
            env.transform,
            new Vector3(-13.4f, 1.15f, fieldCenterZ),
            new Vector3(4.2f, 2.3f, fieldLength - 3f),
            backdropMaterial,
            false);
        CreateSceneBlock(
            "GrandstandRightBase",
            env.transform,
            new Vector3(13.4f, 1.15f, fieldCenterZ),
            new Vector3(4.2f, 2.3f, fieldLength - 3f),
            backdropMaterial,
            false);

        for (int tierIndex = 0; tierIndex < 3; tierIndex++)
        {
            float tierHeight = 0.82f + tierIndex * 0.98f;
            float tierWidth = 2.6f + tierIndex * 0.95f;
            float tierX = 10.8f + tierIndex * 1.15f;
            CreateSceneBlock(
                "GrandstandLeftTier_" + tierIndex,
                env.transform,
                new Vector3(-tierX, tierHeight, fieldCenterZ),
                new Vector3(tierWidth, 0.7f, fieldLength - 4.6f),
                tierIndex % 2 == 0 ? backdropMaterial : fieldGridMaterial,
                false);
            CreateSceneBlock(
                "GrandstandRightTier_" + tierIndex,
                env.transform,
                new Vector3(tierX, tierHeight, fieldCenterZ),
                new Vector3(tierWidth, 0.7f, fieldLength - 4.6f),
                tierIndex % 2 == 0 ? backdropMaterial : fieldGridMaterial,
                false);
        }

        for (int crowdRow = 0; crowdRow < 3; crowdRow++)
        {
            for (int crowdColumn = 0; crowdColumn < 8; crowdColumn++)
            {
                float crowdZ = fieldStartZ + 5.2f + crowdColumn * ((fieldLength - 10.4f) / 7f);
                float crowdY = 1.35f + crowdRow * 0.88f;
                float crowdX = 11.2f + crowdRow * 1.05f;
                Material crowdMaterial = (crowdRow + crowdColumn) % 2 == 0 ? laneMaterial : accentPinkMaterial;
                CreateSceneBlock(
                    "CrowdBlockLeft_" + crowdRow + "_" + crowdColumn,
                    env.transform,
                    new Vector3(-crowdX, crowdY, crowdZ),
                    new Vector3(0.42f, 0.42f, 1.1f),
                    crowdMaterial,
                    false);
                CreateSceneBlock(
                    "CrowdBlockRight_" + crowdRow + "_" + crowdColumn,
                    env.transform,
                    new Vector3(crowdX, crowdY, crowdZ),
                    new Vector3(0.42f, 0.42f, 1.1f),
                    crowdMaterial,
                    false);
            }
        }

        CreateSceneBlock(
            "RoofBeamLeft",
            env.transform,
            new Vector3(-8.9f, 7.2f, fieldCenterZ),
            new Vector3(0.18f, 0.18f, fieldLength - 5f),
            fieldGridMaterial,
            false);
        CreateSceneBlock(
            "RoofBeamRight",
            env.transform,
            new Vector3(8.9f, 7.2f, fieldCenterZ),
            new Vector3(0.18f, 0.18f, fieldLength - 5f),
            fieldGridMaterial,
            false);
        for (int trussIndex = 0; trussIndex < 7; trussIndex++)
        {
            float trussZ = fieldStartZ + 4.5f + trussIndex * ((fieldLength - 9f) / 6f);
            CreateSceneBlock(
                "RoofTruss_" + trussIndex,
                env.transform,
                new Vector3(0f, 7.35f, trussZ),
                new Vector3(17.6f, 0.12f, 0.12f),
                accentPinkMaterial,
                false);
        }

        int markerStartYard = Mathf.Max(10, Mathf.Min(config.startingYardLine, 20));
        int markerEndYard = Mathf.Max(config.movingGoalFullChallengeYardLine + 5, 60);
        for (int yardLine = markerStartYard; yardLine <= markerEndYard; yardLine += 5)
        {
            float lineZ = -(yardLine - config.startingYardLine) * Mathf.Max(0.01f, config.worldUnitsPerYard);
            float lineThickness = yardLine % 10 == 0 ? 0.16f : 0.12f;
            CreateGlowTube(
                "YardLine_" + yardLine,
                env.transform,
                new Vector3(0f, fieldLineY, lineZ),
                new Vector3(18f, 0.06f, lineThickness),
                fieldMarkingMaterial,
                fieldGridMaterial);

            CreateGlowTube(
                "HashLeftInner_" + yardLine,
                env.transform,
                new Vector3(-0.95f, fieldLineY + 0.003f, lineZ),
                new Vector3(0.2f, 0.055f, lineThickness),
                fieldMarkingMaterial,
                fieldGridMaterial);
            CreateGlowTube(
                "HashRightInner_" + yardLine,
                env.transform,
                new Vector3(0.95f, fieldLineY + 0.003f, lineZ),
                new Vector3(0.2f, 0.055f, lineThickness),
                fieldMarkingMaterial,
                fieldGridMaterial);
            CreateGlowTube(
                "HashLeftOuter_" + yardLine,
                env.transform,
                new Vector3(-6.1f, fieldLineY + 0.003f, lineZ),
                new Vector3(0.2f, 0.055f, lineThickness),
                fieldMarkingMaterial,
                fieldGridMaterial);
            CreateGlowTube(
                "HashRightOuter_" + yardLine,
                env.transform,
                new Vector3(6.1f, fieldLineY + 0.003f, lineZ),
                new Vector3(0.2f, 0.055f, lineThickness),
                fieldMarkingMaterial,
                fieldGridMaterial);
        }

        CreateSceneBlock(
            "BackdropWall",
            env.transform,
            new Vector3(0f, 5.5f, config.goalDistance + 13f),
            new Vector3(30f, 11f, 0.35f),
            backdropMaterial,
            false);
        CreateSceneBlock(
            "GoalPortalBackplate",
            env.transform,
            new Vector3(0f, 4.55f, config.goalDistance + 1.15f),
            new Vector3(10.4f, 6.4f, 0.1f),
            fieldGridMaterial,
            false);
        CreateGlowTube(
            "GoalPortalTop",
            env.transform,
            new Vector3(0f, 7.35f, config.goalDistance + 0.86f),
            new Vector3(8.9f, 0.1f, 0.16f),
            fieldMarkingMaterial,
            goalMaterial);
        CreateGlowTube(
            "GoalPortalLeft",
            env.transform,
            new Vector3(-4.3f, 4.3f, config.goalDistance + 0.86f),
            new Vector3(0.12f, 5.8f, 0.16f),
            fieldMarkingMaterial,
            goalMaterial);
        CreateGlowTube(
            "GoalPortalRight",
            env.transform,
            new Vector3(4.3f, 4.3f, config.goalDistance + 0.86f),
            new Vector3(0.12f, 5.8f, 0.16f),
            fieldMarkingMaterial,
            goalMaterial);
        CreateSceneBlock(
            "GoalTargetRunway",
            env.transform,
            new Vector3(0f, fieldOverlayY + 0.008f, config.goalDistance - 2.8f),
            new Vector3(5.8f, 0.012f, 3.6f),
            fieldMarkingMaterial,
            false);
        CreateSceneBlock(
            "SkyLine",
            env.transform,
            new Vector3(0f, 0.1f, config.goalDistance + 8f),
            new Vector3(26f, 0.04f, 0.3f),
            accentPinkMaterial,
            false);
        CreateSceneBlock(
            "FutureBannerLeft",
            env.transform,
            new Vector3(-7.8f, 2.4f, config.goalDistance + 6.2f),
            new Vector3(2.2f, 1.1f, 0.08f),
            laneMaterial,
            false);
        CreateSceneBlock(
            "FutureBannerRight",
            env.transform,
            new Vector3(7.8f, 2.4f, config.goalDistance + 6.2f),
            new Vector3(2.2f, 1.1f, 0.08f),
            accentPinkMaterial,
            false);

        CreateEndZoneWord(
            "MUSTANGS",
            env.transform,
            new Vector3(0f, fieldLineY + 0.018f, config.goalDistance + 2.2f),
            1.18f,
            1.72f,
            0.16f,
            0.22f,
            fieldMarkingMaterial,
            accentPinkMaterial);

        crowdLights.Add(CreateSceneLight(
            "CrowdLightLeft",
            env.transform,
            new Vector3(-9f, 4.2f, config.goalDistance + 6f),
            new Color(0.18f, 0.96f, 1f),
            18f,
            2.6f));
        crowdLights.Add(CreateSceneLight(
            "CrowdLightRight",
            env.transform,
            new Vector3(9f, 4.2f, config.goalDistance + 6f),
            new Color(1f, 0.42f, 0.84f),
            18f,
            2.3f));
        crowdLights.Add(CreateSceneLight(
            "GoalLightLeft",
            env.transform,
            new Vector3(-3.5f, 5.4f, config.goalDistance + 0.4f),
            new Color(0.18f, 0.96f, 1f),
            16f,
            2f));
        crowdLights.Add(CreateSceneLight(
            "GoalLightRight",
            env.transform,
            new Vector3(3.5f, 5.4f, config.goalDistance + 0.4f),
            new Color(1f, 0.42f, 0.84f),
            16f,
            2.8f));
        crowdLights.Add(CreateSceneLight(
            "GoalPortalCore",
            env.transform,
            new Vector3(0f, 4.55f, config.goalDistance + 0.78f),
            new Color(1f, 0.92f, 0.38f),
            18f,
            3.1f));
        crowdLights.Add(CreateSceneLight(
            "GoalTurfGlowLeft",
            env.transform,
            new Vector3(-2.6f, 0.42f, config.goalDistance - 0.85f),
            new Color(0.34f, 1f, 0.58f),
            9f,
            3.2f));
        crowdLights.Add(CreateSceneLight(
            "GoalTurfGlowRight",
            env.transform,
            new Vector3(2.6f, 0.42f, config.goalDistance - 0.85f),
            new Color(1f, 0.42f, 0.9f),
            9f,
            3.2f));
        crowdLights.Add(CreateSceneLight(
            "EndZoneWordLightLeft",
            env.transform,
            new Vector3(-4.2f, 0.85f, config.goalDistance + 2.3f),
            new Color(0.34f, 1f, 0.58f),
            12f,
            3.4f));
        crowdLights.Add(CreateSceneLight(
            "EndZoneWordLightRight",
            env.transform,
            new Vector3(4.2f, 0.85f, config.goalDistance + 2.3f),
            new Color(1f, 0.42f, 0.9f),
            12f,
            3.1f));

        GameObject defenseLine = new GameObject("DefenseLine");
        defenseLine.transform.SetParent(env.transform, false);
        defenseLine.transform.localPosition = new Vector3(0f, 0f, config.defenseLineForwardOffset);

        refs.defenseController = defenseLine.AddComponent<FieldGoalDefenseController>();
        refs.defenseController.config = config;
        refs.defenseController.defenseRoot = defenseLine.transform;

        endZoneRenderers.Add(CreateSceneBlock(
            "DefenseShadowBand",
            env.transform,
            new Vector3(0f, fieldOverlayY + 0.003f, config.defenseLineForwardOffset),
            new Vector3(8.6f, 0.012f, 0.36f),
            fieldGridMaterial,
            false).GetComponent<Renderer>());

        for (int blockerIndex = 0; blockerIndex < 5; blockerIndex++)
        {
            Material blockerAccent = blockerIndex % 2 == 0 ? laneMaterial : accentPinkMaterial;
            defenseBlockers.Add(CreateDefenseBlockerStub(
                defenseLine.transform,
                "DefenseBlocker_" + (blockerIndex + 1),
                refereeDarkMaterial,
                fieldGridMaterial,
                blockerAccent));
        }

        GameObject goalAssembly = new GameObject("GoalAssembly");
        goalAssembly.transform.SetParent(env.transform, false);
        goalAssembly.transform.localPosition = new Vector3(0f, 0f, config.goalDistance);

        refs.movingGoalController = goalAssembly.AddComponent<FieldGoalMovingGoalController>();
        refs.movingGoalController.goalRoot = goalAssembly.transform;
        refs.movingGoalController.ApplyConfig(config);

        goalRenderers.Add(CreateSceneBlock(
            "GoalSupport",
            goalAssembly.transform,
            new Vector3(0f, 1.55f, 0.32f),
            new Vector3(0.22f, 3.1f, 0.22f),
            goalMaterial,
            true).GetComponent<Renderer>());
        goalRenderers.Add(CreateSceneBlock(
            "Crossbar",
            goalAssembly.transform,
            new Vector3(0f, config.crossbarHeight, 0f),
            new Vector3(config.uprightInnerHalfWidth * 2f + 0.24f, 0.16f, 0.16f),
            goalMaterial,
            true).GetComponent<Renderer>());
        goalRenderers.Add(CreateSceneBlock(
            "LeftUpright",
            goalAssembly.transform,
            new Vector3(-config.uprightInnerHalfWidth, config.crossbarHeight + 2.4f, 0f),
            new Vector3(0.16f, 4.8f, 0.16f),
            goalMaterial,
            true).GetComponent<Renderer>());
        goalRenderers.Add(CreateSceneBlock(
            "RightUpright",
            goalAssembly.transform,
            new Vector3(config.uprightInnerHalfWidth, config.crossbarHeight + 2.4f, 0f),
            new Vector3(0.16f, 4.8f, 0.16f),
            goalMaterial,
            true).GetComponent<Renderer>());
        goalRenderers.Add(CreateSceneBlock(
            "GoalHalo",
            goalAssembly.transform,
            new Vector3(0f, config.crossbarHeight + 3.2f, -0.2f),
            new Vector3(config.uprightInnerHalfWidth * 2.5f, 0.08f, 0.08f),
            accentPinkMaterial,
            false).GetComponent<Renderer>());
        goalRenderers.Add(CreateSceneBlock(
            "CrossbarAura",
            goalAssembly.transform,
            new Vector3(0f, config.crossbarHeight, -0.08f),
            new Vector3(config.uprightInnerHalfWidth * 2f + 1f, 0.22f, 0.42f),
            fieldMarkingMaterial,
            false).GetComponent<Renderer>());
        goalRenderers.Add(CreateSceneBlock(
            "LeftUprightAura",
            goalAssembly.transform,
            new Vector3(-config.uprightInnerHalfWidth, config.crossbarHeight + 2.4f, -0.08f),
            new Vector3(0.34f, 5.2f, 0.34f),
            fieldMarkingMaterial,
            false).GetComponent<Renderer>());
        goalRenderers.Add(CreateSceneBlock(
            "RightUprightAura",
            goalAssembly.transform,
            new Vector3(config.uprightInnerHalfWidth, config.crossbarHeight + 2.4f, -0.08f),
            new Vector3(0.34f, 5.2f, 0.34f),
            fieldMarkingMaterial,
            false).GetComponent<Renderer>());

        crowdLights.Add(CreateSceneLight(
            "CrossbarBloomLight",
            goalAssembly.transform,
            new Vector3(0f, config.crossbarHeight + 0.22f, -0.18f),
            new Color(1f, 0.94f, 0.34f),
            10f,
            3.6f));
        crowdLights.Add(CreateSceneLight(
            "LeftPostBloomLight",
            goalAssembly.transform,
            new Vector3(-config.uprightInnerHalfWidth, config.crossbarHeight + 2.4f, -0.16f),
            new Color(0.94f, 1f, 0.78f),
            7f,
            2.8f));
        crowdLights.Add(CreateSceneLight(
            "RightPostBloomLight",
            goalAssembly.transform,
            new Vector3(config.uprightInnerHalfWidth, config.crossbarHeight + 2.4f, -0.16f),
            new Color(0.94f, 1f, 0.78f),
            7f,
            2.8f));

        GameObject goalPlane = new GameObject("GoalPlane");
        goalPlane.transform.SetParent(goalAssembly.transform, false);
        goalPlane.transform.localPosition = Vector3.zero;
        refs.goalDetector = goalPlane.AddComponent<GoalDetector>();
        refs.goalDetector.ApplyConfig(config);

        refereeStubs.Add(CreateRefereeStub(
            goalAssembly.transform,
            "LeftReferee",
            new Vector3(-5.35f, 0f, 1.6f),
            refereeDarkMaterial,
            refereeLightMaterial,
            laneMaterial));
        refereeStubs.Add(CreateRefereeStub(
            goalAssembly.transform,
            "RightReferee",
            new Vector3(5.35f, 0f, 1.6f),
            refereeDarkMaterial,
            refereeLightMaterial,
            accentPinkMaterial));

        refs.goalRenderers = goalRenderers.ToArray();
        refs.endZoneRenderers = endZoneRenderers.ToArray();
        refs.crowdLights = crowdLights.ToArray();
        refs.refereeStubs = refereeStubs.ToArray();
        refs.defenseController.blockerRoots = new Transform[defenseBlockers.Count];
        refs.defenseController.bodyVisuals = new Transform[defenseBlockers.Count];
        refs.defenseController.leftArmPivots = new Transform[defenseBlockers.Count];
        refs.defenseController.rightArmPivots = new Transform[defenseBlockers.Count];
        refs.defenseController.hitboxTransforms = new Transform[defenseBlockers.Count];
        refs.defenseController.hitboxes = new FieldGoalDefenseHitbox[defenseBlockers.Count];
        for (int i = 0; i < defenseBlockers.Count; i++)
        {
            refs.defenseController.blockerRoots[i] = defenseBlockers[i].root;
            refs.defenseController.bodyVisuals[i] = defenseBlockers[i].bodyVisual;
            refs.defenseController.leftArmPivots[i] = defenseBlockers[i].leftArmPivot;
            refs.defenseController.rightArmPivots[i] = defenseBlockers[i].rightArmPivot;
            refs.defenseController.hitboxTransforms[i] = defenseBlockers[i].hitbox;
            refs.defenseController.hitboxes[i] = defenseBlockers[i].hitboxComponent;
        }

        refs.defenseController.ApplyConfig(config);
        return refs;
    }

    private static void CreateGlowTube(
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material coreMaterial,
        Material glowMaterial)
    {
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            name,
            parent,
            localPosition + Vector3.up * (localScale.y * 0.5f),
            localScale,
            coreMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            name + "Glow",
            parent,
            localPosition + Vector3.up * Mathf.Max(0.01f, localScale.y * 0.18f),
            new Vector3(localScale.x + 0.12f, localScale.y * 0.36f, localScale.z + 0.12f),
            glowMaterial);
    }

    private static void CreateEndZoneWord(
        string text,
        Transform parent,
        Vector3 localPosition,
        float letterWidth,
        float letterHeight,
        float tubeThickness,
        float letterSpacing,
        Material coreMaterial,
        Material glowMaterial)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        GameObject wordRoot = new GameObject("EndZoneWord_" + text);
        wordRoot.transform.SetParent(parent, false);
        wordRoot.transform.localPosition = localPosition;

        float totalWidth = text.Length * letterWidth + Mathf.Max(0, text.Length - 1) * letterSpacing;
        float cursorX = -totalWidth * 0.5f + letterWidth * 0.5f;
        for (int i = 0; i < text.Length; i++)
        {
            CreateTubeLetter(
                text[i],
                wordRoot.transform,
                new Vector3(cursorX, 0f, 0f),
                letterWidth,
                letterHeight,
                tubeThickness,
                coreMaterial,
                glowMaterial);
            cursorX += letterWidth + letterSpacing;
        }
    }

    private static void CreateTubeLetter(
        char character,
        Transform parent,
        Vector3 localPosition,
        float letterWidth,
        float letterHeight,
        float tubeThickness,
        Material coreMaterial,
        Material glowMaterial)
    {
        GameObject letterRoot = new GameObject("Letter_" + character);
        letterRoot.transform.SetParent(parent, false);
        letterRoot.transform.localPosition = localPosition;

        switch (char.ToUpperInvariant(character))
        {
            case 'M':
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.5f, -0.5f), new Vector2(-0.5f, 0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.5f, 0.5f), new Vector2(-0.05f, -0.02f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.05f, -0.02f), new Vector2(0.5f, 0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, -0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                break;
            case 'U':
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.5f, 0.5f), new Vector2(-0.5f, -0.32f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, -0.32f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                break;
            case 'S':
                CreateTubeSegment(letterRoot.transform, new Vector2(0.45f, 0.5f), new Vector2(-0.45f, 0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.5f, 0.5f), new Vector2(-0.5f, 0f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.45f, 0f), new Vector2(0.45f, 0f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, -0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(0.45f, -0.5f), new Vector2(-0.45f, -0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                break;
            case 'T':
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.5f, 0.5f), new Vector2(0.5f, 0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(0f, 0.5f), new Vector2(0f, -0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                break;
            case 'A':
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.5f, -0.5f), new Vector2(-0.05f, 0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(0.5f, -0.5f), new Vector2(0.05f, 0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.28f, 0f), new Vector2(0.28f, 0f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                break;
            case 'N':
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.5f, -0.5f), new Vector2(-0.5f, 0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.5f, 0.5f), new Vector2(0.5f, -0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(0.5f, -0.5f), new Vector2(0.5f, 0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                break;
            case 'G':
                CreateTubeSegment(letterRoot.transform, new Vector2(0.45f, 0.5f), new Vector2(-0.45f, 0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.5f, 0.5f), new Vector2(-0.5f, -0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.45f, -0.5f), new Vector2(0.45f, -0.5f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(0.08f, -0.04f), new Vector2(0.45f, -0.04f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                CreateTubeSegment(letterRoot.transform, new Vector2(0.5f, -0.04f), new Vector2(0.5f, 0.22f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                break;
            default:
                CreateTubeSegment(letterRoot.transform, new Vector2(-0.5f, 0f), new Vector2(0.5f, 0f), letterWidth, letterHeight, tubeThickness, coreMaterial, glowMaterial);
                break;
        }
    }

    private static void CreateTubeSegment(
        Transform parent,
        Vector2 from,
        Vector2 to,
        float letterWidth,
        float letterHeight,
        float tubeThickness,
        Material coreMaterial,
        Material glowMaterial)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        if (length <= 0.0001f)
        {
            return;
        }

        Vector2 midpoint = (from + to) * 0.5f;
        float angle = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
        Vector3 segmentPosition = new Vector3(midpoint.x * letterWidth, 0f, midpoint.y * letterHeight);
        Quaternion segmentRotation = Quaternion.Euler(0f, angle, 0f);

        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "Segment",
            parent,
            segmentPosition + Vector3.up * tubeThickness * 0.5f,
            segmentRotation,
            new Vector3(tubeThickness, tubeThickness, length * letterHeight),
            coreMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "SegmentGlow",
            parent,
            segmentPosition + Vector3.up * tubeThickness * 0.18f,
            segmentRotation,
            new Vector3(tubeThickness * 1.4f, tubeThickness * 0.34f, length * letterHeight + tubeThickness * 0.65f),
            glowMaterial);
    }

    private static DefenseBlockerStubRefs CreateDefenseBlockerStub(
        Transform parent,
        string name,
        Material baseMaterial,
        Material coreGlowMaterial,
        Material accentMaterial)
    {
        DefenseBlockerStubRefs refs = new DefenseBlockerStubRefs();

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;
        refs.root = root.transform;

        Rigidbody body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "BarPad",
            root.transform,
            new Vector3(0f, 0.03f, 0f),
            new Vector3(0.82f, 0.05f, 0.42f),
            baseMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "BarUnderglow",
            root.transform,
            new Vector3(0f, 0.012f, 0f),
            new Vector3(1.02f, 0.018f, 0.58f),
            coreGlowMaterial);

        GameObject bodyRoot = new GameObject("ArcadeBarVisual");
        bodyRoot.transform.SetParent(root.transform, false);
        bodyRoot.transform.localPosition = new Vector3(0f, 1.08f, 0f);
        bodyRoot.transform.localScale = new Vector3(1.18f, 0.18f, 0.48f);
        refs.bodyVisual = bodyRoot.transform;

        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "BarGlow",
            bodyRoot.transform,
            Vector3.zero,
            new Vector3(1.12f, 1.5f, 1.2f),
            coreGlowMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "BarCore",
            bodyRoot.transform,
            Vector3.zero,
            Vector3.one,
            baseMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "BarAccent",
            bodyRoot.transform,
            new Vector3(0f, 0f, 0.56f),
            new Vector3(0.86f, 0.26f, 0.08f),
            accentMaterial);

        refs.leftArmPivot = new GameObject("ArcadeLeftPulsePivot").transform;
        refs.leftArmPivot.SetParent(bodyRoot.transform, false);
        refs.leftArmPivot.localPosition = new Vector3(-0.68f, 0f, 0f);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "LeftPulse",
            refs.leftArmPivot,
            Vector3.zero,
            new Vector3(0.14f, 0.22f, 0.14f),
            coreGlowMaterial);

        refs.rightArmPivot = new GameObject("ArcadeRightPulsePivot").transform;
        refs.rightArmPivot.SetParent(bodyRoot.transform, false);
        refs.rightArmPivot.localPosition = new Vector3(0.68f, 0f, 0f);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "RightPulse",
            refs.rightArmPivot,
            Vector3.zero,
            new Vector3(0.14f, 0.22f, 0.14f),
            coreGlowMaterial);

        GameObject hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(root.transform, false);
        hitbox.transform.localPosition = new Vector3(0f, 1.08f, 0f);
        hitbox.transform.localScale = new Vector3(1.18f, 0.32f, 0.56f);
        BoxCollider hitboxCollider = hitbox.AddComponent<BoxCollider>();
        hitboxCollider.size = Vector3.one;
        refs.hitbox = hitbox.transform;
        refs.hitboxComponent = hitbox.AddComponent<FieldGoalDefenseHitbox>();
        refs.hitboxComponent.blockerLabel = "TIMING BAR";

        return refs;
    }

    private static RefereeStubRefs CreateRefereeStub(
        Transform parent,
        string name,
        Vector3 localPosition,
        Material darkMaterial,
        Material lightMaterial,
        Material accentMaterial)
    {
        RefereeStubRefs refs = new RefereeStubRefs();

        GameObject anchor = new GameObject(name + "Anchor");
        anchor.transform.SetParent(parent, false);
        anchor.transform.localPosition = localPosition;
        refs.anchor = anchor.transform;

        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "Stand",
            anchor.transform,
            new Vector3(0f, 0.15f, 0f),
            new Vector3(1.2f, 0.28f, 1.2f),
            accentMaterial);

        GameObject root = new GameObject(name);
        root.transform.SetParent(anchor.transform, false);
        root.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        refs.root = root.transform;

        CreatePrimitiveVisual(
            PrimitiveType.Capsule,
            "Body",
            root.transform,
            new Vector3(0f, 0.78f, 0f),
            new Vector3(0.35f, 0.62f, 0.24f),
            darkMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "ShirtStripe",
            root.transform,
            new Vector3(0f, 0.84f, 0.13f),
            new Vector3(0.5f, 1.05f, 0.05f),
            lightMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "LegLeft",
            root.transform,
            new Vector3(-0.12f, 0.3f, 0f),
            new Vector3(0.1f, 0.58f, 0.1f),
            darkMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "LegRight",
            root.transform,
            new Vector3(0.12f, 0.3f, 0f),
            new Vector3(0.1f, 0.58f, 0.1f),
            darkMaterial);
        CreatePrimitiveVisual(
            PrimitiveType.Sphere,
            "Head",
            root.transform,
            new Vector3(0f, 1.45f, 0f),
            new Vector3(0.26f, 0.28f, 0.24f),
            lightMaterial);

        refs.leftArmPivot = new GameObject("LeftArmPivot").transform;
        refs.leftArmPivot.SetParent(root.transform, false);
        refs.leftArmPivot.localPosition = new Vector3(-0.24f, 1.02f, 0f);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "LeftArm",
            refs.leftArmPivot,
            new Vector3(-0.16f, -0.2f, 0f),
            new Vector3(0.11f, 0.46f, 0.11f),
            lightMaterial);

        refs.rightArmPivot = new GameObject("RightArmPivot").transform;
        refs.rightArmPivot.SetParent(root.transform, false);
        refs.rightArmPivot.localPosition = new Vector3(0.24f, 1.02f, 0f);
        CreatePrimitiveVisual(
            PrimitiveType.Cube,
            "RightArm",
            refs.rightArmPivot,
            new Vector3(0.16f, -0.2f, 0f),
            new Vector3(0.11f, 0.46f, 0.11f),
            lightMaterial);

        return refs;
    }

    private static UiRefs BuildUi()
    {
        UiRefs refs = new UiRefs();

        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2560f, 1440f);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();

        GameObject hudRoot = new GameObject("HUDRoot", typeof(RectTransform));
        hudRoot.transform.SetParent(canvasGo.transform, false);
        StretchRect(hudRoot.GetComponent<RectTransform>());

        refs.ui = hudRoot.AddComponent<NeonFieldGoalUI>();

        Color cyan = new Color(0.22f, 0.95f, 1f, 1f);
        Color pink = new Color(1f, 0.34f, 0.88f, 1f);
        Color panelColor = new Color(0.03f, 0.06f, 0.12f, 0.84f);

        CreateUiPanel("GameplayTopRibbon", hudRoot.transform, new Color(0.03f, 0.06f, 0.12f, 0.72f));
        SetRect(hudRoot.transform.Find("GameplayTopRibbon").GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1560f, 110f), new Vector2(0f, -112f));

        GameObject titlePanel = CreateUiPanel("TitlePanel", hudRoot.transform, new Color(0.04f, 0.08f, 0.14f, 0.82f));
        SetRect(titlePanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(440f, 68f), new Vector2(0f, -58f));
        TextMeshProUGUI titleText = CreateTmp("TitleText", titlePanel.transform, "NEON FIELDGOAL", 28, TextAlignmentOptions.Center);
        StretchRect(titleText.rectTransform);
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(1f, 0.94f, 0.34f, 1f);
        titleText.characterSpacing = 3f;

        GameObject goalPanel = CreateUiPanel("GoalPanel", hudRoot.transform, panelColor);
        SetRect(goalPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, 152f), new Vector2(216f, -110f));
        refs.ui.goalsText = CreateTmp("GoalsText", goalPanel.transform, "SCORE 0", 44, TextAlignmentOptions.Center);
        SetRect(refs.ui.goalsText.rectTransform, new Vector2(0.5f, 0.7f), new Vector2(0.5f, 0.7f), new Vector2(300f, 48f), new Vector2(0f, 6f));
        refs.ui.goalsText.fontStyle = FontStyles.Bold;
        refs.ui.goalsText.color = pink;
        refs.ui.runStatsText = CreateTmp("RunStatsText", goalPanel.transform, "GOALS 0  |  LONGEST --", 20, TextAlignmentOptions.Center);
        SetRect(refs.ui.runStatsText.rectTransform, new Vector2(0.5f, 0.33f), new Vector2(0.5f, 0.33f), new Vector2(320f, 28f), Vector2.zero);
        refs.ui.runStatsText.color = new Color(0.78f, 0.95f, 1f, 0.92f);
        refs.ui.multiplierText = CreateTmp("MultiplierText", goalPanel.transform, "x1 READY", 22, TextAlignmentOptions.Center);
        SetRect(refs.ui.multiplierText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(280f, 32f), new Vector2(0f, 20f));
        refs.ui.multiplierText.color = new Color(1f, 0.94f, 0.34f, 0.96f);

        GameObject timerPanel = CreateUiPanel("TimerPanel", hudRoot.transform, panelColor);
        SetRect(timerPanel.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(300f, 138f), new Vector2(-216f, -102f));
        refs.ui.timerText = CreateTmp("TimerText", timerPanel.transform, "01:00", 42, TextAlignmentOptions.Center);
        SetRect(refs.ui.timerText.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), new Vector2(240f, 48f), new Vector2(0f, 6f));
        refs.ui.timerText.fontStyle = FontStyles.Bold;
        refs.ui.timerText.color = cyan;
        refs.ui.windText = CreateTmp("WindText", timerPanel.transform, "WIND CALM", 18, TextAlignmentOptions.Center);
        SetRect(refs.ui.windText.rectTransform, new Vector2(0.5f, 0.22f), new Vector2(0.5f, 0.22f), new Vector2(240f, 26f), Vector2.zero);
        refs.ui.windText.color = new Color(0.66f, 0.88f, 0.92f, 0.8f);

        GameObject modePlate = CreateUiPanel("GameplayKickStatePlate", hudRoot.transform, new Color(0.02f, 0.09f, 0.08f, 0.72f));
        SetRect(modePlate.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(940f, 66f), new Vector2(0f, -122f));

        refs.ui.modeText = CreateTmp("ModeText", hudRoot.transform, "PICK A MODE", 20, TextAlignmentOptions.Center);
        SetRect(refs.ui.modeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(560f, 32f), new Vector2(0f, -102f));
        refs.ui.modeText.color = new Color(1f, 0.94f, 0.34f, 0.92f);
        refs.ui.modeText.fontStyle = FontStyles.Bold;

        refs.ui.kickStateText = CreateTmp("KickStateText", hudRoot.transform, "20 YD  |  3 PTS  |  SET UPRIGHTS", 18, TextAlignmentOptions.Center);
        SetRect(refs.ui.kickStateText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(860f, 28f), new Vector2(0f, -136f));
        refs.ui.kickStateText.color = new Color(0.8f, 0.98f, 0.72f, 0.95f);

        GameObject statusPanel = CreateUiPanel("GameplayBottomPlate", hudRoot.transform, new Color(0.03f, 0.06f, 0.12f, 0.76f));
        SetRect(statusPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1320f, 118f), new Vector2(0f, 294f));
        GameObject statusGlow = CreateUiPanel("GameplayBottomGlow", hudRoot.transform, new Color(0.22f, 0.95f, 1f, 0.12f));
        SetRect(statusGlow.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(980f, 28f), new Vector2(0f, 334f));

        refs.ui.statusText = CreateTmp("StatusText", hudRoot.transform, "Line it up and let it fly.", 34, TextAlignmentOptions.Center);
        SetRect(refs.ui.statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1180f, 48f), new Vector2(0f, 316f));
        refs.ui.statusText.color = new Color(0.98f, 0.90f, 0.98f, 1f);

        refs.ui.hintText = CreateTmp("HintText", hudRoot.transform, "Left thumb moves. Right thumb swipes up to kick.", 24, TextAlignmentOptions.Center);
        SetRect(refs.ui.hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1260f, 34f), new Vector2(0f, 274f));
        refs.ui.hintText.color = new Color(0.76f, 0.92f, 1f, 0.94f);

        GameObject controlsRoot = new GameObject("ControlsRoot", typeof(RectTransform), typeof(CanvasGroup));
        controlsRoot.transform.SetParent(hudRoot.transform, false);
        StretchRect(controlsRoot.GetComponent<RectTransform>());
        refs.ui.controlsCanvasGroup = controlsRoot.GetComponent<CanvasGroup>();

        GameObject movePanel = CreateUiPanel("MovePanel", controlsRoot.transform, panelColor);
        SetRect(movePanel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(620f, 240f), new Vector2(360f, 150f));
        TextMeshProUGUI moveLabel = CreateTmp("MoveLabel", movePanel.transform, "MOVE", 28, TextAlignmentOptions.Top);
        SetRect(moveLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(300f, 40f), new Vector2(0f, -24f));
        moveLabel.color = cyan;
        moveLabel.fontStyle = FontStyles.Bold;

        Button moveLeftButton = CreateButton("MoveLeftButton", movePanel.transform, "LEFT", new Vector2(220f, 110f), new Vector2(-120f, -28f));
        Button moveRightButton = CreateButton("MoveRightButton", movePanel.transform, "RIGHT", new Vector2(220f, 110f), new Vector2(120f, -28f));
        refs.ui.moveLeftButton = moveLeftButton.gameObject.AddComponent<FieldGoalHoldButton>();
        refs.ui.moveLeftButton.targetGraphic = moveLeftButton.GetComponent<Image>();
        refs.ui.moveRightButton = moveRightButton.gameObject.AddComponent<FieldGoalHoldButton>();
        refs.ui.moveRightButton.targetGraphic = moveRightButton.GetComponent<Image>();

        GameObject kickPad = CreateUiPanel("KickPad", controlsRoot.transform, new Color(0.04f, 0.08f, 0.16f, 0.9f));
        SetRect(kickPad.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(500f, 320f), new Vector2(-330f, 170f));
        TextMeshProUGUI kickLabel = CreateTmp("KickLabel", kickPad.transform, "KICK PAD", 30, TextAlignmentOptions.Top);
        SetRect(kickLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(360f, 40f), new Vector2(0f, -24f));
        kickLabel.color = pink;
        kickLabel.fontStyle = FontStyles.Bold;

        Image powerFill = CreateImage("PowerFill", kickPad.transform, new Color(0.18f, 0.96f, 1f, 0.22f));
        StretchRect(powerFill.rectTransform);
        powerFill.rectTransform.offsetMin = new Vector2(18f, 18f);
        powerFill.rectTransform.offsetMax = new Vector2(-18f, -18f);
        powerFill.type = Image.Type.Filled;
        powerFill.fillMethod = Image.FillMethod.Vertical;
        powerFill.fillOrigin = 0;
        powerFill.fillAmount = 0f;

        for (int i = 1; i <= 3; i++)
        {
            Image marker = CreateImage("KickMarker_" + i, kickPad.transform, new Color(1f, 1f, 1f, 0.08f));
            SetRect(marker.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 2f), new Vector2(0f, 20f + i * 68f));
            marker.rectTransform.offsetMin = new Vector2(20f, marker.rectTransform.offsetMin.y);
            marker.rectTransform.offsetMax = new Vector2(-20f, marker.rectTransform.offsetMax.y);
        }

        Image kickReticle = CreateImage("KickReticle", kickPad.transform, new Color(1f, 0.96f, 1f, 0.96f));
        SetRect(kickReticle.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(44f, 44f), Vector2.zero);

        refs.kickInput = kickPad.AddComponent<KickInputController>();
        refs.kickInput.trackingRect = kickPad.GetComponent<RectTransform>();
        refs.kickInput.maxVerticalPixels = 290f;
        refs.kickInput.maxHorizontalPixels = 190f;

        refs.ui.kickInputController = refs.kickInput;
        refs.ui.kickPowerFill = powerFill;
        refs.ui.kickReticle = kickReticle.rectTransform;

        GameObject announce = CreateUiPanel("AnnouncementPanel", hudRoot.transform, new Color(0.04f, 0.08f, 0.16f, 0.92f));
        announce.AddComponent<CanvasGroup>();
        SetRect(announce.GetComponent<RectTransform>(), new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), new Vector2(780f, 220f), Vector2.zero);
        refs.ui.announcementGroup = announce.GetComponent<CanvasGroup>();
        refs.ui.announcementBackdrop = announce.GetComponent<Image>();
        refs.ui.announcementTitleText = CreateTmp("AnnouncementTitle", announce.transform, "IT'S GOOD", 78, TextAlignmentOptions.Center);
        SetRect(refs.ui.announcementTitleText.rectTransform, new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.66f), new Vector2(680f, 88f), Vector2.zero);
        refs.ui.announcementTitleText.fontStyle = FontStyles.Bold;
        refs.ui.announcementSubtitleText = CreateTmp("AnnouncementSubtitle", announce.transform, "", 30, TextAlignmentOptions.Center);
        SetRect(refs.ui.announcementSubtitleText.rectTransform, new Vector2(0.5f, 0.3f), new Vector2(0.5f, 0.3f), new Vector2(680f, 50f), Vector2.zero);
        refs.ui.announcementSubtitleText.color = new Color(0.94f, 0.94f, 1f, 0.92f);

        GameObject results = CreateUiPanel("ResultsPanel", hudRoot.transform, new Color(0.03f, 0.06f, 0.12f, 0.96f));
        SetRect(results.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(980f, 520f), Vector2.zero);
        refs.ui.resultsPanel = results;
        refs.ui.resultsTitleText = CreateTmp("ResultsTitle", results.transform, "TIME'S UP", 80, TextAlignmentOptions.Center);
        SetRect(refs.ui.resultsTitleText.rectTransform, new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), new Vector2(760f, 90f), Vector2.zero);
        refs.ui.resultsTitleText.fontStyle = FontStyles.Bold;
        refs.ui.resultsTitleText.color = cyan;
        refs.ui.resultsBodyText = CreateTmp("ResultsBody", results.transform, "", 36, TextAlignmentOptions.Center);
        SetRect(refs.ui.resultsBodyText.rectTransform, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(800f, 220f), Vector2.zero);
        refs.ui.resultsBodyText.color = new Color(1f, 0.92f, 0.98f, 1f);
        refs.ui.restartButton = CreateButton("RestartButton", results.transform, "RESTART", new Vector2(280f, 96f), new Vector2(-170f, -160f));
        refs.ui.modeSelectButton = CreateButton("ModeSelectButton", results.transform, "MODES", new Vector2(280f, 96f), new Vector2(170f, -160f));

        GameObject titleScreenPanel = CreateUiPanel("TitleScreenPanel", hudRoot.transform, new Color(0.02f, 0.04f, 0.08f, 0.96f));
        StretchRect(titleScreenPanel.GetComponent<RectTransform>());
        refs.ui.titleScreenPanel = titleScreenPanel;
        refs.ui.titleScreenBodyText = CreateTmp("TitleScreenBodyText", titleScreenPanel.transform, "Pick your mode and start stacking local legend runs.", 30, TextAlignmentOptions.Center);
        SetRect(refs.ui.titleScreenBodyText.rectTransform, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), new Vector2(980f, 120f), Vector2.zero);
        refs.ui.titleScreenBodyText.color = new Color(0.88f, 0.96f, 1f, 0.94f);
        refs.ui.titleScreenRecordsText = CreateTmp("TitleScreenRecordsText", titleScreenPanel.transform, "BEST SCORE 0  |  BEST BANGER --  |  BEST HEAT 0\nSTREET ROOKIE  |  LOCAL BOARD EMPTY", 24, TextAlignmentOptions.Center);
        SetRect(refs.ui.titleScreenRecordsText.rectTransform, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(980f, 70f), Vector2.zero);
        refs.ui.titleScreenRecordsText.color = new Color(1f, 0.92f, 0.34f, 0.92f);
        refs.ui.arcadeRushButton = CreateButton("ArcadeRushButton", titleScreenPanel.transform, "SCORE ATTACK", new Vector2(360f, 110f), new Vector2(0f, -40f));
        refs.ui.clutchBlitzButton = CreateButton("ClutchBlitzButton", titleScreenPanel.transform, "CLUTCH BLITZ", new Vector2(360f, 110f), new Vector2(0f, -180f));

        return refs;
    }

    private static void CreateSceneLightRig(Transform parent)
    {
        GameObject lightGo = new GameObject("Directional Light");
        lightGo.transform.SetParent(parent, false);
        lightGo.transform.rotation = Quaternion.Euler(50f, -28f, 0f);
        Light directional = lightGo.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 0.18f;
        directional.color = new Color(0.2f, 0.32f, 0.24f);

        GameObject cyanRimGo = new GameObject("Neon Rim Light Cyan");
        cyanRimGo.transform.SetParent(parent, false);
        cyanRimGo.transform.rotation = Quaternion.Euler(334f, 144f, 0f);
        Light cyanRimLight = cyanRimGo.AddComponent<Light>();
        cyanRimLight.type = LightType.Directional;
        cyanRimLight.intensity = 0.34f;
        cyanRimLight.color = new Color(0.32f, 0.98f, 1f);

        GameObject magentaRimGo = new GameObject("Neon Rim Light Magenta");
        magentaRimGo.transform.SetParent(parent, false);
        magentaRimGo.transform.rotation = Quaternion.Euler(338f, 214f, 0f);
        Light magentaRimLight = magentaRimGo.AddComponent<Light>();
        magentaRimLight.type = LightType.Directional;
        magentaRimLight.intensity = 0.28f;
        magentaRimLight.color = new Color(1f, 0.36f, 0.9f);
    }

    private static void CreatePostProcessingRig(Transform parent, VolumeProfile volumeProfile)
    {
        if (volumeProfile == null)
        {
            return;
        }

        GameObject volumeGo = new GameObject("Global Volume");
        volumeGo.transform.SetParent(parent, false);
        Volume volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = volumeProfile;
    }

    private static VolumeProfile LoadOrCreateNeonFieldGoalVolumeProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        Bloom bloom = GetOrAddVolumeComponent<Bloom>(profile);
        bloom.active = true;
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(3.2f);
        bloom.scatter.Override(0.96f);
        bloom.clamp.Override(65472f);
        bloom.highQualityFiltering.Override(true);

        Tonemapping tonemapping = GetOrAddVolumeComponent<Tonemapping>(profile);
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        ColorAdjustments colorAdjustments = GetOrAddVolumeComponent<ColorAdjustments>(profile);
        colorAdjustments.active = true;
        colorAdjustments.postExposure.Override(-0.08f);
        colorAdjustments.contrast.Override(22f);
        colorAdjustments.saturation.Override(18f);
        colorAdjustments.colorFilter.Override(new Color(0.96f, 1f, 0.98f, 1f));

        Vignette vignette = GetOrAddVolumeComponent<Vignette>(profile);
        vignette.active = true;
        vignette.intensity.Override(0.22f);
        vignette.smoothness.Override(0.82f);
        vignette.rounded.Override(false);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static T GetOrAddVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (!profile.TryGet(out T component))
        {
            component = profile.Add<T>(true);
        }

        return component;
    }

    private static KickAimGuide CreateKickAimGuide(Transform parent, Material guideMaterial)
    {
        GameObject guideRoot = new GameObject("KickAimGuide");
        guideRoot.transform.SetParent(parent, false);

        LineRenderer lineRenderer = guideRoot.AddComponent<LineRenderer>();
        lineRenderer.sharedMaterial = guideMaterial;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.enabled = false;

        KickAimGuide kickAimGuide = guideRoot.AddComponent<KickAimGuide>();
        kickAimGuide.lineRenderer = lineRenderer;

        GameObject landingMarker = CreatePrimitiveVisual(
            PrimitiveType.Cylinder,
            "LandingMarker",
            guideRoot.transform,
            Vector3.zero,
            new Vector3(0.28f, 0.02f, 0.28f),
            guideMaterial);
        kickAimGuide.landingMarker = landingMarker.transform;
        landingMarker.SetActive(false);

        return kickAimGuide;
    }

    private static GameObject CreateSceneBlock(
        string name,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Material material,
        bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        if (!keepCollider)
        {
            DestroyCollider(go);
        }

        return go;
    }

    private static GameObject CreatePrimitiveVisual(
        PrimitiveType primitiveType,
        string name,
        Transform parent,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        return CreatePrimitiveVisual(primitiveType, name, parent, localPosition, Quaternion.identity, localScale, material);
    }

    private static GameObject CreatePrimitiveVisual(
        PrimitiveType primitiveType,
        string name,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Material material)
    {
        GameObject go = GameObject.CreatePrimitive(primitiveType);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        DestroyCollider(go);
        return go;
    }

    private static Light CreateSceneLight(string name, Transform parent, Vector3 position, Color color, float range, float intensity)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        return light;
    }

    private static GameObject CreateUiPanel(string name, Transform parent, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        Image image = panel.GetComponent<Image>();
        image.color = color;
        return panel;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateTmp(
        string name,
        Transform parent,
        string text,
        int fontSize,
        TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = new Color(0.94f, 0.98f, 1f);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private static Button CreateButton(string name, Transform parent, string label, Vector2 size, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.08f, 0.16f, 0.28f, 0.95f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.14f, 0.24f, 0.4f, 1f);
        colors.pressedColor = new Color(0.04f, 0.1f, 0.18f, 1f);
        colors.selectedColor = colors.normalColor;
        button.colors = colors;

        SetRect(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), size, anchoredPos);

        TextMeshProUGUI text = CreateTmp("Label", go.transform, label, 30, TextAlignmentOptions.Center);
        StretchRect(text.rectTransform);
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 3f;
        return button;
    }

    private static void EnsureEventSystem()
    {
        EventSystem existing = Object.FindObjectOfType<EventSystem>();
        if (existing == null)
        {
            GameObject es = new GameObject("EventSystem");
            existing = es.AddComponent<EventSystem>();
        }

        ConfigureInputModule(existing);
    }

    private static void ConfigureInputModule(EventSystem eventSystem)
    {
        if (eventSystem == null)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacy != null)
        {
            legacy.enabled = false;
        }

        TouchInputModule touch = eventSystem.GetComponent<TouchInputModule>();
        if (touch != null)
        {
            touch.enabled = false;
        }

        InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule == null)
        {
            inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        inputSystemModule.enabled = true;
#else
        StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacy == null)
        {
            legacy = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        legacy.enabled = true;
#endif
    }

    private static void EnableSceneInBuildSettings(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path != scenePath)
            {
                continue;
            }

            scenes[i] = new EditorBuildSettingsScene(scenePath, true);
            found = true;
            break;
        }

        if (!found)
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 size,
        Vector2 anchoredPosition)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        rect.localScale = Vector3.one;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static Material LoadOrCreateLitMaterial(string path, Color baseColor, Color emissionColor, float metallic, float smoothness)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        SetMaterialColor(mat, "_BaseColor", baseColor);
        SetMaterialColor(mat, "_Color", baseColor);
        SetMaterialColor(mat, "_EmissionColor", emissionColor);
        SetMaterialFloat(mat, "_Metallic", metallic);
        SetMaterialFloat(mat, "_Smoothness", smoothness);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
        }

        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static void SetMaterialColor(Material mat, string property, Color value)
    {
        if (mat != null && mat.HasProperty(property))
        {
            mat.SetColor(property, value);
        }
    }

    private static void SetMaterialFloat(Material mat, string property, float value)
    {
        if (mat != null && mat.HasProperty(property))
        {
            mat.SetFloat(property, value);
        }
    }

    private static void DestroyCollider(GameObject go)
    {
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static void CloseTmpImporterWindows()
    {
        EditorWindow[] windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        for (int i = 0; i < windows.Length; i++)
        {
            EditorWindow window = windows[i];
            if (window != null && window.titleContent != null && window.titleContent.text == "TMP Importer")
            {
                window.Close();
            }
        }
    }
}
#endif
