using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PDollarGestureRecognizer;

public class UIGestureLine : Graphic
{
    [SerializeField] private float thickness = 8f;
    [SerializeField] private float drawSpeed = 0.75f;
    [SerializeField] private float loopDelay = 0.3f;
    [SerializeField] private float fadeDuration = 1.4f;
    [SerializeField] private float drawStartThreshold = 0.5f;

    private List<Vector2> points = new List<Vector2>();

    private enum Phase { Idle, Drawing, Waiting, Looping }
    private Phase phase = Phase.Idle;

    private float drawProgress = 1f;
    private float fadeAlpha = 1f;
    private float loopProgress = 0f;
    private float waitTimer = 0f;

    public void SetGesture(Gesture gesture, bool animate)
    {
        points.Clear();
        foreach (Point p in gesture.Points)
            points.Add(new Vector2(p.X, -p.Y));
        NormalizeToRect();

        if (animate)
        {
            drawProgress = 0f;
            fadeAlpha = 1f;
            phase = Phase.Drawing;
        }
        else
        {
            drawProgress = 1f;
            fadeAlpha = 1f;
            phase = Phase.Idle;
        }
        SetVerticesDirty();
    }

    void Update()
    {
        if (phase == Phase.Idle) return;

        switch (phase)
        {
            case Phase.Drawing:
                drawProgress += Time.unscaledDeltaTime * drawSpeed;
                if (drawProgress >= 1f)
                {
                    drawProgress = 1f;
                    waitTimer = loopDelay;
                    phase = Phase.Waiting;
                }
                SetVerticesDirty();
                break;

            case Phase.Waiting:
                waitTimer -= Time.unscaledDeltaTime;
                if (waitTimer <= 0f)
                {
                    loopProgress = 0f;
                    drawProgress = 0f;
                    phase = Phase.Looping;
                }
                break;

            case Phase.Looping:
                loopProgress += Time.unscaledDeltaTime / fadeDuration;
                fadeAlpha = Mathf.Clamp01(1f - loopProgress);

                // Новая линия рисуется с той же скоростью drawSpeed, но стартует после порога
                if (loopProgress >= drawStartThreshold)
                    drawProgress = Mathf.Clamp01(drawProgress + Time.unscaledDeltaTime * drawSpeed);

                // Ждём пока ОБА завершатся
                if (loopProgress >= 1f && drawProgress >= 1f)
                {
                    fadeAlpha = 1f;
                    drawProgress = 1f;
                    waitTimer = loopDelay;
                    phase = Phase.Waiting;
                }
                SetVerticesDirty();
                break;

        }
    }

    void NormalizeToRect()
    {
        if (points.Count == 0) return;
        Vector2 min = points[0], max = points[0];
        foreach (Vector2 p in points) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        Vector2 size = max - min;
        if (size.x <= 0.01f) size.x = 1f;
        if (size.y <= 0.01f) size.y = 1f;
        Rect r = rectTransform.rect;
        float padding = 12f;
        float scale = Mathf.Min((r.width - padding * 2f) / size.x, (r.height - padding * 2f) / size.y);
        Vector2 center = (min + max) * 0.5f;
        for (int i = 0; i < points.Count; i++)
            points[i] = (points[i] - center) * scale;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2) return;

        if (phase == Phase.Looping)
        {
            Color fadedColor = color;
            fadedColor.a = color.a * fadeAlpha;
            DrawSegments(vh, points.Count, fadedColor);

            if (drawProgress > 0f)
            {
                int newCount = Mathf.Clamp(Mathf.RoundToInt(points.Count * drawProgress), 2, points.Count);
                DrawSegments(vh, newCount, color);
            }
        }
        else
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(points.Count * drawProgress), 2, points.Count);
            DrawSegments(vh, count, color);
        }
    }

    void DrawSegments(VertexHelper vh, int count, Color col)
    {
        for (int i = 0; i < count - 1; i++)
            AddLine(vh, points[i], points[i + 1], col);
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        if (points != null && points.Count > 0) { NormalizeToRect(); SetVerticesDirty(); }
    }

    void AddLine(VertexHelper vh, Vector2 a, Vector2 b, Color col)
    {
        Vector2 dir = (b - a).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * thickness * 0.5f;
        int index = vh.currentVertCount;
        vh.AddVert(a - normal, col, Vector2.zero);
        vh.AddVert(a + normal, col, Vector2.zero);
        vh.AddVert(b + normal, col, Vector2.zero);
        vh.AddVert(b - normal, col, Vector2.zero);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index, index + 2, index + 3);
    }
}