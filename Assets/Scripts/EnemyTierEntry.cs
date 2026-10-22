using UnityEngine;

public enum EnemyTier
{
    Common  = 0,
    Uncommon = 1,
    Elite   = 2,
    Boss    = 3
}

[System.Serializable]
public class EnemyTierEntry
{
    [Tooltip("Должно совпадать с именем в JSON")]
    public string name;
    public GameObject prefab;

    [Header("Баланс")]
    [Tooltip("Стоимость из бюджета директора")]
    public int cost = 1;

    [Tooltip("С какой секунды игры этот враг начинает спавниться")]
    public float unlockTime = 0f;

    [Range(0f, 1f)]
    [Tooltip("Относительный вес при случайном выборе (среди одного тира)")]
    public float weight = 1f;

    public EnemyTier tier = EnemyTier.Common;
}
