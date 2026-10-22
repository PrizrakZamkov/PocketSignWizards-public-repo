[System.Serializable]
public class LevelData
{
    public int id;
    public string name;
    public string biome;
    public Wave[] waves;
}

[System.Serializable]
public class Wave
{
    public int id;
    public float duration;
    public MiniWave[] miniWaves;
}

[System.Serializable]
public class MiniWave
{
    public float time;
    public EnemyGroup[] enemies;
}

[System.Serializable]
public class EnemyGroup
{
    public string type;
    public int count;
}