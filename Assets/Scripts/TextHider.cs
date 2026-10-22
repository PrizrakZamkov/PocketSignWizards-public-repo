using UnityEngine;

public class TextHider : MonoBehaviour
{
    [SerializeField] private GameObject text;
    [SerializeField] private RectTransform joystickZone;
    [SerializeField] private Canvas canvas;

    private int joystickFingerId = -1;

    void Update()
    {
        if (Input.touchCount > 0)
            HandleTouch();
        else
            HandleMouse();
    }

    void HandleTouch()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);

            if (t.phase == TouchPhase.Began && IsInsideJoystick(t.position))
            {
                joystickFingerId = t.fingerId;
                text.SetActive(false);
            }

            if (t.fingerId == joystickFingerId &&
                (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
            {
                joystickFingerId = -1;
                text.SetActive(true);
            }
        }
    }

    void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0) && IsInsideJoystick(Input.mousePosition))
            text.SetActive(false);

        if (Input.GetMouseButtonUp(0))
            text.SetActive(true);
    }

    bool IsInsideJoystick(Vector2 screenPos)
    {
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(
            joystickZone,
            screenPos,
            cam
        );
    }
}