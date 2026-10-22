using UnityEngine;

[CreateAssetMenu(fileName = "SignDatabase", menuName = "Sign Database")]
public class SignDatabase : ScriptableObject
{
    public SignEntry[] signs;
}

[System.Serializable]
public class SignEntry
{
    public int id;
    public string name;
    public TextAsset xmlFile;
    [TextArea] public string description;
}