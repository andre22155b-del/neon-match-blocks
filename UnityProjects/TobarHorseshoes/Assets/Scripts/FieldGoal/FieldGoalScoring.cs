using UnityEngine;

public enum FieldGoalMode
{
    ArcadeRush = 0,
    ClutchBlitz = 1
}

public struct FieldGoalScoreResult
{
    public FieldGoalScoreResult(int pointsAwarded, int rawPoints, int multiplier, bool clutchActive, bool perfectKick)
    {
        PointsAwarded = pointsAwarded;
        RawPoints = rawPoints;
        Multiplier = multiplier;
        ClutchActive = clutchActive;
        PerfectKick = perfectKick;
    }

    public int PointsAwarded { get; }
    public int RawPoints { get; }
    public int Multiplier { get; }
    public bool ClutchActive { get; }
    public bool PerfectKick { get; }
}

public static class FieldGoalScoring
{
    public static float GetRoundDuration(NeonFieldGoalConfig config, FieldGoalMode mode)
    {
        if (config == null)
        {
            return mode == FieldGoalMode.ClutchBlitz ? 35f : 60f;
        }

        return mode == FieldGoalMode.ClutchBlitz
            ? config.clutchModeRoundDurationSeconds
            : config.roundDurationSeconds;
    }

    public static bool IsClutchActive(NeonFieldGoalConfig config, FieldGoalMode mode, float timeLeft)
    {
        if (mode == FieldGoalMode.ClutchBlitz)
        {
            return true;
        }

        float clutchWindowSeconds = config != null ? config.clutchWindowSeconds : 10f;
        return timeLeft <= clutchWindowSeconds;
    }

    public static int GetMultiplier(NeonFieldGoalConfig config, int madeStreak, bool clutchActive)
    {
        int streak = Mathf.Max(0, madeStreak);
        int makesPerStep = config != null ? Mathf.Max(1, config.makesPerMultiplierStep) : 2;
        int multiplier = 1 + Mathf.Max(0, streak - 1) / makesPerStep;

        if (clutchActive)
        {
            int clutchMinimum = config != null ? Mathf.Max(1, config.clutchMinimumMultiplier) : 2;
            multiplier = Mathf.Max(multiplier, clutchMinimum);
        }

        int maximumMultiplier = config != null ? Mathf.Max(1, config.maxScoreMultiplier) : 5;
        return Mathf.Clamp(multiplier, 1, maximumMultiplier);
    }

    public static FieldGoalScoreResult Evaluate(
        NeonFieldGoalConfig config,
        FieldGoalMode mode,
        float timeLeft,
        int madeStreak,
        bool perfectKick)
    {
        bool clutchActive = IsClutchActive(config, mode, timeLeft);
        int multiplier = GetMultiplier(config, madeStreak, clutchActive);
        int rawPoints = config != null ? Mathf.Max(1, config.pointsPerGoal) : 3;

        if (perfectKick)
        {
            rawPoints += config != null ? Mathf.Max(0, config.perfectKickBonusPoints) : 2;
        }

        if (clutchActive)
        {
            rawPoints += config != null ? Mathf.Max(0, config.clutchBonusPoints) : 1;
        }

        if (perfectKick && clutchActive)
        {
            rawPoints += config != null ? Mathf.Max(0, config.clutchPerfectBonusPoints) : 2;
        }

        return new FieldGoalScoreResult(rawPoints * multiplier, rawPoints, multiplier, clutchActive, perfectKick);
    }

    public static string GetModeLabel(FieldGoalMode mode)
    {
        return mode == FieldGoalMode.ClutchBlitz ? "CLUTCH BLITZ" : "ARCADE RUSH";
    }
}
