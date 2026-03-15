using UnityEngine;

[DisallowMultipleComponent]
public class FieldGoalCameraJuiceController : MonoBehaviour
{
    public NeonFieldGoalConfig config;
    public Camera targetCamera;
    public float noiseFrequency = 22f;
    public float clutchPulseSpeed = 3.3f;

    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private float baseFieldOfView;
    private float trauma;
    private float kickback;
    private float verticalLift;
    private float zoomBoost;
    private float clutchBlend;
    private bool clutchActive;
    private float slowMotionTimer;
    private float slowMotionScale = 1f;
    private float defaultFixedDeltaTime;
    private float noiseSeed;
    private bool baseStateCached;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (Mathf.Approximately(defaultFixedDeltaTime, 0f))
        {
            defaultFixedDeltaTime = Time.fixedDeltaTime;
        }

        noiseSeed = Random.Range(10f, 1000f);
        CacheBaseState();
    }

    private void OnEnable()
    {
        if (Mathf.Approximately(defaultFixedDeltaTime, 0f))
        {
            defaultFixedDeltaTime = Time.fixedDeltaTime;
        }

        CacheBaseState();
    }

    private void OnDisable()
    {
        RestoreTimeScale();
    }

    private void OnDestroy()
    {
        RestoreTimeScale();
    }

    public void ApplyConfig(NeonFieldGoalConfig fieldGoalConfig)
    {
        config = fieldGoalConfig;
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        CacheBaseState();
    }

    public void SetClutchMode(bool active, bool instant = false)
    {
        clutchActive = active;
        if (instant)
        {
            clutchBlend = active ? 1f : 0f;
        }
    }

    public void PlayKick(float power, bool perfectKick)
    {
        AddTrauma(FieldGoalCameraMath.ComputeKickTrauma(config, power, perfectKick));

        float kickbackDistance = config != null ? config.cameraKickbackDistance : 0.22f;
        float kickFovBoost = config != null ? config.cameraKickFovBoost : 1.35f;
        float shapedPower = Mathf.Clamp01(power);

        kickback = Mathf.Max(kickback, kickbackDistance * Mathf.Lerp(0.45f, 1f, shapedPower) * (perfectKick ? 1.12f : 1f));
        verticalLift = Mathf.Max(verticalLift, 0.03f + shapedPower * 0.05f);
        zoomBoost = Mathf.Max(zoomBoost, kickFovBoost * Mathf.Lerp(0.35f, 1f, shapedPower) * (perfectKick ? 1.08f : 1f));

        TriggerSlowMotion(FieldGoalCameraMath.GetSlowMotion(config, false, perfectKick));
    }

    public void PlayGoal(int multiplier, bool perfectKick, bool clutchScoring)
    {
        AddTrauma(FieldGoalCameraMath.ComputeGoalTrauma(config, multiplier, perfectKick, clutchScoring));

        float goalZoom = config != null ? config.cameraGoalFovBoost : 4.1f;
        float multiplierBoost = Mathf.Min(1.45f, 0.92f + Mathf.Max(0, multiplier - 1) * 0.11f);
        zoomBoost = Mathf.Max(zoomBoost, goalZoom * multiplierBoost * (clutchScoring ? 1.08f : 1f));
        kickback = Mathf.Max(kickback, (config != null ? config.cameraKickbackDistance : 0.22f) * 0.45f);
        verticalLift = Mathf.Max(verticalLift, 0.07f + Mathf.Min(0.05f, Mathf.Max(0, multiplier - 1) * 0.012f));

        TriggerSlowMotion(FieldGoalCameraMath.GetSlowMotion(config, true, perfectKick));
    }

    public void PlayMiss()
    {
        AddTrauma(config != null ? config.cameraMissShake : 0.1f);
        kickback = Mathf.Max(kickback, (config != null ? config.cameraKickbackDistance : 0.22f) * 0.32f);
        zoomBoost = Mathf.Max(zoomBoost, (config != null ? config.cameraKickFovBoost : 1.35f) * 0.42f);
    }

    public void PlayFinalDrive()
    {
        AddTrauma(config != null ? config.cameraFinalDriveShake : 0.16f);
        zoomBoost = Mathf.Max(zoomBoost, (config != null ? config.cameraClutchFovBoost : 1.6f) * 0.8f);
    }

    public void ResetPresentation()
    {
        trauma = 0f;
        kickback = 0f;
        verticalLift = 0f;
        zoomBoost = 0f;
        slowMotionTimer = 0f;
        slowMotionScale = 1f;
        RestoreTimeScale();
        RestoreCameraImmediate();
    }

    private void LateUpdate()
    {
        if (!baseStateCached)
        {
            CacheBaseState();
        }

        if (targetCamera == null || !baseStateCached)
        {
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;
        clutchBlend = Mathf.MoveTowards(clutchBlend, clutchActive ? 1f : 0f, deltaTime * 2.4f);
        trauma = Mathf.MoveTowards(trauma, 0f, deltaTime * (config != null ? config.cameraShakeDecay : 2.8f));
        kickback = Mathf.MoveTowards(kickback, 0f, deltaTime * (config != null ? config.cameraPositionSharpness : 11f));
        verticalLift = Mathf.MoveTowards(verticalLift, 0f, deltaTime * (config != null ? config.cameraPositionSharpness : 11f) * 1.25f);
        zoomBoost = Mathf.MoveTowards(zoomBoost, 0f, deltaTime * (config != null ? config.cameraFovSharpness : 8f) * 1.1f);

        UpdateSlowMotion(deltaTime);
        ApplyCamera(deltaTime);
    }

    private void CacheBaseState()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            return;
        }

        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;
        baseFieldOfView = targetCamera.fieldOfView;
        baseStateCached = true;
    }

    private void ApplyCamera(float deltaTime)
    {
        float shakeAmount = trauma * trauma;
        float shakeAngle = config != null ? config.cameraShakeAngle : 1.65f;
        float shakeDistance = config != null ? config.cameraShakeDistance : 0.12f;
        float clutchPulse = clutchBlend > 0f ? Mathf.Sin(Time.unscaledTime * clutchPulseSpeed) * clutchBlend : 0f;

        Vector3 noiseOffset = new Vector3(
            GetNoise(0.11f),
            GetNoise(0.39f) * 0.7f,
            0f) * shakeDistance * shakeAmount;
        Vector3 targetPosition = baseLocalPosition + noiseOffset + new Vector3(0f, verticalLift + clutchPulse * 0.018f, -kickback);

        Vector3 rotationOffset = new Vector3(
            GetNoise(0.63f) * shakeAngle * shakeAmount - verticalLift * 20f,
            GetNoise(0.87f) * shakeAngle * 0.3f * shakeAmount,
            GetNoise(0.21f) * shakeAngle * shakeAmount + clutchPulse * (config != null ? config.cameraClutchRollAngle : 0.7f));
        Quaternion targetRotation = baseLocalRotation * Quaternion.Euler(rotationOffset);

        float targetFov = baseFieldOfView +
            zoomBoost +
            clutchBlend * (config != null ? config.cameraClutchFovBoost : 1.6f) +
            Mathf.Abs(clutchPulse) * 0.35f;

        float positionLerp = Damp(config != null ? config.cameraPositionSharpness : 11f, deltaTime);
        float rotationLerp = Damp(config != null ? config.cameraRotationSharpness : 9f, deltaTime);
        float fovLerp = Damp(config != null ? config.cameraFovSharpness : 8f, deltaTime);

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, positionLerp);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, rotationLerp);
        targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFov, fovLerp);
    }

    private void UpdateSlowMotion(float deltaTime)
    {
        if (slowMotionTimer <= 0f)
        {
            if (!Mathf.Approximately(Time.timeScale, 1f))
            {
                RestoreTimeScale();
            }

            return;
        }

        slowMotionTimer -= deltaTime;
        ApplyTimeScale(slowMotionScale);
        if (slowMotionTimer <= 0f)
        {
            RestoreTimeScale();
        }
    }

    private void TriggerSlowMotion(FieldGoalSlowMotion slowMotion)
    {
        if (!slowMotion.IsActive)
        {
            return;
        }

        if (slowMotionTimer > slowMotion.Duration && slowMotion.Scale >= slowMotionScale)
        {
            return;
        }

        slowMotionTimer = slowMotion.Duration;
        slowMotionScale = slowMotion.Scale;
        ApplyTimeScale(slowMotionScale);
    }

    private void AddTrauma(float value)
    {
        trauma = Mathf.Clamp01(trauma + Mathf.Max(0f, value));
    }

    private void RestoreCameraImmediate()
    {
        if (!baseStateCached || targetCamera == null)
        {
            return;
        }

        transform.localPosition = baseLocalPosition;
        transform.localRotation = baseLocalRotation;
        targetCamera.fieldOfView = baseFieldOfView;
    }

    private void ApplyTimeScale(float scale)
    {
        float clampedScale = Mathf.Clamp(scale, 0.05f, 1f);
        Time.timeScale = clampedScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * clampedScale;
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = 1f;
        if (!Mathf.Approximately(defaultFixedDeltaTime, 0f))
        {
            Time.fixedDeltaTime = defaultFixedDeltaTime;
        }
    }

    private float GetNoise(float offset)
    {
        return (Mathf.PerlinNoise(noiseSeed + offset, Time.unscaledTime * noiseFrequency + offset) - 0.5f) * 2f;
    }

    private static float Damp(float sharpness, float deltaTime)
    {
        return 1f - Mathf.Exp(-Mathf.Max(0.01f, sharpness) * Mathf.Max(0f, deltaTime));
    }
}
