using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class KickInputController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public event Action<float, float> KickReleased;
    public event Action<float, float, bool> PreviewChanged;

    public RectTransform trackingRect;
    public float maxVerticalPixels = 380f;
    public float maxHorizontalPixels = 220f;
    [Range(0f, 1f)] public float minimumKickPower = 0.12f;

    private Vector2 startPosition;
    private float currentPower;
    private float currentAim;
    private bool dragging;
    private bool inputEnabled = true;

    public float CurrentPower => currentPower;
    public float CurrentAim => currentAim;
    public bool IsPreviewActive => dragging && inputEnabled;

    public void ApplyConfig(NeonFieldGoalConfig config)
    {
        if (config == null)
        {
            return;
        }

        minimumKickPower = Mathf.Clamp01(config.minKickPower);
    }

    public void SetInputEnabled(bool value)
    {
        inputEnabled = value;
        if (!inputEnabled)
        {
            dragging = false;
            currentPower = 0f;
            currentAim = 0f;
            PreviewChanged?.Invoke(0f, 0f, false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!inputEnabled)
        {
            return;
        }

        dragging = true;
        currentPower = 0f;
        currentAim = 0f;
        startPosition = eventData.position;
        PreviewChanged?.Invoke(0f, 0f, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!dragging || !inputEnabled)
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
        if (!dragging)
        {
            return;
        }

        Vector2 delta = eventData.position - startPosition;
        currentPower = Mathf.Clamp01(Mathf.Max(0f, delta.y) / Mathf.Max(1f, maxVerticalPixels));
        currentAim = Mathf.Clamp(delta.x / Mathf.Max(1f, maxHorizontalPixels), -1f, 1f);

        dragging = false;
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
        KickReleased?.Invoke(releasedPower, releasedAim);
    }
}
