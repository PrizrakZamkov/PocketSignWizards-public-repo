using UnityEngine;
using UnityEngine.UI;

public class SignCooldownCrossUI : MonoBehaviour
{
    [Header("Cooldown Target")]
    [SerializeField] private string gestureName;

    [Header("Cross Settings")]
    [SerializeField] private Color crossColor = new Color(1f, 0.05f, 0.35f, 0.4f);
    [SerializeField] private float lineWidth = 8f;
    [SerializeField] private float padding = 4f;

    [Header("Animation")]
    [SerializeField] private float fadeSpeed = 10f;

    private RectTransform rect;

    private GameObject crossRoot;
    private Image lineA;
    private Image lineB;

    private CanvasGroup canvasGroup;
    private float targetAlpha = 0f;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        CreateCross();
    }

    public void Init(string newGestureName, Color color, float width)
    {
        gestureName = newGestureName;
        crossColor = color;
        lineWidth = width;

        ApplyColor();
        UpdateSize();
    }

    private void Update()
    {
        if (SpellSystem.Instance == null || string.IsNullOrEmpty(gestureName))
        {
            targetAlpha = 0f;
            UpdateFade();
            return;
        }

        bool onCooldown =
            SpellSystem.Instance.GetCooldown(gestureName) > 0f;

        targetAlpha = onCooldown ? crossColor.a : 0f;

        UpdateFade();
        UpdateSize();
    }

    private void UpdateFade()
    {
        canvasGroup.alpha = Mathf.Lerp(
            canvasGroup.alpha,
            targetAlpha,
            Time.deltaTime * fadeSpeed
        );

        bool shouldBeVisible = canvasGroup.alpha > 0.01f;

        if (crossRoot.activeSelf != shouldBeVisible)
            crossRoot.SetActive(shouldBeVisible);
    }

    private void CreateCross()
    {
        crossRoot = new GameObject("CooldownCross");
        crossRoot.transform.SetParent(transform, false);

        RectTransform rootRect =
            crossRoot.AddComponent<RectTransform>();

        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        canvasGroup =
            crossRoot.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;

        lineA = CreateLine("LineA", 45f);
        lineB = CreateLine("LineB", -45f);
    }

    private Image CreateLine(string lineName, float rotation)
    {
        GameObject obj = new GameObject(lineName);
        obj.transform.SetParent(crossRoot.transform, false);

        Image img = obj.AddComponent<Image>();
        img.raycastTarget = false;

        RectTransform lineRect =
            img.GetComponent<RectTransform>();

        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);

        lineRect.localRotation =
            Quaternion.Euler(0f, 0f, rotation);

        return img;
    }

    private void ApplyColor()
    {
        if (lineA == null || lineB == null)
            return;

        lineA.color = crossColor;
        lineB.color = crossColor;
    }

    private void UpdateSize()
    {
        if (rect == null || lineA == null || lineB == null)
            return;

        float w = rect.rect.width + padding * 2f;
        float h = rect.rect.height + padding * 2f;

        float diagonal =
            Mathf.Sqrt(w * w + h * h);

        SetLineSize(lineA, diagonal);
        SetLineSize(lineB, diagonal);
    }

    private void SetLineSize(Image img, float length)
    {
        RectTransform lineRect =
            img.GetComponent<RectTransform>();

        lineRect.sizeDelta =
            new Vector2(length, lineWidth);
    }
}