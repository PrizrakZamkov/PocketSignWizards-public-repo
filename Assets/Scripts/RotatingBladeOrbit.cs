using UnityEngine;

/// <summary>
/// Создаётся через SpellSystem. Вращается вокруг игрока и наносит урон при касании.
/// </summary>
public class RotatingBladeOrbit : MonoBehaviour
{
    private Transform  target;
    private float      orbitRadius;
    private float      speed;
    private float      angle;
    private int        damage;
    private LayerMask  enemyLayer;
    private Color      color;

    private LineRenderer lr;
    private float lifetime;
    private float elapsed;

    private float damageCooldown = 0.4f;
    private float lastDamageTime;

    public void Init(Transform target, float radius, float speed, float startAngle,
                     int damage, LayerMask enemyLayer, float lifetime, Color color)
    {
        this.target      = target;
        this.orbitRadius = radius;
        this.speed       = speed;
        this.angle       = startAngle;
        this.damage      = damage;
        this.enemyLayer  = enemyLayer;
        this.lifetime    = lifetime;
        this.color       = color;

        lr = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (target == null) { Destroy(gameObject); return; }

        elapsed += Time.deltaTime;
        if (elapsed >= lifetime) { Destroy(gameObject); return; }

        angle += speed * Time.deltaTime;

        float rad   = angle * Mathf.Deg2Rad;
        Vector3 pos = target.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * orbitRadius;
        transform.position = pos;

        // Визуал — короткая черта
        if (lr != null)
        {
            float a    = 1f - elapsed / lifetime;
            Color c    = color; c.a = a;
            lr.startColor = lr.endColor = c;

            Vector3 tang = new Vector3(-Mathf.Sin(rad), Mathf.Cos(rad), 0f) * 0.28f;
            lr.SetPosition(0, pos - tang);
            lr.SetPosition(1, pos + tang);
        }

        // Урон
        if (Time.time - lastDamageTime > damageCooldown)
        {
            foreach (var col in Physics2D.OverlapCircleAll(pos, 0.35f, enemyLayer))
            {
                col.GetComponent<Enemy>()?.Damage(damage);
                lastDamageTime = Time.time;
                break;
            }
        }
    }
}
