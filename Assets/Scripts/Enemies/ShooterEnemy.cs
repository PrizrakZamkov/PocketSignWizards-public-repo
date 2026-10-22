using UnityEngine;
using System.Collections;

public class ShooterEnemy : EnemyBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 1.4f;
    [SerializeField] private int burst = 1;
    [SerializeField] private float burstDelay = 0.1f;

    [Header("Detection")]
    [SerializeField] private float attackRadius = 6f;

    private float lastShot = -10f;
    private bool isShooting;

    public override void UpdateBehaviour()
    {
        if (isShooting) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > attackRadius) return;

        if (Time.time - lastShot < fireRate) return;
        lastShot = Time.time;

        StartCoroutine(ShootBurst());
    }

    private IEnumerator ShootBurst()
    {
        isShooting = true;

        for (int i = 0; i < burst; i++)
        {
            Vector2 dir = (player.position - transform.position).normalized;

            var p = Instantiate(projectilePrefab, transform.position, Quaternion.identity)
                .GetComponent<Projectile>();

            p.Init(dir, 8);

            if (i < burst - 1)
                yield return new WaitForSeconds(burstDelay);
        }

        isShooting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}