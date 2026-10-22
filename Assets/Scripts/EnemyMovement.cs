using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private Transform playerTransform;

    [Header("Основные параметры")]
    [SerializeField] public float speed = 3.0f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Separation")]
    [SerializeField] private float separationRadius = 0.8f;
    [SerializeField] private float separationForce = 2.5f;
    [SerializeField] private LayerMask enemyLayer;          // ← Добавил обратно!

    [Header("Предсказание движения игрока")]
    [SerializeField] private float predictionMultiplier = 0.65f;
    [SerializeField] private float maxPredictionTime = 1.1f;

    private float lastAttackTime = 0f;
    private Enemy enemy;
    private SpriteRenderer spriteRenderer;
    private EnemyStatusEffects statusEffects;

    // Knockback
    private Vector2 knockbackVelocity;
    private bool isKnockedBack;

    void Start()
    {
        playerTransform = Player.Instance ? Player.Instance.transform : null;
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemy = GetComponent<Enemy>();
        statusEffects = GetComponent<EnemyStatusEffects>();
    }

    private void FixedUpdate()
    {
        if (playerTransform == null) return;
        if (enemy != null && enemy.isDead) return;

        // Knockback
        if (isKnockedBack)
        {
            transform.position += (Vector3)(knockbackVelocity * Time.fixedDeltaTime);
            knockbackVelocity *= 0.82f;
            if (knockbackVelocity.magnitude < 0.05f)
                isKnockedBack = false;
            return;
        }

        // Статус-эффекты
        if (statusEffects != null && (statusEffects.IsStunned || statusEffects.IsFrozen))
            return;

        float currentSpeed = speed * (statusEffects != null ? statusEffects.SpeedMultiplier : 1f);

        Vector2 enemyPos = transform.position;
        Vector2 playerPos = playerTransform.position;

        // === БЕЗОПАСНОЕ ПРЕДСКАЗАНИЕ ===
        Vector2 playerVelocity = GetPlayerVelocity();

        float distance = Vector2.Distance(enemyPos, playerPos);
        float predictionTime = Mathf.Clamp(distance / 8f, 0.1f, maxPredictionTime);

        Vector2 predictedPlayerPos = playerPos + playerVelocity * predictionTime * predictionMultiplier;

        Vector2 dirToPredicted = (predictedPlayerPos - enemyPos).normalized;
        Vector2 separation = GetSeparation();

        Vector2 finalDir = (dirToPredicted + separation).normalized;

        // Поворот спрайта
        if (spriteRenderer != null)
            spriteRenderer.flipX = dirToPredicted.x < 0;

        // Движение
        transform.position += (Vector3)(finalDir * currentSpeed * Time.fixedDeltaTime);
    }

    // Безопасный метод получения скорости игрока
    private Vector2 GetPlayerVelocity()
    {
        if (Player.Instance == null) return Vector2.zero;

        // Попробуй разные способы получения скорости
        var rb = Player.Instance.GetComponent<Rigidbody2D>();
        if (rb != null)
            return rb.linearVelocity;

        // Если у тебя скорость хранится по-другому, замени здесь
        // return Player.Instance.currentVelocity; 
        return Vector2.zero;
    }

    Vector2 GetSeparation()
    {
        Collider2D[] neighbors = Physics2D.OverlapCircleAll(transform.position, separationRadius, enemyLayer);
        Vector2 force = Vector2.zero;

        foreach (var n in neighbors)
        {
            if (n.gameObject == gameObject) continue;

            Vector2 diff = (Vector2)(transform.position - n.transform.position);
            float dist = diff.magnitude;

            if (dist > 0.01f)
            {
                force += diff.normalized / (dist * dist);   // сильное отталкивание вблизи
            }
        }

        return force * separationForce;
    }

    public void TriggerKnockback(Vector3 direction, float force, float duration)
    {
        knockbackVelocity = direction.normalized * force;
        isKnockedBack = true;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (enemy != null && enemy.isDead) return;
        if (Time.time < lastAttackTime + attackCooldown) return;
        if (statusEffects != null && (statusEffects.IsStunned || statusEffects.IsFrozen)) return;

        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(damage);
            lastAttackTime = Time.time;
        }
    }
}