using UnityEngine;

public class TitlePulse : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    [SerializeField] private float amplitude = 0.1f;

    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        float t = Mathf.Sin(Time.unscaledTime * speed) * amplitude;
        transform.localScale = baseScale * (1f + t);
    }
}