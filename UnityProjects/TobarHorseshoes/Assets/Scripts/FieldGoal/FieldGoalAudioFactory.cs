using UnityEngine;

public static class FieldGoalAudioFactory
{
    private const int SampleRate = 44100;

    private static AudioClip kickClip;
    private static AudioClip goalClip;
    private static AudioClip missClip;
    private static AudioClip crowdClip;
    private static AudioClip ambientClip;
    private static AudioClip perfectClip;
    private static AudioClip longBombClip;
    private static AudioClip heatClip;
    private static AudioClip clutchClip;
    private static AudioClip nearMissClip;
    private static AudioClip streakBreakClip;
    private static AudioClip finalDriveClip;
    private static AudioClip movingGoalClip;

    public static AudioClip GetKickClip()
    {
        if (kickClip == null)
        {
            kickClip = CreateClip("NeonFieldGoal_Kick", 0.22f, SampleKick);
        }

        return kickClip;
    }

    public static AudioClip GetGoalClip()
    {
        if (goalClip == null)
        {
            goalClip = CreateClip("NeonFieldGoal_Goal", 0.95f, SampleGoal);
        }

        return goalClip;
    }

    public static AudioClip GetMissClip()
    {
        if (missClip == null)
        {
            missClip = CreateClip("NeonFieldGoal_Miss", 0.55f, SampleMiss);
        }

        return missClip;
    }

    public static AudioClip GetCrowdClip()
    {
        if (crowdClip == null)
        {
            crowdClip = CreateClip("NeonFieldGoal_Crowd", 1.2f, SampleCrowd);
        }

        return crowdClip;
    }

    public static AudioClip GetAmbientClip()
    {
        if (ambientClip == null)
        {
            ambientClip = CreateClip("NeonFieldGoal_Ambient", 3.2f, SampleAmbient);
        }

        return ambientClip;
    }

    public static AudioClip GetCueClip(FieldGoalAudioCue cue)
    {
        switch (cue)
        {
            case FieldGoalAudioCue.Perfect:
                return GetPerfectClip();
            case FieldGoalAudioCue.LongBomb:
                return GetLongBombClip();
            case FieldGoalAudioCue.Heat:
                return GetHeatClip();
            case FieldGoalAudioCue.Clutch:
                return GetClutchClip();
            case FieldGoalAudioCue.NearMiss:
                return GetNearMissClip();
            case FieldGoalAudioCue.StreakBreak:
                return GetStreakBreakClip();
            case FieldGoalAudioCue.FinalDrive:
                return GetFinalDriveClip();
            case FieldGoalAudioCue.MovingGoal:
                return GetMovingGoalClip();
            default:
                return null;
        }
    }

    public static AudioClip GetPerfectClip()
    {
        if (perfectClip == null)
        {
            perfectClip = CreateClip("NeonFieldGoal_Perfect", 0.46f, SamplePerfect);
        }

        return perfectClip;
    }

    public static AudioClip GetLongBombClip()
    {
        if (longBombClip == null)
        {
            longBombClip = CreateClip("NeonFieldGoal_LongBomb", 0.78f, SampleLongBomb);
        }

        return longBombClip;
    }

    public static AudioClip GetHeatClip()
    {
        if (heatClip == null)
        {
            heatClip = CreateClip("NeonFieldGoal_Heat", 0.58f, SampleHeat);
        }

        return heatClip;
    }

    public static AudioClip GetClutchClip()
    {
        if (clutchClip == null)
        {
            clutchClip = CreateClip("NeonFieldGoal_Clutch", 0.62f, SampleClutch);
        }

        return clutchClip;
    }

    public static AudioClip GetNearMissClip()
    {
        if (nearMissClip == null)
        {
            nearMissClip = CreateClip("NeonFieldGoal_NearMiss", 0.5f, SampleNearMiss);
        }

        return nearMissClip;
    }

    public static AudioClip GetStreakBreakClip()
    {
        if (streakBreakClip == null)
        {
            streakBreakClip = CreateClip("NeonFieldGoal_StreakBreak", 0.55f, SampleStreakBreak);
        }

        return streakBreakClip;
    }

    public static AudioClip GetFinalDriveClip()
    {
        if (finalDriveClip == null)
        {
            finalDriveClip = CreateClip("NeonFieldGoal_FinalDrive", 0.84f, SampleFinalDrive);
        }

        return finalDriveClip;
    }

    public static AudioClip GetMovingGoalClip()
    {
        if (movingGoalClip == null)
        {
            movingGoalClip = CreateClip("NeonFieldGoal_MovingGoal", 0.56f, SampleMovingGoal);
        }

        return movingGoalClip;
    }

    private static AudioClip CreateClip(string name, float duration, System.Func<float, float> sampler)
    {
        int sampleCount = Mathf.CeilToInt(duration * SampleRate);
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            data[i] = Mathf.Clamp(sampler(t), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
        clip.hideFlags = HideFlags.DontSave;
        clip.SetData(data, 0);
        return clip;
    }

    private static float SampleKick(float t)
    {
        float env = Mathf.Exp(-16f * t);
        float click = Mathf.Sin(2f * Mathf.PI * 140f * t) * env;
        float thump = Mathf.Sin(2f * Mathf.PI * (55f + 40f * (1f - t)) * t) * Mathf.Exp(-8f * t);
        float shimmer = Mathf.Sin(2f * Mathf.PI * 720f * t) * Mathf.Exp(-24f * t) * 0.18f;
        return click * 0.6f + thump * 0.55f + shimmer;
    }

    private static float SampleGoal(float t)
    {
        float env = Mathf.Clamp01(1f - t / 0.95f);
        float hit = Mathf.Sin(2f * Mathf.PI * 523.25f * t);
        float octave = Mathf.Sin(2f * Mathf.PI * 1046.5f * t) * 0.35f;
        float glide = Mathf.Sin(2f * Mathf.PI * (780f + 220f * t) * t) * 0.3f;
        float sparkle = Mathf.Sin(2f * Mathf.PI * 1760f * t) * Mathf.Exp(-10f * t) * 0.16f;
        float gate = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 8f * t);
        return (hit * 0.42f + octave + glide + sparkle) * env * gate;
    }

    private static float SampleMiss(float t)
    {
        float env = Mathf.Exp(-6f * t);
        float downSweep = Mathf.Sin(2f * Mathf.PI * (340f - 180f * t) * t);
        float buzz = Saw(38f * t) * 0.12f;
        return (downSweep * 0.42f + buzz) * env;
    }

    private static float SampleCrowd(float t)
    {
        float env = Mathf.Clamp01(1f - t / 1.2f);
        float hiss = HashNoise(t * 1700f) * 0.24f;
        float rumble = Mathf.Sin(2f * Mathf.PI * 96f * t) * 0.1f;
        float cheer = Mathf.Sin(2f * Mathf.PI * 220f * t) * Mathf.Sin(2f * Mathf.PI * 2.8f * t) * 0.08f;
        return (hiss + rumble + cheer) * env;
    }

    private static float SampleAmbient(float t)
    {
        float phase = t % 3.2f;
        float padA = Mathf.Sin(2f * Mathf.PI * 110f * phase) * 0.07f;
        float padB = Mathf.Sin(2f * Mathf.PI * 164.81f * phase) * 0.05f;
        float pulse = Mathf.Sin(2f * Mathf.PI * 0.5f * phase) * 0.5f + 0.5f;
        float shimmer = Mathf.Sin(2f * Mathf.PI * (640f + 45f * Mathf.Sin(2f * Mathf.PI * 0.2f * phase)) * phase) * 0.015f;
        float air = HashNoise(phase * 1200f) * 0.02f;
        return (padA + padB) * (0.6f + pulse * 0.4f) + shimmer + air;
    }

    private static float SamplePerfect(float t)
    {
        float env = Mathf.Exp(-7f * t);
        float toneA = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.24f;
        float toneB = Mathf.Sin(2f * Mathf.PI * 1320f * t) * 0.16f;
        float chime = Mathf.Sin(2f * Mathf.PI * (1480f + 260f * t) * t) * 0.18f;
        return (toneA + toneB + chime) * env;
    }

    private static float SampleLongBomb(float t)
    {
        float env = Mathf.Clamp01(1f - t / 0.78f);
        float low = Mathf.Sin(2f * Mathf.PI * 98f * t) * 0.18f;
        float brass = Mathf.Sin(2f * Mathf.PI * 196f * t) * (0.16f + 0.08f * Mathf.Sin(2f * Mathf.PI * 3.2f * t));
        float laser = Mathf.Sin(2f * Mathf.PI * (520f + 240f * t) * t) * 0.14f;
        float grit = HashNoise(t * 1900f) * 0.05f;
        return (low + brass + laser + grit) * env;
    }

    private static float SampleHeat(float t)
    {
        float env = Mathf.Exp(-5.5f * t);
        float saw = Saw(210f * t) * 0.16f;
        float pulse = Mathf.Sin(2f * Mathf.PI * 420f * t) * (0.18f + 0.08f * Mathf.Sin(2f * Mathf.PI * 7f * t));
        return (saw + pulse) * env;
    }

    private static float SampleClutch(float t)
    {
        float env = Mathf.Clamp01(1f - t / 0.62f);
        float alarm = Mathf.Sin(2f * Mathf.PI * (320f + 70f * Mathf.Sin(2f * Mathf.PI * 4f * t)) * t) * 0.2f;
        float under = Mathf.Sin(2f * Mathf.PI * 140f * t) * 0.12f;
        return (alarm + under) * env;
    }

    private static float SampleNearMiss(float t)
    {
        float env = Mathf.Exp(-6.4f * t);
        float bend = Mathf.Sin(2f * Mathf.PI * (430f - 170f * t) * t) * 0.24f;
        float hiss = HashNoise(t * 1600f) * 0.04f;
        return (bend + hiss) * env;
    }

    private static float SampleStreakBreak(float t)
    {
        float env = Mathf.Exp(-5.8f * t);
        float drop = Mathf.Sin(2f * Mathf.PI * (250f - 130f * t) * t) * 0.22f;
        float buzz = Saw(55f * t) * 0.08f;
        return (drop + buzz) * env;
    }

    private static float SampleFinalDrive(float t)
    {
        float env = Mathf.Clamp01(1f - t / 0.84f);
        float siren = Mathf.Sin(2f * Mathf.PI * (260f + 90f * Mathf.Sin(2f * Mathf.PI * 2.6f * t)) * t) * 0.18f;
        float sub = Mathf.Sin(2f * Mathf.PI * 84f * t) * 0.12f;
        float grit = HashNoise(t * 2400f) * 0.05f;
        return (siren + sub + grit) * env;
    }

    private static float SampleMovingGoal(float t)
    {
        float env = Mathf.Exp(-5f * t);
        float sweep = Mathf.Sin(2f * Mathf.PI * (460f + 180f * Mathf.Sin(2f * Mathf.PI * 1.5f * t)) * t) * 0.18f;
        float ping = Mathf.Sin(2f * Mathf.PI * 980f * t) * Mathf.Exp(-18f * t) * 0.12f;
        return (sweep + ping) * env;
    }

    private static float Saw(float x)
    {
        return 2f * (x - Mathf.Floor(x + 0.5f));
    }

    private static float HashNoise(float x)
    {
        float v = Mathf.Sin(x * 12.9898f + 78.233f) * 43758.5453f;
        return (v - Mathf.Floor(v)) * 2f - 1f;
    }
}
