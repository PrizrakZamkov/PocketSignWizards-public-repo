using UnityEngine;

public class FastEnemy : EnemyBehaviour
{
    [SerializeField] private float speedMult = 1.85f;
    [SerializeField] private float hpMult = 0.7f;

    public override void OnSpawn()
    {
        if (movement) movement.speed *= speedMult;
        enemy.hp = Mathf.RoundToInt(enemy.hp * hpMult);
    }
}