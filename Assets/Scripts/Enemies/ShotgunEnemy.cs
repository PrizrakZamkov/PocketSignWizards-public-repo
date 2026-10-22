using UnityEngine;

public class ShotgunEnemy : EnemyBehaviour
{
    [Header("Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 5f;
    [SerializeField] private int pellets = 7;
    [SerializeField] private float spreadAngle = 30f;

    [Header("Detection")]
    [SerializeField] private float attackRadius = 6f;

    private float lastShot = -10f;

    public override void UpdateBehaviour()
    {
        if (Time.time - lastShot < fireRate) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > attackRadius) return;

        lastShot = Time.time;

        Vector2 mainDir = (player.position - transform.position).normalized;

        for (int i = 0; i < pellets; i++)
        {
            float offset = (i - (pellets - 1) / 2f) * (spreadAngle / (pellets - 1));
            Quaternion rot = Quaternion.Euler(0, 0, offset);
            Vector2 dir = rot * mainDir;

            var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity)
                .GetComponent<Projectile>();

            proj.Init(dir, 7);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}