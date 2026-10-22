using UnityEngine;

public class Projectile : MonoBehaviour
{
    public int damage = 10;
    public float speed = 8f;
    private Vector2 direction;

    public void Init(Vector2 dir, int dmg)
    {
        direction = dir.normalized;
        damage = dmg;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Destroy(gameObject, 5f);
    }

    void Update() => transform.position += (Vector3)direction * speed * Time.deltaTime;

    void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;
        if (col.TryGetComponent<Player>(out var p))
        {
            p.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}