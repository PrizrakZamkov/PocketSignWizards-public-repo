using UnityEngine;

public class MortarEnemy : EnemyBehaviour
{
    [SerializeField] private GameObject mortarProjectilePrefab; // специальный префаб с Explosion
    [SerializeField] private float fireRate = 3.5f;

    private float lastShot = -10f;

    public override void UpdateBehaviour()
    {
        if (Time.time - lastShot < fireRate) return;

        lastShot = Time.time;
        Vector2 targetPos = player.position;

        var projObj = Instantiate(mortarProjectilePrefab, transform.position, Quaternion.identity);
        var mortar = projObj.GetComponent<MortarProjectile>(); // создай этот скрипт ниже
        if (mortar) mortar.Launch(targetPos, 12);
    }
}