using UnityEngine;

public static class FieldGoalKickMath
{
    public static float ShapePower(NeonFieldGoalConfig config, float power01)
    {
        float clamped = Mathf.Clamp01(power01);
        float exponent = config != null ? Mathf.Max(0.45f, config.kickPowerExponent) : 1f;
        return Mathf.Pow(clamped, exponent);
    }

    public static float ComputePerfectKickWeight(NeonFieldGoalConfig config, float power01, float extraWindow = 0f)
    {
        if (config == null)
        {
            return 0f;
        }

        float window = Mathf.Max(0.01f, config.perfectKickWindow + Mathf.Max(0f, extraWindow));
        float distance = Mathf.Abs(Mathf.Clamp01(power01) - Mathf.Clamp01(config.perfectKickCenter));
        return Mathf.Clamp01(1f - distance / window);
    }

    public static bool IsPerfectKick(NeonFieldGoalConfig config, float power01, float aimInput, float extraWindow = 0f)
    {
        return ComputePerfectKickWeight(config, power01, extraWindow) >= 0.72f && Mathf.Abs(aimInput) <= 0.18f;
    }

    public static Vector3 ComputeLaunchDirection(
        Transform origin,
        Vector3 targetPosition,
        NeonFieldGoalConfig config,
        float aimInput)
    {
        if (origin == null)
        {
            return Vector3.forward;
        }

        float maxAimDegrees = config != null ? config.maxAimDegrees : 0f;
        Vector3 horizontal = Quaternion.AngleAxis(aimInput * maxAimDegrees, Vector3.up) * origin.forward;
        horizontal.y = 0f;
        if (horizontal.sqrMagnitude < 0.0001f)
        {
            horizontal = Vector3.forward;
        }
        else
        {
            horizontal.Normalize();
        }

        if (config != null && config.aimAssistStrength > 0f)
        {
            Vector3 toTarget = targetPosition - origin.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                toTarget.Normalize();
                horizontal = Vector3.Slerp(horizontal, toTarget, config.aimAssistStrength);
                horizontal.Normalize();
            }
        }

        float launchAngle = config != null ? config.launchAngleDegrees : 0f;
        Quaternion pitch = Quaternion.AngleAxis(-launchAngle, origin.right);
        Vector3 launchDirection = pitch * horizontal;
        return launchDirection.sqrMagnitude > 0.0001f ? launchDirection.normalized : origin.forward;
    }

    public static float ComputeImpulse(NeonFieldGoalConfig config, float power01, float extraPerfectWindow = 0f)
    {
        if (config == null)
        {
            return Mathf.Clamp01(power01);
        }

        float shapedPower = ShapePower(config, power01);
        float impulse = Mathf.Lerp(config.minKickImpulse, config.maxKickImpulse, shapedPower);
        impulse += ComputePerfectKickWeight(config, power01, extraPerfectWindow) * Mathf.Max(0f, config.perfectKickImpulseBonus);
        return impulse;
    }

    public static float ComputeCurveTorque(NeonFieldGoalConfig config, float aimInput, float power01)
    {
        if (config == null)
        {
            return Mathf.Clamp(aimInput, -1f, 1f);
        }

        float shapedPower = ShapePower(config, power01);
        float torqueScale = Mathf.Lerp(0.72f, 1.15f, shapedPower);
        return Mathf.Clamp(aimInput, -1f, 1f) * config.maxCurveTorque * torqueScale;
    }

    public static Vector3 ComputeInitialVelocity(NeonFieldGoalConfig config, Vector3 launchDirection, float impulse)
    {
        float mass = config != null ? Mathf.Max(0.01f, config.footballMass) : 1f;
        return launchDirection.normalized * (impulse / mass);
    }

    public static float GetKickReleaseDuration(NeonFieldGoalConfig config)
    {
        return config != null ? Mathf.Max(0f, config.kickReleaseDuration) : 0.08f;
    }

    public static float EvaluateKickReleaseBlend(NeonFieldGoalConfig config, float normalizedTime)
    {
        float t = Mathf.Clamp01(normalizedTime);
        float ease = config != null ? Mathf.Max(0.5f, config.kickReleaseEase) : 1.45f;
        float eased = 1f - Mathf.Pow(1f - t, ease);
        return Mathf.SmoothStep(0f, 1f, eased);
    }

    public static float GetGravityScale(NeonFieldGoalConfig config, float verticalVelocity, float releaseProgress01)
    {
        float releaseScale = config != null ? config.releaseGravityScale : 0.72f;
        float risingScale = config != null ? config.risingGravityScale : 0.94f;
        float fallingScale = config != null ? config.fallingGravityScale : 1.08f;

        float apexBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, -1f, verticalVelocity));
        float flightScale = Mathf.Lerp(risingScale, fallingScale, apexBlend);
        return Mathf.Lerp(releaseScale, flightScale, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(releaseProgress01)));
    }

    public static void SampleTrajectory(
        NeonFieldGoalConfig config,
        Vector3 startPosition,
        Vector3 releaseVelocity,
        float timeStep,
        Vector3[] points)
    {
        SampleTrajectory(config, startPosition, releaseVelocity, timeStep, Vector3.zero, points);
    }

    public static void SampleTrajectory(
        NeonFieldGoalConfig config,
        Vector3 startPosition,
        Vector3 releaseVelocity,
        float timeStep,
        Vector3 windAcceleration,
        Vector3[] points)
    {
        if (points == null || points.Length == 0)
        {
            return;
        }

        Vector3 position = startPosition;
        Vector3 velocity = Vector3.zero;
        float step = Mathf.Max(0.01f, timeStep);
        float elapsed = 0f;
        float releaseDuration = GetKickReleaseDuration(config);
        float appliedReleaseBlend = 0f;

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = position;
            float releaseProgress = releaseDuration > 0.0001f
                ? Mathf.Clamp01((elapsed + step) / releaseDuration)
                : 1f;
            float releaseBlend = releaseDuration > 0.0001f
                ? EvaluateKickReleaseBlend(config, releaseProgress)
                : 1f;
            float releaseDelta = releaseBlend - appliedReleaseBlend;

            if (releaseDelta > 0f)
            {
                velocity += releaseVelocity * releaseDelta;
                appliedReleaseBlend = releaseBlend;
            }

            float gravityScale = GetGravityScale(config, velocity.y, releaseProgress);
            velocity += Physics.gravity * gravityScale * step;
            velocity += windAcceleration * step;
            position += velocity * step;
            elapsed += step;
        }
    }
}
