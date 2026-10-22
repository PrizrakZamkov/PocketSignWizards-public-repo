using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDatabase", menuName = "Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    public EnemyPrefabEntry[] enemies;
}