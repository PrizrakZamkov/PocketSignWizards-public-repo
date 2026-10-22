using UnityEngine;

public class ShineGlobal : MonoBehaviour
{
    public static float TimeValue;

    [SerializeField] private float speed = 1f;

    void Update()
    {
        TimeValue += Time.unscaledDeltaTime * speed;
    }
}