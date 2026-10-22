using UnityEngine;

/// <summary>
/// Орбитальный спутник — медленно вращается вокруг игрока, наносит урон при контакте.
/// </summary>
public class OrbitalSatellite : MonoBehaviour
{
    private Transform target;
    private float     orbitRadius;
    private float     speed;
    private float     angle;
    private int       damage;
    private LayerMask enemyLayer;
    private float     lifetime;
    private float     elapsed;

    private LineRenderer lr;
    private float damageCooldown = 0.6f;
    private float lastDamageTime;

    public void Init(Transform target, float radius, float speed, float startAngle,
                     int damage, LayerMask enemyLayer, float lifetime)
    {
        this.target      = target;
        this.orbitRadius = radius;
        this.speed       = speed;
        this.angle       = startAngle;
        this.damage      = damage;
        this.enemyLayer  = enemyLayer;
        this.lifetime    = lifetime;

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

        // Обновляем позиции кружка LineRenderer
        if (lr != null)
        {
            for (int i = 0; i < lr.positionCount; i++)
            {
                float a2  = (float)i / lr.positionCount * 2f * Mathf.PI;
                float r   = 0.22f;
                lr.SetPosition(i, pos + new Vector3(Mathf.Cos(a2) * r, Mathf.Sin(a2) * r, 0f));
            }

            float alpha  = 1f - elapsed / lifetime;
            Color c      = lr.startColor; c.a = alpha;
            lr.startColor = lr.endColor = c;
        }

        // Урон
        if (Time.time - lastDamageTime > damageCooldown)
        {
            foreach (var col in Physics2D.OverlapCircleAll(pos, 0.28f, enemyLayer))
            {
                col.GetComponent<Enemy>()?.Damage(damage);
                lastDamageTime = Time.time;
                break;
            }
        }
    }
}
