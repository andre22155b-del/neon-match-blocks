using UnityEngine;

public static class FieldGoalWindMath
{
    public static float GetWindChallenge01(NeonFieldGoalConfig config, int yardLine)
    {
        int startYardLine = config != null ? Mathf.Max(1, config.windStartYardLine) : 35;
        int maxYardLine = config != null ? Mathf.Max(startYardLine, config.windMaxYardLine) : 60;
        if (yardLine < startYardLine)
        {
            return 0f;
        }

        return maxYardLine > startYardLine
            ? Mathf.InverseLerp(startYardLine, maxYardLine, yardLine)
            : 1f;
    }

    public static float GetWindStrength01(NeonFieldGoalConfig config, int yardLine)
    {
        if (yardLine < (config != null ? Mathf.Max(1, config.windStartYardLine) : 35))
        {
            return 0f;
        }

        return Mathf.Lerp(0.24f, 1f, GetWindChallenge01(config, yardLine));
    }

    public static float GetWindAccelerationMagnitude(NeonFieldGoalConfig config, int yardLine)
    {
        if (config == null)
        {
            return 0f;
        }

        if (yardLine < Mathf.Max(1, config.windStartYardLine))
        {
            return 0f;
        }

        float challenge01 = GetWindChallenge01(config, yardLine);
        return Mathf.Max(0f, config.windBaseAcceleration) + Mathf.Max(0f, config.windExtraAcceleration) * challenge01;
    }

    public static Vector3 GetWindAcceleration(
        NeonFieldGoalConfig config,
        int yardLine,
        float direction01,
        bool perfectKick,
        float extraPerfectResistance = 0f)
    {
        float direction = Mathf.Clamp(direction01, -1f, 1f);
        if (Mathf.Abs(direction) <= 0.001f)
        {
            return Vector3.zero;
        }

        float magnitude = GetWindAccelerationMagnitude(config, yardLine);
        if (magnitude <= 0f)
        {
            return Vector3.zero;
        }

        if (perfectKick && config != null)
        {
            magnitude *= 1f - Mathf.Clamp01(config.perfectKickWindResistance + Mathf.Max(0f, extraPerfectResistance));
        }

        return Vector3.right * direction * magnitude;
    }

    public static float GetDistancePressure01(NeonFieldGoalConfig config, int yardLine)
    {
        int startYardLine = config != null ? Mathf.Max(1, config.startingYardLine) : 20;
        int longBombYardLine = config != null ? Mathf.Max(startYardLine, config.longBombStartYardLine) : 50;
        int maxYardLine = config != null
            ? Mathf.Max(longBombYardLine, Mathf.Max(config.movingGoalFullChallengeYardLine, config.windMaxYardLine))
            : 65;
        return maxYardLine > startYardLine
            ? Mathf.InverseLerp(startYardLine, maxYardLine, Mathf.Max(startYardLine, yardLine))
            : 1f;
    }

    public static string GetHudLabel(float direction01, float strength01)
    {
        if (strength01 <= 0.02f || Mathf.Abs(direction01) <= 0.02f)
        {
            return "WIND CALM";
        }

        string arrows = GetArrowLabel(direction01);
        string intensity = GetIntensityLabel(strength01);
        return "WIND " + arrows + " " + intensity;
    }

    public static string GetShortCallout(float direction01, float strength01)
    {
        if (strength01 <= 0.02f || Mathf.Abs(direction01) <= 0.02f)
        {
            return "Calm air.";
        }

        return Mathf.Sign(direction01) < 0f
            ? "Wind pushing left."
            : "Wind pushing right.";
    }

    private static string GetArrowLabel(float direction01)
    {
        float absDirection = Mathf.Abs(direction01);
        string arrow = absDirection >= 0.66f ? "<<<" : absDirection >= 0.33f ? "<<" : "<";
        return direction01 < 0f ? arrow : arrow.Replace("<", ">");
    }

    private static string GetIntensityLabel(float strength01)
    {
        if (strength01 >= 0.8f)
        {
            return "HEAVY";
        }

        if (strength01 >= 0.45f)
        {
            return "BREEZE";
        }

        return "LIGHT";
    }
}
