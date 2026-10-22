using UnityEngine;

public class MineEnemy : EnemyBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 2.2f;
    [SerializeField] private int damage = 25;

    [Header("VFX")]
    [SerializeField] private GameObject explosionParticlesPrefab;

    public void Awake()
    {
        base.Awake();

        if (movement)
            movement.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Explode();
        }
    }

    void Explode()
    {
        // Спавн частиц
        if (explosionParticlesPrefab != null)
        {
            Instantiate(
                explosionParticlesPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(transform.position, explosionRadius);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Player>(out var p))
            {
                p.TakeDamage(damage);
            }
        }

        enemy.Damage(9999);
    }
}