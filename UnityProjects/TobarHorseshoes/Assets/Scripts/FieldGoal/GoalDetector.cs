using System;
using UnityEngine;

public class GoalDetector : MonoBehaviour
{
    public event Action<FootballProjectile> GoalScored;

    [Header("Goal Dimensions")]
    public float uprightInnerHalfWidth = 2.82f;
    public float crossbarHeight = 3.05f;
    public float planeDepthPadding = 0.05f;

    private FootballProjectile trackedBall;
    private Vector3 previousLocalPosition;
    private bool hasPreviousSample;

    public void ApplyConfig(NeonFieldGoalConfig config)
    {
        if (config == null)
        {
            return;
        }

        uprightInnerHalfWidth = config.uprightInnerHalfWidth;
        crossbarHeight = config.crossbarHeight;
        planeDepthPadding = config.goalPlaneDepthPadding;
    }

    public void TrackBall(FootballProjectile ball)
    {
        trackedBall = ball;
        hasPreviousSample = false;

        if (trackedBall != null)
        {
            previousLocalPosition = transform.InverseTransformPoint(trackedBall.transform.position);
        }
    }

    public void ClearTrackedBall()
    {
        trackedBall = null;
        hasPreviousSample = false;
    }

    private void FixedUpdate()
    {
        if (trackedBall == null || !trackedBall.HasBeenKicked)
        {
            return;
        }

        Vector3 currentLocalPosition = transform.InverseTransformPoint(trackedBall.transform.position);
        if (!hasPreviousSample)
        {
            previousLocalPosition = currentLocalPosition;
            hasPreviousSample = true;
            return;
        }

        if (!trackedBall.HasScored &&
            IsValidGoalCrossing(previousLocalPosition, currentLocalPosition, uprightInnerHalfWidth, crossbarHeight, planeDepthPadding) &&
            trackedBall.MarkGoalScored())
        {
            GoalScored?.Invoke(trackedBall);
        }

        previousLocalPosition = currentLocalPosition;
    }

    public static bool IsValidGoalCrossing(
        Vector3 previousLocalPosition,
        Vector3 currentLocalPosition,
        float halfWidth,
        float requiredHeight,
        float depthPadding)
    {
        float minimumForwardTravel = Mathf.Max(0.005f, depthPadding);
        bool movedFrontToBack = previousLocalPosition.z < 0f &&
                                currentLocalPosition.z >= 0f &&
                                (currentLocalPosition.z - previousLocalPosition.z) >= minimumForwardTravel;
        if (!movedFrontToBack)
        {
            return false;
        }

        float deltaZ = currentLocalPosition.z - previousLocalPosition.z;
        if (Mathf.Abs(deltaZ) <= 0.0001f)
        {
            return false;
        }

        float interpolation = Mathf.InverseLerp(previousLocalPosition.z, currentLocalPosition.z, 0f);
        Vector3 crossingPoint = Vector3.Lerp(previousLocalPosition, currentLocalPosition, interpolation);
        return Mathf.Abs(crossingPoint.x) <= halfWidth && crossingPoint.y >= requiredHeight;
    }
}
