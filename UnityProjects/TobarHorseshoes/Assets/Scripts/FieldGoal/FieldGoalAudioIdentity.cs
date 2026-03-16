using UnityEngine;

public enum FieldGoalAudioCue
{
    None = 0,
    Perfect = 1,
    LongBomb = 2,
    Heat = 3,
    Clutch = 4,
    NearMiss = 5,
    StreakBreak = 6,
    FinalDrive = 7,
    MovingGoal = 8
}

public static class FieldGoalAudioIdentity
{
    public static FieldGoalAudioCue GetGoalCue(FieldGoalScoreResult scoreResult)
    {
        if (scoreResult.LongBomb)
        {
            return FieldGoalAudioCue.LongBomb;
        }

        if (scoreResult.PerfectKick)
        {
            return FieldGoalAudioCue.Perfect;
        }

        if (scoreResult.Multiplier >= 3)
        {
            return FieldGoalAudioCue.Heat;
        }

        if (scoreResult.ClutchActive)
        {
            return FieldGoalAudioCue.Clutch;
        }

        return FieldGoalAudioCue.None;
    }

    public static FieldGoalAudioCue GetMissCue(
        GoalCrossingMissType missType,
        bool longBombLine,
        bool clutchActive,
        bool brokeHeat)
    {
        if (brokeHeat)
        {
            return FieldGoalAudioCue.StreakBreak;
        }

        if (missType != GoalCrossingMissType.None || longBombLine || clutchActive)
        {
            return FieldGoalAudioCue.NearMiss;
        }

        return FieldGoalAudioCue.None;
    }

    public static float GetGoalCrowdScale(FieldGoalScoreResult scoreResult, int streak)
    {
        float scale = 1f;
        if (scoreResult.ClutchActive)
        {
            scale += 0.14f;
        }

        if (scoreResult.PerfectKick)
        {
            scale += 0.18f;
        }

        if (scoreResult.LongBomb)
        {
            scale += 0.24f;
        }

        scale += Mathf.Clamp(scoreResult.Multiplier - 1, 0, 4) * 0.08f;
        scale += Mathf.Clamp(streak - 1, 0, 4) * 0.04f;
        return scale;
    }

    public static float GetNearMissCrowdScale(bool longBombLine, bool clutchActive)
    {
        float scale = 0.3f;
        if (longBombLine)
        {
            scale += 0.18f;
        }

        if (clutchActive)
        {
            scale += 0.12f;
        }

        return scale;
    }

    public static float GetKickSfxScale(bool longBombLine, bool perfectKick, bool clutchActive)
    {
        float scale = 0.86f;
        if (perfectKick)
        {
            scale += 0.12f;
        }

        if (longBombLine)
        {
            scale += 0.1f;
        }

        if (clutchActive)
        {
            scale += 0.08f;
        }

        return scale;
    }

    public static float GetAmbientScale(bool clutchActive, int multiplier, bool movingGoalLive)
    {
        float scale = 1f;
        if (movingGoalLive)
        {
            scale += 0.1f;
        }

        if (clutchActive)
        {
            scale += 0.18f;
        }

        scale += Mathf.Clamp(multiplier - 1, 0, 4) * 0.05f;
        return scale;
    }

    public static int GetCuePriority(FieldGoalAudioCue cue)
    {
        switch (cue)
        {
            case FieldGoalAudioCue.NearMiss:
                return 1;
            case FieldGoalAudioCue.MovingGoal:
                return 2;
            case FieldGoalAudioCue.Heat:
            case FieldGoalAudioCue.Clutch:
            case FieldGoalAudioCue.StreakBreak:
                return 3;
            case FieldGoalAudioCue.Perfect:
                return 4;
            case FieldGoalAudioCue.FinalDrive:
                return 5;
            case FieldGoalAudioCue.LongBomb:
                return 6;
            default:
                return 0;
        }
    }

    public static float GetCueCooldown(FieldGoalAudioCue cue)
    {
        switch (cue)
        {
            case FieldGoalAudioCue.NearMiss:
                return 0.16f;
            case FieldGoalAudioCue.MovingGoal:
                return 0.24f;
            case FieldGoalAudioCue.Heat:
            case FieldGoalAudioCue.Clutch:
                return 0.28f;
            case FieldGoalAudioCue.StreakBreak:
                return 0.32f;
            case FieldGoalAudioCue.Perfect:
                return 0.34f;
            case FieldGoalAudioCue.FinalDrive:
                return 0.42f;
            case FieldGoalAudioCue.LongBomb:
                return 0.48f;
            default:
                return 0f;
        }
    }

    public static float GetCueDuckIntensity(FieldGoalAudioCue cue)
    {
        switch (cue)
        {
            case FieldGoalAudioCue.NearMiss:
                return 0.25f;
            case FieldGoalAudioCue.MovingGoal:
                return 0.35f;
            case FieldGoalAudioCue.Heat:
            case FieldGoalAudioCue.Clutch:
                return 0.5f;
            case FieldGoalAudioCue.StreakBreak:
                return 0.58f;
            case FieldGoalAudioCue.Perfect:
                return 0.68f;
            case FieldGoalAudioCue.FinalDrive:
                return 0.78f;
            case FieldGoalAudioCue.LongBomb:
                return 1f;
            default:
                return 0f;
        }
    }

    public static bool ShouldPlayCue(
        FieldGoalAudioCue incomingCue,
        FieldGoalAudioCue activeCue,
        float secondsSinceLastCue,
        float baseCooldown)
    {
        if (incomingCue == FieldGoalAudioCue.None)
        {
            return false;
        }

        if (activeCue == FieldGoalAudioCue.None)
        {
            return true;
        }

        float requiredGap = Mathf.Max(baseCooldown, GetCueCooldown(activeCue));
        if (secondsSinceLastCue >= requiredGap)
        {
            return true;
        }

        return GetCuePriority(incomingCue) > GetCuePriority(activeCue);
    }

    public static float GetCueVolumeScale(FieldGoalAudioCue cue)
    {
        switch (cue)
        {
            case FieldGoalAudioCue.Perfect:
                return 0.94f;
            case FieldGoalAudioCue.LongBomb:
                return 1f;
            case FieldGoalAudioCue.Heat:
                return 0.9f;
            case FieldGoalAudioCue.Clutch:
                return 0.88f;
            case FieldGoalAudioCue.NearMiss:
                return 0.72f;
            case FieldGoalAudioCue.StreakBreak:
                return 0.8f;
            case FieldGoalAudioCue.FinalDrive:
                return 0.94f;
            case FieldGoalAudioCue.MovingGoal:
                return 0.84f;
            default:
                return 0f;
        }
    }
}
