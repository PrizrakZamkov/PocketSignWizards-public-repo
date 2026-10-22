using System.Collections;
using UnityEngine;

public class ImpactFlash : MonoBehaviour
{
    [SerializeField] private float defaultFlashTime = 0.08f;

    private SpriteRenderer spriteRenderer;
    private Material materialInstance;
    private Coroutine flashRoutine;

    public Material Material => materialInstance;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        materialInstance = new Material(spriteRenderer.material);
        spriteRenderer.material = materialInstance;

        materialInstance.SetFloat("_FlashAmount", 0f);
        materialInstance.SetFloat("_DissolveAmount", 0f);
    }

    public void Flash()
    {
        Flash(defaultFlashTime);
    }

    public void Flash(float time)
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashRoutine(time));
    }

    public void SetWhite()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        materialInstance.SetFloat("_FlashAmount", 1f);
    }

    private IEnumerator FlashRoutine(float time)
    {
        materialInstance.SetFloat("_FlashAmount", 1f);

        yield return new WaitForSeconds(time);

        materialInstance.SetFloat("_FlashAmount", 0f);
        flashRoutine = null;
    }
}