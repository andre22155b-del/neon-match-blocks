using System;
using UnityEngine;

public enum FieldGoalLoopState
{
    Menu = 0,
    Aim = 1,
    ChargingPower = 2,
    Kick = 3,
    BallInAir = 4,
    Result = 5,
    ProgressionUpdate = 6
}

public class FieldGoalGameManager : MonoBehaviour
{
    private const string ProgressionProfileKey = "NeonFieldGoal.ProgressionProfile";
    private const string BestScoreKey = "NeonFieldGoal.BestScore";
    private const string BestLongestKickKey = "NeonFieldGoal.BestLongestKick";

    public event Action<int> RoundEnded;

    [Header("Core")]
    public NeonFieldGoalConfig config;
    public NeonFieldGoalUI ui;
    public KickerLaneMover kickerLaneMover;
    public KickInputController kickInputController;
    public GoalDetector goalDetector;
    public GoalPresentationController goalPresentationController;
    public RefereePresentationController refereePresentationController;
    public KickAimGuide kickAimGuide;
    public FieldGoalCameraJuiceController cameraJuiceController;
    public FieldGoalMovingGoalController movingGoalController;
    public FieldGoalDefenseController defenseController;

    [Header("Scene")]
    public Transform ballSpawnPoint;
    public Transform spawnParent;
    public FootballProjectile footballPrefab;

    private FootballProjectile activeBall;
    private FootballProjectile pooledBall;
    private float timeLeft;
    private float respawnTimer;
    private int goals;
    private int score;
    private int streak;
    private int bestMultiplier;
    private int longestMadeKick;
    private int bestScoreRecord;
    private int bestLongestKickRecord;
    private int bestStreakRecord;
    private int roundBestStreak;
    private int roundPerfectGoals;
    private int roundLongBombGoals;
    private int latestRunPlacement;
    private GoalCrossingMissType lastCrossingMissType;
    private float currentWindDirection;
    private float currentWindStrength;
    private bool finalDriveTriggered;
    private bool lastKickWasPerfect;
    private bool modeSelected;
    private bool roundActive;
    private bool waitingForRespawn;
    private FieldGoalMode currentMode;
    private FieldGoalLoopState currentLoopState = FieldGoalLoopState.Menu;
    private FieldGoalProgressionProfile progressionProfile;
    private FieldGoalRewardProfile rewardProfile;
    private Transform cachedBallSpawnPoint;
    private Vector3 initialBallSpawnLocalPosition;

    private void Start()
    {
        CacheSpawnPointAnchor();
        EnsureKickAimGuide();
        EnsureCameraJuiceController();

        if (kickInputController != null)
        {
            kickInputController.KickReleased += OnKickReleased;
            kickInputController.PreviewChanged += OnKickPreviewChanged;
        }

        if (goalDetector != null)
        {
            goalDetector.GoalScored += OnGoalScored;
            goalDetector.GoalCrossingMissed += OnGoalCrossingMissed;
        }

        if (ui != null)
        {
            ui.RestartPressed += RestartRound;
            ui.ModeSelectPressed += ShowModeSelect;
            ui.ModeSelected += StartSelectedMode;
            ui.BindControls(kickerLaneMover, kickInputController);
        }

        LoadPersistentRecords();
        ApplyConfig();
        EnsureDefenseController();
        if (goalPresentationController != null)
        {
            goalPresentationController.PlayAmbient();
        }

        if (ui != null)
        {
            ShowModeSelect();
        }
        else
        {
            currentMode = FieldGoalMode.ArcadeRush;
            modeSelected = true;
            StartRound();
        }
    }

    private void OnDestroy()
    {
        if (kickInputController != null)
        {
            kickInputController.KickReleased -= OnKickReleased;
            kickInputController.PreviewChanged -= OnKickPreviewChanged;
        }

        if (goalDetector != null)
        {
            goalDetector.GoalScored -= OnGoalScored;
            goalDetector.GoalCrossingMissed -= OnGoalCrossingMissed;
        }

        if (ui != null)
        {
            ui.RestartPressed -= RestartRound;
            ui.ModeSelectPressed -= ShowModeSelect;
            ui.ModeSelected -= StartSelectedMode;
        }

        UnsubscribeActiveBall();
    }

    private void Update()
    {
        if (!roundActive)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        timeLeft = Mathf.Max(0f, timeLeft - deltaTime);
        if (ui != null)
        {
            ui.SetTimer(timeLeft);
        }

        bool clutchActive = FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft);
        SyncPresentationState(clutchActive);
        if (cameraJuiceController != null)
        {
            cameraJuiceController.SetClutchMode(clutchActive);
        }

        if (!finalDriveTriggered && clutchActive)
        {
            finalDriveTriggered = true;
            if (ui != null)
            {
                ui.ShowFinalDrive();
            }

            if (goalPresentationController != null)
            {
                goalPresentationController.PlayFinalDrive();
            }

            if (cameraJuiceController != null)
            {
                cameraJuiceController.PlayFinalDrive();
            }
        }

        if (timeLeft <= 0f)
        {
            EndRound();
            return;
        }

        if (kickInputController != null && kickInputController.IsPreviewActive)
        {
            OnKickPreviewChanged(kickInputController.CurrentPower, kickInputController.CurrentAim, true);
        }

        if (!waitingForRespawn)
        {
            return;
        }

        respawnTimer -= deltaTime;
        if (respawnTimer <= 0f)
        {
            waitingForRespawn = false;
            SpawnBall();
            if (kickInputController != null)
            {
                kickInputController.SetInputEnabled(true);
            }
        }
    }

    public void RestartRound()
    {
        if (!modeSelected)
        {
            return;
        }

        TearDownActiveBall();
        ApplyConfig();
        StartRound();
    }

    private void ApplyConfig()
    {
        CacheSpawnPointAnchor();
        EnsureKickAimGuide();
        EnsureCameraJuiceController();

        if (kickerLaneMover != null)
        {
            kickerLaneMover.ApplyConfig(config);
        }

        if (kickInputController != null)
        {
            kickInputController.ApplyConfig(config);
        }

        if (goalDetector != null)
        {
            goalDetector.ApplyConfig(config);
        }

        if (goalPresentationController != null)
        {
            goalPresentationController.ApplyConfig(config);
        }

        if (refereePresentationController != null)
        {
            refereePresentationController.ApplyConfig(config);
        }

        if (movingGoalController != null)
        {
            movingGoalController.ApplyConfig(config);
        }

        if (defenseController != null)
        {
            defenseController.ApplyConfig(config);
        }

        if (kickAimGuide != null)
        {
            kickAimGuide.ApplyConfig(config);
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.ApplyConfig(config);
        }

        Ensure3DCameraPresentation();
        ApplyRuntimeAtmosphere();
        EnsureRuntimeVisualPolish();
    }

    private void EnsureKickAimGuide()
    {
        if (kickAimGuide != null)
        {
            return;
        }

        GameObject guideRoot = new GameObject("KickAimGuideRuntime");
        guideRoot.transform.SetParent(spawnParent != null ? spawnParent.parent : transform, false);

        LineRenderer lineRenderer = guideRoot.AddComponent<LineRenderer>();
        lineRenderer.sharedMaterial = CreateGuideMaterial();
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.enabled = false;

        kickAimGuide = guideRoot.AddComponent<KickAimGuide>();
        kickAimGuide.lineRenderer = lineRenderer;

        GameObject landingMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        landingMarker.name = "LandingMarker";
        landingMarker.transform.SetParent(guideRoot.transform, false);
        landingMarker.transform.localScale = new Vector3(0.28f, 0.02f, 0.28f);

        Collider landingCollider = landingMarker.GetComponent<Collider>();
        if (landingCollider != null)
        {
            Destroy(landingCollider);
        }

        Renderer landingRenderer = landingMarker.GetComponent<Renderer>();
        if (landingRenderer != null)
        {
            landingRenderer.sharedMaterial = lineRenderer.sharedMaterial;
        }

        kickAimGuide.landingMarker = landingMarker.transform;
        landingMarker.SetActive(false);
    }

    private Material CreateGuideMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = new Color(0.22f, 0.95f, 1f, 0.92f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", new Color(0.46f, 1f, 0.78f) * 3.4f);
            material.EnableKeyword("_EMISSION");
        }

        return material;
    }

    private void ApplyRuntimeAtmosphere()
    {
        RenderSettings.ambientLight = new Color(0.05f, 0.09f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.02f, 0.1f, 0.08f);
        RenderSettings.fogDensity = 0.0125f;
    }

    private void EnsureCameraJuiceController()
    {
        if (cameraJuiceController != null)
        {
            return;
        }

        Camera sourceCamera = Camera.main;
        if (sourceCamera == null && kickerLaneMover != null)
        {
            sourceCamera = kickerLaneMover.GetComponentInChildren<Camera>();
        }

        if (sourceCamera == null)
        {
            return;
        }

        cameraJuiceController = sourceCamera.GetComponent<FieldGoalCameraJuiceController>();
        if (cameraJuiceController == null)
        {
            cameraJuiceController = sourceCamera.gameObject.AddComponent<FieldGoalCameraJuiceController>();
        }
    }

    private void EnsureDefenseController()
    {
        Material baseMaterial = CreateRuntimeDefenseMaterial(
            new Color(0.05f, 0.08f, 0.12f),
            new Color(0.02f, 0.05f, 0.08f) * 0.22f);
        Material glowMaterial = CreateRuntimeDefenseMaterial(
            new Color(0.22f, 0.76f, 0.54f),
            new Color(0.2f, 1f, 0.66f) * 1.18f);
        Material accentMaterial = CreateRuntimeDefenseMaterial(
            new Color(0.88f, 0.24f, 0.78f),
            new Color(1f, 0.32f, 0.88f) * 1.32f);

        const int blockerCount = 5;
        if (defenseController == null)
        {
            Transform defenseParent = spawnParent != null ? spawnParent.parent : transform;
            GameObject defenseRootGo = new GameObject("DefenseRuntime");
            defenseRootGo.transform.SetParent(defenseParent, false);
            defenseRootGo.transform.localPosition = new Vector3(0f, 0f, config != null ? config.defenseLineForwardOffset : 4.75f);

            defenseController = defenseRootGo.AddComponent<FieldGoalDefenseController>();
            defenseController.config = config;
            defenseController.defenseRoot = defenseRootGo.transform;
        }

        if (defenseController.defenseRoot == null)
        {
            defenseController.defenseRoot = defenseController.transform;
        }

        if (defenseController.blockerRoots == null || defenseController.blockerRoots.Length != blockerCount)
        {
            defenseController.blockerRoots = new Transform[blockerCount];
        }

        if (defenseController.bodyVisuals == null || defenseController.bodyVisuals.Length != blockerCount)
        {
            defenseController.bodyVisuals = new Transform[blockerCount];
        }

        if (defenseController.leftArmPivots == null || defenseController.leftArmPivots.Length != blockerCount)
        {
            defenseController.leftArmPivots = new Transform[blockerCount];
        }

        if (defenseController.rightArmPivots == null || defenseController.rightArmPivots.Length != blockerCount)
        {
            defenseController.rightArmPivots = new Transform[blockerCount];
        }

        if (defenseController.hitboxTransforms == null || defenseController.hitboxTransforms.Length != blockerCount)
        {
            defenseController.hitboxTransforms = new Transform[blockerCount];
        }

        if (defenseController.hitboxes == null || defenseController.hitboxes.Length != blockerCount)
        {
            defenseController.hitboxes = new FieldGoalDefenseHitbox[blockerCount];
        }

        for (int i = 0; i < blockerCount; i++)
        {
            Transform blockerRoot = defenseController.blockerRoots[i];
            if (blockerRoot == null)
            {
                GameObject blockerRootGo = new GameObject("RuntimeBlocker_" + (i + 1));
                blockerRootGo.transform.SetParent(defenseController.defenseRoot, false);
                blockerRoot = blockerRootGo.transform;
                blockerRoot.localPosition = Vector3.zero;
                defenseController.blockerRoots[i] = blockerRoot;
            }

            Rigidbody blockerBody = blockerRoot.GetComponent<Rigidbody>();
            if (blockerBody == null)
            {
                blockerBody = blockerRoot.gameObject.AddComponent<Rigidbody>();
            }

            blockerBody.isKinematic = true;
            blockerBody.useGravity = false;

            EnsureRuntimeDefensePad(blockerRoot, "ArcadePad", baseMaterial, new Vector3(0f, 0.03f, 0f), new Vector3(0.82f, 0.05f, 0.42f));
            EnsureRuntimeDefensePad(blockerRoot, "ArcadeUnderglow", glowMaterial, new Vector3(0f, 0.012f, 0f), new Vector3(1.02f, 0.018f, 0.58f));

            Transform bodyVisual = defenseController.bodyVisuals[i];
            if (bodyVisual == null || bodyVisual.Find("BarCore") == null)
            {
                if (bodyVisual != null)
                {
                    bodyVisual.gameObject.SetActive(false);
                }

                bodyVisual = new GameObject("ArcadeBarVisual").transform;
                bodyVisual.SetParent(blockerRoot, false);
                bodyVisual.localPosition = new Vector3(0f, 1.08f, 0f);
                bodyVisual.localRotation = Quaternion.identity;
                bodyVisual.localScale = new Vector3(1.18f, 0.18f, 0.48f);

                CreateRuntimeDefensePrimitive(
                    PrimitiveType.Cube,
                    "BarGlow",
                    bodyVisual,
                    Vector3.zero,
                    Quaternion.identity,
                    new Vector3(1.12f, 1.5f, 1.2f),
                    glowMaterial,
                    false);
                CreateRuntimeDefensePrimitive(
                    PrimitiveType.Cube,
                    "BarCore",
                    bodyVisual,
                    Vector3.zero,
                    Quaternion.identity,
                    Vector3.one,
                    baseMaterial,
                    false);
                CreateRuntimeDefensePrimitive(
                    PrimitiveType.Cube,
                    "BarAccent",
                    bodyVisual,
                    new Vector3(0f, 0f, 0.56f),
                    Quaternion.identity,
                    new Vector3(0.86f, 0.26f, 0.08f),
                    i % 2 == 0 ? accentMaterial : glowMaterial,
                    false);

                defenseController.bodyVisuals[i] = bodyVisual;
            }

            bodyVisual.localPosition = new Vector3(0f, 1.08f, 0f);
            bodyVisual.localRotation = Quaternion.identity;
            bodyVisual.localScale = new Vector3(1.18f, 0.18f, 0.48f);
            EnsureRuntimeDefensePad(bodyVisual, "BarGlow", glowMaterial, Vector3.zero, new Vector3(1.12f, 1.5f, 1.2f));
            EnsureRuntimeDefensePad(bodyVisual, "BarCore", baseMaterial, Vector3.zero, Vector3.one);
            EnsureRuntimeDefensePad(bodyVisual, "BarAccent", i % 2 == 0 ? accentMaterial : glowMaterial, new Vector3(0f, 0f, 0.56f), new Vector3(0.86f, 0.26f, 0.08f));

            Transform leftArmPivot = defenseController.leftArmPivots[i];
            if (leftArmPivot == null || leftArmPivot.parent != bodyVisual)
            {
                if (leftArmPivot != null)
                {
                    leftArmPivot.gameObject.SetActive(false);
                }

                leftArmPivot = new GameObject("ArcadeLeftPulsePivot").transform;
                leftArmPivot.SetParent(bodyVisual, false);
                defenseController.leftArmPivots[i] = leftArmPivot;
            }
            leftArmPivot.localPosition = new Vector3(-0.68f, 0f, 0f);
            leftArmPivot.localRotation = Quaternion.identity;
            EnsureRuntimeDefensePad(leftArmPivot, "LeftPulse", i % 2 == 0 ? accentMaterial : glowMaterial, Vector3.zero, new Vector3(0.14f, 0.22f, 0.14f));

            Transform rightArmPivot = defenseController.rightArmPivots[i];
            if (rightArmPivot == null || rightArmPivot.parent != bodyVisual)
            {
                if (rightArmPivot != null)
                {
                    rightArmPivot.gameObject.SetActive(false);
                }

                rightArmPivot = new GameObject("ArcadeRightPulsePivot").transform;
                rightArmPivot.SetParent(bodyVisual, false);
                defenseController.rightArmPivots[i] = rightArmPivot;
            }
            rightArmPivot.localPosition = new Vector3(0.68f, 0f, 0f);
            rightArmPivot.localRotation = Quaternion.identity;
            EnsureRuntimeDefensePad(rightArmPivot, "RightPulse", i % 2 == 0 ? accentMaterial : glowMaterial, Vector3.zero, new Vector3(0.14f, 0.22f, 0.14f));

            Transform hitbox = defenseController.hitboxTransforms[i];
            FieldGoalDefenseHitbox hitboxComponent = defenseController.hitboxes[i];
            if (hitbox == null)
            {
                hitbox = new GameObject("Hitbox").transform;
                hitbox.SetParent(blockerRoot, false);
                hitbox.localPosition = new Vector3(0f, 1.08f, 0f);
                hitbox.localScale = new Vector3(1.18f, 0.32f, 0.56f);
                defenseController.hitboxTransforms[i] = hitbox;
            }

            BoxCollider collider = hitbox.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = hitbox.gameObject.AddComponent<BoxCollider>();
                collider.size = Vector3.one;
            }

            if (hitboxComponent == null)
            {
                hitboxComponent = hitbox.GetComponent<FieldGoalDefenseHitbox>();
            }

            if (hitboxComponent == null)
            {
                hitboxComponent = hitbox.gameObject.AddComponent<FieldGoalDefenseHitbox>();
            }

            hitboxComponent.blockerLabel = "TIMING BAR";
            defenseController.hitboxes[i] = hitboxComponent;
        }

        defenseController.ApplyConfig(config);
        defenseController.ShowMenuPose(GetStartingYardLine());
    }

    private void EnsureRuntimeDefensePad(Transform parent, string name, Material material, Vector3 localPosition, Vector3 localScale)
    {
        if (parent == null)
        {
            return;
        }

        Transform existing = parent.Find(name);
        if (existing != null)
        {
            existing.localPosition = localPosition;
            existing.localScale = localScale;
            Renderer existingRenderer = existing.GetComponent<Renderer>();
            if (existingRenderer != null)
            {
                existingRenderer.sharedMaterial = material;
            }

            return;
        }

        CreateRuntimeDefensePrimitive(
            PrimitiveType.Cube,
            name,
            parent,
            localPosition,
            Quaternion.identity,
            localScale,
            material,
            false);
    }

    private void Ensure3DCameraPresentation()
    {
        Camera sourceCamera = cameraJuiceController != null ? cameraJuiceController.targetCamera : Camera.main;
        if (sourceCamera == null || ballSpawnPoint == null)
        {
            return;
        }

        sourceCamera.orthographic = false;
        sourceCamera.fieldOfView = FieldGoalCameraMath.GetFirstPersonFov(config);
        sourceCamera.nearClipPlane = Mathf.Min(sourceCamera.nearClipPlane, 0.05f);
        ApplyFirstPersonCameraPose(sourceCamera);

        if (cameraJuiceController != null)
        {
            cameraJuiceController.ApplyConfig(config);
        }
    }

    private void ApplyFirstPersonCameraPose(Camera sourceCamera)
    {
        if (sourceCamera == null || ballSpawnPoint == null)
        {
            return;
        }

        Vector3 localPosition = FieldGoalCameraMath.GetFirstPersonLocalPosition(config, ballSpawnPoint.localPosition);
        Quaternion localRotation = FieldGoalCameraMath.GetFirstPersonLookRotation(
            config,
            localPosition,
            config != null ? config.goalDistance : 21.5f);

        Transform rigParent = ballSpawnPoint.parent;
        if (sourceCamera.transform.parent == rigParent)
        {
            sourceCamera.transform.localPosition = localPosition;
            sourceCamera.transform.localRotation = localRotation;
            return;
        }

        if (rigParent != null)
        {
            sourceCamera.transform.position = rigParent.TransformPoint(localPosition);
            Vector3 worldForward = rigParent.TransformDirection(localRotation * Vector3.forward);
            sourceCamera.transform.rotation = Quaternion.LookRotation(worldForward.normalized, rigParent.up);
            return;
        }

        sourceCamera.transform.position = ballSpawnPoint.position + Vector3.up * (config != null ? config.cameraFirstPersonEyeHeight : 1.72f) - ballSpawnPoint.forward * (config != null ? config.cameraFirstPersonBackOffset : 1.35f);
        Vector3 lookTarget = ballSpawnPoint.position + ballSpawnPoint.forward * (config != null ? config.goalDistance : 21.5f);
        lookTarget.y = config != null ? config.cameraFirstPersonLookHeight : 3.15f;
        sourceCamera.transform.rotation = Quaternion.LookRotation((lookTarget - sourceCamera.transform.position).normalized, Vector3.up);
    }

    private void EnsureRuntimeVisualPolish()
    {
        if (config == null)
        {
            return;
        }

        Transform polishParent = spawnParent != null ? spawnParent.parent : transform;
        if (polishParent == null)
        {
            return;
        }

        if (GameObject.Find("GoalPortalBackplate") != null || GameObject.Find("TunnelFrameLeft") != null)
        {
            return;
        }

        Transform existing = polishParent.Find("VisualRuntimePolish");
        if (existing != null)
        {
            return;
        }

        GameObject polishRoot = new GameObject("VisualRuntimePolish");
        polishRoot.transform.SetParent(polishParent, false);

        Material fieldGridMaterial = CreateRuntimeDefenseMaterial(
            new Color(0.08f, 0.24f, 0.12f),
            new Color(0.34f, 1f, 0.58f) * 3.8f);
        Material fieldMarkingMaterial = CreateRuntimeDefenseMaterial(
            new Color(0.74f, 1f, 0.82f),
            new Color(0.68f, 1f, 0.8f) * 4.8f);
        Material laneMaterial = CreateRuntimeDefenseMaterial(
            new Color(0.06f, 0.22f, 0.12f),
            new Color(0.3f, 1f, 0.48f) * 4.2f);
        Material goalMaterial = CreateRuntimeDefenseMaterial(
            new Color(0.18f, 0.22f, 0.06f),
            new Color(1f, 0.94f, 0.3f) * 4.8f);
        Material accentMaterial = CreateRuntimeDefenseMaterial(
            new Color(1f, 0.38f, 0.88f),
            new Color(1f, 0.36f, 0.9f) * 2.8f);

        float rearFieldPadding = Mathf.Max(8f, Mathf.Max(1, config.startingYardLine) * Mathf.Max(0.05f, config.worldUnitsPerYard) + 4f);
        float laneStartZ = -rearFieldPadding;
        float tunnelFrameZ = laneStartZ + 1.8f;

        CreateRuntimeDefensePrimitive(
            PrimitiveType.Cube,
            "TunnelFrameLeft",
            polishRoot.transform,
            new Vector3(-7.4f, 3.2f, tunnelFrameZ),
            Quaternion.identity,
            new Vector3(0.22f, 5.8f, 0.22f),
            fieldGridMaterial,
            false);
        CreateRuntimeDefensePrimitive(
            PrimitiveType.Cube,
            "TunnelFrameRight",
            polishRoot.transform,
            new Vector3(7.4f, 3.2f, tunnelFrameZ),
            Quaternion.identity,
            new Vector3(0.22f, 5.8f, 0.22f),
            fieldGridMaterial,
            false);
        CreateRuntimeDefensePrimitive(
            PrimitiveType.Cube,
            "TunnelFrameTop",
            polishRoot.transform,
            new Vector3(0f, 5.95f, tunnelFrameZ),
            Quaternion.identity,
            new Vector3(15.2f, 0.18f, 0.18f),
            accentMaterial,
            false);
        CreateRuntimeDefensePrimitive(
            PrimitiveType.Cube,
            "GoalPortalBackplate",
            polishRoot.transform,
            new Vector3(0f, 4.55f, config.goalDistance + 1.15f),
            Quaternion.identity,
            new Vector3(10.4f, 6.4f, 0.1f),
            fieldGridMaterial,
            false);
        CreateRuntimeDefensePrimitive(
            PrimitiveType.Cube,
            "GoalPortalTop",
            polishRoot.transform,
            new Vector3(0f, 7.4f, config.goalDistance + 0.86f),
            Quaternion.identity,
            new Vector3(8.9f, 0.1f, 0.16f),
            fieldMarkingMaterial,
            false);
        CreateRuntimeDefensePrimitive(
            PrimitiveType.Cube,
            "GoalPortalLeft",
            polishRoot.transform,
            new Vector3(-4.3f, 4.3f, config.goalDistance + 0.86f),
            Quaternion.identity,
            new Vector3(0.12f, 5.8f, 0.16f),
            fieldMarkingMaterial,
            false);
        CreateRuntimeDefensePrimitive(
            PrimitiveType.Cube,
            "GoalPortalRight",
            polishRoot.transform,
            new Vector3(4.3f, 4.3f, config.goalDistance + 0.86f),
            Quaternion.identity,
            new Vector3(0.12f, 5.8f, 0.16f),
            fieldMarkingMaterial,
            false);
        CreateRuntimeDefensePrimitive(
            PrimitiveType.Cube,
            "GoalTargetRunway",
            polishRoot.transform,
            new Vector3(0f, -0.037f, config.goalDistance - 2.8f),
            Quaternion.identity,
            new Vector3(5.8f, 0.012f, 3.6f),
            goalMaterial,
            false);

        for (int chevronIndex = 0; chevronIndex < 6; chevronIndex++)
        {
            float chevronZ = laneStartZ + 3.2f + chevronIndex * 4.3f;
            float chevronX = config.laneHalfWidth + 0.64f + chevronIndex * 0.04f;
            CreateRuntimeDefensePrimitive(
                PrimitiveType.Cube,
                "LaneChevronLeft_" + chevronIndex,
                polishRoot.transform,
                new Vector3(-chevronX, -0.02f, chevronZ),
                Quaternion.Euler(0f, -26f, 0f),
                new Vector3(0.12f, 0.045f, 0.72f),
                fieldMarkingMaterial,
                false);
            CreateRuntimeDefensePrimitive(
                PrimitiveType.Cube,
                "LaneChevronRight_" + chevronIndex,
                polishRoot.transform,
                new Vector3(chevronX, -0.02f, chevronZ),
                Quaternion.Euler(0f, 26f, 0f),
                new Vector3(0.12f, 0.045f, 0.72f),
                fieldMarkingMaterial,
                false);
        }

        CreateRuntimePointLight(
            "GoalPortalCoreLight",
            polishRoot.transform,
            new Vector3(0f, 4.55f, config.goalDistance + 0.78f),
            new Color(1f, 0.92f, 0.38f),
            18f,
            3.1f);
        CreateRuntimePointLight(
            "GoalPortalSideLightLeft",
            polishRoot.transform,
            new Vector3(-3.1f, 4.1f, config.goalDistance + 0.52f),
            new Color(0.3f, 1f, 0.48f),
            12f,
            2.2f);
        CreateRuntimePointLight(
            "GoalPortalSideLightRight",
            polishRoot.transform,
            new Vector3(3.1f, 4.1f, config.goalDistance + 0.52f),
            new Color(1f, 0.42f, 0.9f),
            12f,
            2.2f);
    }

    private static Material CreateRuntimeDefenseMaterial(Color baseColor, Color emissionColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", baseColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", baseColor);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", emissionColor);
            material.EnableKeyword("_EMISSION");
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.9f);
        }

        return material;
    }

    private static Light CreateRuntimePointLight(string name, Transform parent, Vector3 localPosition, Color color, float range, float intensity)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = intensity;
        return light;
    }

    private static GameObject CreateRuntimeDefensePrimitive(
        PrimitiveType primitiveType,
        string name,
        Transform parent,
        Vector3 localPosition,
        Quaternion localRotation,
        Vector3 localScale,
        Material material,
        bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(primitiveType);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        if (!keepCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        return go;
    }

    private void StartRound()
    {
        goals = 0;
        score = 0;
        streak = 0;
        bestMultiplier = 1;
        longestMadeKick = 0;
        roundBestStreak = 0;
        roundPerfectGoals = 0;
        roundLongBombGoals = 0;
        latestRunPlacement = 0;
        lastCrossingMissType = GoalCrossingMissType.None;
        currentWindDirection = 0f;
        currentWindStrength = 0f;
        lastKickWasPerfect = false;
        timeLeft = FieldGoalScoring.GetRoundDuration(config, currentMode);
        finalDriveTriggered = FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft);
        waitingForRespawn = false;
        roundActive = true;
        SetLoopState(FieldGoalLoopState.ProgressionUpdate);

        ApplyCurrentKickSpot(resetLanePosition: true);
        UpdateMovingGoalChallenge(snapToCenter: true);

        if (kickerLaneMover != null)
        {
            kickerLaneMover.SetControlsEnabled(true);
        }

        if (kickInputController != null)
        {
            kickInputController.SetInputEnabled(true);
        }

        if (ui != null)
        {
            ui.HideTitleScreen();
            ui.HideResults();
            ui.SetControlsEnabled(true);
            ui.SetScore(score);
            ui.SetRunStats(goals, longestMadeKick, roundBestStreak);
            ui.SetMultiplier(FieldGoalScoring.GetMultiplier(config, 0, finalDriveTriggered), 0);
            ui.SetModeLabel(GetModeHudLabel());
            ui.SetKickState(GetCurrentYardLine(), GetCurrentKickBasePoints(), IsMovingGoalLive());
            ui.SetTimer(timeLeft);
            if (currentMode == FieldGoalMode.ClutchBlitz)
            {
                ui.ShowModeIntro("CLUTCH BLITZ", "35 seconds. Every kick is pressure.");
                ui.ShowStatus("Pressure starts now.");
                ui.SetHint(GetKickSpotHint());
            }
            else
            {
                ui.ShowModeIntro("SCORE ATTACK", "60 seconds. Build heat and hunt the board.");
                ui.ShowStatus("Line it up and cash the first heater.");
                ui.SetHint(GetKickSpotHint());
            }
        }

        if (goalPresentationController != null)
        {
            goalPresentationController.ResetPresentation();
            SyncPresentationState(finalDriveTriggered);
            goalPresentationController.PlayAmbient();
            if (finalDriveTriggered)
            {
                goalPresentationController.PlayFinalDrive();
            }
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.SetClutchMode(finalDriveTriggered, true);
            cameraJuiceController.ClearBallFollow();
            cameraJuiceController.SetPressure(GetCurrentYardLine(), currentWindStrength);
            cameraJuiceController.ResetPresentation();
            if (finalDriveTriggered)
            {
                cameraJuiceController.PlayFinalDrive();
            }
        }

        if (refereePresentationController != null)
        {
            refereePresentationController.ResetPose();
        }

        if (movingGoalController != null)
        {
            movingGoalController.ResetMotion(true);
        }

        if (defenseController != null)
        {
            defenseController.ResetDefense(GetCurrentYardLine(), goals, true);
        }

        if (kickAimGuide != null)
        {
            kickAimGuide.Hide();
        }

        SpawnBall();
    }

    private void EndRound()
    {
        roundActive = false;
        waitingForRespawn = false;
        SetLoopState(FieldGoalLoopState.Menu);

        if (goalDetector != null)
        {
            goalDetector.ClearTrackedBall();
        }

        if (kickerLaneMover != null)
        {
            kickerLaneMover.SetControlsEnabled(false);
        }

        if (kickInputController != null)
        {
            kickInputController.SetInputEnabled(false);
        }

        if (goalPresentationController != null)
        {
            goalPresentationController.ResetPresentation();
            goalPresentationController.StopAmbient();
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.SetClutchMode(false, true);
            cameraJuiceController.ClearBallFollow();
            cameraJuiceController.ResetPresentation();
        }

        if (refereePresentationController != null)
        {
            refereePresentationController.ResetPose();
        }

        if (kickAimGuide != null)
        {
            kickAimGuide.Hide();
        }

        if (ui != null)
        {
            ui.SetControlsEnabled(false);
            ui.ShowStatus("Clock hit zero.");
        }

        SavePersistentRecords();

        if (ui != null)
        {
            ui.ShowResults(
                score,
                goals,
                longestMadeKick,
                bestMultiplier,
                roundBestStreak,
                FieldGoalScoring.GetModeLabel(currentMode),
                BuildProgressionSummary());
        }

        RoundEnded?.Invoke(score);
    }

    private void SpawnBall()
    {
        TearDownActiveBall();
        ApplyCurrentKickSpot(resetLanePosition: false);

        if (footballPrefab == null || ballSpawnPoint == null)
        {
            return;
        }

        activeBall = AcquireBallInstance();
        if (activeBall == null)
        {
            return;
        }

        activeBall.ApplyConfig(config);
        activeBall.ApplyAppearance(
            config != null ? config.footballVisualPrefab : null,
            config != null ? config.footballMesh : null,
            config != null ? config.footballMaterial : null);
        activeBall.PlaceAt(ballSpawnPoint);
        activeBall.Settled += OnBallSettled;
        activeBall.OutOfBounds += OnBallOutOfBounds;

        if (goalDetector != null)
        {
            goalDetector.TrackBall(activeBall);
        }

        activeBall.BlockedByDefense += OnBallBlockedByDefense;

        lastCrossingMissType = GoalCrossingMissType.None;
        RefreshWindForCurrentKick();
        UpdateMovingGoalChallenge(snapToCenter: false);
        UpdateDefenseChallenge(snapToReset: false);
        if (cameraJuiceController != null)
        {
            cameraJuiceController.ClearBallFollow();
            cameraJuiceController.SetPressure(GetCurrentYardLine(), currentWindStrength);
        }

        if (kickAimGuide != null)
        {
            kickAimGuide.Hide();
        }

        if (ui != null && roundActive)
        {
            ui.SetRunStats(goals, longestMadeKick, roundBestStreak);
            ui.SetModeLabel(GetModeHudLabel());
            ui.SetKickState(GetCurrentYardLine(), GetCurrentKickBasePoints(), IsMovingGoalLive());
            ui.SetWind(currentWindDirection, currentWindStrength);
            ui.ShowStatus(GetKickSpotStatus());
            ui.SetHint(GetKickSpotHint());
        }

        SyncPresentationState(roundActive && FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft));
        SetLoopState(FieldGoalLoopState.Aim);
    }

    private void OnKickReleased(float power, float aim)
    {
        if (!CanReleaseKick() || timeLeft <= 0f || ballSpawnPoint == null || config == null)
        {
            return;
        }

        Vector3 targetPosition = goalDetector != null ? goalDetector.transform.position : ballSpawnPoint.position + ballSpawnPoint.forward * 10f;
        Vector3 launchDirection = FieldGoalKickMath.ComputeLaunchDirection(ballSpawnPoint, targetPosition, config, aim);
        float impulse = FieldGoalKickMath.ComputeImpulse(config, power, rewardProfile.PerfectWindowBonus);
        float curveTorque = FieldGoalKickMath.ComputeCurveTorque(config, aim, power);
        bool perfectKick = FieldGoalKickMath.IsPerfectKick(config, power, aim, rewardProfile.PerfectWindowBonus);
        bool clutchActive = FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft);
        bool longBombLine = FieldGoalScoring.IsLongBomb(config, GetCurrentYardLine());
        Vector3 windAcceleration = GetCurrentWindAcceleration(perfectKick);
        lastKickWasPerfect = perfectKick;

        SetLoopState(FieldGoalLoopState.Kick);
        activeBall.Kick(launchDirection, impulse, curveTorque, windAcceleration);
        SetLoopState(FieldGoalLoopState.BallInAir);

        if (defenseController != null)
        {
            defenseController.ReactToKick(power, perfectKick);
        }

        if (goalPresentationController != null)
        {
            goalPresentationController.PlayKick(longBombLine, perfectKick, clutchActive);
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.PlayKick(power, perfectKick);
            cameraJuiceController.BeginBallFollow(activeBall);
        }

        if (ui != null)
        {
            if (perfectKick)
            {
                ui.ShowPerfectStrike();
            }
            else if (FieldGoalScoring.IsLongBomb(config, GetCurrentYardLine()))
            {
                ui.ShowStatus(IsWindLive() ? "Launch the banger through the wind." : "Launch the banger.");
            }
            else if (power >= 0.92f)
            {
                ui.ShowStatus("BOOM.");
                ui.PulseStatus();
            }
            else
            {
                ui.ShowStatus("Ball up.");
            }
        }

        if (kickInputController != null)
        {
            kickInputController.SetInputEnabled(false);
        }

        if (kickAimGuide != null)
        {
            kickAimGuide.Hide();
        }
    }

    private void OnGoalScored(FootballProjectile scoredBall)
    {
        if (!roundActive || timeLeft <= 0f || waitingForRespawn || scoredBall == null || scoredBall != activeBall)
        {
            return;
        }

        SetLoopState(FieldGoalLoopState.Result);

        int madeYardLine = GetCurrentYardLine();
        bool movingGoalWasLive = FieldGoalScoring.IsMovingGoalActive(config, madeYardLine);
        streak += 1;
        roundBestStreak = Mathf.Max(roundBestStreak, streak);
        goals += 1;
        FieldGoalScoreResult scoreResult = FieldGoalScoring.Evaluate(
            config,
            currentMode,
            timeLeft,
            streak,
            lastKickWasPerfect,
            madeYardLine,
            rewardProfile.PerfectPointBonus);
        score += scoreResult.PointsAwarded;
        bestMultiplier = Mathf.Max(bestMultiplier, scoreResult.Multiplier);
        longestMadeKick = Mathf.Max(longestMadeKick, scoreResult.YardLine);
        if (scoreResult.PerfectKick)
        {
            roundPerfectGoals += 1;
        }

        if (scoreResult.LongBomb)
        {
            roundLongBombGoals += 1;
        }

        if (ui != null)
        {
            ui.SetScore(score);
            ui.SetRunStats(goals, longestMadeKick, roundBestStreak);
            ui.SetMultiplier(scoreResult.Multiplier, streak);
            ui.SetModeLabel(GetModeHudLabel());
            ui.SetKickState(GetCurrentYardLine(), GetCurrentKickBasePoints(), IsMovingGoalLive());
            ui.PulseGoals();
            ui.ShowGoalBurst(
                scoreResult.PointsAwarded,
                scoreResult.Multiplier,
                scoreResult.PerfectKick,
                scoreResult.ClutchActive,
                scoreResult.YardLine,
                scoreResult.LongBomb);
            if (scoreResult.LongBomb)
            {
                ui.ShowStatus(scoreResult.ClutchActive ? "Street cash. Banger dropped." : "Banger dropped.");
            }
            else if (IsMovingGoalLive())
            {
                ui.ShowStatus(scoreResult.ClutchActive ? "Clutch cash. Goal's moving now." : "It's good. Goal's swaying now.");
            }
            else
            {
                ui.ShowStatus(scoreResult.ClutchActive ? "Clutch cash." : "It's good.");
            }

            ui.SetHint(GetKickSpotHint());
        }

        if (goalPresentationController != null)
        {
            goalPresentationController.PlayGoalCelebration(scoreResult, streak);
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.PlayGoal(scoreResult.Multiplier, scoreResult.PerfectKick, scoreResult.ClutchActive);
        }

        if (refereePresentationController != null)
        {
            refereePresentationController.CelebrateGoal(
                scoreResult.LongBomb,
                scoreResult.PerfectKick,
                scoreResult.ClutchActive,
                scoreResult.Multiplier);
        }

        lastCrossingMissType = GoalCrossingMissType.None;
        UpdateMovingGoalChallenge(snapToCenter: false);
        UpdateDefenseChallenge(snapToReset: false);
        SyncPresentationState(scoreResult.ClutchActive);
        if (!movingGoalWasLive && IsMovingGoalLive() && goalPresentationController != null)
        {
            goalPresentationController.PlayMovingGoalActivated();
        }
        lastKickWasPerfect = false;
        ScheduleRespawn(config != null ? config.respawnDelayAfterGoal : 0.9f);
    }

    private void OnBallSettled(FootballProjectile settledBall)
    {
        if (!roundActive || waitingForRespawn || settledBall == null || settledBall != activeBall || settledBall.HasScored)
        {
            return;
        }

        SetLoopState(FieldGoalLoopState.Result);

        if (goalPresentationController != null)
        {
            goalPresentationController.PlayMissFeedback(
                FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft),
                IsLongBombLine(),
                streak >= 2);
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.PlayMiss();
        }

        ResetStreak(GetMissResetStatus("No good. Same line. Clock's still cooking."));
        ScheduleRespawn(config != null ? config.respawnDelayAfterMiss : 0.65f);
    }

    private void OnBallOutOfBounds(FootballProjectile ball)
    {
        if (!roundActive || waitingForRespawn || ball == null || ball != activeBall || ball.HasScored)
        {
            return;
        }

        SetLoopState(FieldGoalLoopState.Result);

        if (goalPresentationController != null)
        {
            goalPresentationController.PlayMissFeedback(
                FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft),
                IsLongBombLine(),
                streak >= 2);
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.PlayMiss();
        }

        ResetStreak(GetMissResetStatus("Out of range. Same line. Clock's still cooking."));
        ScheduleRespawn(config != null ? config.respawnDelayAfterMiss : 0.65f);
    }

    private void OnBallBlockedByDefense(FootballProjectile blockedBall, FieldGoalDefenseHitbox hitbox)
    {
        if (!roundActive || waitingForRespawn || blockedBall == null || blockedBall != activeBall || blockedBall.HasScored)
        {
            return;
        }

        SetLoopState(FieldGoalLoopState.Result);

        if (goalPresentationController != null)
        {
            goalPresentationController.PlayMissFeedback(
                FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft),
                IsLongBombLine(),
                streak >= 2);
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.PlayMiss();
        }

        ResetStreak("TIMING BAR GOT IT. Same line. Clock's still cooking.");
        if (ui != null)
        {
            ui.PulseStatus();
        }

        ScheduleRespawn(config != null ? Mathf.Min(config.respawnDelayAfterMiss, 0.45f) : 0.45f);
    }

    private void OnGoalCrossingMissed(GoalCrossingEvaluation evaluation)
    {
        if (!roundActive || waitingForRespawn)
        {
            return;
        }

        lastCrossingMissType = evaluation.MissType;

        if (goalPresentationController != null)
        {
            goalPresentationController.PlayNearMissFeedback(
                evaluation.MissType,
                IsLongBombLine(),
                FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft));
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.PlayNearMiss(IsLongBombLine(), FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft));
        }

        if (ui != null)
        {
            ui.ShowSpotlight(
                GetNearMissHeadline(evaluation.MissType),
                GetNearMissSubtitle(evaluation.MissType),
                new Color(1f, 0.58f, 0.28f, 0.96f));
            ui.ShowStatus(GetNearMissStatus(evaluation.MissType));
            ui.PulseStatus();
        }
    }

    private void OnKickPreviewChanged(float power, float aim, bool active)
    {
        if (kickAimGuide == null)
        {
            return;
        }

        if (!active || !CanPreviewKick() || ballSpawnPoint == null || config == null)
        {
            if (!active && currentLoopState == FieldGoalLoopState.ChargingPower)
            {
                SetLoopState(FieldGoalLoopState.Aim);
            }

            kickAimGuide.Hide();
            return;
        }

        Vector3 targetPosition = goalDetector != null ? goalDetector.transform.position : ballSpawnPoint.position + ballSpawnPoint.forward * 10f;
        bool perfectPreview = FieldGoalKickMath.IsPerfectKick(config, power, aim, rewardProfile.PerfectWindowBonus);
        kickAimGuide.UpdatePreview(ballSpawnPoint, targetPosition, power, aim, GetCurrentWindAcceleration(perfectPreview));
        if (currentLoopState == FieldGoalLoopState.Aim)
        {
            SetLoopState(FieldGoalLoopState.ChargingPower);
        }
    }

    private void ScheduleRespawn(float delay)
    {
        waitingForRespawn = true;
        respawnTimer = Mathf.Max(0.1f, delay);
        SetLoopState(FieldGoalLoopState.ProgressionUpdate);
    }

    private void ResetStreak(string status)
    {
        if (streak > 0 && ui != null)
        {
            ui.SetMultiplier(
                FieldGoalScoring.GetMultiplier(
                    config,
                    0,
                    roundActive && FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft)),
                0);
            ui.PulseStatus();
        }

        streak = 0;
        lastKickWasPerfect = false;
        SyncPresentationState(roundActive && FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft));

        if (ui != null && !string.IsNullOrEmpty(status))
        {
            ui.ShowStatus(status);
        }
    }

    private void StartSelectedMode(FieldGoalMode mode)
    {
        currentMode = mode;
        modeSelected = true;
        TearDownActiveBall();
        ApplyConfig();
        StartRound();
    }

    private void ShowModeSelect()
    {
        modeSelected = false;
        roundActive = false;
        waitingForRespawn = false;
        finalDriveTriggered = false;
        lastKickWasPerfect = false;
        timeLeft = 0f;
        score = 0;
        goals = 0;
        streak = 0;
        bestMultiplier = 1;
        longestMadeKick = 0;
        lastCrossingMissType = GoalCrossingMissType.None;
        currentWindDirection = 0f;
        currentWindStrength = 0f;
        SetLoopState(FieldGoalLoopState.Menu);

        TearDownActiveBall();

        if (goalDetector != null)
        {
            goalDetector.ClearTrackedBall();
        }

        if (kickerLaneMover != null)
        {
            kickerLaneMover.SetDepthOffset(0f);
            kickerLaneMover.ResetLanePosition();
            kickerLaneMover.SetControlsEnabled(false);
        }
        else if (ballSpawnPoint != null && cachedBallSpawnPoint == ballSpawnPoint)
        {
            ballSpawnPoint.localPosition = initialBallSpawnLocalPosition;
        }

        if (kickInputController != null)
        {
            kickInputController.SetInputEnabled(false);
        }

        if (refereePresentationController != null)
        {
            refereePresentationController.ResetPose();
        }

        if (movingGoalController != null)
        {
            movingGoalController.ResetMotion(true);
        }

        if (defenseController != null)
        {
            defenseController.ShowMenuPose(GetStartingYardLine());
        }

        if (goalPresentationController != null)
        {
            goalPresentationController.ResetPresentation();
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.SetClutchMode(false, true);
            cameraJuiceController.ClearBallFollow();
            cameraJuiceController.ResetPresentation();
        }

        if (kickAimGuide != null)
        {
            kickAimGuide.Hide();
        }

        if (ui != null)
        {
            ui.SetControlsEnabled(false);
            ui.HideResults();
            ui.ShowTitleScreen();
            ui.SetScore(0);
            ui.SetRunStats(0, 0, 0);
            ui.SetMultiplier(1, 0);
            ui.SetModeLabel("PICK A MODE");
            ui.SetKickState(GetStartingYardLine(), FieldGoalScoring.GetBasePointsForYardLine(config, GetStartingYardLine()), false);
            ui.SetWind(0f, 0f);
            ui.SetBestRecords(bestScoreRecord, bestLongestKickRecord, bestStreakRecord, rewardProfile.Title, FieldGoalProgression.GetLeaderboardTag(progressionProfile));
            ui.ShowStatus("Pick your run.");
            ui.SetHint("Slide for a lane under the timing bars. Wind starts building on deeper kicks, and 50+ YD bangers are worth 5.");
        }

        SyncPresentationState(false);
    }

    private void SyncPresentationState(bool clutchActive)
    {
        if (cameraJuiceController != null)
        {
            cameraJuiceController.SetPressure(GetCurrentYardLine(), currentWindStrength);
        }

        if (goalPresentationController == null)
        {
            return;
        }

        int multiplier = FieldGoalScoring.GetMultiplier(config, streak, clutchActive);
        goalPresentationController.SetRunIntensity(
            streak,
            multiplier,
            clutchActive,
            modeSelected && IsMovingGoalLive(),
            GetCurrentYardLine(),
            currentWindStrength);
    }

    private void CacheSpawnPointAnchor()
    {
        if (ballSpawnPoint == null || cachedBallSpawnPoint == ballSpawnPoint)
        {
            return;
        }

        cachedBallSpawnPoint = ballSpawnPoint;
        initialBallSpawnLocalPosition = ballSpawnPoint.localPosition;
    }

    private void ApplyCurrentKickSpot(bool resetLanePosition)
    {
        float depthOffset = FieldGoalScoring.GetKickDepthOffset(config, goals);

        if (kickerLaneMover != null)
        {
            kickerLaneMover.SetDepthOffset(depthOffset);
            if (resetLanePosition)
            {
                kickerLaneMover.ResetLanePosition();
            }
        }
        else if (ballSpawnPoint != null && cachedBallSpawnPoint == ballSpawnPoint)
        {
            Vector3 localPosition = initialBallSpawnLocalPosition;
            localPosition.z += depthOffset;
            ballSpawnPoint.localPosition = localPosition;
        }

        if (ui != null && modeSelected)
        {
            ui.SetModeLabel(GetModeHudLabel());
        }
    }

    private void UpdateMovingGoalChallenge(bool snapToCenter)
    {
        if (movingGoalController == null)
        {
            return;
        }

        movingGoalController.SetCurrentYardLine(GetCurrentYardLine(), snapToCenter);
    }

    private void UpdateDefenseChallenge(bool snapToReset)
    {
        if (defenseController == null)
        {
            return;
        }

        defenseController.ResetDefense(GetCurrentYardLine(), goals, snapToReset);
    }

    private int GetCurrentYardLine()
    {
        return FieldGoalScoring.GetCurrentYardLine(config, goals);
    }

    private int GetStartingYardLine()
    {
        return config != null ? Mathf.Max(1, config.startingYardLine) : 20;
    }

    private int GetYardsPerGoalStep()
    {
        return config != null ? Mathf.Max(1, config.yardsPerGoalStep) : 5;
    }

    private int GetMovingGoalStartYardLine()
    {
        return config != null ? Mathf.Max(1, config.movingGoalStartYardLine) : 50;
    }

    private int GetLongBombStartYardLine()
    {
        return config != null ? Mathf.Max(1, config.longBombStartYardLine) : 50;
    }

    private int GetWindStartYardLine()
    {
        return config != null ? Mathf.Max(1, config.windStartYardLine) : 35;
    }

    private int GetCurrentKickBasePoints()
    {
        return FieldGoalScoring.GetBasePointsForYardLine(config, GetCurrentYardLine());
    }

    private string GetModeHudLabel()
    {
        return FieldGoalScoring.GetModeHudLabel(config, currentMode, goals);
    }

    private string GetKickSpotStatus()
    {
        if (IsLongBombLine())
        {
            return GetCurrentYardLine() + "-yard line. 5-point banger territory. Wild timing bars, wind, and swaying uprights.";
        }

        if (IsMovingGoalLive())
        {
            return GetCurrentYardLine() + "-yard line. Timing bars are pulsing, the goal's moving, and the air is live.";
        }

        if (IsWindLive())
        {
            return GetCurrentYardLine() + "-yard line. Read the wind and time the lane under the bars.";
        }

        return GetCurrentYardLine() + "-yard line. Time the bars, find the lane, let it rip.";
    }

    private string GetKickSpotHint()
    {
        int currentYardLine = GetCurrentYardLine();
        int nextYardLine = currentYardLine + GetYardsPerGoalStep();
        int movingGoalStartYardLine = GetMovingGoalStartYardLine();
        int longBombStartYardLine = GetLongBombStartYardLine();
        int windStartYardLine = GetWindStartYardLine();

        if (FieldGoalScoring.IsLongBomb(config, currentYardLine))
        {
            return currentYardLine + "-yard line live. 5-point bangers, wild timing bars, wind, and swaying uprights.";
        }

        if (FieldGoalScoring.IsMovingGoalActive(config, currentYardLine))
        {
            return currentYardLine + "-yard line live. Hit the rhythm gap. The uprights sway and the wind is in play.";
        }

        if (IsWindLive())
        {
            return currentYardLine + "-yard line live. " + FieldGoalWindMath.GetShortCallout(currentWindDirection, currentWindStrength);
        }

        if (nextYardLine >= longBombStartYardLine)
        {
            return currentYardLine + "-yard line live. Next make gets you to the " + nextYardLine + " for a 5-point banger and moving uprights.";
        }

        if (nextYardLine >= movingGoalStartYardLine)
        {
            return currentYardLine + "-yard line live. Next make brings the moving goal at the " + nextYardLine + ".";
        }

        if (nextYardLine >= windStartYardLine)
        {
            return currentYardLine + "-yard line live. Next make brings the wind into play at the " + nextYardLine + ".";
        }

        if (currentYardLine == GetStartingYardLine())
        {
            return currentYardLine + "-yard line start. Move to find a gap under the bars and go back to the " + nextYardLine + ".";
        }

        return currentYardLine + "-yard line live. Next make moves you to the " + nextYardLine + ".";
    }

    private bool IsMovingGoalLive()
    {
        return FieldGoalScoring.IsMovingGoalActive(config, GetCurrentYardLine());
    }

    private bool IsLongBombLine()
    {
        return FieldGoalScoring.IsLongBomb(config, GetCurrentYardLine());
    }

    private bool IsWindLive()
    {
        return currentWindStrength > 0.02f;
    }

    private void RefreshWindForCurrentKick()
    {
        int yardLine = GetCurrentYardLine();
        float windStrength01 = FieldGoalWindMath.GetWindStrength01(config, yardLine);
        currentWindStrength = windStrength01;
        if (windStrength01 <= 0.02f)
        {
            currentWindDirection = 0f;
            return;
        }

        float randomDirection = UnityEngine.Random.Range(-1f, 1f);
        if (Mathf.Abs(randomDirection) < 0.16f)
        {
            randomDirection = Mathf.Sign(UnityEngine.Random.Range(-1f, 1f)) * 0.16f;
        }

        if (Mathf.Abs(currentWindDirection) > 0.02f)
        {
            currentWindDirection = Mathf.Lerp(currentWindDirection, randomDirection, 0.62f);
        }
        else
        {
            currentWindDirection = randomDirection;
        }
    }

    private Vector3 GetCurrentWindAcceleration(bool perfectKick)
    {
        return FieldGoalWindMath.GetWindAcceleration(
            config,
            GetCurrentYardLine(),
            currentWindDirection,
            perfectKick,
            rewardProfile.PerfectWindResistanceBonus);
    }

    private void LoadPersistentRecords()
    {
        FieldGoalProgressionProfile loadedProfile = null;
        if (PlayerPrefs.HasKey(ProgressionProfileKey))
        {
            string json = PlayerPrefs.GetString(ProgressionProfileKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                loadedProfile = JsonUtility.FromJson<FieldGoalProgressionProfile>(json);
            }
        }

        progressionProfile = FieldGoalProgression.Sanitize(loadedProfile);
        progressionProfile.bestScore = Mathf.Max(progressionProfile.bestScore, PlayerPrefs.GetInt(BestScoreKey, 0));
        progressionProfile.bestLongestKick = Mathf.Max(progressionProfile.bestLongestKick, PlayerPrefs.GetInt(BestLongestKickKey, 0));
        progressionProfile.bestStreak = Mathf.Max(progressionProfile.bestStreak, 0);
        progressionProfile.rewardTier = FieldGoalProgression.GetRewardTier(progressionProfile);
        rewardProfile = FieldGoalProgression.BuildRewardProfile(progressionProfile);
        bestScoreRecord = progressionProfile.bestScore;
        bestLongestKickRecord = progressionProfile.bestLongestKick;
        bestStreakRecord = progressionProfile.bestStreak;
    }

    private void SavePersistentRecords()
    {
        progressionProfile = FieldGoalProgression.Sanitize(progressionProfile);
        latestRunPlacement = FieldGoalProgression.RecordRun(
            progressionProfile,
            FieldGoalProgression.CreateRunRecord(currentMode, score, goals, longestMadeKick, bestMultiplier, roundBestStreak),
            roundPerfectGoals,
            roundLongBombGoals);

        rewardProfile = FieldGoalProgression.BuildRewardProfile(progressionProfile);
        bestScoreRecord = progressionProfile.bestScore;
        bestLongestKickRecord = progressionProfile.bestLongestKick;
        bestStreakRecord = progressionProfile.bestStreak;

        PlayerPrefs.SetString(ProgressionProfileKey, JsonUtility.ToJson(progressionProfile));
        PlayerPrefs.SetInt(BestScoreKey, bestScoreRecord);
        PlayerPrefs.SetInt(BestLongestKickKey, bestLongestKickRecord);
        PlayerPrefs.Save();

        if (ui != null)
        {
            ui.SetBestRecords(bestScoreRecord, bestLongestKickRecord, bestStreakRecord, rewardProfile.Title, FieldGoalProgression.GetLeaderboardTag(progressionProfile));
        }
    }

    private string BuildProgressionSummary()
    {
        string placementLabel = FieldGoalProgression.GetPlacementLabel(latestRunPlacement);
        return placementLabel +
            "  |  BEST STREAK " + bestStreakRecord +
            "\n" +
            rewardProfile.Title + " // " + rewardProfile.RewardLabel +
            "\n" +
            rewardProfile.NextUnlockLabel;
    }

    private string GetMissResetStatus(string fallbackStatus)
    {
        GoalCrossingMissType missType = lastCrossingMissType;
        lastCrossingMissType = GoalCrossingMissType.None;

        if (missType == GoalCrossingMissType.None)
        {
            return fallbackStatus;
        }

        return GetNearMissStatus(missType) + " Same line. Clock's still cooking.";
    }

    private static string GetNearMissHeadline(GoalCrossingMissType missType)
    {
        switch (missType)
        {
            case GoalCrossingMissType.WideLeft:
            case GoalCrossingMissType.WideLeftLow:
                return "JUST LEFT";
            case GoalCrossingMissType.WideRight:
            case GoalCrossingMissType.WideRightLow:
                return "JUST RIGHT";
            case GoalCrossingMissType.Low:
                return "JUST LOW";
            default:
                return "NO GOOD";
        }
    }

    private static string GetNearMissSubtitle(GoalCrossingMissType missType)
    {
        switch (missType)
        {
            case GoalCrossingMissType.WideLeft:
                return "Hooked left outside the uprights.";
            case GoalCrossingMissType.WideRight:
                return "Pushed right outside the uprights.";
            case GoalCrossingMissType.Low:
                return "Had the line. Needed more lift.";
            case GoalCrossingMissType.WideLeftLow:
                return "Left and low. Almost a banger.";
            case GoalCrossingMissType.WideRightLow:
                return "Right and low. Almost a banger.";
            default:
                return "Close one.";
        }
    }

    private static string GetNearMissStatus(GoalCrossingMissType missType)
    {
        switch (missType)
        {
            case GoalCrossingMissType.WideLeft:
                return "Hooked left.";
            case GoalCrossingMissType.WideRight:
                return "Pushed right.";
            case GoalCrossingMissType.Low:
                return "Came up short.";
            case GoalCrossingMissType.WideLeftLow:
                return "Low and left.";
            case GoalCrossingMissType.WideRightLow:
                return "Low and right.";
            default:
                return "No good.";
        }
    }

    private void TearDownActiveBall()
    {
        UnsubscribeActiveBall();

        if (activeBall != null)
        {
            if (goalDetector != null)
            {
                goalDetector.ClearTrackedBall();
            }

            if (cameraJuiceController != null)
            {
                cameraJuiceController.ClearBallFollow();
            }

            activeBall.gameObject.SetActive(false);
            activeBall = null;
        }
    }

    private void UnsubscribeActiveBall()
    {
        if (activeBall == null)
        {
            return;
        }

        activeBall.Settled -= OnBallSettled;
        activeBall.OutOfBounds -= OnBallOutOfBounds;
        activeBall.BlockedByDefense -= OnBallBlockedByDefense;
    }

    private FootballProjectile AcquireBallInstance()
    {
        if (footballPrefab == null)
        {
            return null;
        }

        if (pooledBall == null)
        {
            pooledBall = Instantiate(footballPrefab, spawnParent == null ? transform : spawnParent);
        }
        else
        {
            pooledBall.transform.SetParent(spawnParent == null ? transform : spawnParent, false);
            pooledBall.gameObject.SetActive(true);
        }

        return pooledBall;
    }

    private bool CanPreviewKick()
    {
        return roundActive &&
               !waitingForRespawn &&
               activeBall != null &&
               !activeBall.HasBeenKicked &&
               (currentLoopState == FieldGoalLoopState.Aim || currentLoopState == FieldGoalLoopState.ChargingPower);
    }

    private bool CanReleaseKick()
    {
        return roundActive &&
               !waitingForRespawn &&
               activeBall != null &&
               !activeBall.HasBeenKicked &&
               (currentLoopState == FieldGoalLoopState.Aim || currentLoopState == FieldGoalLoopState.ChargingPower);
    }

    private void SetLoopState(FieldGoalLoopState nextState)
    {
        currentLoopState = nextState;
        if (cameraJuiceController != null)
        {
            cameraJuiceController.SetLoopState(nextState);
        }
    }
}
