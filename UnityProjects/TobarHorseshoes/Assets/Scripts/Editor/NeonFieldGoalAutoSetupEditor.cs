#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
    private const string FootballPrefabPath = PrefabsFolder + "/FootballProjectile.prefab";

    private const string FloorMaterialPath = MaterialsFolder + "/FieldFloor.mat";
    private const string LaneMaterialPath = MaterialsFolder + "/LaneGlow.mat";
    private const string GoalMaterialPath = MaterialsFolder + "/GoalGlow.mat";
    private const string TrajectoryMaterialPath = MaterialsFolder + "/TrajectoryGuide.mat";
    private const string BackdropMaterialPath = MaterialsFolder + "/Backdrop.mat";
    private const string AccentPinkMaterialPath = MaterialsFolder + "/AccentPink.mat";
    private const string RefereeDarkMaterialPath = MaterialsFolder + "/RefereeDark.mat";
    private const string RefereeLightMaterialPath = MaterialsFolder + "/RefereeLight.mat";
    private const string FootballMaterialPath = MaterialsFolder + "/Football.mat";

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

    private struct GoalSceneRefs
    {
        public GoalDetector goalDetector;
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
            new Color(0.02f, 0.03f, 0.07f),
            new Color(0.04f, 0.12f, 0.22f) * 1.1f,
            0.15f,
            0.86f);
        Material laneMaterial = LoadOrCreateLitMaterial(
            LaneMaterialPath,
            new Color(0.08f, 0.18f, 0.34f),
            new Color(0.15f, 0.92f, 1f) * 2.8f,
            0.03f,
            0.92f);
        Material goalMaterial = LoadOrCreateLitMaterial(
            GoalMaterialPath,
            new Color(0.07f, 0.16f, 0.28f),
            new Color(0.18f, 0.96f, 1f) * 3.5f,
            0.02f,
            0.94f);
        Material trajectoryMaterial = LoadOrCreateLitMaterial(
            TrajectoryMaterialPath,
            new Color(0.08f, 0.18f, 0.26f),
            new Color(0.28f, 0.95f, 1f) * 4.1f,
            0f,
            0.84f);
        Material backdropMaterial = LoadOrCreateLitMaterial(
            BackdropMaterialPath,
            new Color(0.03f, 0.04f, 0.12f),
            new Color(0.08f, 0.16f, 0.34f) * 1.8f,
            0.04f,
            0.82f);
        Material accentPinkMaterial = LoadOrCreateLitMaterial(
            AccentPinkMaterialPath,
            new Color(0.18f, 0.07f, 0.22f),
            new Color(1f, 0.32f, 0.86f) * 3.2f,
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
            new Color(0.46f, 0.24f, 0.1f),
            new Color(0.28f, 0.14f, 0.08f) * 0.7f,
            0.03f,
            0.76f);

        NeonFieldGoalConfig config = LoadOrCreateConfig();
        FootballProjectile footballPrefab = LoadOrCreateFootballPrefab(config, footballMaterial);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(0.32f, 0.36f, 0.46f);

        GameObject worldRoot = new GameObject("NeonFieldGoalRoot");

        GameplayRigRefs gameplayRig = BuildGameplayRig(worldRoot.transform, config, laneMaterial);
        CreateSceneLightRig(worldRoot.transform);
        GoalSceneRefs goalScene = BuildFieldAndGoal(
            worldRoot.transform,
            config,
            floorMaterial,
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

        if (config.pointsPerGoal <= 0)
        {
            config.pointsPerGoal = 3;
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

    private static FootballProjectile LoadOrCreateFootballPrefab(NeonFieldGoalConfig config, Material footballMaterial)
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

        GameObject lace = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lace.name = "Laces";
        lace.transform.SetParent(ballVisual.transform, false);
        lace.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        lace.transform.localScale = new Vector3(0.32f, 0.08f, 0.08f);
        DestroyCollider(lace);
        MeshRenderer laceRenderer = lace.GetComponent<MeshRenderer>();
        if (laceRenderer != null)
        {
            laceRenderer.sharedMaterial = LoadOrCreateLitMaterial(
                MaterialsFolder + "/FootballLaces.mat",
                new Color(0.96f, 0.96f, 0.96f),
                new Color(0.12f, 0.12f, 0.12f) * 0.15f,
                0f,
                0.56f);
        }

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
        cameraGo.transform.localPosition = new Vector3(0f, 2.65f, -7.8f);
        cameraGo.transform.LookAt(new Vector3(0f, 3.1f, config.goalDistance));

        refs.gameplayCamera = cameraGo.GetComponent<Camera>();
        refs.gameplayCamera.fieldOfView = 52f;
        refs.gameplayCamera.clearFlags = CameraClearFlags.SolidColor;
        refs.gameplayCamera.backgroundColor = new Color(0.03f, 0.04f, 0.08f);
        refs.gameplayCamera.nearClipPlane = 0.1f;
        refs.gameplayCamera.farClipPlane = 120f;

        FieldGoalCameraJuiceController cameraJuice = cameraGo.AddComponent<FieldGoalCameraJuiceController>();
        cameraJuice.targetCamera = refs.gameplayCamera;

        return refs;
    }

    private static GoalSceneRefs BuildFieldAndGoal(
        Transform parent,
        NeonFieldGoalConfig config,
        Material floorMaterial,
        Material laneMaterial,
        Material goalMaterial,
        Material backdropMaterial,
        Material accentPinkMaterial,
        Material refereeDarkMaterial,
        Material refereeLightMaterial)
    {
        GoalSceneRefs refs = new GoalSceneRefs();

        List<Renderer> goalRenderers = new List<Renderer>();
        List<Renderer> endZoneRenderers = new List<Renderer>();
        List<Light> crowdLights = new List<Light>();
        List<RefereeStubRefs> refereeStubs = new List<RefereeStubRefs>();

        GameObject env = new GameObject("Environment");
        env.transform.SetParent(parent, false);

        CreateSceneBlock(
            "FieldBase",
            env.transform,
            new Vector3(0f, -0.56f, config.goalDistance * 0.55f + 3f),
            new Vector3(22f, 1f, config.goalDistance + 20f),
            floorMaterial,
            true);
        Renderer laneMain = CreateSceneBlock(
            "LaneGlow",
            env.transform,
            new Vector3(0f, -0.48f, config.goalDistance * 0.42f),
            new Vector3(config.laneHalfWidth * 2.35f, 0.04f, config.goalDistance * 0.84f),
            laneMaterial,
            false).GetComponent<Renderer>();
        endZoneRenderers.Add(laneMain);
        endZoneRenderers.Add(CreateSceneBlock(
            "LaneEdgeLeft",
            env.transform,
            new Vector3(-config.laneHalfWidth - 0.55f, -0.47f, config.goalDistance * 0.42f),
            new Vector3(0.12f, 0.05f, config.goalDistance * 0.86f),
            accentPinkMaterial,
            false).GetComponent<Renderer>());
        endZoneRenderers.Add(CreateSceneBlock(
            "LaneEdgeRight",
            env.transform,
            new Vector3(config.laneHalfWidth + 0.55f, -0.47f, config.goalDistance * 0.42f),
            new Vector3(0.12f, 0.05f, config.goalDistance * 0.86f),
            accentPinkMaterial,
            false).GetComponent<Renderer>());
        endZoneRenderers.Add(CreateSceneBlock(
            "EndZoneGlow",
            env.transform,
            new Vector3(0f, -0.46f, config.goalDistance + 2.35f),
            new Vector3(8.8f, 0.04f, 4.8f),
            accentPinkMaterial,
            false).GetComponent<Renderer>());

        CreateSceneBlock(
            "BackdropWall",
            env.transform,
            new Vector3(0f, 5.5f, config.goalDistance + 13f),
            new Vector3(30f, 11f, 0.35f),
            backdropMaterial,
            false);
        CreateSceneBlock(
            "SkyLine",
            env.transform,
            new Vector3(0f, 0.1f, config.goalDistance + 8f),
            new Vector3(26f, 0.04f, 0.3f),
            accentPinkMaterial,
            false);

        for (int i = -9; i <= 9; i++)
        {
            float x = i * 1.4f;
            CreateSceneBlock(
                "FloorGridX_" + i,
                env.transform,
                new Vector3(x, -0.465f, config.goalDistance * 0.52f),
                new Vector3(0.025f, 0.01f, config.goalDistance + 8f),
                i % 3 == 0 ? laneMaterial : backdropMaterial,
                false);
        }

        for (int i = 0; i <= 14; i++)
        {
            float z = i * 1.8f;
            CreateSceneBlock(
                "FloorGridZ_" + i,
                env.transform,
                new Vector3(0f, -0.465f, z),
                new Vector3(18f, 0.01f, 0.025f),
                i % 4 == 0 ? accentPinkMaterial : backdropMaterial,
                false);
        }

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
            2f));

        GameObject goalAssembly = new GameObject("GoalAssembly");
        goalAssembly.transform.SetParent(env.transform, false);
        goalAssembly.transform.localPosition = new Vector3(0f, 0f, config.goalDistance);

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

        GameObject titlePanel = CreateUiPanel("TitlePanel", hudRoot.transform, new Color(0.03f, 0.05f, 0.13f, 0.72f));
        SetRect(titlePanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(700f, 160f), new Vector2(0f, -96f));
        TextMeshProUGUI titleText = CreateTmp("TitleText", titlePanel.transform, "NEON FIELDGOAL", 66, TextAlignmentOptions.Center);
        StretchRect(titleText.rectTransform);
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = cyan;
        titleText.characterSpacing = 4f;

        GameObject goalPanel = CreateUiPanel("GoalPanel", hudRoot.transform, panelColor);
        SetRect(goalPanel.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 120f), new Vector2(190f, -94f));
        refs.ui.goalsText = CreateTmp("GoalsText", goalPanel.transform, "GOALS 0", 42, TextAlignmentOptions.Center);
        StretchRect(refs.ui.goalsText.rectTransform);
        refs.ui.goalsText.fontStyle = FontStyles.Bold;
        refs.ui.goalsText.color = pink;

        GameObject timerPanel = CreateUiPanel("TimerPanel", hudRoot.transform, panelColor);
        SetRect(timerPanel.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(300f, 120f), new Vector2(-190f, -94f));
        refs.ui.timerText = CreateTmp("TimerText", timerPanel.transform, "01:00", 42, TextAlignmentOptions.Center);
        StretchRect(refs.ui.timerText.rectTransform);
        refs.ui.timerText.fontStyle = FontStyles.Bold;
        refs.ui.timerText.color = cyan;

        refs.ui.statusText = CreateTmp("StatusText", hudRoot.transform, "Line it up and let it fly.", 40, TextAlignmentOptions.Center);
        SetRect(refs.ui.statusText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1200f, 52f), new Vector2(0f, 310f));
        refs.ui.statusText.color = new Color(0.98f, 0.90f, 0.98f, 1f);

        refs.ui.hintText = CreateTmp("HintText", hudRoot.transform, "Left thumb moves. Right thumb swipes up to kick.", 28, TextAlignmentOptions.Center);
        SetRect(refs.ui.hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1400f, 40f), new Vector2(0f, 270f));
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
        SetRect(refs.ui.resultsBodyText.rectTransform, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(760f, 160f), Vector2.zero);
        refs.ui.resultsBodyText.color = new Color(1f, 0.92f, 0.98f, 1f);
        refs.ui.restartButton = CreateButton("RestartButton", results.transform, "RESTART", new Vector2(280f, 96f), new Vector2(0f, -160f));

        return refs;
    }

    private static void CreateSceneLightRig(Transform parent)
    {
        GameObject lightGo = new GameObject("Directional Light");
        lightGo.transform.SetParent(parent, false);
        lightGo.transform.rotation = Quaternion.Euler(50f, -28f, 0f);
        Light directional = lightGo.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.intensity = 1.05f;
        directional.color = new Color(0.78f, 0.86f, 1f);
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
        GameObject go = GameObject.CreatePrimitive(primitiveType);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
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
        tmp.enableWordWrapping = true;
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
        if (mat != null)
        {
            return mat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        mat = new Material(shader);
        SetMaterialColor(mat, "_BaseColor", baseColor);
        SetMaterialColor(mat, "_Color", baseColor);
        SetMaterialColor(mat, "_EmissionColor", emissionColor);
        if (mat.HasProperty("_Metallic"))
        {
            mat.SetFloat("_Metallic", metallic);
        }

        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", smoothness);
        }

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
        }

        AssetDatabase.CreateAsset(mat, path);
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
