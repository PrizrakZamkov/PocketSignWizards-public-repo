using UnityEngine;

public class IllusionistEnemy : EnemyBehaviour
{
    [Header("Иллюзионист")]
    [SerializeField] private GameObject illusionPrefab;     // ??? Важно! Отдельный префаб
    [SerializeField] private int illusionCount = 4;
    [SerializeField] private float radius = 1.4f;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float alpha = 0.35f;

    private GameObject[] illusions;

    public override void OnSpawn()
    {
        if (illusionPrefab == null)
        {
            Debug.LogError("IllusionistEnemy: illusionPrefab не назначен!");
            return;
        }

        illusions = new GameObject[illusionCount];

        for (int i = 0; i < illusionCount; i++)
        {
            GameObject ill = Instantiate(illusionPrefab, transform.position, Quaternion.identity);

            // Настраиваем внешний вид
            var sr = ill.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
                sr.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder - 1;
            }

            illusions[i] = ill;
        }
    }

    public override void UpdateBehaviour()
    {
        if (illusions == null) return;

        float baseAngle = Time.time * rotationSpeed;

        for (int i = 0; i < illusions.Length; i++)
        {
            if (illusions[i] == null) continue;

            float angle = baseAngle + (i * 360f / illusions.Length);
            float rad = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
            illusions[i].transform.position = transform.position + offset;
        }
    }

    public override void OnDeath()
    {
        if (illusions == null) return;

        foreach (var illusion in illusions)
        {
            if (illusion != null)
                Destroy(illusion);
        }
    }

    private void OnDestroy()
    {
        // Дополнительная защита
        if (illusions != null)
        {
            foreach (var illusion in illusions)
            {
                if (illusion != null)
                    Destroy(illusion);
            }
        }
    }
}