using UnityEngine;

public class TurretEnemy : EnemyBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 0.8f;
    [SerializeField] private float activationRange = 12f;

    private float lastShot = -10f;

    public void Awake()
    {
        base.Awake();
        if (movement) movement.enabled = false;
    }

    public override void UpdateBehaviour()
    {
        if (Vector2.Distance(transform.position, player.position) > activationRange) return;
        if (Time.time - lastShot < fireRate) return;

        lastShot = Time.time;
        Vector2 dir = (player.position - transform.position).normalized;
        var p = Instantiate(projectilePrefab, transform.position, Quaternion.identity).GetComponent<Projectile>();
        p.Init(dir, 10);
    }
}