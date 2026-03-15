using System;
using UnityEngine;

public class FieldGoalGameManager : MonoBehaviour
{
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

    [Header("Scene")]
    public Transform ballSpawnPoint;
    public Transform spawnParent;
    public FootballProjectile footballPrefab;

    private FootballProjectile activeBall;
    private float timeLeft;
    private float respawnTimer;
    private int goals;
    private int score;
    private int streak;
    private int bestMultiplier;
    private bool finalDriveTriggered;
    private bool lastKickWasPerfect;
    private bool modeSelected;
    private bool roundActive;
    private bool waitingForRespawn;
    private FieldGoalMode currentMode;

    private void Start()
    {
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
        }

        if (ui != null)
        {
            ui.RestartPressed += RestartRound;
            ui.ModeSelectPressed += ShowModeSelect;
            ui.ModeSelected += StartSelectedMode;
            ui.BindControls(kickerLaneMover, kickInputController);
        }

        ApplyConfig();
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

        if (kickAimGuide != null)
        {
            kickAimGuide.ApplyConfig(config);
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.ApplyConfig(config);
        }
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
        Shader shader = Shader.Find("Sprites/Default");
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
        return material;
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

    private void StartRound()
    {
        goals = 0;
        score = 0;
        streak = 0;
        bestMultiplier = 1;
        lastKickWasPerfect = false;
        timeLeft = FieldGoalScoring.GetRoundDuration(config, currentMode);
        finalDriveTriggered = FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft);
        waitingForRespawn = false;
        roundActive = true;

        if (kickerLaneMover != null)
        {
            kickerLaneMover.ResetLanePosition();
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
            ui.SetMultiplier(FieldGoalScoring.GetMultiplier(config, 0, finalDriveTriggered), 0);
            ui.SetModeLabel(FieldGoalScoring.GetModeLabel(currentMode));
            ui.SetTimer(timeLeft);
            if (currentMode == FieldGoalMode.ClutchBlitz)
            {
                ui.ShowModeIntro("CLUTCH BLITZ", "35 seconds. Every kick is pressure.");
                ui.ShowStatus("Pressure starts now.");
                ui.SetHint("Clutch scoring is live from snap one.");
            }
            else
            {
                ui.ShowModeIntro("ARCADE RUSH", "Build heat and cash out in clutch.");
                ui.ShowStatus("Line it up and let it fly.");
                ui.SetHint("Left thumb moves. Right thumb swipes up to kick.");
            }
        }

        if (goalPresentationController != null)
        {
            goalPresentationController.ResetPresentation();
            goalPresentationController.PlayAmbient();
            if (finalDriveTriggered)
            {
                goalPresentationController.PlayFinalDrive();
            }
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.SetClutchMode(finalDriveTriggered, true);
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
            ui.ShowResults(score, goals, bestMultiplier, FieldGoalScoring.GetModeLabel(currentMode));
        }

        RoundEnded?.Invoke(score);
    }

    private void SpawnBall()
    {
        TearDownActiveBall();

        if (footballPrefab == null || ballSpawnPoint == null)
        {
            return;
        }

        activeBall = Instantiate(footballPrefab, spawnParent == null ? transform : spawnParent);
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

        if (kickAimGuide != null)
        {
            kickAimGuide.Hide();
        }

        if (ui != null && roundActive)
        {
            ui.ShowStatus("Swipe up and split the uprights.");
        }
    }

    private void OnKickReleased(float power, float aim)
    {
        if (!roundActive || timeLeft <= 0f || waitingForRespawn || activeBall == null || activeBall.HasBeenKicked || ballSpawnPoint == null || config == null)
        {
            return;
        }

        Vector3 targetPosition = goalDetector != null ? goalDetector.transform.position : ballSpawnPoint.position + ballSpawnPoint.forward * 10f;
        Vector3 launchDirection = FieldGoalKickMath.ComputeLaunchDirection(ballSpawnPoint, targetPosition, config, aim);
        float impulse = FieldGoalKickMath.ComputeImpulse(config, power);
        float curveTorque = FieldGoalKickMath.ComputeCurveTorque(config, aim, power);
        bool perfectKick = FieldGoalKickMath.IsPerfectKick(config, power, aim);
        lastKickWasPerfect = perfectKick;

        activeBall.Kick(launchDirection, impulse, curveTorque);

        if (goalPresentationController != null)
        {
            goalPresentationController.PlayKick();
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.PlayKick(power, perfectKick);
        }

        if (ui != null)
        {
            if (perfectKick)
            {
                ui.ShowPerfectStrike();
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

        streak += 1;
        goals += 1;
        FieldGoalScoreResult scoreResult = FieldGoalScoring.Evaluate(config, currentMode, timeLeft, streak, lastKickWasPerfect);
        score += scoreResult.PointsAwarded;
        bestMultiplier = Mathf.Max(bestMultiplier, scoreResult.Multiplier);

        if (ui != null)
        {
            ui.SetScore(score);
            ui.SetMultiplier(scoreResult.Multiplier, streak);
            ui.PulseGoals();
            ui.ShowGoalBurst(scoreResult.PointsAwarded, scoreResult.Multiplier, scoreResult.PerfectKick, scoreResult.ClutchActive);
            ui.ShowStatus(scoreResult.ClutchActive ? "Clutch cash." : "It's good.");
            ui.SetHint("3 points per make. Perfect strikes and clutch stack with heat.");
        }

        if (goalPresentationController != null)
        {
            goalPresentationController.PlayGoalCelebration();
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.PlayGoal(scoreResult.Multiplier, scoreResult.PerfectKick, scoreResult.ClutchActive);
        }

        if (refereePresentationController != null)
        {
            refereePresentationController.CelebrateGoal();
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

        if (goalPresentationController != null)
        {
            goalPresentationController.PlayMissFeedback();
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.PlayMiss();
        }

        ResetStreak("No good. Multiplier reset.");
        ScheduleRespawn(config != null ? config.respawnDelayAfterMiss : 0.65f);
    }

    private void OnBallOutOfBounds(FootballProjectile ball)
    {
        if (!roundActive || waitingForRespawn || ball == null || ball != activeBall || ball.HasScored)
        {
            return;
        }

        if (goalPresentationController != null)
        {
            goalPresentationController.PlayMissFeedback();
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.PlayMiss();
        }

        ResetStreak("Out of range. Multiplier reset.");
        ScheduleRespawn(config != null ? config.respawnDelayAfterMiss : 0.65f);
    }

    private void OnKickPreviewChanged(float power, float aim, bool active)
    {
        if (kickAimGuide == null)
        {
            return;
        }

        if (!active || !roundActive || waitingForRespawn || activeBall == null || activeBall.HasBeenKicked || ballSpawnPoint == null || config == null)
        {
            kickAimGuide.Hide();
            return;
        }

        Vector3 targetPosition = goalDetector != null ? goalDetector.transform.position : ballSpawnPoint.position + ballSpawnPoint.forward * 10f;
        kickAimGuide.UpdatePreview(ballSpawnPoint, targetPosition, power, aim);
    }

    private void ScheduleRespawn(float delay)
    {
        waitingForRespawn = true;
        respawnTimer = Mathf.Max(0.1f, delay);
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

        TearDownActiveBall();

        if (goalDetector != null)
        {
            goalDetector.ClearTrackedBall();
        }

        if (kickerLaneMover != null)
        {
            kickerLaneMover.ResetLanePosition();
            kickerLaneMover.SetControlsEnabled(false);
        }

        if (kickInputController != null)
        {
            kickInputController.SetInputEnabled(false);
        }

        if (refereePresentationController != null)
        {
            refereePresentationController.ResetPose();
        }

        if (goalPresentationController != null)
        {
            goalPresentationController.ResetPresentation();
        }

        if (cameraJuiceController != null)
        {
            cameraJuiceController.SetClutchMode(false, true);
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
            ui.SetMultiplier(1, 0);
            ui.SetModeLabel("PICK A MODE");
            ui.ShowStatus("Choose your mode.");
            ui.SetHint("Arcade Rush ramps into clutch. Clutch Blitz starts hot.");
        }
    }

    private void TearDownActiveBall()
    {
        UnsubscribeActiveBall();

        if (activeBall != null)
        {
            Destroy(activeBall.gameObject);
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
    }
}
