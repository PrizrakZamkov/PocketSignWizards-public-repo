using UnityEngine;

public class TankEnemy : EnemyBehaviour
{
    [SerializeField] private float speedMult = 0.55f;
    [SerializeField] private float hpMult = 4.5f;

    public override void OnSpawn()
    {
        if (movement) movement.speed *= speedMult;
        enemy.hp = Mathf.RoundToInt(enemy.hp * hpMult);
    }
}