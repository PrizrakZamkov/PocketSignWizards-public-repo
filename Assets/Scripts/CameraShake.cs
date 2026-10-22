using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    [SerializeField] private float defaultDuration = 0.12f;
    [SerializeField] private float defaultStrength = 0.12f;

    private Vector3 startLocalPosition;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        Instance = this;
        startLocalPosition = transform.localPosition;
    }

    public void Shake()
    {
        Shake(defaultDuration, defaultStrength);
    }

    public void Shake(float duration, float strength)
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, strength));
    }
    public void StopShake()
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);
    }
    private IEnumerator ShakeRoutine(float duration, float strength)
    {
        float time = 0f;

        while (time < duration)
        {
            float x = Random.Range(-1f, 1f) * strength;
            float y = Random.Range(-1f, 1f) * strength;

            transform.localPosition = startLocalPosition + new Vector3(x, y, 0f);

            time += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = startLocalPosition;
        shakeCoroutine = null;
    }
}