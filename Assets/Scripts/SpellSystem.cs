using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// поставить на сцену
/// В инспекторе: перетащить SpellData ассеты, назначитть слой врагов и UI
/// </summary>
/// 
public enum ParticleSpawnType
{
    Player,
    CastCenter
}
[RequireComponent(typeof(AudioSource))]
public class SpellSystem : MonoBehaviour
{
    public static SpellSystem Instance;

    [Header("Spell Registry")]
    [SerializeField] private List<SpellData> registeredSpells;

    [Header("Layers")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Cooldown UI")]
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private float cooldownTextDuration = 1.8f;

    [Header("Screen Flash (необязательно)")]
    [Tooltip("Полноэкранный Image в Canvas (Raycast Target = OFF, начальный alpha = 0)")]
    [SerializeField] private Image flashOverlay;

    [Header("Projectile Prefab")]
    [Tooltip("Prefab с SpriteRenderer + Rigidbody2D (Gravity=0) + CircleCollider2D (Trigger) + SpellProjectile")]
    [SerializeField] private GameObject projectilePrefab;

    [Header("Magic Circle")]
    [SerializeField] private float magicCircleRadius = 1.8f;
    [SerializeField] private float magicCircleDuration = 1.4f;

    [Header("Direction Indicator")]
    [SerializeField] private float dirIndicatorLength = 3.2f;
    [SerializeField] private float dirIndicatorDuration = 0.7f;

    // ── References ────────────────────────────────────────────────────────────
    private AudioSource audioSource;
    private CameraShake cameraShake;
    private Player player;
    private PlayerMovement playerMovement;

    // ── State ─────────────────────────────────────────────────────────────────
    private Dictionary<string, SpellData> spellDict = new();
    private Dictionary<string, float>     cooldowns  = new();

    // Активные орбиты и клинки (чтобы перезапускать их)
    private readonly List<GameObject> activeBlades   = new();
    private readonly List<GameObject> activeOrbitals = new();

    private Coroutine spiralRoutine;
    private float spiralExtraTime = 0f;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        Instance = this;
        audioSource = GetComponent<AudioSource>();

        foreach (var sd in registeredSpells)
            if (sd != null && !string.IsNullOrEmpty(sd.gestureName))
                spellDict[sd.gestureName] = sd;
    }

    void Start()
    {
        cameraShake    = CameraShake.Instance;
        player         = Player.Instance;
        playerMovement = PlayerMovement.Instance;
    }

    void Update() => TickCooldowns();

    // ── Public API ────────────────────────────────────────────────────────────

    public bool   HasSpell(string name)  => spellDict.ContainsKey(name);
    public bool   IsAvailable(string name) => !cooldowns.ContainsKey(name);
    public float  GetCooldown(string name) => cooldowns.TryGetValue(name, out float t) ? t : 0f;
    public Color  GetSpellColor(string name)
        => spellDict.TryGetValue(name, out var sd) ? sd.spellColor : Color.white;

    /// <summary>Попытаться скастовать заклинание. Возвращает true при успехе.</summary>
    public bool TryCast(string gestureName, Vector2[] screenPoints)
    {
        if (!spellDict.TryGetValue(gestureName, out SpellData spell)) return false;

        if (!IsAvailable(gestureName))
        {
            StartCoroutine(ShowCooldownText(gestureName));
            return false;
        }

        // Вычисляем центр и направление
        Vector2 screenCenter = ComputeCenter(screenPoints);
        Vector3 worldCenter  = ScreenToWorld(screenCenter);
        Vector2 castDir      = spell.isDirectional ? GetFacingDir() : Vector2.right;

        StartCoroutine(CastRoutine(spell, worldCenter, castDir, screenPoints));
        return true;
    }

    // ── Cast Routine ──────────────────────────────────────────────────────────

    IEnumerator CastRoutine(SpellData s, Vector3 worldCenter, Vector2 castDir, Vector2[] screenPoints)
    {
        // Пре-каст эффекты
        if (s.showMagicCircle) StartCoroutine(AnimateMagicCircle(s.spellColor));
        if (s.isDirectional) StartCoroutine(ShowDirectionIndicator(castDir, s.spellColor));
        if (s.useParticles && s.particlePrefab != null)
        {
            Vector3 particlePos =
                s.particleSpawnType == ParticleSpawnType.Player
                ? player.transform.position
                : worldCenter;

            GameObject particle = Instantiate(s.particlePrefab, particlePos, Quaternion.identity);
            particle.transform.localScale = new Vector3(s.particleScale, s.particleScale, s.particleScale);
        }

        // Звук
        if (s.castSound != null) audioSource.PlayOneShot(s.castSound);

        // Минимальный шейк на любой каст
        cameraShake?.Shake();

        // Выполняем заклинание
        ExecuteSpell(s, worldCenter, castDir, screenPoints);

        // Кулдаун
        if (s.cooldown > 0f) cooldowns[s.gestureName] = s.cooldown;

        yield return null;
    }

    // ── Dispatch ──────────────────────────────────────────────────────────────

    void ExecuteSpell(SpellData s, Vector3 center, Vector2 dir, Vector2[] screenPts)
    {
        switch (s.spellType)
        {
            case SpellType.LineStrike:             CastLineStrike(s, screenPts);              break;
            case SpellType.Explosion:              CastExplosion(s, center);                  break;
            //case SpellType.ForwardWave:            StartCoroutine(CastForwardWave(s, dir)); break;
            case SpellType.ForwardWave:            StartCoroutine(CastForwardWaveCenter(s, center)); break;
            case SpellType.CircularBlast:          CastCircularBlast(s);                      break;
            case SpellType.ChainLightning:         StartCoroutine(CastChainLightning(s));     break;
            case SpellType.IceExplosion:           CastIceExplosion(s, center);               break;
            case SpellType.FireField:              StartCoroutine(CastFireField(s, center));  break;
            case SpellType.PushAndSlow:            CastPushAndSlow(s);                        break;
            //case SpellType.FreezeForward:          CastFreezeForward(s, dir);                 break;
            case SpellType.FreezeForward:          CastFreezeForwardCenter(s, center);                 break;
            case SpellType.Ignite:                 CastIgnite(s, center);                     break;
            case SpellType.Slow:                   CastSlow(s, center);                       break;
            case SpellType.RotatingBlades:         CastRotatingBlades(s);                     break;
            case SpellType.OrbitalSatellites:      CastOrbitalSatellites(s);                  break;
            //case SpellType.Laser:                  StartCoroutine(CastLaser(s, dir)); break;
            case SpellType.Laser:                  StartCoroutine(CastLaserCenter(s, center)); break;
            //case SpellType.DirectionalProjectiles: CastDirectionalProjectiles(s, dir); break;
            case SpellType.DirectionalProjectiles: CastDirectionalProjectilesCenter(s, center); break;
            case SpellType.RandomProjectiles:      CastRandomProjectiles(s);                  break;
            case SpellType.SpiralProjectiles:      StartCoroutine(CastSpiralProjectiles(s));  break;
            case SpellType.Teleport:               CastTeleport(s, center);                   break;
            case SpellType.Heal:                   CastHeal(s);                               break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ЗАКЛИНАНИЯ
    // ══════════════════════════════════════════════════════════════════════════

    // ── 1. Line Strike ────────────────────────────────────────────────────────
    void CastLineStrike(SpellData s, Vector2[] screenPts)
    {
        HashSet<Enemy> hit = new();
        foreach (var sp in screenPts)
        {
            Vector3 wp = ScreenToWorld(sp);
            foreach (var col in Physics2D.OverlapCircleAll(wp, 0.5f, enemyLayer))
            {
                var e = col.GetComponent<Enemy>();
                if (e != null && hit.Add(e)) e.Damage(s.damage);
            }
        }
    }

    // ── 2. Explosion ──────────────────────────────────────────────────────────
    void CastExplosion(SpellData s, Vector3 center)
    {
        DamageRadius(s, center, s.radius);
        KnockbackRadius(center, s.radius, s.pushForce);
        SpawnFadingCircle(center, s.radius, s.spellColor, 0.55f);
        ScreenFlash(s.spellColor, 0.18f);
        cameraShake?.Shake();
    }

    // ── 3. Forward Wave ───────────────────────────────────────────────────────
    IEnumerator CastForwardWave(SpellData s, Vector2 dir)
    {
        Vector3 origin = player.transform.position;
        float elapsed = 0f;
        HashSet<Enemy> hit = new();

        LineRenderer lr = MakeLineRenderer(s.spellColor, 0.15f, 0.04f, -14);

        while (elapsed < s.waveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / s.waveDuration;
            float dist = t * (s.waveSpeed * s.waveDuration);
            Vector3 front = origin + (Vector3)(dir * dist);

            // Проверяем врагов в зоне волны
            foreach (var col in Physics2D.OverlapCircleAll(front, s.waveWidth * 0.5f, enemyLayer))
            {
                var e = col.GetComponent<Enemy>();
                if (e != null && hit.Add(e))
                {
                    e.Damage(s.damage);
                    ApplyKnockback(col.transform, dir, s.pushForce * 0.5f);
                }
            }

            // Визуал — перпендикулярная линия
            Vector2 perp = new(-dir.y, dir.x);
            lr.positionCount = 2;
            lr.SetPosition(0, front + (Vector3)(perp * s.waveWidth * 0.5f));
            lr.SetPosition(1, front - (Vector3)(perp * s.waveWidth * 0.5f));
            Color c = s.spellColor; c.a = 1f - t;
            lr.startColor = lr.endColor = c;

            yield return null;
        }
        Destroy(lr.gameObject);
    }

    // ── 3. Forward Wave ───────────────────────────────────────────────────────
    IEnumerator CastForwardWaveCenter(SpellData s, Vector3 center)
    {
        Vector3 origin  = player.transform.position;
        Vector3 dir = (center - player.transform.position).normalized;
        float   elapsed = 0f;
        HashSet<Enemy> hit = new();

        LineRenderer lr = MakeLineRenderer(s.spellColor, 0.15f, 0.04f, -14);

        while (elapsed < s.waveDuration)
        {
            elapsed += Time.deltaTime;
            float t       = elapsed / s.waveDuration;
            float dist    = t * (s.waveSpeed * s.waveDuration);
            Vector3 front = origin + (Vector3)(dir * dist);

            // Проверяем врагов в зоне волны
            foreach (var col in Physics2D.OverlapCircleAll(front, s.waveWidth * 0.5f, enemyLayer))
            {
                var e = col.GetComponent<Enemy>();
                if (e != null && hit.Add(e))
                {
                    e.Damage(s.damage);
                    ApplyKnockback(col.transform, dir, s.pushForce * 0.5f);
                }
            }

            // Визуал — перпендикулярная линия
            Vector2 perp = new(-dir.y, dir.x);
            lr.positionCount = 2;
            lr.SetPosition(0, front + (Vector3)(perp * s.waveWidth * 0.5f));
            lr.SetPosition(1, front - (Vector3)(perp * s.waveWidth * 0.5f));
            Color c = s.spellColor; c.a = 1f - t;
            lr.startColor = lr.endColor = c;

            yield return null;
        }
        Destroy(lr.gameObject);
    }

    // ── 4. Circular Blast ─────────────────────────────────────────────────────
    void CastCircularBlast(SpellData s)
    {
        Vector3 pos = player.transform.position;
        DamageRadius(s, pos, s.radius);
        KnockbackRadius(pos, s.radius, s.pushForce);
        SpawnFadingCircle(pos, s.radius, s.spellColor, 0.6f);
        SpawnFadingCircle(pos, s.radius * 0.5f, s.spellColor, 0.4f);
        ScreenFlash(s.spellColor, 0.15f);
        cameraShake?.Shake();
    }

    // ── 5. Chain Lightning ────────────────────────────────────────────────────
    IEnumerator CastChainLightning(SpellData s)
    {
        Vector3 playerPos = player.transform.position;

        Enemy first = FindNearest(playerPos, 999f, null);
        if (first == null) yield break;

        List<Enemy>     chain   = new() { first };
        List<Vector3>   anchors = new() { playerPos, first.transform.position };

        first.Damage(s.damage);

        for (int i = 0; i < s.chainCount - 1; i++)
        {
            Enemy next = FindNearest(chain[^1].transform.position, s.chainRadius, chain);
            if (next == null) break;
            chain.Add(next);
            anchors.Add(next.transform.position);
            next.Damage(Mathf.RoundToInt(s.damage * Mathf.Pow(0.75f, i + 1)));
        }

        for (int i = 0; i < anchors.Count - 1; i++)
            StartCoroutine(AnimateLightning(anchors[i], anchors[i + 1], s.spellColor, 0.45f));

        ScreenFlash(s.spellColor, 0.1f);
        cameraShake?.Shake();
        yield return null;
    }

    // ── 6. Ice Explosion ──────────────────────────────────────────────────────
    void CastIceExplosion(SpellData s, Vector3 center)
    {
        foreach (var col in Physics2D.OverlapCircleAll(center, s.radius, enemyLayer))
        {
            var e = col.GetComponent<Enemy>();
            if (e == null) continue;
            e.Damage(s.damage);
            col.GetComponent<EnemyStatusEffects>()?.Apply(StatusEffectType.Frozen, s.freezeDuration);
        }
        SpawnFadingCircle(center, s.radius, s.spellColor, 0.75f);
        ScreenFlash(s.spellColor, 0.13f);
        cameraShake?.Shake();
    }

    // ── 7. Fire Field ─────────────────────────────────────────────────────────
    IEnumerator CastFireField(SpellData s, Vector3 center)
    {
        LineRenderer circle = MakeCircleLR(center, s.radius, s.spellColor, -15);
        float elapsed = 0f;

        while (elapsed < s.fieldDuration)
        {
            elapsed += s.fieldTickInterval;
            yield return new WaitForSeconds(s.fieldTickInterval);

            foreach (var col in Physics2D.OverlapCircleAll(center, s.radius, enemyLayer))
            {
                var e = col.GetComponent<Enemy>();
                if (e == null) continue;
                e.Damage(s.fieldDamagePerTick);
                col.GetComponent<EnemyStatusEffects>()?.Apply(StatusEffectType.Burning, s.burnDuration);
            }

            if (circle != null)
            {
                Color c = s.spellColor;
                c.a = Mathf.Lerp(0.9f, 0.2f, elapsed / s.fieldDuration);
                circle.startColor = circle.endColor = c;
            }
        }

        if (circle != null) Destroy(circle.gameObject);
    }

    // ── 8. Push + Slow ────────────────────────────────────────────────────────
    void CastPushAndSlow(SpellData s)
    {
        Vector3 pos = player.transform.position;
        foreach (var col in Physics2D.OverlapCircleAll(pos, s.radius, enemyLayer))
        {
            var e = col.GetComponent<Enemy>();
            if (e == null) continue;
            Vector3 dir = (col.transform.position - pos).normalized;
            ApplyKnockback(col.transform, dir, s.pushForce);
            col.GetComponent<EnemyStatusEffects>()?.Apply(StatusEffectType.Slowed, s.slowDuration);
        }
        SpawnFadingCircle(pos, s.radius, s.spellColor, 0.5f);
        ScreenFlash(s.spellColor, 0.1f);
    }

    // ── 9. Freeze Forward ─────────────────────────────────────────────────────
    void CastFreezeForward(SpellData s, Vector2 dir)
    {
        Vector3 pos = player.transform.position;
        foreach (var col in Physics2D.OverlapCircleAll(pos, s.radius, enemyLayer))
        {
            Vector2 toEnemy = ((Vector2)col.transform.position - (Vector2)pos).normalized;
            if (Vector2.Dot(dir, toEnemy) < 0.25f) continue; // конус ~75°

            var e = col.GetComponent<Enemy>();
            if (e == null) continue;
            e.Damage(s.damage);
            col.GetComponent<EnemyStatusEffects>()?.Apply(StatusEffectType.Frozen, s.freezeDuration);
        }
        StartCoroutine(AnimateCone(pos, dir, s.radius, 75f, s.spellColor, 0.65f));
    }
    // ── 9. Freeze Forward Center Version ─────────────────────────────────────────────────────
    void CastFreezeForwardCenter(SpellData s, Vector3 center)
    {
        Vector3 pos = player.transform.position;
        Vector3 dir = (center - player.transform.position).normalized;

        foreach (var col in Physics2D.OverlapCircleAll(pos, s.radius, enemyLayer))
        {
            Vector2 toEnemy = ((Vector2)col.transform.position - (Vector2)pos).normalized;
            if (Vector2.Dot(dir, toEnemy) < 0.25f) continue; // конус ~75°

            var e = col.GetComponent<Enemy>();
            if (e == null) continue;
            e.Damage(s.damage);
            col.GetComponent<EnemyStatusEffects>()?.Apply(StatusEffectType.Frozen, s.freezeDuration);
        }
        StartCoroutine(AnimateCone(pos, dir, s.radius, 75f, s.spellColor, 0.65f));
    }

    // ── 10. Ignite ────────────────────────────────────────────────────────────
    void CastIgnite(SpellData s, Vector3 center)
    {
        foreach (var col in Physics2D.OverlapCircleAll(center, s.radius, enemyLayer))
        {
            var e = col.GetComponent<Enemy>();
            if (e == null) continue;
            e.Damage(s.damage);
            col.GetComponent<EnemyStatusEffects>()?.Apply(StatusEffectType.Burning, s.burnDuration);
        }
        SpawnFadingCircle(center, s.radius, s.spellColor, 0.5f);
    }

    // ── 11. Slow ──────────────────────────────────────────────────────────────
    void CastSlow(SpellData s, Vector3 center)
    {
        foreach (var col in Physics2D.OverlapCircleAll(center, s.radius, enemyLayer))
            col.GetComponent<EnemyStatusEffects>()?.Apply(StatusEffectType.Slowed, s.slowDuration);
        SpawnFadingCircle(center, s.radius, s.spellColor, 0.5f);
    }

    // ── 12. Rotating Blades ───────────────────────────────────────────────────
    void CastRotatingBlades(SpellData s)
    {
        foreach (var b in activeBlades) if (b) Destroy(b);
        activeBlades.Clear();

        for (int i = 0; i < s.bladeCount; i++)
        {
            float startAngle = 360f / s.bladeCount * i;
            var go = new GameObject($"Blade_{i}");

            var lr = go.AddComponent<LineRenderer>();
            lr.material     = new Material(Shader.Find("Sprites/Default"));
            lr.startColor   = lr.endColor = s.spellColor;
            lr.startWidth   = lr.endWidth = 0.09f;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.sortingOrder  = -14;

            var comp = go.AddComponent<RotatingBladeOrbit>();
            comp.Init(player.transform, s.bladeOrbitRadius, s.bladeSpeed,
                      startAngle, s.damage, enemyLayer, s.bladeLifetime, s.spellColor);

            activeBlades.Add(go);
        }
    }

    // ── 13. Orbital Satellites ────────────────────────────────────────────────
    void CastOrbitalSatellites(SpellData s)
    {
        foreach (var o in activeOrbitals) if (o) Destroy(o);
        activeOrbitals.Clear();

        for (int i = 0; i < s.orbitalCount; i++)
        {
            float startAngle = 360f / s.orbitalCount * i;
            var go = new GameObject($"Orbital_{i}");

            // Процедурный кружок через LineRenderer (без спрайта)
            var lr = go.AddComponent<LineRenderer>();
            lr.material     = new Material(Shader.Find("Sprites/Default"));
            lr.startColor   = lr.endColor = s.spellColor;
            lr.startWidth   = lr.endWidth = 0.18f;
            lr.loop          = true;
            lr.useWorldSpace = true;
            lr.sortingOrder  = -14;
            MakeCirclePositions(lr, 0.22f, 12);

            var comp = go.AddComponent<OrbitalSatellite>();
            comp.Init(player.transform, s.orbitalRadius, s.orbitalSpeed,
                      startAngle, s.damage, enemyLayer, s.orbitalLifetime);

            activeOrbitals.Add(go);
        }
    }
    
    // ── 14. Laser ─────────────────────────────────────────────────────────────
    IEnumerator CastLaser(SpellData s, Vector2 dir)
    {
        Vector3 origin = player.transform.position;
        Vector3 target = origin + (Vector3)(dir * s.laserRange);

        // Урон по лучу
        HashSet<Enemy> hit = new();
        foreach (var h in Physics2D.RaycastAll(origin, dir, s.laserRange, enemyLayer))
        {
            var e = h.collider.GetComponent<Enemy>();
            if (e != null && hit.Add(e)) e.Damage(s.damage);
        }

        // Визуал — широкий луч, затухает
        LineRenderer lr = MakeLineRenderer(Color.white, 0.35f, 0.06f, -13);
        lr.positionCount = 2;
        lr.SetPosition(0, origin);
        lr.SetPosition(1, target);

        ScreenFlash(Color.white, 0.12f);
        cameraShake?.Shake();

        float elapsed = 0f;
        while (elapsed < s.laserDuration)
        {
            elapsed += Time.deltaTime;
            float a = 1f - elapsed / s.laserDuration;
            Color c = Color.white; c.a = a;
            lr.startColor = lr.endColor = c;
            // Сужаем к концу
            lr.startWidth = Mathf.Lerp(0.35f, 0.06f, elapsed / s.laserDuration);
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    // ── 14. Laser ─────────────────────────────────────────────────────────────
    IEnumerator CastLaserCenter(SpellData s, Vector3 center)
    {
        Vector3 origin = player.transform.position;
        Vector3 dir = (center - player.transform.position).normalized;
        Vector3 target = origin + (Vector3)(dir * s.laserRange);

        // Урон по лучу
        HashSet<Enemy> hit = new();
        foreach (var h in Physics2D.RaycastAll(origin, dir, s.laserRange, enemyLayer))
        {
            var e = h.collider.GetComponent<Enemy>();
            if (e != null && hit.Add(e)) e.Damage(s.damage);
        }

        // Визуал — широкий луч, затухает
        LineRenderer lr = MakeLineRenderer(Color.white, 0.35f, 0.06f, -13);
        lr.positionCount = 2;
        lr.SetPosition(0, origin);
        lr.SetPosition(1, target);

        ScreenFlash(Color.white, 0.12f);
        cameraShake?.Shake();

        float elapsed = 0f;
        while (elapsed < s.laserDuration)
        {
            elapsed += Time.deltaTime;
            float a = 1f - elapsed / s.laserDuration;
            Color c = Color.white; c.a = a;
            lr.startColor = lr.endColor = c;
            // Сужаем к концу
            lr.startWidth = Mathf.Lerp(0.35f, 0.06f, elapsed / s.laserDuration);
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    // ── 15. Directional Projectiles ───────────────────────────────────────────
    void CastDirectionalProjectiles(SpellData s, Vector2 dir)
    {
        if (projectilePrefab == null) { Debug.LogWarning("SpellSystem: projectilePrefab не назначен"); return; }
        float half = s.projectileSpread * 0.5f;
        float step = s.projectileCount > 1 ? s.projectileSpread / (s.projectileCount - 1) : 0f;
        for (int i = 0; i < s.projectileCount; i++)
            SpawnProjectile(s, player.transform.position, Rotate(dir, -half + step * i));
    }

    // ── 15. Directional Projectiles ───────────────────────────────────────────
    void CastDirectionalProjectilesCenter(SpellData s, Vector3 center)
    {
        Vector3 dir = (center - player.transform.position).normalized;
        if (projectilePrefab == null) { Debug.LogWarning("SpellSystem: projectilePrefab не назначен"); return; }
        float half = s.projectileSpread * 0.5f;
        float step = s.projectileCount > 1 ? s.projectileSpread / (s.projectileCount - 1) : 0f;
        for (int i = 0; i < s.projectileCount; i++)
            SpawnProjectile(s, player.transform.position, Rotate(dir, -half + step * i));
    }

    // ── 16. Random Projectiles ────────────────────────────────────────────────
    void CastRandomProjectiles(SpellData s)
    {
        if (projectilePrefab == null) { Debug.LogWarning("SpellSystem: projectilePrefab не назначен"); return; }
        for (int i = 0; i < s.projectileCount; i++)
            SpawnProjectile(s, player.transform.position, Random.insideUnitCircle.normalized);
    }

    // ── 17. Spiral Projectiles ────────────────────────────────────────────────
    //IEnumerator CastSpiralProjectiles(SpellData s)
    //{
    //    if (projectilePrefab == null) { Debug.LogWarning("SpellSystem: projectilePrefab не назначен"); yield break; }
    //    float angleStep = 360f / s.projectileCount;
    //    for (int i = 0; i < s.projectileCount; i++)
    //    {
    //        float rad = (angleStep * i) * Mathf.Deg2Rad;
    //        Vector2 dir = new(Mathf.Cos(rad), Mathf.Sin(rad));
    //        SpawnProjectile(s, player.transform.position, dir);
    //        yield return new WaitForSeconds(0.045f);
    //    }
    //}
    IEnumerator CastSpiralProjectiles(SpellData s)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("SpellSystem: projectilePrefab не назначен");
            yield break;
        }

        // ВАРИАНТ 1: если спираль уже работает — просто добавляем ей время
        if (spiralRoutine != null)
        {
            spiralExtraTime += s.projectileLifetime;
            yield break;
        }

        spiralRoutine = StartCoroutine(SpiralProjectilesRoutine(s));

        /*
        // ВАРИАНТ 2: если спираль уже работает — перезапускаем заново
        if (spiralRoutine != null)
        {
            StopCoroutine(spiralRoutine);
            spiralRoutine = null;
        }

        spiralRoutine = StartCoroutine(SpiralProjectilesRoutine(s));
        */

        yield break;
    }

    IEnumerator SpiralProjectilesRoutine(SpellData s)
    {
        float timer = 0f;
        float totalTime = s.projectileLifetime;

        float angle = 0f;
        float shootDelay = 0.045f;
        float angleStep = 360f / Mathf.Max(1, s.projectileCount);

        while (timer < totalTime)
        {
            Vector2 dir = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            SpawnProjectile(s, player.transform.position, dir);

            angle += angleStep;

            yield return new WaitForSeconds(shootDelay);

            timer += shootDelay;

            if (spiralExtraTime > 0f)
            {
                totalTime += spiralExtraTime;
                spiralExtraTime = 0f;
            }
        }

        spiralRoutine = null;
        spiralExtraTime = 0f;
    }

    // ── 18. Teleport ──────────────────────────────────────────────────────────
    //void CastTeleport(SpellData s, Vector3 worldCenter)
    //{
    //    SpawnFadingCircle(player.transform.position, 0.6f, s.spellColor, 0.35f);
    //    ScreenFlash(Color.white, 0.16f);
    //    player.transform.position = worldCenter;
    //    SpawnFadingCircle(worldCenter, 0.6f, s.spellColor, 0.5f);
    //    cameraShake?.Shake();
    //}

    void CastTeleport(SpellData s, Vector3 worldCenter)
    {
        Vector3 oldPlayerPos = player.transform.position;

        // Эффект исчезновения
        SpawnFadingCircle(oldPlayerPos, 2f, s.spellColor, 0.35f);
        ScreenFlash(Color.white, 0.16f);

        // Ищем врагов в точке телепорта
        Collider2D[] enemies = Physics2D.OverlapCircleAll(worldCenter, s.radius, enemyLayer);

        foreach (var col in enemies)
        {
            if (col == null) continue;

            // Перемещаем врага на старое место игрока
            col.transform.position = oldPlayerPos;

            // Можно чуть раскидать вокруг точки
            Vector2 offset = Random.insideUnitCircle * 0.8f;
            col.transform.position += (Vector3)offset;
        }

        // Телепорт игрока
        player.transform.position = worldCenter;

        // Эффект появления
        SpawnFadingCircle(worldCenter, 0.6f, s.spellColor, 0.5f);

        cameraShake?.Shake();
    }

    // ── 19. Heal ──────────────────────────────────────────────────────────────
    void CastHeal(SpellData s)
    {
        player.Heal(s.healAmount);
        StartCoroutine(AnimateMagicCircle(s.spellColor));
        ScreenFlash(s.spellColor, 0.22f);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ВИЗУАЛЬНЫЕ ХЕЛПЕРЫ
    // ══════════════════════════════════════════════════════════════════════════

    IEnumerator AnimateMagicCircle(Color color)
    {
        var go = new GameObject("MagicCircle");
        go.transform.position = player.transform.position;

        LineRenderer lr = MakeCircleLRLocal(go, magicCircleRadius, color, -15);

        // Второй внутренний круг для красоты
        var go2 = new GameObject("MagicCircleInner");
        go2.transform.SetParent(go.transform);
        go2.transform.localPosition = Vector3.zero;
        LineRenderer lr2 = MakeCircleLRLocal(go2, magicCircleRadius * 0.6f, color, -15);

        float half    = magicCircleDuration * 0.45f;
        float elapsed = 0f;

        while (elapsed < magicCircleDuration)
        {
            elapsed += Time.deltaTime;

            float scale = Mathf.Clamp01(elapsed / half);
            float alpha = elapsed < half ? 1f : 1f - (elapsed - half) / (magicCircleDuration - half);

            go.transform.position   = player.transform.position;
            go.transform.localScale = Vector3.one * scale;

            Color c = color; c.a = alpha;
            if (lr  != null) lr.startColor  = lr.endColor  = c;
            if (lr2 != null) lr2.startColor = lr2.endColor = c;

            // Медленное вращение второго круга
            go2.transform.Rotate(0f, 0f, 45f * Time.deltaTime);

            yield return null;
        }
        Destroy(go);
    }

    IEnumerator ShowDirectionIndicator(Vector2 dir, Color color)
    {
        var go = new GameObject("DirIndicator");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material     = new Material(Shader.Find("Sprites/Default"));
        lr.useWorldSpace = true;
        lr.sortingOrder  = -13;
        lr.startWidth    = lr.endWidth = 0.06f;

        float cone = 28f;
        lr.positionCount = 4;

        float elapsed = 0f;
        while (elapsed < dirIndicatorDuration)
        {
            elapsed += Time.deltaTime;
            float a = 1f - elapsed / dirIndicatorDuration;
            Color c = color; c.a = a;
            lr.startColor = lr.endColor = c;

            Vector3 origin = player.transform.position;
            Vector3 tip    = origin + (Vector3)(dir * dirIndicatorLength);
            Vector3 left   = origin + (Vector3)(Rotate(dir,  cone) * dirIndicatorLength * 0.75f);
            Vector3 right  = origin + (Vector3)(Rotate(dir, -cone) * dirIndicatorLength * 0.75f);

            lr.SetPosition(0, left);
            lr.SetPosition(1, origin);
            lr.SetPosition(2, right);
            lr.SetPosition(3, tip);

            yield return null;
        }
        Destroy(go);
    }

    IEnumerator AnimateLightning(Vector3 from, Vector3 to, Color color, float duration)
    {
        LineRenderer lr = MakeLineRenderer(color, 0.09f, 0.02f, -13);
        lr.positionCount = 8;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < 8; i++)
            {
                float t   = i / 7f;
                Vector3 p = Vector3.Lerp(from, to, t);
                if (i > 0 && i < 7)
                    p += new Vector3(Random.Range(-0.28f, 0.28f), Random.Range(-0.28f, 0.28f), 0f);
                lr.SetPosition(i, p);
            }
            float a = 1f - elapsed / duration;
            Color c = color; c.a = a;
            lr.startColor = lr.endColor = c;
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    IEnumerator AnimateCone(Vector3 origin, Vector2 dir, float length, float angleDeg, Color color, float duration)
    {
        LineRenderer lr = MakeLineRenderer(color, 0.07f, 0.07f, -14);
        int arcSegs = 18;
        float half  = angleDeg * 0.5f;
        lr.positionCount = arcSegs + 3;

        lr.SetPosition(0, origin);
        for (int i = 0; i <= arcSegs; i++)
        {
            float t   = i / (float)arcSegs;
            float ang = Mathf.Lerp(half, -half, t);
            lr.SetPosition(1 + i, origin + (Vector3)(Rotate(dir, ang) * length));
        }
        lr.SetPosition(arcSegs + 2, origin);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c = color; c.a = 1f - elapsed / duration;
            lr.startColor = lr.endColor = c;
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    // Затухающий круг-область (короткий, показывает зону удара)
    void SpawnFadingCircle(Vector3 center, float radius, Color color, float duration)
        => StartCoroutine(FadingCircleRoutine(center, radius, color, duration));

    IEnumerator FadingCircleRoutine(Vector3 center, float radius, Color color, float duration)
    {
        LineRenderer lr = MakeCircleLR(center, radius, color, -15);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color c = color; c.a = 1f - elapsed / duration;
            lr.startColor = lr.endColor = c;
            yield return null;
        }
        if (lr) Destroy(lr.gameObject);
    }

    void ScreenFlash(Color color, float duration)
    {
        if (flashOverlay == null) return;
        StartCoroutine(FlashRoutine(color, duration));
    }

    IEnumerator FlashRoutine(Color color, float duration)
    {
        color.a = 0.28f;
        flashOverlay.color = color;
        flashOverlay.gameObject.SetActive(true);

        float elapsed = 0f;
        Color start = flashOverlay.color;
        Color end   = color; end.a = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            flashOverlay.color = Color.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        flashOverlay.gameObject.SetActive(false);
    }

    IEnumerator ShowCooldownText(string gestureName)
    {
        if (cooldownText == null) yield break;
        float remaining = GetCooldown(gestureName);
        cooldownText.text = $"{gestureName} is reloading ({remaining:F1}s)";
        cooldownText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(cooldownTextDuration);
        cooldownText.gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // УТИЛИТЫ
    // ══════════════════════════════════════════════════════════════════════════

    void DamageRadius(SpellData s, Vector3 center, float radius)
    {
        foreach (var col in Physics2D.OverlapCircleAll(center, radius, enemyLayer))
            col.GetComponent<Enemy>()?.Damage(s.damage);
    }

    void KnockbackRadius(Vector3 center, float radius, float force)
    {
        foreach (var col in Physics2D.OverlapCircleAll(center, radius, enemyLayer))
        {
            Vector3 dir = (col.transform.position - center).normalized;
            ApplyKnockback(col.transform, dir, force);
        }
    }

    void ApplyKnockback(Transform t, Vector3 dir, float force)
    {
        var em = t.GetComponent<EnemyMovement>();
        if (em != null)
            em.TriggerKnockback(dir, force, 0.22f);
    }

    void SpawnProjectile(SpellData s, Vector3 origin, Vector2 dir)
    {
        var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
        go.GetComponent<SpellProjectile>()?.Init(dir, s.projectileSpeed, s.damage,
                                                  s.projectileLifetime, s.spellColor, enemyLayer);
    }

    Enemy FindNearest(Vector3 pos, float range, List<Enemy> exclude)
    {
        Enemy best = null;
        float bestDist = float.MaxValue;
        foreach (var col in Physics2D.OverlapCircleAll(pos, range, enemyLayer))
        {
            var e = col.GetComponent<Enemy>();
            if (e == null || e.isDead) continue;
            if (exclude != null && exclude.Contains(e)) continue;
            float d = Vector2.Distance(pos, col.transform.position);
            if (d < bestDist) { bestDist = d; best = e; }
        }
        return best;
    }

    Vector2 GetFacingDir()
    {
        if (playerMovement == null) return Vector2.right;
        if (playerMovement.LastDirection != Vector2.zero) return playerMovement.LastDirection;
        return playerMovement.FacingRight ? Vector2.right : Vector2.left;
    }

    Vector2 ComputeCenter(Vector2[] pts)
    {
        Vector2 sum = Vector2.zero;
        foreach (var p in pts) sum += p;
        return pts.Length > 0 ? sum / pts.Length : Vector2.zero;
    }

    Vector3 ScreenToWorld(Vector2 sp)
    {
        Vector3 w = Camera.main.ScreenToWorldPoint(new Vector3(sp.x, sp.y, 10f));
        w.z = 0; return w;
    }

    static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }

    void TickCooldowns()
    {
        var keys = new List<string>(cooldowns.Keys);
        foreach (var k in keys)
        {
            cooldowns[k] -= Time.deltaTime;
            if (cooldowns[k] <= 0f) cooldowns.Remove(k);
        }
    }

    // ── LineRenderer фабрики ──────────────────────────────────────────────────

    LineRenderer MakeLineRenderer(Color color, float sw, float ew, int order)
    {
        var go = new GameObject("LineEffect");
        var lr = go.AddComponent<LineRenderer>();
        lr.material     = new Material(Shader.Find("Sprites/Default"));
        lr.startColor   = lr.endColor = color;
        lr.startWidth   = sw; lr.endWidth = ew;
        lr.useWorldSpace = true;
        lr.sortingOrder  = order;
        return lr;
    }

    LineRenderer MakeCircleLR(Vector3 center, float radius, Color color, int order)
    {
        var go = new GameObject("CircleEffect");
        go.transform.position = center;
        return MakeCircleLRLocal(go, radius, color, order);
    }

    LineRenderer MakeCircleLRLocal(GameObject go, float radius, Color color, int order)
    {
        var lr = go.AddComponent<LineRenderer>();
        lr.material     = new Material(Shader.Find("Sprites/Default"));
        lr.startColor   = lr.endColor = color;
        lr.startWidth   = lr.endWidth = 0.055f;
        lr.useWorldSpace = false;
        lr.loop          = true;
        lr.sortingOrder  = order;
        MakeCirclePositions(lr, radius, 36);
        return lr;
    }

    static void MakeCirclePositions(LineRenderer lr, float radius, int segs)
    {
        lr.positionCount = segs;
        for (int i = 0; i < segs; i++)
        {
            float a = (float)i / segs * 2f * Mathf.PI;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
        }
    }
}
