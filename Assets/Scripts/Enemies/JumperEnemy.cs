using UnityEngine;
using System.Collections;

public class JumperEnemy : EnemyBehaviour
{
    [Header("Прыгун")]
    [SerializeField] private float jumpDistance = 5.5f;
    [SerializeField] private float jumpDuration = 0.35f;
    [SerializeField] private float jumpCooldown = 2.2f;
    [SerializeField] private float warningTime = 0.45f;
    [SerializeField] private float jumpHeight = 0.8f;

    [Header("Эффекты")]
    [SerializeField] private Color warningColor = new Color(1f, 0.4f, 0.2f, 1f);
    [SerializeField] private float squashAmount = 0.6f;

    private float lastJumpTime = -10f;
    private bool isJumping;

    private SpriteRenderer sr;
    private Vector3 originalScale;

    public void Awake()
    {
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    public override void UpdateBehaviour()
    {
        if (isJumping) return;

        if (movement == null || status.IsStunned || status.IsFrozen) return;
        if (Time.time < lastJumpTime + jumpCooldown) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > 1.5f && distanceToPlayer < jumpDistance + 2f)
        {
            StartCoroutine(JumpRoutine());
        }
    }

    private IEnumerator JumpRoutine()
    {
        isJumping = true;
        lastJumpTime = Time.time;

        movement.enabled = false;

        Vector2 startPos = transform.position;
        Vector2 targetPos = (Vector2)player.position;

        // фиксируем цель один раз (без дерганий)
        Vector2 direction = (targetPos - startPos).normalized;
        float distance = Mathf.Min(jumpDistance, Vector2.Distance(startPos, targetPos) * 0.95f);
        Vector2 finalTarget = startPos + direction * distance;

        // === 1. Предупреждение ===
        Color originalColor = sr.color;
        Vector3 squashedScale = new Vector3(originalScale.x * 1.2f, originalScale.y * squashAmount, 1f);

        transform.localScale = squashedScale;
        sr.color = warningColor;

        yield return new WaitForSeconds(warningTime);

        // === 2. Прыжок ===
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);

            float progress = EaseOutQuad(t);
            float height = Mathf.Sin(t * Mathf.PI) * jumpHeight;

            Vector2 newPos = Vector2.Lerp(startPos, finalTarget, progress);
            transform.position = new Vector3(newPos.x, newPos.y, 0f) + Vector3.up * height;

            // squash/stretch
            float stretch = Mathf.Lerp(0.85f, 1.25f, Mathf.Sin(t * Mathf.PI));
            transform.localScale = new Vector3(originalScale.x * stretch, originalScale.y / stretch, 1f);

            yield return null;
        }

        // === 3. Приземление ===
        transform.position = new Vector3(finalTarget.x, finalTarget.y, 0f);
        transform.localScale = originalScale;
        sr.color = originalColor;

        yield return new WaitForSeconds(0.15f);

        movement.enabled = true;
        isJumping = false;
    }

    private float EaseOutQuad(float t)
    {
        return 1 - (1 - t) * (1 - t);
    }

    public override void OnDeath()
    {
        StopAllCoroutines();
        isJumping = false;
    }
}