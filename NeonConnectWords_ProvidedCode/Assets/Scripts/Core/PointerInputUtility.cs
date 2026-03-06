using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Unifies desktop mouse and mobile touch input for board interactions.
/// </summary>
public static class PointerInputUtility
{
    public static bool TryGetPreviewPointer(out Vector2 screenPosition, out int pointerId)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled)
            {
                screenPosition = touch.position;
                pointerId = touch.fingerId;
                return true;
            }
        }

        screenPosition = Input.mousePosition;
        pointerId = -1;
        return Input.mousePresent;
    }

    public static bool TryGetTapOrClick(out Vector2 screenPosition, out int pointerId)
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended)
            {
                screenPosition = touch.position;
                pointerId = touch.fingerId;
                return true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            pointerId = -1;
            return true;
        }

        screenPosition = default;
        pointerId = -1;
        return false;
    }

    public static bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return pointerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(pointerId)
            : EventSystem.current.IsPointerOverGameObject();
    }
}
