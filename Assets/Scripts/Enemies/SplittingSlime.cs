using UnityEngine;

public class SplittingSlime : EnemyBehaviour
{
    [SerializeField] private GameObject smallerPrefab; // средний или маленький
    [SerializeField] private int splits = 2;

    public override void OnDeath()
    {
        if (smallerPrefab == null) return;

        for (int i = 0; i < splits; i++)
        {
            Vector3 offset = Random.insideUnitCircle * 0.6f;
            Instantiate(smallerPrefab, transform.position + offset, Quaternion.identity);
        }
    }
}