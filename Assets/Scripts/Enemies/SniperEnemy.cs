using UnityEngine;

public class SniperEnemy : EnemyBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float fireRate = 4.5f;           // очень редко
    [SerializeField] private float aimTime = 1.2f;            // время прицеливания
    [SerializeField] private LineRenderer aimLine;            // линия наводки

    private float lastShotTime = -10f;
    private bool isAiming = false;
    private float aimTimer = 0f;

    public void Awake()
    {
        base.Awake();
        if (aimLine == null) aimLine = GetComponent<LineRenderer>();
        if (aimLine) aimLine.enabled = false;
    }

    public override void UpdateBehaviour()
    {
        if (isAiming)
        {
            aimTimer += Time.deltaTime;
            Vector2 dir = (player.position - transform.position).normalized;
            if (aimLine)
            {
                aimLine.enabled = true;
                aimLine.SetPosition(0, transform.position);
                aimLine.SetPosition(1, (Vector2)transform.position + dir * 20f);
            }

            if (aimTimer >= aimTime)
            {
                Shoot();
                isAiming = false;
                if (aimLine) aimLine.enabled = false;
            }
            return;
        }

        if (Time.time - lastShotTime > fireRate)
        {
            isAiming = true;
            aimTimer = 0f;
        }
    }

    void Shoot()
    {
        lastShotTime = Time.time;
        Vector2 dir = (player.position - transform.position).normalized;
        var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity).GetComponent<Projectile>();
        proj.Init(dir, 18); // высокий урон
    }
}