using System.Collections;
using UnityEngine;

public class MortarProjectile : MonoBehaviour
{
    public int damage = 20;
    public float explosionRadius = 2.5f;
    private Vector2 target;

    public void Launch(Vector2 targetPos, float height = 10f)
    {
        target = targetPos;
        // Простая парабола (можно улучшить)
        StartCoroutine(FlyRoutine());
    }

    IEnumerator FlyRoutine()
    {
        Vector2 start = transform.position;
        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime * 1.2f;
            float height = Mathf.Sin(time * Mathf.PI) * 3f;
            transform.position = Vector2.Lerp(start, target, time) + Vector2.up * height;
            yield return null;
        }

        // Взрыв
        Collider2D[] hits = Physics2D.OverlapCircleAll(target, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<Player>(out var p))
                p.TakeDamage(damage);
        }

        // Добавить explosion particle
        Destroy(gameObject);
    }
}