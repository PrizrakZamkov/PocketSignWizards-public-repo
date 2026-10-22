using System.Collections;
using UnityEngine;

public class SwordsmanEnemy : EnemyBehaviour
{
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private float windupTime = 0.5f;

    private float lastAttackTime = -10f;

    public override void UpdateBehaviour()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine()
    {
        lastAttackTime = Time.time;
        movement.enabled = false;

        // Замах (анимация/эффект)
        yield return new WaitForSeconds(windupTime);

        if (Vector2.Distance(transform.position, player.position) <= attackRange + 0.5f)
        {
            Player.Instance.TakeDamage(attackDamage);
            // Можно добавить ударный particle
        }

        movement.enabled = true;
    }
}