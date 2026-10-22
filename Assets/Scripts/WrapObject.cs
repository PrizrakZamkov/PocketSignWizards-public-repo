using UnityEngine;

public class WrapObject : MonoBehaviour
{
    private void OnEnable()
    {
        if (WorldWrapManager.Instance != null)
            WorldWrapManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (WorldWrapManager.Instance != null)
            WorldWrapManager.Instance.Unregister(this);
    }
}