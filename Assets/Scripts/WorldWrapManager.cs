using System.Collections.Generic;
using UnityEngine;

public class WorldWrapManager : MonoBehaviour
{
    public static WorldWrapManager Instance;

    [Header("Center")]
    [SerializeField] private Transform center; // Player

    [Header("Wrap Size")]
    [SerializeField] private float width = 50f;
    [SerializeField] private float height = 35f;

    [Header("Optimization")]
    [SerializeField] private float checkInterval = 0.1f;
    [SerializeField] private int checksPerFrame = 80;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private readonly List<WrapObject> objects = new();
    private float timer;
    private int index;

    private float halfW;
    private float halfH;

    private void Awake()
    {
        Instance = this;
        Recalculate();
    }

    private void OnValidate()
    {
        Recalculate();
    }

    private void Recalculate()
    {
        halfW = width * 0.5f;
        halfH = height * 0.5f;
    }

    private void Update()
    {
        if (center == null || objects.Count == 0) return;

        timer += Time.deltaTime;
        if (timer < checkInterval) return;

        timer = 0f;

        int checkedCount = 0;

        while (checkedCount < checksPerFrame && objects.Count > 0)
        {
            if (index >= objects.Count)
                index = 0;

            WrapObject obj = objects[index];

            if (obj == null)
            {
                objects.RemoveAt(index);
                continue;
            }

            Wrap(obj.transform);
            index++;
            checkedCount++;
        }
    }

    public void Register(WrapObject obj)
    {
        if (obj != null && !objects.Contains(obj))
            objects.Add(obj);
    }

    public void Unregister(WrapObject obj)
    {
        objects.Remove(obj);
    }

    private void Wrap(Transform obj)
    {
        Vector3 pos = obj.position;
        Vector3 c = center.position;

        float dx = pos.x - c.x;
        float dy = pos.y - c.y;

        bool changed = false;

        if (dx > halfW)
        {
            pos.x -= width;
            changed = true;
        }
        else if (dx < -halfW)
        {
            pos.x += width;
            changed = true;
        }

        if (dy > halfH)
        {
            pos.y -= height;
            changed = true;
        }
        else if (dy < -halfH)
        {
            pos.y += height;
            changed = true;
        }

        if (changed)
            obj.position = pos;
    }

    public Vector2 GetWrappedDirection(Vector2 from, Vector2 to)
    {
        Vector2 dir = to - from;

        if (Mathf.Abs(dir.x) > halfW)
            dir.x -= Mathf.Sign(dir.x) * width;

        if (Mathf.Abs(dir.y) > halfH)
            dir.y -= Mathf.Sign(dir.y) * height;

        return dir;
    }

    public float GetWrappedDistance(Vector2 from, Vector2 to)
    {
        return GetWrappedDirection(from, to).magnitude;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || center == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center.position, new Vector3(width, height, 0f));
    }
}