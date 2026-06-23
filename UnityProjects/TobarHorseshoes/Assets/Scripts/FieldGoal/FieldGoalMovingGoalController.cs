using UnityEngine;

public class FieldGoalMovingGoalController : MonoBehaviour
{
    public NeonFieldGoalConfig config;
    public Transform goalRoot;

    private Vector3 baseLocalPosition;
    private float currentOffset;
    private float targetAmplitude;
    private float targetSpeed;
    private bool basePositionCached;

    private void Awake()
    {
        CacheBasePosition();
    }

    public void ApplyConfig(NeonFieldGoalConfig fieldGoalConfig)
    {
        config = fieldGoalConfig;
        CacheBasePosition();
    }

    public void SetCurrentYardLine(int yardLine, bool snapToCenter)
    {
        CacheBasePosition();
        targetAmplitude = FieldGoalScoring.GetMovingGoalAmplitude(config, yardLine);
        targetSpeed = FieldGoalScoring.GetMovingGoalSpeed(config, yardLine);

        if (snapToCenter)
        {
            currentOffset = 0f;
            ApplyOffset(0f);
        }
    }

    public void ResetMotion(bool snapToCenter)
    {
        targetAmplitude = 0f;
        targetSpeed = 0f;

        if (snapToCenter)
        {
            currentOffset = 0f;
            ApplyOffset(0f);
        }
    }

    private void Update()
    {
        if (goalRoot == null)
        {
            return;
        }

        float desiredOffset = 0f;
        if (targetAmplitude > 0.001f && targetSpeed > 0.001f)
        {
            desiredOffset = Mathf.Sin(Time.unscaledTime * targetSpeed * Mathf.PI * 2f) * targetAmplitude;
        }

        float sharpness = config != null ? Mathf.Max(0.1f, config.movingGoalSharpness) : 5.5f;
        float blend = 1f - Mathf.Exp(-sharpness * Time.unscaledDeltaTime);
        currentOffset = Mathf.Lerp(currentOffset, desiredOffset, blend);
        ApplyOffset(currentOffset);
    }

    private void CacheBasePosition()
    {
        if (goalRoot == null)
        {
            goalRoot = transform;
        }

        if (basePositionCached)
        {
            return;
        }

        baseLocalPosition = goalRoot.localPosition;
        basePositionCached = true;
    }

    private void ApplyOffset(float offset)
    {
        if (goalRoot == null)
        {
            return;
        }

        goalRoot.localPosition = baseLocalPosition + Vector3.right * offset;
    }
}
