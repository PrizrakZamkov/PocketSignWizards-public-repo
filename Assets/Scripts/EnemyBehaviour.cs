using UnityEngine;

public abstract class EnemyBehaviour : MonoBehaviour
{
    protected Enemy enemy;
    protected EnemyMovement movement;
    protected EnemyStatusEffects status;
    protected Transform player;

    protected virtual void Awake()
    {
        enemy = GetComponent<Enemy>();
        movement = GetComponent<EnemyMovement>();
        status = GetComponent<EnemyStatusEffects>();
    }
    protected virtual void Start()
    {
        if (Player.Instance != null)
            player = Player.Instance.transform;
        else
            Debug.LogWarning("Player.Instance not found on " + gameObject.name);
    }

    public virtual void OnSpawn() { }
    public virtual void UpdateBehaviour() { }   // вызывать из Update врага
    public virtual void OnDeath() { }
}