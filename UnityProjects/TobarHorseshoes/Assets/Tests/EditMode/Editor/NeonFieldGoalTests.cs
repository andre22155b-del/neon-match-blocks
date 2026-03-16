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
    public void GoalDetectorClassifiesWideLeftMiss()
    {
        GoalCrossingEvaluation evaluation = GoalDetector.EvaluateCrossing(
            new Vector3(-3.2f, 3.6f, -0.22f),
            new Vector3(-3.05f, 3.65f, 0.18f),
            2.82f,
            3.05f,
            0.05f);

        Assert.IsTrue(evaluation.HasFrontToBackCrossing);
        Assert.IsFalse(evaluation.IsGoal);
        Assert.AreEqual(GoalCrossingMissType.WideLeft, evaluation.MissType);
    }

    [Test]
    public void GoalDetectorClassifiesWideRightLowMiss()
    {
        GoalCrossingEvaluation evaluation = GoalDetector.EvaluateCrossing(
            new Vector3(3.1f, 2.75f, -0.24f),
            new Vector3(3.2f, 2.8f, 0.22f),
            2.82f,
            3.05f,
            0.05f);

        Assert.IsTrue(evaluation.HasFrontToBackCrossing);
        Assert.IsFalse(evaluation.IsGoal);
        Assert.AreEqual(GoalCrossingMissType.WideRightLow, evaluation.MissType);
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
        Assert.NotNull(FieldGoalAudioFactory.GetCueClip(FieldGoalAudioCue.Perfect));
        Assert.NotNull(FieldGoalAudioFactory.GetCueClip(FieldGoalAudioCue.LongBomb));
        Assert.NotNull(FieldGoalAudioFactory.GetCueClip(FieldGoalAudioCue.FinalDrive));
    }

    [Test]
    public void AudioIdentityPrioritizesLongBombCueOverPerfectAndClutch()
    {
        FieldGoalScoreResult result = new FieldGoalScoreResult(
            pointsAwarded: 20,
            rawPoints: 10,
            multiplier: 2,
            clutchActive: true,
            perfectKick: true,
            yardLine: 55,
            longBomb: true);

        Assert.AreEqual(FieldGoalAudioCue.LongBomb, FieldGoalAudioIdentity.GetGoalCue(result));
    }

    [Test]
    public void AudioIdentityUsesStreakBreakCueWhenHeatDies()
    {
        FieldGoalAudioCue cue = FieldGoalAudioIdentity.GetMissCue(
            GoalCrossingMissType.None,
            longBombLine: false,
            clutchActive: false,
            brokeHeat: true);

        Assert.AreEqual(FieldGoalAudioCue.StreakBreak, cue);
    }

    [Test]
    public void AudioIdentityAmbientScaleRisesInClutchWithMovingGoal()
    {
        float baseScale = FieldGoalAudioIdentity.GetAmbientScale(false, 1, false);
        float hypedScale = FieldGoalAudioIdentity.GetAmbientScale(true, 4, true);

        Assert.Greater(hypedScale, baseScale);
    }

    [Test]
    public void AudioIdentityBlocksLowerPriorityCueInsideCooldown()
    {
        bool shouldPlay = FieldGoalAudioIdentity.ShouldPlayCue(
            FieldGoalAudioCue.NearMiss,
            FieldGoalAudioCue.LongBomb,
            0.08f,
            0.18f);

        Assert.IsFalse(shouldPlay);
    }

    [Test]
    public void AudioIdentityAllowsHigherPriorityCueToOverrideCooldown()
    {
        bool shouldPlay = FieldGoalAudioIdentity.ShouldPlayCue(
            FieldGoalAudioCue.LongBomb,
            FieldGoalAudioCue.NearMiss,
            0.08f,
            0.18f);

        Assert.IsTrue(shouldPlay);
    }

    [Test]
    public void AudioIdentityUsesStrongerDuckForLongBombThanNearMiss()
    {
        float longBombDuck = FieldGoalAudioIdentity.GetCueDuckIntensity(FieldGoalAudioCue.LongBomb);
        float nearMissDuck = FieldGoalAudioIdentity.GetCueDuckIntensity(FieldGoalAudioCue.NearMiss);

        Assert.Greater(longBombDuck, nearMissDuck);
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
        config.longBombStartYardLine = 50;
        config.longBombPointsPerGoal = 5;
        config.perfectKickBonusPoints = 2;
        config.clutchBonusPoints = 1;
        config.clutchPerfectBonusPoints = 2;
        config.makesPerMultiplierStep = 2;
        config.maxScoreMultiplier = 5;
        config.clutchMinimumMultiplier = 2;
        config.clutchWindowSeconds = 10f;

        FieldGoalScoreResult result = FieldGoalScoring.Evaluate(config, FieldGoalMode.ArcadeRush, 8f, 3, true, 35);

        Assert.AreEqual(2, result.Multiplier);
        Assert.AreEqual(8, result.RawPoints);
        Assert.AreEqual(16, result.PointsAwarded);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void LongBombBasePointsJumpToFiveAtFiftyYards()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.pointsPerGoal = 3;
        config.longBombStartYardLine = 50;
        config.longBombPointsPerGoal = 5;

        Assert.AreEqual(3, FieldGoalScoring.GetBasePointsForYardLine(config, 45));
        Assert.AreEqual(5, FieldGoalScoring.GetBasePointsForYardLine(config, 50));
        Assert.IsTrue(FieldGoalScoring.IsLongBomb(config, 55));

        Object.DestroyImmediate(config);
    }

    [Test]
    public void YardLineProgressionStartsAtTwentyAndAddsFivePerGoal()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.startingYardLine = 20;
        config.yardsPerGoalStep = 5;

        Assert.AreEqual(20, FieldGoalScoring.GetCurrentYardLine(config, 0));
        Assert.AreEqual(25, FieldGoalScoring.GetCurrentYardLine(config, 1));
        Assert.AreEqual(35, FieldGoalScoring.GetCurrentYardLine(config, 3));

        Object.DestroyImmediate(config);
    }

    [Test]
    public void KickDepthOffsetMovesBackFiveTenthsPerMadeKickByDefault()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.startingYardLine = 20;
        config.yardsPerGoalStep = 5;
        config.worldUnitsPerYard = 0.1f;

        Assert.AreEqual(0f, FieldGoalScoring.GetKickDepthOffset(config, 0));
        Assert.AreEqual(-0.5f, FieldGoalScoring.GetKickDepthOffset(config, 1));
        Assert.AreEqual(-1f, FieldGoalScoring.GetKickDepthOffset(config, 2));

        Object.DestroyImmediate(config);
    }

    [Test]
    public void KickReleaseBlendStartsAtZeroAndEndsAtOne()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.kickReleaseEase = 1.45f;

        Assert.AreEqual(0f, FieldGoalKickMath.EvaluateKickReleaseBlend(config, 0f), 0.0001f);
        Assert.Greater(FieldGoalKickMath.EvaluateKickReleaseBlend(config, 0.5f), 0.5f);
        Assert.AreEqual(1f, FieldGoalKickMath.EvaluateKickReleaseBlend(config, 1f), 0.0001f);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void GravityScaleIsLighterOnReleaseAndHeavierOnFall()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.releaseGravityScale = 0.72f;
        config.risingGravityScale = 0.94f;
        config.fallingGravityScale = 1.08f;

        float releaseScale = FieldGoalKickMath.GetGravityScale(config, 4f, 0f);
        float risingScale = FieldGoalKickMath.GetGravityScale(config, 4f, 1f);
        float fallingScale = FieldGoalKickMath.GetGravityScale(config, -6f, 1f);

        Assert.Less(releaseScale, risingScale);
        Assert.Greater(fallingScale, risingScale);

        Object.DestroyImmediate(config);
    }

    [Test]
    public void MovingGoalActivatesAtFiftyYardLine()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.movingGoalStartYardLine = 50;

        Assert.IsFalse(FieldGoalScoring.IsMovingGoalActive(config, 45));
        Assert.IsTrue(FieldGoalScoring.IsMovingGoalActive(config, 50));

        Object.DestroyImmediate(config);
    }

    [Test]
    public void MovingGoalAmplitudeAndSpeedRampWithLongerKicks()
    {
        NeonFieldGoalConfig config = ScriptableObject.CreateInstance<NeonFieldGoalConfig>();
        config.movingGoalStartYardLine = 50;
        config.movingGoalFullChallengeYardLine = 65;
        config.movingGoalBaseSideOffset = 0.34f;
        config.movingGoalExtraSideOffset = 0.18f;
        config.movingGoalBaseSpeed = 0.32f;
        config.movingGoalExtraSpeed = 0.18f;

        Assert.AreEqual(0f, FieldGoalScoring.GetMovingGoalAmplitude(config, 45), 0.0001f);
        Assert.AreEqual(0.34f, FieldGoalScoring.GetMovingGoalAmplitude(config, 50), 0.0001f);
        Assert.Greater(FieldGoalScoring.GetMovingGoalAmplitude(config, 65), FieldGoalScoring.GetMovingGoalAmplitude(config, 50));
        Assert.Greater(FieldGoalScoring.GetMovingGoalSpeed(config, 65), FieldGoalScoring.GetMovingGoalSpeed(config, 50));

        Object.DestroyImmediate(config);
    }
}
