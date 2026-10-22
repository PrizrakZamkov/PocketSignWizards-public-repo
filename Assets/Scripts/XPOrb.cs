using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class XPOrb : MonoBehaviour
{
    [SerializeField] private float flyTime = 0.45f;
    [SerializeField] private float shrinkTime = 0.12f;
    [SerializeField] private float spawnTime = 0.45f;
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float destroyTime = 0.45f;

    [Header("Audio")]
    [SerializeField] private AudioClip collectClip;
    [SerializeField] private float pitchMin = 0.95f;
    [SerializeField] private float pitchMax = 1.1f;
    [SerializeField] private float volume = 0.7f;
    [SerializeField] private AudioMixerGroup mixerGroup;

    private AudioSource audioSource;

    private bool collected;
    private Vector3 startScale;

    private void Awake()
    {
        startScale = transform.localScale;

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (mixerGroup != null)
            audioSource.outputAudioMixerGroup = mixerGroup;
    }

    private void Start()
    {
        StartCoroutine(Spawn());
        StartCoroutine(Destroy());
    }
    private IEnumerator Spawn()
    {
        float timer = 0f;

        while (timer < spawnTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / spawnTime);
            transform.localScale = Vector3.Lerp(Vector3.zero, startScale, t);

            yield return null;
        }
    }
    private IEnumerator Destroy()
    {
        yield return new WaitForSeconds(lifeTime);

        float timer = 0f;

        while (timer < destroyTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / destroyTime);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }
        Destroy(gameObject);
    }

    public void Collect()
    {
        if (collected) return;

        collected = true;

        PlayCollectSound();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;

        StartCoroutine(FlyToUI());
    }

    private IEnumerator FlyToUI()
    {
        Transform target = GameController.Instance.XPTarget;

        Vector3 startPos = transform.position;
        float timer = 0f;

        while (timer < flyTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / flyTime);
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            Vector3 end = Camera.main.ScreenToWorldPoint(target.position);
            end.z = 0f;

            transform.position = Vector3.Lerp(startPos, end, t);

            // размер НЕ меняем во время полёта
            transform.localScale = startScale;

            yield return null;
        }

        timer = 0f;

        while (timer < shrinkTime)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / shrinkTime);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);

            yield return null;
        }

        GameController.Instance.AddXP(1);
        Destroy(gameObject);
    }

    void PlayCollectSound()
    {
        if (collectClip == null) return;

        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(collectClip, volume);
    }




}