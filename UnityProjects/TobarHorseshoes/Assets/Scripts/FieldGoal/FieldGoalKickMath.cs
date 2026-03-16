using UnityEngine;

public static class FieldGoalKickMath
{
    public static float ShapePower(NeonFieldGoalConfig config, float power01)
    {
        float clamped = Mathf.Clamp01(power01);
        float exponent = config != null ? Mathf.Max(0.45f, config.kickPowerExponent) : 1f;
        return Mathf.Pow(clamped, exponent);
    }

    public static float ComputePerfectKickWeight(NeonFieldGoalConfig config, float power01)
    {
        if (config == null)
        {
            return 0f;
        }

        float window = Mathf.Max(0.01f, config.perfectKickWindow);
        float distance = Mathf.Abs(Mathf.Clamp01(power01) - Mathf.Clamp01(config.perfectKickCenter));
        return Mathf.Clamp01(1f - distance / window);
    }

    public static bool IsPerfectKick(NeonFieldGoalConfig config, float power01, float aimInput)
    {
        return ComputePerfectKickWeight(config, power01) >= 0.72f && Mathf.Abs(aimInput) <= 0.18f;
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

    public static float ComputeImpulse(NeonFieldGoalConfig config, float power01)
    {
        if (config == null)
        {
            return Mathf.Clamp01(power01);
        }

        float shapedPower = ShapePower(config, power01);
        float impulse = Mathf.Lerp(config.minKickImpulse, config.maxKickImpulse, shapedPower);
        impulse += ComputePerfectKickWeight(config, power01) * Mathf.Max(0f, config.perfectKickImpulseBonus);
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

    public static void SampleTrajectory(Vector3 startPosition, Vector3 initialVelocity, float timeStep, Vector3[] points)
    {
        if (points == null || points.Length == 0)
        {
            return;
        }

        Vector3 position = startPosition;
        Vector3 velocity = initialVelocity;
        float step = Mathf.Max(0.01f, timeStep);

        for (int i = 0; i < points.Length; i++)
        {
            points[i] = position;
            velocity += Physics.gravity * step;
            position += velocity * step;
        }
    }
}
