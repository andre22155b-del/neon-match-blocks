using UnityEngine;

public enum FieldGoalMode
{
    ArcadeRush = 0,
    ClutchBlitz = 1
}

public struct FieldGoalScoreResult
{
    public FieldGoalScoreResult(int pointsAwarded, int rawPoints, int multiplier, bool clutchActive, bool perfectKick, int yardLine, bool longBomb)
    {
        PointsAwarded = pointsAwarded;
        RawPoints = rawPoints;
        Multiplier = multiplier;
        ClutchActive = clutchActive;
        PerfectKick = perfectKick;
        YardLine = yardLine;
        LongBomb = longBomb;
    }

    public int PointsAwarded { get; }
    public int RawPoints { get; }
    public int Multiplier { get; }
    public bool ClutchActive { get; }
    public bool PerfectKick { get; }
    public int YardLine { get; }
    public bool LongBomb { get; }
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
        bool perfectKick,
        int yardLine,
        int extraPerfectPointBonus = 0)
    {
        bool clutchActive = IsClutchActive(config, mode, timeLeft);
        int multiplier = GetMultiplier(config, madeStreak, clutchActive);
        int normalizedYardLine = Mathf.Max(1, yardLine);
        bool longBomb = IsLongBomb(config, normalizedYardLine);
        int rawPoints = GetBasePointsForYardLine(config, normalizedYardLine);

        if (perfectKick)
        {
            rawPoints += config != null ? Mathf.Max(0, config.perfectKickBonusPoints) : 2;
            rawPoints += Mathf.Max(0, extraPerfectPointBonus);
        }

        if (clutchActive)
        {
            rawPoints += config != null ? Mathf.Max(0, config.clutchBonusPoints) : 1;
        }

        if (perfectKick && clutchActive)
        {
            rawPoints += config != null ? Mathf.Max(0, config.clutchPerfectBonusPoints) : 2;
        }

        return new FieldGoalScoreResult(rawPoints * multiplier, rawPoints, multiplier, clutchActive, perfectKick, normalizedYardLine, longBomb);
    }

    public static int GetCurrentYardLine(NeonFieldGoalConfig config, int goalsMade)
    {
        int startingYardLine = config != null ? Mathf.Max(1, config.startingYardLine) : 20;
        int yardsPerGoalStep = config != null ? Mathf.Max(1, config.yardsPerGoalStep) : 5;
        return startingYardLine + Mathf.Max(0, goalsMade) * yardsPerGoalStep;
    }

    public static float GetKickDepthOffset(NeonFieldGoalConfig config, int goalsMade)
    {
        int startingYardLine = config != null ? Mathf.Max(1, config.startingYardLine) : 20;
        int currentYardLine = GetCurrentYardLine(config, goalsMade);
        float worldUnitsPerYard = config != null ? Mathf.Max(0.01f, config.worldUnitsPerYard) : 0.1f;
        return -(currentYardLine - startingYardLine) * worldUnitsPerYard;
    }

    public static bool IsLongBomb(NeonFieldGoalConfig config, int yardLine)
    {
        int longBombStartYardLine = config != null ? Mathf.Max(1, config.longBombStartYardLine) : 50;
        return yardLine >= longBombStartYardLine;
    }

    public static int GetBasePointsForYardLine(NeonFieldGoalConfig config, int yardLine)
    {
        if (IsLongBomb(config, yardLine))
        {
            return config != null ? Mathf.Max(1, config.longBombPointsPerGoal) : 5;
        }

        return config != null ? Mathf.Max(1, config.pointsPerGoal) : 3;
    }

    public static bool IsMovingGoalActive(NeonFieldGoalConfig config, int yardLine)
    {
        int activationYardLine = config != null ? Mathf.Max(1, config.movingGoalStartYardLine) : 50;
        return yardLine >= activationYardLine;
    }

    public static float GetMovingGoalChallengeProgress(NeonFieldGoalConfig config, int yardLine)
    {
        if (!IsMovingGoalActive(config, yardLine))
        {
            return 0f;
        }

        int activationYardLine = config != null ? Mathf.Max(1, config.movingGoalStartYardLine) : 50;
        int fullChallengeYardLine = config != null
            ? Mathf.Max(activationYardLine, config.movingGoalFullChallengeYardLine)
            : 65;
        return Mathf.InverseLerp(activationYardLine, fullChallengeYardLine, yardLine);
    }

    public static float GetMovingGoalAmplitude(NeonFieldGoalConfig config, int yardLine)
    {
        if (!IsMovingGoalActive(config, yardLine))
        {
            return 0f;
        }

        float challengeProgress = GetMovingGoalChallengeProgress(config, yardLine);
        float baseSideOffset = config != null ? Mathf.Max(0f, config.movingGoalBaseSideOffset) : 0.34f;
        float extraSideOffset = config != null ? Mathf.Max(0f, config.movingGoalExtraSideOffset) : 0.18f;
        return baseSideOffset + extraSideOffset * challengeProgress;
    }

    public static float GetMovingGoalSpeed(NeonFieldGoalConfig config, int yardLine)
    {
        if (!IsMovingGoalActive(config, yardLine))
        {
            return 0f;
        }

        float challengeProgress = GetMovingGoalChallengeProgress(config, yardLine);
        float baseSpeed = config != null ? Mathf.Max(0f, config.movingGoalBaseSpeed) : 0.32f;
        float extraSpeed = config != null ? Mathf.Max(0f, config.movingGoalExtraSpeed) : 0.18f;
        return baseSpeed + extraSpeed * challengeProgress;
    }

    public static string GetModeHudLabel(NeonFieldGoalConfig config, FieldGoalMode mode, int goalsMade)
    {
        return GetModeLabel(mode) + " | " + GetCurrentYardLine(config, goalsMade) + " YD LINE";
    }

    public static string GetModeLabel(FieldGoalMode mode)
    {
        return mode == FieldGoalMode.ClutchBlitz ? "CLUTCH BLITZ" : "SCORE ATTACK";
    }
}
