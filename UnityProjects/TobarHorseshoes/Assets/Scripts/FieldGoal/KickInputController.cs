using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class KickInputController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, ICancelHandler
{
    public event Action<float, float> KickReleased;
    public event Action<float, float, bool> PreviewChanged;

    public RectTransform trackingRect;
    public float maxVerticalPixels = 380f;
    public float maxHorizontalPixels = 220f;
    [Range(0f, 1f)] public float minimumKickPower = 0.12f;
    [Range(0f, 0.3f)] public float releaseDebounceSeconds = 0.1f;

    private Vector2 startPosition;
    private float currentPower;
    private float currentAim;
    private float releaseCooldown;
    private int activePointerId = int.MinValue;
    private bool dragging;
    private bool inputEnabled = true;

    public float CurrentPower => currentPower;
    public float CurrentAim => currentAim;
    public bool IsPreviewActive => dragging && inputEnabled;

    private void Update()
    {
        if (releaseCooldown > 0f)
        {
            releaseCooldown = Mathf.Max(0f, releaseCooldown - Time.unscaledDeltaTime);
        }
    }

    public void ApplyConfig(NeonFieldGoalConfig config)
    {
        if (config == null)
        {
            return;
        }

        minimumKickPower = Mathf.Clamp01(config.minKickPower);
        releaseDebounceSeconds = Mathf.Max(0f, config.kickReleaseDebounceSeconds);
    }

    public void SetInputEnabled(bool value)
    {
        inputEnabled = value;
        if (!inputEnabled)
        {
            CancelGesture();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!inputEnabled || dragging || releaseCooldown > 0f || eventData == null)
        {
            return;
        }

        dragging = true;
        activePointerId = eventData.pointerId;
        currentPower = 0f;
        currentAim = 0f;
        startPosition = eventData.position;
        PreviewChanged?.Invoke(0f, 0f, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || !inputEnabled || eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        Vector2 delta = eventData.position - startPosition;
        currentPower = Mathf.Clamp01(Mathf.Max(0f, delta.y) / Mathf.Max(1f, maxVerticalPixels));
        currentAim = Mathf.Clamp(delta.x / Mathf.Max(1f, maxHorizontalPixels), -1f, 1f);
        PreviewChanged?.Invoke(currentPower, currentAim, true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!dragging || eventData == null || eventData.pointerId != activePointerId)
        {
            return;
        }

        Vector2 delta = eventData.position - startPosition;
        currentPower = Mathf.Clamp01(Mathf.Max(0f, delta.y) / Mathf.Max(1f, maxVerticalPixels));
        currentAim = Mathf.Clamp(delta.x / Mathf.Max(1f, maxHorizontalPixels), -1f, 1f);

        dragging = false;
        activePointerId = int.MinValue;
        PreviewChanged?.Invoke(0f, 0f, false);

        if (!inputEnabled || currentPower < minimumKickPower)
        {
            currentPower = 0f;
            currentAim = 0f;
            return;
        }

        float releasedPower = currentPower;
        float releasedAim = currentAim;
        currentPower = 0f;
        currentAim = 0f;
        releaseCooldown = Mathf.Max(0f, releaseDebounceSeconds);
        KickReleased?.Invoke(releasedPower, releasedAim);
    }

    public void OnCancel(BaseEventData eventData)
    {
        CancelGesture();
    }

    private void OnDisable()
    {
        CancelGesture();
    }

    private void CancelGesture()
    {
        if (!dragging && Mathf.Approximately(currentPower, 0f) && Mathf.Approximately(currentAim, 0f))
        {
            activePointerId = int.MinValue;
            return;
        }

        dragging = false;
        activePointerId = int.MinValue;
        currentPower = 0f;
        currentAim = 0f;
        PreviewChanged?.Invoke(0f, 0f, false);
    }
}
