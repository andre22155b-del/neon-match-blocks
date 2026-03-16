using UnityEngine;

public readonly struct FieldGoalSlowMotion
{
    public FieldGoalSlowMotion(float scale, float duration)
    {
        Scale = scale;
        Duration = duration;
    }

    public float Scale { get; }
    public float Duration { get; }
    public bool IsActive => Duration > 0f && Scale < 0.999f;
}

public static class FieldGoalCameraMath
{
    public static float ComputeKickTrauma(NeonFieldGoalConfig config, float power01, bool perfectKick)
    {
        float baseShake = config != null ? Mathf.Max(0.01f, config.cameraKickShake) : 0.14f;
        float powerFactor = Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(power01));
        float trauma = baseShake * powerFactor;

        if (perfectKick)
        {
            trauma *= 1.25f;
        }

        return trauma;
    }

    public static float ComputeGoalTrauma(NeonFieldGoalConfig config, int multiplier, bool perfectKick, bool clutchActive)
    {
        float baseShake = config != null ? Mathf.Max(0.01f, config.cameraGoalShake) : 0.26f;
        float multiplierFactor = 1f + Mathf.Clamp(multiplier - 1, 0, 4) * 0.16f;
        float trauma = baseShake * multiplierFactor;

        if (perfectKick)
        {
            trauma *= 1.18f;
        }

        if (clutchActive)
        {
            trauma *= 1.1f;
        }

        return trauma;
    }

    public static FieldGoalSlowMotion GetSlowMotion(NeonFieldGoalConfig config, bool goalEvent, bool perfectKick)
    {
        if (!perfectKick)
        {
            return new FieldGoalSlowMotion(1f, 0f);
        }

        if (goalEvent)
        {
            return new FieldGoalSlowMotion(
                config != null ? Mathf.Clamp(config.perfectGoalSlowMoScale, 0.05f, 1f) : 0.72f,
                config != null ? Mathf.Max(0f, config.perfectGoalSlowMoDuration) : 0.12f);
        }

        return new FieldGoalSlowMotion(
            config != null ? Mathf.Clamp(config.perfectKickSlowMoScale, 0.05f, 1f) : 0.86f,
            config != null ? Mathf.Max(0f, config.perfectKickSlowMoDuration) : 0.08f);
    }
}
