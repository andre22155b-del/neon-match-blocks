using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FieldGoalHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public event Action<bool> HoldChanged;

    public Image targetGraphic;
    public Color releasedColor = new Color(0.08f, 0.16f, 0.28f, 0.92f);
    public Color pressedColor = new Color(0.22f, 0.92f, 1f, 1f);

    private bool interactable = true;
    private bool isPressed;

    private void Awake()
    {
        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Image>();
        }

        ApplyVisual();
    }

    public void SetInteractable(bool value)
    {
        interactable = value;
        if (!interactable)
        {
            SetPressed(false);
        }

        ApplyVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable)
        {
            return;
        }

        SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }

    private void SetPressed(bool value)
    {
        if (isPressed == value)
        {
            return;
        }

        isPressed = value;
        ApplyVisual();
        HoldChanged?.Invoke(isPressed);
    }

    private void ApplyVisual()
    {
        if (targetGraphic == null)
        {
            return;
        }

        Color baseColor = isPressed ? pressedColor : releasedColor;
        if (!interactable)
        {
            baseColor.a *= 0.5f;
        }

        targetGraphic.color = baseColor;
    }
}
