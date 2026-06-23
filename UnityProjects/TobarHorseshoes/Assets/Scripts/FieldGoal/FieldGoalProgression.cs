using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FieldGoalRunRecord
{
    public string modeId;
    public string modeLabel;
    public int score;
    public int goals;
    public int longestKick;
    public int bestMultiplier;
    public int bestStreak;
    public int timestampUnix;
}

[Serializable]
public class FieldGoalProgressionProfile
{
    public int bestScore;
    public int bestLongestKick;
    public int bestStreak;
    public int lifetimeScore;
    public int lifetimeGoals;
    public int lifetimePerfectKicks;
    public int lifetimeLongBombs;
    public int totalRuns;
    public int rewardTier;
    public List<FieldGoalRunRecord> topRuns = new List<FieldGoalRunRecord>();
}

public struct FieldGoalRewardProfile
{
    public FieldGoalRewardProfile(
        int tier,
        string title,
        string rewardLabel,
        string nextUnlockLabel,
        float perfectWindowBonus,
        float perfectWindResistanceBonus,
        int perfectPointBonus)
    {
        Tier = tier;
        Title = title;
        RewardLabel = rewardLabel;
        NextUnlockLabel = nextUnlockLabel;
        PerfectWindowBonus = perfectWindowBonus;
        PerfectWindResistanceBonus = perfectWindResistanceBonus;
        PerfectPointBonus = perfectPointBonus;
    }

    public int Tier { get; }
    public string Title { get; }
    public string RewardLabel { get; }
    public string NextUnlockLabel { get; }
    public float PerfectWindowBonus { get; }
    public float PerfectWindResistanceBonus { get; }
    public int PerfectPointBonus { get; }
}

public static class FieldGoalProgression
{
    public const int MaxStoredRuns = 5;

    public static FieldGoalProgressionProfile Sanitize(FieldGoalProgressionProfile profile)
    {
        if (profile == null)
        {
            profile = new FieldGoalProgressionProfile();
        }

        if (profile.topRuns == null)
        {
            profile.topRuns = new List<FieldGoalRunRecord>();
        }

        profile.bestScore = Mathf.Max(0, profile.bestScore);
        profile.bestLongestKick = Mathf.Max(0, profile.bestLongestKick);
        profile.bestStreak = Mathf.Max(0, profile.bestStreak);
        profile.lifetimeScore = Mathf.Max(0, profile.lifetimeScore);
        profile.lifetimeGoals = Mathf.Max(0, profile.lifetimeGoals);
        profile.lifetimePerfectKicks = Mathf.Max(0, profile.lifetimePerfectKicks);
        profile.lifetimeLongBombs = Mathf.Max(0, profile.lifetimeLongBombs);
        profile.totalRuns = Mathf.Max(0, profile.totalRuns);

        profile.topRuns.RemoveAll(run => run == null);
        profile.topRuns.Sort(CompareRuns);
        if (profile.topRuns.Count > MaxStoredRuns)
        {
            profile.topRuns.RemoveRange(MaxStoredRuns, profile.topRuns.Count - MaxStoredRuns);
        }

        profile.rewardTier = GetRewardTier(profile);
        return profile;
    }

    public static FieldGoalRunRecord CreateRunRecord(
        FieldGoalMode mode,
        int score,
        int goals,
        int longestKick,
        int bestMultiplier,
        int bestStreak)
    {
        return new FieldGoalRunRecord
        {
            modeId = mode.ToString(),
            modeLabel = FieldGoalScoring.GetModeLabel(mode),
            score = Mathf.Max(0, score),
            goals = Mathf.Max(0, goals),
            longestKick = Mathf.Max(0, longestKick),
            bestMultiplier = Mathf.Max(1, bestMultiplier),
            bestStreak = Mathf.Max(0, bestStreak),
            timestampUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    }

    public static int RecordRun(
        FieldGoalProgressionProfile profile,
        FieldGoalRunRecord run,
        int perfectGoals,
        int longBombGoals)
    {
        if (profile == null || run == null)
        {
            return 0;
        }

        Sanitize(profile);

        profile.totalRuns += 1;
        profile.lifetimeScore += Mathf.Max(0, run.score);
        profile.lifetimeGoals += Mathf.Max(0, run.goals);
        profile.lifetimePerfectKicks += Mathf.Max(0, perfectGoals);
        profile.lifetimeLongBombs += Mathf.Max(0, longBombGoals);
        profile.bestScore = Mathf.Max(profile.bestScore, Mathf.Max(0, run.score));
        profile.bestLongestKick = Mathf.Max(profile.bestLongestKick, Mathf.Max(0, run.longestKick));
        profile.bestStreak = Mathf.Max(profile.bestStreak, Mathf.Max(0, run.bestStreak));

        int insertionIndex = 0;
        while (insertionIndex < profile.topRuns.Count && CompareRuns(profile.topRuns[insertionIndex], run) <= 0)
        {
            insertionIndex++;
        }

        profile.topRuns.Insert(insertionIndex, run);
        int placement = insertionIndex + 1;
        if (profile.topRuns.Count > MaxStoredRuns)
        {
            profile.topRuns.RemoveRange(MaxStoredRuns, profile.topRuns.Count - MaxStoredRuns);
        }

        profile.rewardTier = GetRewardTier(profile);
        return placement <= MaxStoredRuns ? placement : 0;
    }

    public static FieldGoalRewardProfile BuildRewardProfile(FieldGoalProgressionProfile profile)
    {
        int tier = GetRewardTier(profile);
        switch (tier)
        {
            case 3:
                return new FieldGoalRewardProfile(
                    tier,
                    "OVERDRIVE",
                    "Perfect makes cash +1 and cut more wind.",
                    "MAXED. Time to hunt async legend runs.",
                    0.03f,
                    0.14f,
                    1);
            case 2:
                return new FieldGoalRewardProfile(
                    tier,
                    "WIND CUTTER",
                    "Perfect releases bend less under heavy wind.",
                    "Next unlock: OVERDRIVE at 500 lifetime score or 90 best score.",
                    0.02f,
                    0.1f,
                    0);
            case 1:
                return new FieldGoalRewardProfile(
                    tier,
                    "HEAT HAND",
                    "Perfect window is wider. Rhythm kicks feel cleaner.",
                    "Next unlock: WIND CUTTER at 40 lifetime goals or a 50-yard best bomb.",
                    0.02f,
                    0f,
                    0);
            default:
                return new FieldGoalRewardProfile(
                    0,
                    "STREET ROOKIE",
                    "Stack score to unlock stronger rewards.",
                    "Next unlock: HEAT HAND at 15 lifetime goals or a 60-point run.",
                    0f,
                    0f,
                    0);
        }
    }

    public static string GetLeaderboardTag(FieldGoalProgressionProfile profile)
    {
        profile = Sanitize(profile);
        if (profile.topRuns.Count <= 0)
        {
            return "LOCAL BOARD EMPTY";
        }

        FieldGoalRunRecord topRun = profile.topRuns[0];
        return "LOCAL LEGEND " + topRun.score.ToString("N0") + " // " + topRun.modeLabel;
    }

    public static string GetPlacementLabel(int placement)
    {
        return placement > 0 ? "LOCAL #" + placement : "OFF THE LOCAL BOARD";
    }

    public static int GetRewardTier(FieldGoalProgressionProfile profile)
    {
        if (profile == null)
        {
            return 0;
        }

        if (profile.lifetimeScore >= 500 || profile.bestScore >= 90)
        {
            return 3;
        }

        if (profile.lifetimeGoals >= 40 || profile.bestLongestKick >= 50)
        {
            return 2;
        }

        if (profile.lifetimeGoals >= 15 || profile.bestScore >= 60)
        {
            return 1;
        }

        return 0;
    }

    private static int CompareRuns(FieldGoalRunRecord a, FieldGoalRunRecord b)
    {
        if (ReferenceEquals(a, b))
        {
            return 0;
        }

        if (a == null)
        {
            return 1;
        }

        if (b == null)
        {
            return -1;
        }

        int scoreCompare = b.score.CompareTo(a.score);
        if (scoreCompare != 0)
        {
            return scoreCompare;
        }

        int longestCompare = b.longestKick.CompareTo(a.longestKick);
        if (longestCompare != 0)
        {
            return longestCompare;
        }

        int multiplierCompare = b.bestMultiplier.CompareTo(a.bestMultiplier);
        if (multiplierCompare != 0)
        {
            return multiplierCompare;
        }

        return b.bestStreak.CompareTo(a.bestStreak);
    }
}
