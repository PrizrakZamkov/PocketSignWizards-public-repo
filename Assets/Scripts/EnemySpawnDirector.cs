using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Директор спавна врагов для Vampire Survivors–образной игры.
///
/// Как подключить:
///   1. Добавь этот компонент на любой GameObject (например GameController).
///   2. Назначь SpawnDirectorConfig в инспекторе.
///   3. Назначь Transform playerTransform (или укажи тег "Player").
///   4. Создай SpawnDirectorConfig через Assets ▶ Create ▶ Spawn Director Config.
///   5. Вызови StartSpawning() когда игра начинается, StopSpawning() при паузе/конце.
///
/// Директор работает независимо от старой волновой системы — можно использовать
/// обе параллельно (JSON-волны для сценарных событий + директор для фона).
/// </summary>
public class EnemySpawnDirector : MonoBehaviour
{
    public static EnemySpawnDirector Instance;
    // ─────────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────────
    [Header("Настройки")]
    [SerializeField] private SpawnDirectorConfig config;
    [SerializeField] private Transform playerTransform;

    [Header("Камера (для edge-спавна)")]
    [SerializeField] private Camera gameCamera;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    // ─────────────────────────────────────────────────────────────
    //  Runtime State
    // ─────────────────────────────────────────────────────────────
    private float currentBudget;
    private float elapsedTime;
    private float nextEliteEvent;
    private float nextHordeRush;
    private float nextBossEvent;

    private bool isRunning;
    private bool isHordeRushActive;

    // Адаптивная сложность
    private int killsInLastSecond;
    private float killRateTimer;
    private float currentKillRate;

    // Список живых врагов для счётчика maxAliveEnemies
    private readonly List<GameObject> aliveEnemies = new List<GameObject>(256);

    // Кешированные prefab-словари по тирам
    private readonly List<EnemyTierEntry>[] tierLists = new List<EnemyTierEntry>[4];

    // ─────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────

    /// <summary>Сообщи директору об убийстве — для адаптивной сложности.</summary>
    public void ReportKill() => killsInLastSecond++;

    public void StartSpawning()
    {
        if (isRunning) return;
        isRunning = true;
        StartCoroutine(SpawnLoop());
        StartCoroutine(EventLoop());
        StartCoroutine(KillRateTracker());
    }

    public void StopSpawning()
    {
        isRunning = false;
        StopAllCoroutines();
    }

    public float ElapsedTime => elapsedTime;
    public float CurrentBudgetPerSec => BudgetPerSecond();
    public int AliveCount => CountAlive();

    // ─────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        BuildTierLists();

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (gameCamera == null)
            gameCamera = Camera.main;

        // Инициализация таймеров событий
        nextEliteEvent = config.eliteEventInterval > 0 ? config.eliteEventInterval : float.MaxValue;
        nextHordeRush  = config.hordeRushInterval  > 0 ? config.hordeRushInterval  : float.MaxValue;
        nextBossEvent  = config.bossInterval        > 0 ? config.bossInterval        : float.MaxValue;
    }

    // ─────────────────────────────────────────────────────────────
    //  Корутины
    // ─────────────────────────────────────────────────────────────

    private IEnumerator SpawnLoop()
    {
        while (isRunning)
        {
            float interval = Random.Range(config.minSpawnInterval, config.maxSpawnInterval);
            yield return new WaitForSeconds(interval);

            if (!isRunning) yield break;

            // Пополняем бюджет
            currentBudget = Mathf.Min(
                currentBudget + BudgetPerSecond() * interval,
                config.maxBudget * (isHordeRushActive ? config.hordeRushBudgetMultiplier : 1f)
            );

            // Не спавним если некуда
            if (config.maxAliveEnemies > 0 && CountAlive() >= config.maxAliveEnemies)
                continue;

            // Выбираем паттерн и спавним
            SpawnPattern pattern = ChoosePattern();
            ExecutePattern(pattern);
        }
    }

    private IEnumerator EventLoop()
    {
        while (isRunning)
        {
            yield return new WaitForSeconds(1f);
            elapsedTime += 1f;

            // Elite Event
            if (elapsedTime >= nextEliteEvent)
            {
                SpawnEliteEvent();
                nextEliteEvent = elapsedTime + config.eliteEventInterval;
            }

            // Horde Rush
            if (elapsedTime >= nextHordeRush)
            {
                StartCoroutine(HordeRushEvent());
                nextHordeRush = elapsedTime + config.hordeRushInterval;
            }

            // Boss
            if (elapsedTime >= nextBossEvent)
            {
                SpawnBossEvent();
                nextBossEvent = elapsedTime + config.bossInterval;
            }
        }
    }

    private IEnumerator KillRateTracker()
    {
        while (isRunning)
        {
            yield return new WaitForSeconds(1f);
            currentKillRate = killsInLastSecond;
            killsInLastSecond = 0;
        }
    }

    private IEnumerator HordeRushEvent()
    {
        Debug.Log("[Director] ⚡ HORDE RUSH START");
        isHordeRushActive = true;
        yield return new WaitForSeconds(config.hordeRushDuration);
        isHordeRushActive = false;
        Debug.Log("[Director] ⚡ HORDE RUSH END");
    }

    // ─────────────────────────────────────────────────────────────
    //  Паттерны спавна
    // ─────────────────────────────────────────────────────────────

    private enum SpawnPattern { Edge, Surround, Pincer, HordeEdge }

    private SpawnPattern ChoosePattern()
    {
        float roll = Random.value;

        if (roll < config.surroundChance)         return SpawnPattern.Surround;
        roll -= config.surroundChance;
        if (roll < config.pincerChance)           return SpawnPattern.Pincer;
        roll -= config.pincerChance;
        if (roll < config.hordeEdgeChance)        return SpawnPattern.HordeEdge;

        return SpawnPattern.Edge;
    }

    private void ExecutePattern(SpawnPattern pattern)
    {
        switch (pattern)
        {
            case SpawnPattern.Edge:      SpawnFromRandomEdge(1);  break;
            case SpawnPattern.Surround:  SpawnSurround();          break;
            case SpawnPattern.Pincer:    SpawnPincer();            break;
            case SpawnPattern.HordeEdge: SpawnFromRandomEdge(3);  break;
        }
    }

    /// <summary>Спавн из одного/нескольких случайных краёв экрана.</summary>
    private void SpawnFromRandomEdge(int edgeCount)
    {
        List<int> edges = RandomEdges(edgeCount);

        foreach (int edge in edges)
        {
            int spent = 0;
            int budget = Mathf.RoundToInt(currentBudget / edgeCount);

            while (spent < budget)
            {
                EnemyTierEntry entry = PickEnemy(EnemyTier.Common, EnemyTier.Uncommon);
                if (entry == null) break;
                if (spent + entry.cost > budget) break;

                Vector3 pos = GetEdgeSpawnPosition(edge) + RandomScatter();
                SpawnEnemy(entry, pos);
                spent += entry.cost;
            }

            currentBudget -= spent;
        }
    }

    /// <summary>Спавн кольцом вокруг игрока.</summary>
    private void SpawnSurround()
    {
        if (playerTransform == null) return;

        int count = Mathf.Max(4, Mathf.RoundToInt(currentBudget / 2f));
        int spawnCount = 0;

        for (int i = 0; i < count; i++)
        {
            EnemyTierEntry entry = PickEnemy(EnemyTier.Common, EnemyTier.Uncommon);
            if (entry == null) break;
            if (currentBudget < entry.cost) break;

            float angle = (360f / count) * i;
            float r = GetCameraRadius() + config.offscreenMargin;
            Vector3 offset = Quaternion.Euler(0, 0, angle) * Vector3.right * r;
            Vector3 pos = playerTransform.position + offset + RandomScatter();

            SpawnEnemy(entry, pos);
            currentBudget -= entry.cost;
            spawnCount++;
        }

        if (spawnCount > 0)
            Debug.Log($"[Director] Surround: {spawnCount} врагов");
    }

    /// <summary>Спавн с двух противоположных краёв.</summary>
    private void SpawnPincer()
    {
        int edge1 = Random.Range(0, 4);
        int edge2 = (edge1 + 2) % 4; // противоположный

        int halfBudget = Mathf.RoundToInt(currentBudget / 2f);

        foreach (int edge in new[] { edge1, edge2 })
        {
            int spent = 0;
            while (spent < halfBudget)
            {
                EnemyTierEntry entry = PickEnemy(EnemyTier.Common, EnemyTier.Uncommon);
                if (entry == null) break;
                if (spent + entry.cost > halfBudget) break;

                SpawnEnemy(entry, GetEdgeSpawnPosition(edge) + RandomScatter());
                spent += entry.cost;
            }
            currentBudget -= spent;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Special Events
    // ─────────────────────────────────────────────────────────────

    private void SpawnEliteEvent()
    {
        int count = config.eliteCountPerEvent;
        Debug.Log($"[Director] 🔴 ELITE EVENT x{count}");

        for (int i = 0; i < count; i++)
        {
            EnemyTierEntry entry = PickEnemy(EnemyTier.Elite, EnemyTier.Elite);
            if (entry == null) entry = PickEnemy(EnemyTier.Uncommon, EnemyTier.Uncommon);
            if (entry == null) continue;

            int edge = Random.Range(0, 4);
            SpawnEnemy(entry, GetEdgeSpawnPosition(edge));
        }
    }

    private void SpawnBossEvent()
    {
        Debug.Log("[Director] ☠️ BOSS EVENT");

        EnemyTierEntry boss = PickEnemy(EnemyTier.Boss, EnemyTier.Boss);
        if (boss == null)
        {
            Debug.LogWarning("[Director] Нет врагов тира Boss — пропускаем");
            return;
        }

        int edge = Random.Range(0, 4);
        SpawnEnemy(boss, GetEdgeSpawnPosition(edge));
    }

    // ─────────────────────────────────────────────────────────────
    //  Вспомогательные методы
    // ─────────────────────────────────────────────────────────────

    private void SpawnEnemy(EnemyTierEntry entry, Vector3 pos)
    {
        if (entry?.prefab == null) return;

        GameObject go = Instantiate(entry.prefab, pos, Quaternion.identity);
        aliveEnemies.Add(go);
    }

    /// <summary>Возвращает позицию за пределами экрана на указанном крае (0=Top,1=Right,2=Bottom,3=Left).</summary>
    private Vector3 GetEdgeSpawnPosition(int edge)
    {
        if (playerTransform == null) return Vector3.zero;

        Vector3 camPos = gameCamera != null ? gameCamera.transform.position : playerTransform.position;
        float halfH = gameCamera != null ? gameCamera.orthographicSize + config.offscreenMargin : 6f;
        float halfW = gameCamera != null ? halfH * gameCamera.aspect + config.offscreenMargin : 10f;

        return edge switch
        {
            0 => new Vector3(camPos.x + Random.Range(-halfW, halfW), camPos.y + halfH, 0f),  // Top
            1 => new Vector3(camPos.x + halfW, camPos.y + Random.Range(-halfH, halfH), 0f),  // Right
            2 => new Vector3(camPos.x + Random.Range(-halfW, halfW), camPos.y - halfH, 0f),  // Bottom
            3 => new Vector3(camPos.x - halfW, camPos.y + Random.Range(-halfH, halfH), 0f),  // Left
            _ => Vector3.zero
        };
    }

    private Vector3 RandomScatter()
    {
        Vector2 v = Random.insideUnitCircle * config.groupScatterRadius;
        return new Vector3(v.x, v.y, 0f);
    }

    private List<int> RandomEdges(int count)
    {
        var list = new List<int>(4) { 0, 1, 2, 3 };
        var result = new List<int>(count);

        for (int i = 0; i < Mathf.Min(count, 4); i++)
        {
            int idx = Random.Range(0, list.Count);
            result.Add(list[idx]);
            list.RemoveAt(idx);
        }

        return result;
    }

    /// <summary>Выбирает случайного врага из указанного диапазона тиров с учётом веса и времени разблокировки.</summary>
    private EnemyTierEntry PickEnemy(EnemyTier minTier, EnemyTier maxTier)
    {
        // Адаптивный выбор: при высоком kill-rate немного сдвигаем к более дорогим
        bool boosted = currentKillRate >= config.killsPerSecondThreshold;

        var candidates = new List<(EnemyTierEntry entry, float w)>();

        for (int t = (int)minTier; t <= (int)maxTier; t++)
        {
            foreach (var entry in tierLists[t])
            {
                if (entry.unlockTime > elapsedTime) continue;
                if (entry.cost > currentBudget + 1) continue;  // +1 небольшой запас

                float w = entry.weight;
                if (boosted && t > (int)minTier) w *= config.aggressiveBudgetMultiplier;
                candidates.Add((entry, w));
            }
        }

        if (candidates.Count == 0) return null;

        float total = 0f;
        foreach (var c in candidates) total += c.w;

        float roll = Random.Range(0f, total);
        float acc = 0f;

        foreach (var (entry, w) in candidates)
        {
            acc += w;
            if (roll <= acc) return entry;
        }

        return candidates[^1].entry;
    }

    private float BudgetPerSecond()
    {
        float minutes = elapsedTime / 60f;
        float rate = config.baseBudgetPerSecond + config.budgetGrowthPerMinute * minutes;

        if (isHordeRushActive)
            rate *= config.hordeRushBudgetMultiplier;
        else if (currentKillRate >= config.killsPerSecondThreshold)
            rate *= config.aggressiveBudgetMultiplier;

        return rate;
    }

    private int CountAlive()
    {
        aliveEnemies.RemoveAll(go => go == null);
        return aliveEnemies.Count;
    }

    private float GetCameraRadius()
    {
        if (gameCamera == null) return 6f;
        float h = gameCamera.orthographicSize;
        float w = h * gameCamera.aspect;
        return Mathf.Sqrt(w * w + h * h);
    }

    private void BuildTierLists()
    {
        for (int i = 0; i < 4; i++)
            tierLists[i] = new List<EnemyTierEntry>();

        if (config?.enemies == null) return;

        foreach (var entry in config.enemies)
        {
            if (entry?.prefab == null) continue;
            tierLists[(int)entry.tier].Add(entry);
        }

        Debug.Log($"[Director] База врагов: Common={tierLists[0].Count}, " +
                  $"Uncommon={tierLists[1].Count}, Elite={tierLists[2].Count}, Boss={tierLists[3].Count}");
    }

    // ─────────────────────────────────────────────────────────────
    //  Gizmos
    // ─────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || gameCamera == null) return;

        float h = gameCamera.orthographicSize + (config != null ? config.offscreenMargin : 1.5f);
        float w = h * gameCamera.aspect + (config != null ? config.offscreenMargin : 1.5f);

        Vector3 c = gameCamera.transform.position;
        c.z = 0f;

        Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
        Gizmos.DrawWireCube(c, new Vector3(w * 2f, h * 2f, 0f));
    }
}
