using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public static PlayerAttack Instance;
    public float segmentRadius = 0.5f;
    public int damage = 20;
    public LayerMask enemyLayer;
    public float minPercentageForAttack = 0.85f;
    private CameraShake cameraShake;
    private GameController gameController;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        cameraShake = CameraShake.Instance;
        gameController = GameController.Instance;
    }

    public void Attack(string attackName, float percentage, Vector2[] line)
    {

        if (percentage < minPercentageForAttack)
            return;
        Debug.Log(attackName + " " + gameController.IsAttackAvailable(attackName));
        if (!gameController.IsAttackAvailable(attackName))
            return;

        HashSet<Collider2D> damaged = new HashSet<Collider2D>();

        foreach (Vector2 screenPoint in line)
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(
                new Vector3(screenPoint.x, screenPoint.y, 10f)
            );

            Collider2D[] hits = Physics2D.OverlapCircleAll(
                worldPoint,
                segmentRadius,
                enemyLayer
            );

            foreach (Collider2D hit in hits)
            {
                if (damaged.Contains(hit))
                    continue;

                damaged.Add(hit);

                hit.GetComponent<Enemy>()?.Damage(damage);
            }
        }
        if (damaged.Count != 0)
        {
            cameraShake.Shake();
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}