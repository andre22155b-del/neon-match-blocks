using System;
using UnityEngine;

public enum GoalCrossingMissType
{
    None = 0,
    WideLeft = 1,
    WideRight = 2,
    Low = 3,
    WideLeftLow = 4,
    WideRightLow = 5
}

public struct GoalCrossingEvaluation
{
    public bool HasFrontToBackCrossing;
    public bool IsGoal;
    public GoalCrossingMissType MissType;
    public Vector3 CrossingPoint;
}

public class GoalDetector : MonoBehaviour
{
    public event Action<FootballProjectile> GoalScored;
    public event Action<GoalCrossingEvaluation> GoalCrossingMissed;

    [Header("Goal Dimensions")]
    public float uprightInnerHalfWidth = 2.82f;
    public float crossbarHeight = 3.05f;
    public float planeDepthPadding = 0.05f;

    private FootballProjectile trackedBall;
    private Vector3 previousLocalPosition;
    private bool hasPreviousSample;
    private bool crossingResolved;

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
        crossingResolved = false;

        if (trackedBall != null)
        {
            previousLocalPosition = transform.InverseTransformPoint(trackedBall.transform.position);
        }
    }

    public void ClearTrackedBall()
    {
        trackedBall = null;
        hasPreviousSample = false;
        crossingResolved = false;
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

        GoalCrossingEvaluation crossingEvaluation = EvaluateCrossing(
            previousLocalPosition,
            currentLocalPosition,
            uprightInnerHalfWidth,
            crossbarHeight,
            planeDepthPadding);

        if (!crossingResolved && crossingEvaluation.HasFrontToBackCrossing)
        {
            crossingResolved = true;

            if (!trackedBall.HasScored && crossingEvaluation.IsGoal && trackedBall.MarkGoalScored())
            {
                GoalScored?.Invoke(trackedBall);
            }
            else if (!crossingEvaluation.IsGoal && crossingEvaluation.MissType != GoalCrossingMissType.None)
            {
                GoalCrossingMissed?.Invoke(crossingEvaluation);
            }
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
        GoalCrossingEvaluation evaluation = EvaluateCrossing(
            previousLocalPosition,
            currentLocalPosition,
            halfWidth,
            requiredHeight,
            depthPadding);
        return evaluation.IsGoal;
    }

    public static GoalCrossingEvaluation EvaluateCrossing(
        Vector3 previousLocalPosition,
        Vector3 currentLocalPosition,
        float halfWidth,
        float requiredHeight,
        float depthPadding)
    {
        GoalCrossingEvaluation evaluation = default;
        float minimumForwardTravel = Mathf.Max(0.005f, depthPadding);
        bool movedFrontToBack = previousLocalPosition.z < 0f &&
                                currentLocalPosition.z >= 0f &&
                                (currentLocalPosition.z - previousLocalPosition.z) >= minimumForwardTravel;
        if (!movedFrontToBack)
        {
            return evaluation;
        }

        float deltaZ = currentLocalPosition.z - previousLocalPosition.z;
        if (Mathf.Abs(deltaZ) <= 0.0001f)
        {
            return evaluation;
        }

        float interpolation = Mathf.InverseLerp(previousLocalPosition.z, currentLocalPosition.z, 0f);
        Vector3 crossingPoint = Vector3.Lerp(previousLocalPosition, currentLocalPosition, interpolation);
        bool insideWidth = Mathf.Abs(crossingPoint.x) <= halfWidth;
        bool aboveCrossbar = crossingPoint.y >= requiredHeight;

        evaluation.HasFrontToBackCrossing = true;
        evaluation.CrossingPoint = crossingPoint;
        evaluation.IsGoal = insideWidth && aboveCrossbar;
        evaluation.MissType = evaluation.IsGoal
            ? GoalCrossingMissType.None
            : ClassifyMiss(crossingPoint, halfWidth, requiredHeight);
        return evaluation;
    }

    public static GoalCrossingMissType ClassifyMiss(Vector3 crossingPoint, float halfWidth, float requiredHeight)
    {
        bool missedLeft = crossingPoint.x < -halfWidth;
        bool missedRight = crossingPoint.x > halfWidth;
        bool missedLow = crossingPoint.y < requiredHeight;

        if (missedLeft && missedLow)
        {
            return GoalCrossingMissType.WideLeftLow;
        }

        if (missedRight && missedLow)
        {
            return GoalCrossingMissType.WideRightLow;
        }

        if (missedLeft)
        {
            return GoalCrossingMissType.WideLeft;
        }

        if (missedRight)
        {
            return GoalCrossingMissType.WideRight;
        }

        if (missedLow)
        {
            return GoalCrossingMissType.Low;
        }

        return GoalCrossingMissType.None;
    }
}
