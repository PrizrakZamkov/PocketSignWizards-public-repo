using UnityEngine;

public class SpammerEnemy : EnemyBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireInterval = 0.35f;
    [SerializeField] private int projectilesPerBurst = 6;
    [SerializeField] private float spiralSpeed = 180f;

    private float lastFireTime = -10f;
    private float angleOffset = 0f;

    public override void UpdateBehaviour()
    {
        if (Time.time - lastFireTime < fireInterval) return;

        lastFireTime = Time.time;
        angleOffset += Time.deltaTime * spiralSpeed;

        for (int i = 0; i < projectilesPerBurst; i++)
        {
            float angle = (i * 360f / projectilesPerBurst) + angleOffset;
            Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity).GetComponent<Projectile>();
            proj.Init(dir, 5); // слабый урон
        }
    }
}