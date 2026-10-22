using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using static UnityEngine.Rendering.DebugUI.Table;

public class Enemy : MonoBehaviour
{
    [SerializeField] public int hp = 20;

    [Header("XP")]
    [SerializeField] private int ammountOfXP = 1;
    [SerializeField] private float radiusOfSpawn = 0.4f;
    [SerializeField] private GameObject XPPrefab;

    [Header("Damage")]
    [SerializeField] private float impactFrameTime = 0.12f;

    [Header("Death")]
    [SerializeField] private float deathRotateTime = 0.25f;
    [SerializeField] private float deathDissolveTime = 1f;

    [Header("Particles")]
    private Transform player;

    [SerializeField] private ParticleSystem hitParticlesPrefab1;
    [SerializeField] private ParticleSystem hitParticlesPrefab2;

    [SerializeField] private ParticleSystem deathParticlesPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioMixerGroup mixerGroup;

    [SerializeField] private float pitchMin = 0.9f;
    [SerializeField] private float pitchMax = 1.1f;
    [SerializeField] private float volume = 0.8f;

    private AudioSource audioSource;

    private ImpactFlash impactFlash;
    private Material materialInstance;

    public bool isDead = false;
    private EnemyBehaviour behaviour;

    void Awake()
    {
        impactFlash = GetComponent<ImpactFlash>();

        if (impactFlash == null)
            impactFlash = gameObject.AddComponent<ImpactFlash>();

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (mixerGroup != null)
            audioSource.outputAudioMixerGroup = mixerGroup;

        behaviour = GetComponent<EnemyBehaviour>();
    }

    void Start()
    {
        player = PlayerAttack.Instance.transform;
        materialInstance = impactFlash.Material;

        if (behaviour != null)
            behaviour.OnSpawn();
    }
    void Update()
    {
        if (isDead || behaviour == null) return;
        behaviour.UpdateBehaviour();
    }

    public void Damage(int damage)
    {
        if (isDead)
            return;

        hp -= damage;

        if (hp <= 0)
        {
            StartCoroutine(DeathRoutine());
        }
        else
        {
            SpawnParticle(deathParticlesPrefab, Quaternion.identity);
            impactFlash.Flash(impactFrameTime);
            PlaySound(hitClip);
        }
    }

    IEnumerator DeathRoutine()
    {
        if (behaviour != null)
            behaviour.OnDeath();

        isDead = true;

        PlaySound(deathClip);

        SpawnParticle(deathParticlesPrefab, Quaternion.identity);
        impactFlash.SetWhite();

        yield return new WaitForSeconds(impactFrameTime);

        float targetAngle = Random.value < 0.5f ? 90f : -90f;
        float startAngle = transform.eulerAngles.z;

        float rotateTimer = 0f;

        while (rotateTimer < deathRotateTime)
        {
            rotateTimer += Time.deltaTime;

            float t = rotateTimer / deathRotateTime;
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            float angle = Mathf.LerpAngle(startAngle, targetAngle, t);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);

        for (int i = 0; i < ammountOfXP; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-radiusOfSpawn, radiusOfSpawn), Random.RandomRange(-radiusOfSpawn, radiusOfSpawn), 0);
            Instantiate(XPPrefab, transform.position + offset, Quaternion.identity);
        }

        float dissolveTimer = 0f;

        while (dissolveTimer < deathDissolveTime)
        {
            dissolveTimer += Time.deltaTime;

            float value = Mathf.Clamp01(dissolveTimer / deathDissolveTime);
            materialInstance.SetFloat("_DissolveAmount", value);

            yield return null;
        }


        EnemySpawnDirector.Instance?.ReportKill();
        Destroy(gameObject);
    }


    void SpawnParticle(ParticleSystem prefab, Quaternion rotation)
    {
        if (prefab == null)
            return;

        ParticleSystem p = Instantiate(prefab, transform.position, rotation);
        p.Play();

        Destroy(p.gameObject, p.main.duration + p.main.startLifetime.constantMax);
    }
    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        audioSource.pitch = Random.Range(pitchMin, pitchMax);
        audioSource.PlayOneShot(clip, volume);
    }






}