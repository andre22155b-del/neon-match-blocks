using UnityEngine;

public class ThrowController : MonoBehaviour
{
    [SerializeField] private GameConfig config;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private Transform targetStake;

    public void SetConfig(GameConfig gameConfig)
    {
        config = gameConfig;
    }

    public void SetTargetStake(Transform stake)
    {
        targetStake = stake;
    }

    public void SetThrowOrigin(Transform origin)
    {
        throwOrigin = origin;
    }

    public void Throw(HorseshoeProjectile shoe, float power01, float aimInput, float spinInput, AimAssistMode assistMode)
    {
        if (shoe == null || config == null || throwOrigin == null)
        {
            return;
        }

        shoe.MarkThrown();

        float yawOffset = aimInput * config.maxAimDegrees;
        Vector3 horizontal = Quaternion.AngleAxis(yawOffset, Vector3.up) * throwOrigin.forward;
        horizontal.y = 0f;
        horizontal.Normalize();

        if (targetStake != null)
        {
            Vector3 toTarget = targetStake.position - throwOrigin.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                toTarget.Normalize();
                horizontal = Vector3.Slerp(horizontal, toTarget, AssistStrength(assistMode));
                horizontal.Normalize();
            }
        }

        Quaternion pitch = Quaternion.AngleAxis(-config.launchAngleDegrees, throwOrigin.right);
        Vector3 launchDirection = pitch * horizontal;
        launchDirection.Normalize();

        float impulse = Mathf.Lerp(config.minThrowImpulse, config.maxThrowImpulse, Mathf.Clamp01(power01));
        shoe.Body.AddForce(launchDirection * impulse, ForceMode.Impulse);

        float spin = Mathf.Clamp(spinInput, -1f, 1f) * config.maxSpinTorque;
        shoe.Body.AddTorque(Vector3.up * spin, ForceMode.Impulse);
    }

    private float AssistStrength(AimAssistMode mode)
    {
        switch (mode)
        {
            case AimAssistMode.Low:
                return config.aimAssistLow;
            case AimAssistMode.High:
                return config.aimAssistHigh;
            default:
                return 0f;
        }
    }
}
