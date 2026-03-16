using UnityEngine;

public class KickerLaneMover : MonoBehaviour
{
    public NeonFieldGoalConfig config;
    public Transform moverRoot;

    private float baseX;
    private float currentSpeed;
    private float currentLean;
    private Quaternion baseLocalRotation;
    private bool controlsEnabled = true;
    private bool leftPressed;
    private bool rightPressed;

    private void Awake()
    {
        if (moverRoot == null)
        {
            moverRoot = transform;
        }

        baseX = moverRoot.position.x;
        baseLocalRotation = moverRoot.localRotation;
    }

    public void ApplyConfig(NeonFieldGoalConfig fieldGoalConfig)
    {
        config = fieldGoalConfig;
    }

    public void SetControlsEnabled(bool value)
    {
        controlsEnabled = value;
        if (!controlsEnabled)
        {
            leftPressed = false;
            rightPressed = false;
        }
    }

    public void SetLeftPressed(bool pressed)
    {
        leftPressed = pressed;
    }

    public void SetRightPressed(bool pressed)
    {
        rightPressed = pressed;
    }

    public void ResetLanePosition()
    {
        if (moverRoot == null)
        {
            return;
        }

        moverRoot.position = new Vector3(baseX, moverRoot.position.y, moverRoot.position.z);
        currentSpeed = 0f;
        currentLean = 0f;
        moverRoot.localRotation = baseLocalRotation;
        leftPressed = false;
        rightPressed = false;
    }

    private void Update()
    {
        if (config == null || moverRoot == null)
        {
            return;
        }

        float input = 0f;
        if (controlsEnabled && leftPressed)
        {
            input -= 1f;
        }

        if (controlsEnabled && rightPressed)
        {
            input += 1f;
        }

        if (controlsEnabled && (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)))
        {
            input -= 1f;
        }

        if (controlsEnabled && (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)))
        {
            input += 1f;
        }

        input = Mathf.Clamp(input, -1f, 1f);
        float targetSpeed = input * config.moverSpeed;
        float acceleration = Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed)
            ? config.moverAcceleration
            : config.moverDeceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        Vector3 pos = moverRoot.position;
        pos.x += currentSpeed * Time.deltaTime;
        pos.x = Mathf.Clamp(pos.x, baseX - config.laneHalfWidth, baseX + config.laneHalfWidth);
        moverRoot.position = pos;

        float targetLean = config.moverSpeed > 0.01f
            ? -(currentSpeed / config.moverSpeed) * config.moverLeanAngle
            : 0f;
        float leanLerp = 1f - Mathf.Exp(-Mathf.Max(0.1f, config.moverLeanSharpness) * Time.deltaTime);
        currentLean = Mathf.Lerp(currentLean, targetLean, leanLerp);
        moverRoot.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, currentLean);

        if (Mathf.Approximately(pos.x, baseX - config.laneHalfWidth) || Mathf.Approximately(pos.x, baseX + config.laneHalfWidth))
        {
            currentSpeed = 0f;
        }
    }
}
