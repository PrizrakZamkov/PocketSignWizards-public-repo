using UnityEngine;

public class KamikazeEnemy : EnemyBehaviour
{
    [Header("Explosion")]
    [SerializeField] private float triggerDistance = 3.5f;
    [SerializeField] private float chargeTime = 1.1f;
    [SerializeField] private int explosionDamage = 30;
    [SerializeField] private float explosionRadius = 2.8f;

    [Header("VFX")]
    [SerializeField] private GameObject explosionParticlesPrefab;

    private bool charging = false;
    private float chargeTimer = 0f;

    public override void UpdateBehaviour()
    {
        if (charging)
        {
            chargeTimer += Time.deltaTime;

            GetComponent<SpriteRenderer>().color =
                Color.Lerp(Color.white, Color.red, chargeTimer / chargeTime);

            if (chargeTimer >= chargeTime)
                Explode();

            return;
        }

        if (Vector2.Distance(transform.position, player.position) <= triggerDistance)
        {
            charging = true;

            if (movement)
                movement.enabled = false;
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
                p.TakeDamage(explosionDamage);
            }
        }

        enemy.Damage(9999);
    }
}