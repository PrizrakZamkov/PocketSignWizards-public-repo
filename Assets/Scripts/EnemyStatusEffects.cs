using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StatusEffectType
{
    Slowed,
    Stunned,
    Burning,
    Poisoned,
    Frozen
}

/// <summary>
/// Вешается на того же GameObject, что Enemy и EnemyMovement.
/// Применять эффект: GetComponent<EnemyStatusEffects>().Apply(StatusEffectType.Burning, 4f);
/// </summary>
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyStatusEffects : MonoBehaviour
{
    // ── Slow ──────────────────────────────────────────────────────────────────
    [Header("Замедление")]
    [SerializeField] private float slowMultiplier   = 0.35f;
    [SerializeField] private Color slowColor        = new Color(0.65f, 0.15f, 0.90f, 1f);

    // ── Stun ──────────────────────────────────────────────────────────────────
    [Header("Оглушение")]
    [SerializeField] private float stunSpinSpeed    = 420f;   // градусов/сек
    [SerializeField] private Color stunColor        = new Color(1.00f, 0.85f, 0.10f, 1f);

    // ── Burn ──────────────────────────────────────────────────────────────────
    [Header("Горение")]
    [SerializeField] private float burnTickInterval = 0.5f;
    [SerializeField] private int   burnDamagePerTick = 3;
    [SerializeField] private Color burnColor        = new Color(1.00f, 0.35f, 0.05f, 1f);

    // ── Poison ────────────────────────────────────────────────────────────────
    [Header("Яд")]
    [SerializeField] private float poisonTickInterval = 1.0f;
    [SerializeField] private int   poisonDamagePerTick = 1;
    [SerializeField] private Color poisonColor       = new Color(0.25f, 0.90f, 0.30f, 1f);

    // ── Freeze ────────────────────────────────────────────────────────────────
    [Header("Заморозка")]
    [SerializeField] private Color frozenColor      = new Color(0.45f, 0.82f, 1.00f, 1f);
    [Tooltip("Свойство шейдера заморозки (опционально)")]
    [SerializeField] private string frozenShaderProperty = "_FrozenAmount";

    // ── Public state (читает EnemyMovement) ───────────────────────────────────
    public float SpeedMultiplier { get; private set; } = 1f;
    public bool  IsStunned       { get; private set; }
    public bool  IsFrozen        { get; private set; }

    // ── Internal ──────────────────────────────────────────────────────────────
    private SpriteRenderer  sr;
    private Enemy           enemy;
    private Material        mat;

    // Словари: эффект → корутина таймера / корутина DoT
    private readonly Dictionary<StatusEffectType, Coroutine> timers = new();
    private readonly Dictionary<StatusEffectType, Coroutine> dots   = new();
    private Coroutine spinRoutine;

    private Color baseColor;

    // ── Unity ─────────────────────────────────────────────────────────────────
    void Awake()
    {
        sr    = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
        enemy = GetComponent<Enemy>();
    }

    void Start()
    {
        var flash = GetComponent<ImpactFlash>();
        mat = flash != null ? flash.Material : sr.material;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Применить эффект на duration секунд. Повторный вызов перезапускает таймер.</summary>
    public void Apply(StatusEffectType type, float duration)
    {
        if (enemy.isDead) return;

        // Перезапуск таймера если уже активен
        if (timers.TryGetValue(type, out Coroutine old) && old != null)
            StopCoroutine(old);

        OnStart(type);
        timers[type] = StartCoroutine(TimerRoutine(type, duration));
        RefreshTint();
    }

    /// <summary>Снять эффект досрочно.</summary>
    public void Remove(StatusEffectType type)
    {
        if (timers.TryGetValue(type, out Coroutine c) && c != null)
            StopCoroutine(c);
        timers.Remove(type);
        OnEnd(type);
        RefreshTint();
    }

    public bool IsActive(StatusEffectType type) => timers.ContainsKey(type);

    // ── Effect lifecycle ──────────────────────────────────────────────────────

    void OnStart(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Slowed:
                SpeedMultiplier = slowMultiplier;
                break;

            case StatusEffectType.Stunned:
                IsStunned = true;
                if (spinRoutine != null) StopCoroutine(spinRoutine);
                spinRoutine = StartCoroutine(SpinRoutine());
                break;

            case StatusEffectType.Burning:
                RestartDot(type, burnTickInterval, burnDamagePerTick);
                break;

            case StatusEffectType.Poisoned:
                RestartDot(type, poisonTickInterval, poisonDamagePerTick);
                break;

            case StatusEffectType.Frozen:
                IsFrozen      = true;
                SpeedMultiplier = 0f;
                TryShader(frozenShaderProperty, 1f);
                break;
        }
    }

    void OnEnd(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.Slowed:
                if (!IsFrozen) SpeedMultiplier = 1f;
                break;

            case StatusEffectType.Stunned:
                IsStunned = false;
                transform.rotation = Quaternion.identity;
                break;

            case StatusEffectType.Burning:
            case StatusEffectType.Poisoned:
                StopDot(type);
                break;

            case StatusEffectType.Frozen:
                IsFrozen = false;
                SpeedMultiplier = IsActive(StatusEffectType.Slowed) ? slowMultiplier : 1f;
                TryShader(frozenShaderProperty, 0f);
                break;
        }
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    IEnumerator TimerRoutine(StatusEffectType type, float duration)
    {
        yield return new WaitForSeconds(duration);
        timers.Remove(type);
        OnEnd(type);
        RefreshTint();
    }

    /// <summary>Спин 360° пока stunned и жив.</summary>
    IEnumerator SpinRoutine()
    {
        while (IsStunned && !enemy.isDead)
        {
            transform.Rotate(0f, 0f, stunSpinSpeed * Time.deltaTime);
            yield return null;
        }
        transform.rotation = Quaternion.identity;
        spinRoutine = null;
    }

    IEnumerator DotRoutine(StatusEffectType type, float interval, int damage)
    {
        while (IsActive(type) && !enemy.isDead)
        {
            yield return new WaitForSeconds(interval);
            if (IsActive(type) && !enemy.isDead)
                enemy.Damage(damage);
        }
        dots.Remove(type);
    }

    void RestartDot(StatusEffectType type, float interval, int damage)
    {
        StopDot(type);
        dots[type] = StartCoroutine(DotRoutine(type, interval, damage));
    }

    void StopDot(StatusEffectType type)
    {
        if (dots.TryGetValue(type, out Coroutine c) && c != null)
            StopCoroutine(c);
        dots.Remove(type);
    }

    // ── Tint priority: Frozen > Burning > Stunned > Poisoned > Slowed ─────────

    void RefreshTint()
    {
        Color c = baseColor;

        if      (IsActive(StatusEffectType.Frozen))   c = frozenColor;
        else if (IsActive(StatusEffectType.Burning))  c = burnColor;
        else if (IsActive(StatusEffectType.Stunned))  c = stunColor;
        else if (IsActive(StatusEffectType.Poisoned)) c = poisonColor;
        else if (IsActive(StatusEffectType.Slowed))   c = slowColor;

        sr.color = c;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void TryShader(string prop, float value)
    {
        if (mat != null && !string.IsNullOrEmpty(prop))
        {
            try { mat.SetFloat(prop, value); }
            catch { /* шейдер не имеет этого свойства — не критично */ }
        }
    }
}
