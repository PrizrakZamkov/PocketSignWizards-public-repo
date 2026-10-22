using UnityEngine;

public class ZigzagEnemy : EnemyBehaviour
{
    [SerializeField] private float frequency = 7f;
    [SerializeField] private float amplitude = 1.8f;
    private float offset;

    public override void OnSpawn() => offset = Random.value * 10f;

    public override void UpdateBehaviour()
    {
        if (movement == null || status.IsStunned || status.IsFrozen) return;

        Vector2 dirToPlayer = (player.position - transform.position).normalized;
        float zigzag = Mathf.Sin((Time.time + offset) * frequency) * amplitude;

        Vector2 perpendicular = new Vector2(-dirToPlayer.y, dirToPlayer.x);
        Vector2 finalDir = (dirToPlayer + perpendicular * zigzag).normalized;

        // Временно меняем направление движения
        transform.position += (Vector3)(finalDir * movement.speed * status.SpeedMultiplier * Time.deltaTime);
    }
}