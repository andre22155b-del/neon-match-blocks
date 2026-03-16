using System;
using UnityEngine;

public class FieldGoalGameManager : MonoBehaviour
{
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
    private int longestMadeKick;
    private int bestScoreRecord;
    private int bestLongestKickRecord;
    private GoalCrossingMissType lastCrossingMissType;
    private bool finalDriveTriggered;
    private bool lastKickWasPerfect;
    private bool modeSelected;
    private bool roundActive;
    private bool waitingForRespawn;
    private FieldGoalMode currentMode;
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
        longestMadeKick = 0;
        lastCrossingMissType = GoalCrossingMissType.None;
        lastKickWasPerfect = false;
        timeLeft = FieldGoalScoring.GetRoundDuration(config, currentMode);
        finalDriveTriggered = FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft);
        waitingForRespawn = false;
        roundActive = true;

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
            ui.SetRunStats(goals, longestMadeKick);
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
                ui.ShowModeIntro("ARCADE RUSH", "Build heat and cash out in clutch.");
                ui.ShowStatus("Line it up and let it fly.");
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
            ui.ShowResults(score, goals, longestMadeKick, bestMultiplier, FieldGoalScoring.GetModeLabel(currentMode));
        }

        SavePersistentRecords();

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

        lastCrossingMissType = GoalCrossingMissType.None;
        UpdateMovingGoalChallenge(snapToCenter: false);

        if (kickAimGuide != null)
        {
            kickAimGuide.Hide();
        }

        if (ui != null && roundActive)
        {
            ui.SetRunStats(goals, longestMadeKick);
            ui.SetModeLabel(GetModeHudLabel());
            ui.SetKickState(GetCurrentYardLine(), GetCurrentKickBasePoints(), IsMovingGoalLive());
            ui.ShowStatus(GetKickSpotStatus());
            ui.SetHint(GetKickSpotHint());
        }

        SyncPresentationState(roundActive && FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft));
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
        bool clutchActive = FieldGoalScoring.IsClutchActive(config, currentMode, timeLeft);
        bool longBombLine = FieldGoalScoring.IsLongBomb(config, GetCurrentYardLine());
        lastKickWasPerfect = perfectKick;

        activeBall.Kick(launchDirection, impulse, curveTorque);

        if (goalPresentationController != null)
        {
            goalPresentationController.PlayKick(longBombLine, perfectKick, clutchActive);
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
            else if (FieldGoalScoring.IsLongBomb(config, GetCurrentYardLine()))
            {
                ui.ShowStatus("Launch the banger.");
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

        int madeYardLine = GetCurrentYardLine();
        bool movingGoalWasLive = FieldGoalScoring.IsMovingGoalActive(config, madeYardLine);
        streak += 1;
        goals += 1;
        FieldGoalScoreResult scoreResult = FieldGoalScoring.Evaluate(config, currentMode, timeLeft, streak, lastKickWasPerfect, madeYardLine);
        score += scoreResult.PointsAwarded;
        bestMultiplier = Mathf.Max(bestMultiplier, scoreResult.Multiplier);
        longestMadeKick = Mathf.Max(longestMadeKick, scoreResult.YardLine);

        if (ui != null)
        {
            ui.SetScore(score);
            ui.SetRunStats(goals, longestMadeKick);
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
            ui.SetRunStats(0, 0);
            ui.SetMultiplier(1, 0);
            ui.SetModeLabel("PICK A MODE");
            ui.SetKickState(GetStartingYardLine(), FieldGoalScoring.GetBasePointsForYardLine(config, GetStartingYardLine()), false);
            ui.SetBestRecords(bestScoreRecord, bestLongestKickRecord);
            ui.ShowStatus("Pick your run.");
            ui.SetHint("50+ YD bangers are worth 5. Misses keep you on the same line while the clock burns.");
        }

        SyncPresentationState(false);
    }

    private void SyncPresentationState(bool clutchActive)
    {
        if (goalPresentationController == null)
        {
            return;
        }

        int multiplier = FieldGoalScoring.GetMultiplier(config, streak, clutchActive);
        goalPresentationController.SetRunIntensity(streak, multiplier, clutchActive, modeSelected && IsMovingGoalLive());
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
            return GetCurrentYardLine() + "-yard line. 5-point banger territory. Goal's swaying.";
        }

        if (IsMovingGoalLive())
        {
            return GetCurrentYardLine() + "-yard line. Goal's moving side to side.";
        }

        return GetCurrentYardLine() + "-yard line. Swipe up and split the uprights.";
    }

    private string GetKickSpotHint()
    {
        int currentYardLine = GetCurrentYardLine();
        int nextYardLine = currentYardLine + GetYardsPerGoalStep();
        int movingGoalStartYardLine = GetMovingGoalStartYardLine();
        int longBombStartYardLine = GetLongBombStartYardLine();

        if (FieldGoalScoring.IsLongBomb(config, currentYardLine))
        {
            return currentYardLine + "-yard line live. 5-point bangers and swaying uprights.";
        }

        if (FieldGoalScoring.IsMovingGoalActive(config, currentYardLine))
        {
            return currentYardLine + "-yard line live. The uprights are swaying now.";
        }

        if (nextYardLine >= longBombStartYardLine)
        {
            return currentYardLine + "-yard line live. Next make gets you to the " + nextYardLine + " for a 5-point banger and moving uprights.";
        }

        if (nextYardLine >= movingGoalStartYardLine)
        {
            return currentYardLine + "-yard line live. Next make brings the moving goal at the " + nextYardLine + ".";
        }

        if (currentYardLine == GetStartingYardLine())
        {
            return currentYardLine + "-yard line start. Make it and go back to the " + nextYardLine + ".";
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

    private void LoadPersistentRecords()
    {
        bestScoreRecord = PlayerPrefs.GetInt(BestScoreKey, 0);
        bestLongestKickRecord = PlayerPrefs.GetInt(BestLongestKickKey, 0);
    }

    private void SavePersistentRecords()
    {
        bool changed = false;

        if (score > bestScoreRecord)
        {
            bestScoreRecord = score;
            PlayerPrefs.SetInt(BestScoreKey, bestScoreRecord);
            changed = true;
        }

        if (longestMadeKick > bestLongestKickRecord)
        {
            bestLongestKickRecord = longestMadeKick;
            PlayerPrefs.SetInt(BestLongestKickKey, bestLongestKickRecord);
            changed = true;
        }

        if (changed)
        {
            PlayerPrefs.Save();
        }

        if (ui != null)
        {
            ui.SetBestRecords(bestScoreRecord, bestLongestKickRecord);
        }
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
