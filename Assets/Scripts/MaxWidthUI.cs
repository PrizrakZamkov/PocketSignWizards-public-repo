using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MaxWidthUI : MonoBehaviour
{
    [SerializeField] private float maxWidth = 900f;
    [SerializeField] private float sidePadding = 0f;

    private RectTransform rect;
    private RectTransform parentRect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        parentRect = rect.parent as RectTransform;
    }

    void Update()
    {
        if (parentRect == null) return;

        float availableWidth = parentRect.rect.width - sidePadding * 2f;
        float targetWidth = Mathf.Min(availableWidth, maxWidth);

        rect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            targetWidth
        );
    }
}