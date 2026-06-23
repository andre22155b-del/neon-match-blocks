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
    public static Vector3 GetFirstPersonLocalPosition(NeonFieldGoalConfig config, Vector3 ballSpawnLocalPosition)
    {
        float eyeHeight = config != null ? Mathf.Clamp(config.cameraFirstPersonEyeHeight, 1.4f, 2.1f) : 1.72f;
        float backOffset = config != null ? Mathf.Clamp(config.cameraFirstPersonBackOffset, 1f, 2.2f) : 1.35f;
        return new Vector3(ballSpawnLocalPosition.x, eyeHeight, ballSpawnLocalPosition.z - backOffset);
    }

    public static Quaternion GetFirstPersonLookRotation(NeonFieldGoalConfig config, Vector3 localPosition, float goalDistance)
    {
        float lookHeight = config != null ? Mathf.Clamp(config.cameraFirstPersonLookHeight, 2.5f, 4.5f) : 3.15f;
        Vector3 lookTarget = new Vector3(0f, lookHeight, goalDistance);
        Vector3 forward = lookTarget - localPosition;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = Vector3.forward;
        }

        return Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    public static float GetFirstPersonFov(NeonFieldGoalConfig config)
    {
        return config != null ? Mathf.Clamp(config.cameraFirstPersonFov, 50f, 85f) : 68f;
    }

    public static float GetPressureFovBoost(NeonFieldGoalConfig config, float distancePressure01, float windPressure01)
    {
        float baseBoost = config != null ? Mathf.Max(0f, config.cameraDistanceFovBoost) : 4.2f;
        float combinedPressure = Mathf.Clamp01(Mathf.Clamp01(distancePressure01) * 0.82f + Mathf.Clamp01(windPressure01) * 0.18f);
        return baseBoost * combinedPressure;
    }

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

    public static FieldGoalSlowMotion GetNearMissSlowMotion(NeonFieldGoalConfig config, bool longBombLine, bool clutchActive)
    {
        float baseDuration = config != null ? Mathf.Max(0f, config.nearMissSlowMoDuration) : 0.06f;
        if (baseDuration <= 0f)
        {
            return new FieldGoalSlowMotion(1f, 0f);
        }

        float durationScale = 1f;
        if (longBombLine)
        {
            durationScale += 0.28f;
        }

        if (clutchActive)
        {
            durationScale += 0.18f;
        }

        return new FieldGoalSlowMotion(
            config != null ? Mathf.Clamp(config.nearMissSlowMoScale, 0.05f, 1f) : 0.9f,
            baseDuration * durationScale);
    }
}
