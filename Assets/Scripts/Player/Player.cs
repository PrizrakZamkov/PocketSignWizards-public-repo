using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public static Player Instance;

    [Header("HP Bar")]
    [SerializeField] private Transform hpBar;

    private Vector3 hpBarStartScale;
    private int startHp;

    [SerializeField] private int maxHp = 100;
    public int hp = 100;

    [SerializeField] private float impactFrameTime = 0.12f;

    [Header("Invulnerability After Hit")]
    [SerializeField] private float invulnerableTime = 0.8f;
    [SerializeField] private float blinkInterval = 0.08f;
    [SerializeField, Range(0f, 1f)] private float blinkAlpha = 0.35f;

    [Header("Passive Regen")]
    [SerializeField] private bool passiveRegenEnabled = true;
    [SerializeField] private float regenInterval = 3f;
    [SerializeField] private int regenAmount = 2;
    [SerializeField] private bool regenOnlyIfDamaged = true;

    [Header("Death")]
    [SerializeField] private float deathRotateTime = 0.25f;
    [SerializeField] private float deathDissolveTime = 1f;

    [SerializeField] private ParticleSystem hitParticlesPrefab;
    [SerializeField] private ParticleSystem deathParticlesPrefab;

    [Header("Sound")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioSource audioSource;

    private bool isDead = false;
    private bool isInvulnerable = false;

    private SpriteRenderer spriteRenderer;
    private ImpactFlash impactFlash;
    private Material materialInstance;
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;

    private Coroutine hpEffectRoutine;
    private Coroutine regenRoutine;
    private Coroutine invulnerabilityRoutine;

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        hp = maxHp;
        startHp = maxHp;

        if (hpBar != null)
            hpBarStartScale = hpBar.localScale;

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        playerMovement = GetComponent<PlayerMovement>();

        if (spriteRenderer == null)
        {
            Debug.LogError("Player: SpriteRenderer not found in children");
            return;
        }

        impactFlash = spriteRenderer.GetComponent<ImpactFlash>();
        if (impactFlash == null)
            impactFlash = spriteRenderer.gameObject.AddComponent<ImpactFlash>();

        materialInstance = impactFlash.Material;

        if (passiveRegenEnabled)
            regenRoutine = StartCoroutine(PassiveRegenRoutine());
    }

    public Vector2 GetVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (isInvulnerable) return;

        hp -= damage;
        Debug.Log("Player hit: " + damage);
        audioSource.PlayOneShot(hitSound);

        UpdateHpBar(true);

        if (hp <= 0)
        {
            StartCoroutine(DeathRoutine());
            return;
        }

        SpawnParticle(hitParticlesPrefab, Quaternion.identity);
        impactFlash.Flash(impactFrameTime);

        if (invulnerabilityRoutine != null)
            StopCoroutine(invulnerabilityRoutine);

        invulnerabilityRoutine = StartCoroutine(InvulnerabilityRoutine());
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        hp = Mathf.Min(hp + amount, maxHp);
        UpdateHpBar(true);
        impactFlash.Flash(impactFrameTime);
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;

        float timer = 0f;
        bool transparent = false;

        Color originalColor = spriteRenderer.color;

        while (timer < invulnerableTime)
        {
            transparent = !transparent;

            Color c = originalColor;
            c.a = transparent ? blinkAlpha : 1f;
            spriteRenderer.color = c;

            yield return new WaitForSeconds(blinkInterval);
            timer += blinkInterval;
        }

        Color finalColor = originalColor;
        finalColor.a = 1f;
        spriteRenderer.color = finalColor;

        isInvulnerable = false;
        invulnerabilityRoutine = null;
    }

    private void PassiveRegenTick()
    {
        if (isDead) return;
        if (regenAmount <= 0) return;
        if (regenOnlyIfDamaged && hp >= maxHp) return;

        int oldHp = hp;
        hp = Mathf.Min(hp + regenAmount, maxHp);

        if (hp != oldHp)
            UpdateHpBar(false);
    }

    IEnumerator PassiveRegenRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(regenInterval);
            PassiveRegenTick();
        }
    }

    IEnumerator HpChangeEffect(bool strong)
    {
        if (hpBar == null) yield break;

        Transform t = hpBar;

        Vector3 original = t.localScale;
        Vector3 target = original * (strong ? 1.9f : 1.15f);

        float time = 0f;
        float duration = strong ? 0.1f : 0.08f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float lerp = time / duration;
            t.localScale = Vector3.Lerp(original, target, lerp);
            yield return null;
        }

        time = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float lerp = time / duration;
            t.localScale = Vector3.Lerp(target, original, lerp);
            yield return null;
        }

        t.localScale = original;
    }

    IEnumerator DeathRoutine()
    {
        isDead = true;
        audioSource.PlayOneShot(deathSound);
        if (invulnerabilityRoutine != null)
            StopCoroutine(invulnerabilityRoutine);

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        if (regenRoutine != null)
            StopCoroutine(regenRoutine);

        Camera cam = GetComponentInChildren<Camera>();

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (cam != null)
            cam.transform.SetParent(null);

        SpawnParticle(deathParticlesPrefab, Quaternion.identity);
        impactFlash.SetWhite();

        yield return new WaitForSeconds(impactFrameTime);

        float targetAngle = Random.value < 0.5f ? 90f : -90f;
        float startAngle = transform.eulerAngles.z;
        float rotateTimer = 0f;

        while (rotateTimer < deathRotateTime)
        {
            rotateTimer += Time.deltaTime;
            float t = Mathf.Sin((rotateTimer / deathRotateTime) * Mathf.PI * 0.5f);
            float angle = Mathf.LerpAngle(startAngle, targetAngle, t);
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, targetAngle);

        if (playerMovement != null)
            playerMovement.StopMovement();

        float dissolveTimer = 0f;

        while (dissolveTimer < deathDissolveTime)
        {
            dissolveTimer += Time.deltaTime;
            float value = Mathf.Clamp01(dissolveTimer / deathDissolveTime);

            if (materialInstance != null)
                materialInstance.SetFloat("_DissolveAmount", value);

            yield return null;
        }

        SceneManager.LoadScene(0);
        Destroy(gameObject);
    }

    void SpawnParticle(ParticleSystem prefab, Quaternion rotation)
    {
        if (prefab == null) return;

        ParticleSystem p = Instantiate(prefab, transform.position, rotation);
        p.Play();

        Destroy(p.gameObject, p.main.duration + p.main.startLifetime.constantMax);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("XP"))
        {
            XPOrb orb = collision.GetComponent<XPOrb>();
            if (orb != null)
                orb.Collect();
        }
    }

    void UpdateHpBar(bool strongEffect)
    {
        if (hpBar == null) return;

        float t = Mathf.Clamp01((float)hp / startHp);

        Vector3 scale = hpBarStartScale;
        scale.x *= t;
        hpBar.localScale = scale;

        if (hpEffectRoutine != null)
            StopCoroutine(hpEffectRoutine);

        hpEffectRoutine = StartCoroutine(HpChangeEffect(strongEffect));
    }
}