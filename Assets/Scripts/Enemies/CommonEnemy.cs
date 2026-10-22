using UnityEngine;

public class CommonEnemy : EnemyBehaviour
{
    [SerializeField] private float speedMult = 1f;
    [SerializeField] private float hpMult = 1f;

    public override void OnSpawn()
    {
        if (movement) movement.speed *= speedMult;
        enemy.hp = Mathf.RoundToInt(enemy.hp * hpMult);
    }
}