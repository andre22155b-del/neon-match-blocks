using NUnit.Framework;
using UnityEngine;

public class NeonFieldGoalTests
{
    [Test]
    public void GoalDetectorAcceptsValidFrontToBackCrossing()
    {
        bool result = GoalDetector.IsValidGoalCrossing(
            new Vector3(0.1f, 3.6f, -0.25f),
            new Vector3(0.2f, 3.7f, 0.32f),
            2.82f,
            3.05f,
            0.05f);

        Assert.IsTrue(result);
    }

    [Test]
    public void GoalDetectorRejectsBackToFrontCrossing()
    {
        bool result = GoalDetector.IsValidGoalCrossing(
            new Vector3(0f, 3.8f, 0.22f),
            new Vector3(0f, 3.8f, -0.18f),
            2.82f,
            3.05f,
            0.05f);

        Assert.IsFalse(result);
    }

    [Test]
    public void GoalDetectorRejectsLowCrossing()
    {
        bool result = GoalDetector.IsValidGoalCrossing(
            new Vector3(0f, 2.7f, -0.25f),
            new Vector3(0f, 2.8f, 0.2f),
            2.82f,
            3.05f,
            0.05f);

        Assert.IsFalse(result);
    }

    [Test]
    public void FootballProjectileOnlyScoresOnce()
    {
        GameObject go = new GameObject("FootballProjectileTest");
        go.AddComponent<Rigidbody>();
        FootballProjectile projectile = go.AddComponent<FootballProjectile>();

        Assert.IsTrue(projectile.MarkGoalScored());
        Assert.IsFalse(projectile.MarkGoalScored());

        Object.DestroyImmediate(go);
    }

    [Test]
    public void AudioFactoryProvidesFallbackClips()
    {
        Assert.NotNull(FieldGoalAudioFactory.GetKickClip());
        Assert.NotNull(FieldGoalAudioFactory.GetGoalClip());
        Assert.NotNull(FieldGoalAudioFactory.GetMissClip());
        Assert.NotNull(FieldGoalAudioFactory.GetCrowdClip());
        Assert.NotNull(FieldGoalAudioFactory.GetAmbientClip());
    }

    [Test]
    public void ComputeImpulseUsesConfiguredRange()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.minKickImpulse = 7f;
        config.maxKickImpulse = 11f;
        config.kickPowerExponent = 1f;
        config.perfectKickImpulseBonus = 0f;

        Assert.AreEqual(7f, FieldGoalKickMath.ComputeImpulse(config, 0f));
        Assert.AreEqual(9f, FieldGoalKickMath.ComputeImpulse(config, 0.5f));
        Assert.AreEqual(11f, FieldGoalKickMath.ComputeImpulse(config, 1f));

        Object.DestroyImmediate(config);
    }

    [Test]
    public void AimAssistPullsLaunchDirectionCloserToTargetLane()
    {
        GameObject origin = new GameObject("Origin");
        origin.transform.position = new Vector3(3f, 0f, 0f);
        origin.transform.rotation = Quaternion.identity;

        NeonFieldGoalConfig noAssistConfig = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        noAssistConfig.maxAimDegrees = 10f;
        noAssistConfig.launchAngleDegrees = 36f;
        noAssistConfig.aimAssistStrength = 0f;

        NeonFieldGoalConfig assistConfig = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        assistConfig.maxAimDegrees = 10f;
        assistConfig.launchAngleDegrees = 36f;
        assistConfig.aimAssistStrength = 0.5f;

        Vector3 target = new Vector3(0f, 0f, 21.5f);
        Vector3 noAssistDirection = Vector3.ProjectOnPlane(
            FieldGoalKickMath.ComputeLaunchDirection(origin.transform, target, noAssistConfig, 1f),
            Vector3.up).normalized;
        Vector3 assistDirection = Vector3.ProjectOnPlane(
            FieldGoalKickMath.ComputeLaunchDirection(origin.transform, target, assistConfig, 1f),
            Vector3.up).normalized;
        Vector3 targetDirection = Vector3.ProjectOnPlane(target - origin.transform.position, Vector3.up).normalized;

        Assert.Greater(Vector3.Dot(assistDirection, targetDirection), Vector3.Dot(noAssistDirection, targetDirection));

        Object.DestroyImmediate(origin);
        Object.DestroyImmediate(noAssistConfig);
        Object.DestroyImmediate(assistConfig);
    }

    [Test]
    public void ShapePowerBoostsMidRangeArcadeSwipes()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.kickPowerExponent = 0.82f;

        float shaped = FieldGoalKickMath.ShapePower(config, 0.5f);

        Assert.Greater(shaped, 0.5f);
        Assert.Less(shaped, 1f);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void PerfectKickAddsImpulseBonus()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.minKickImpulse = 7f;
        config.maxKickImpulse = 11f;
        config.kickPowerExponent = 1f;
        config.perfectKickCenter = 0.85f;
        config.perfectKickWindow = 0.1f;
        config.perfectKickImpulseBonus = 0.5f;

        float nearPerfect = FieldGoalKickMath.ComputeImpulse(config, 0.85f);
        float offCenter = FieldGoalKickMath.ComputeImpulse(config, 0.65f);

        Assert.Greater(nearPerfect, offCenter);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void CameraMathBoostsPerfectKickTrauma()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.cameraKickShake = 0.14f;

        float regularTrauma = FieldGoalCameraMath.ComputeKickTrauma(config, 0.85f, false);
        float perfectTrauma = FieldGoalCameraMath.ComputeKickTrauma(config, 0.85f, true);

        Assert.Greater(perfectTrauma, regularTrauma);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void CameraMathReturnsGoalSlowMotionForPerfectGoals()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.perfectKickSlowMoScale = 0.86f;
        config.perfectKickSlowMoDuration = 0.08f;
        config.perfectGoalSlowMoScale = 0.72f;
        config.perfectGoalSlowMoDuration = 0.12f;

        FieldGoalSlowMotion kickSlowMotion = FieldGoalCameraMath.GetSlowMotion(config, false, true);
        FieldGoalSlowMotion goalSlowMotion = FieldGoalCameraMath.GetSlowMotion(config, true, true);

        Assert.IsTrue(goalSlowMotion.IsActive);
        Assert.Less(goalSlowMotion.Scale, kickSlowMotion.Scale);
        Assert.Greater(goalSlowMotion.Duration, kickSlowMotion.Duration);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void ClutchBlitzAlwaysForcesClutchRules()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.clutchWindowSeconds = 10f;

        Assert.IsTrue(FieldGoalScoring.IsClutchActive(config, FieldGoalMode.ClutchBlitz, 28f));
        Assert.IsFalse(FieldGoalScoring.IsClutchActive(config, FieldGoalMode.ArcadeRush, 28f));

        Object.DestroyImmediate(config);
    }

    [Test]
    public void ClutchModeStartsAtConfiguredMinimumMultiplier()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.clutchMinimumMultiplier = 2;
        config.maxScoreMultiplier = 5;
        config.makesPerMultiplierStep = 2;

        int multiplier = FieldGoalScoring.GetMultiplier(config, 0, true);

        Assert.AreEqual(2, multiplier);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void ModeSpecificRoundDurationsUseConfigValues()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.roundDurationSeconds = 60f;
        config.clutchModeRoundDurationSeconds = 35f;

        Assert.AreEqual(60f, FieldGoalScoring.GetRoundDuration(config, FieldGoalMode.ArcadeRush));
        Assert.AreEqual(35f, FieldGoalScoring.GetRoundDuration(config, FieldGoalMode.ClutchBlitz));

        Object.DestroyImmediate(config);
    }

    [Test]
    public void ScoreEvaluationUsesThreePointBaseAndMultiplier()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.pointsPerGoal = 3;
        config.perfectKickBonusPoints = 2;
        config.clutchBonusPoints = 1;
        config.clutchPerfectBonusPoints = 2;
        config.makesPerMultiplierStep = 2;
        config.maxScoreMultiplier = 5;
        config.clutchMinimumMultiplier = 2;
        config.clutchWindowSeconds = 10f;

        FieldGoalScoreResult result = FieldGoalScoring.Evaluate(config, FieldGoalMode.ArcadeRush, 8f, 3, true);

        Assert.AreEqual(2, result.Multiplier);
        Assert.AreEqual(8, result.RawPoints);
        Assert.AreEqual(16, result.PointsAwarded);

        Object.DestroyImmediate(config);
    }
}
