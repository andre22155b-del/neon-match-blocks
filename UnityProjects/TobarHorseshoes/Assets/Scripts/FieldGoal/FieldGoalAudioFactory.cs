using UnityEngine;

public static class FieldGoalAudioFactory
{
    private const int SampleRate = 44100;

    private static AudioClip kickClip;
    private static AudioClip goalClip;
    private static AudioClip missClip;
    private static AudioClip crowdClip;
    private static AudioClip ambientClip;

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
