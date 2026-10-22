using System.Collections;
using UnityEngine;

public class TeleporterEnemy : EnemyBehaviour
{
    [SerializeField] private float teleportCooldown = 4f;
    [SerializeField] private float teleportDistance = 5f;
    [SerializeField] private float teleportWarning = 0.4f;

    private float lastTeleport = -10f;

    public override void UpdateBehaviour()
    {
        if (Time.time - lastTeleport < teleportCooldown) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > 3f && dist < 9f)
        {
            StartCoroutine(TeleportRoutine());
        }
    }

    IEnumerator TeleportRoutine()
    {
        lastTeleport = Time.time;
        // Warning effect
        yield return new WaitForSeconds(teleportWarning);

        Vector2 randomDir = Random.insideUnitCircle.normalized;
        transform.position = (Vector2)player.position + randomDir * teleportDistance;
    }
}