using UnityEngine;

public class ShineEffect : MonoBehaviour
{
    [SerializeField] private RectTransform rect;
    [SerializeField] private float width = 300f;
    [SerializeField] private float angle = -20f;
    [SerializeField] private float loopTime = 2.5f;

    private float parentWidth;

    void Start()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();

        parentWidth = ((RectTransform)rect.parent).rect.width;

        rect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    void Update()
    {
        float t = (ShineGlobal.TimeValue % loopTime) / loopTime;

        float start = -parentWidth - width;
        float end = parentWidth + width;

        float x = Mathf.Lerp(start, end, t);

        rect.anchoredPosition = new Vector2(x, 0);
    }
}