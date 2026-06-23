using System;
using UnityEngine;

public class ThrowInput : MonoBehaviour
{
    public event Action<float> PowerChanged;
    public event Action<float> AimChanged;
    public event Action<float> SpinChanged;
    public event Action<float, float, float> ThrowReleased;

    [SerializeField] private GameConfig config;

    private Vector2 startPosition;
    private bool dragging;

    public void SetConfig(GameConfig gameConfig)
    {
        config = gameConfig;
    }

    private void Update()
    {
        if (config == null)
        {
            return;
        }

        if (TryGetPointerDown(out Vector2 downPosition))
        {
            startPosition = downPosition;
            dragging = true;
            PublishInput(0f, 0f, 0f);
            return;
        }

        if (!dragging)
        {
            return;
        }

        if (TryGetPointerPosition(out Vector2 current))
        {
            Vector2 delta = current - startPosition;
            float power = Mathf.Clamp01(delta.magnitude / config.maxSwipePixels);
            float aim = Mathf.Clamp(delta.x / config.maxAimDragPixels, -1f, 1f);
            float spin = Mathf.Clamp(delta.x / config.maxSpinDragPixels, -1f, 1f);
            PublishInput(power, aim, spin);
        }

        if (TryGetPointerUp(out Vector2 releasePosition))
        {
            Vector2 delta = releasePosition - startPosition;
            float power = Mathf.Clamp01(delta.magnitude / config.maxSwipePixels);
            float aim = Mathf.Clamp(delta.x / config.maxAimDragPixels, -1f, 1f);
            float spin = Mathf.Clamp(delta.x / config.maxSpinDragPixels, -1f, 1f);
            dragging = false;
            ThrowReleased?.Invoke(power, aim, spin);
            PublishInput(0f, 0f, 0f);
        }
    }

    private void PublishInput(float power, float aim, float spin)
    {
        PowerChanged?.Invoke(power);
        AimChanged?.Invoke(aim);
        SpinChanged?.Invoke(spin);
    }

    private static bool TryGetPointerDown(out Vector2 position)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                position = touch.position;
                return true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            position = Input.mousePosition;
            return true;
        }

        position = default;
        return false;
    }

    private static bool TryGetPointerPosition(out Vector2 position)
    {
        if (Input.touchCount > 0)
        {
            position = Input.GetTouch(0).position;
            return true;
        }

        if (Input.GetMouseButton(0))
        {
            position = Input.mousePosition;
            return true;
        }

        position = default;
        return false;
    }

    private static bool TryGetPointerUp(out Vector2 position)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                position = touch.position;
                return true;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            position = Input.mousePosition;
            return true;
        }

        position = default;
        return false;
    }
}
