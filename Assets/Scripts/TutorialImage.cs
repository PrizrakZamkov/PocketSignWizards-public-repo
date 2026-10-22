using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TutorialImage : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float delayBeforeFade = 0.5f;
    [SerializeField] private float fadeDuration = 0.4f;

    [Header("Optional")]
    [SerializeField] private bool destroyAfterFade = true;

    private SpriteRenderer sr;
    private float startAlpha;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startAlpha = sr.color.a;
    }

    private void Start()
    {
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        // держим альфу как есть
        yield return new WaitForSeconds(delayBeforeFade);

        Color color = sr.color;

        float time = 0f;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.Clamp01(time / fadeDuration);

            color.a = Mathf.Lerp(startAlpha, 0f, t);
            sr.color = color;

            yield return null;
        }

        color.a = 0f;
        sr.color = color;

        if (destroyAfterFade)
            Destroy(gameObject);
    }
}