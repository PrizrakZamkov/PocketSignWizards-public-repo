using UnityEngine;

public class InvisEnemy : EnemyBehaviour
{
    [SerializeField] private float visibleTime = 2.5f;
    [SerializeField] private float invisibleTime = 1.8f;
    [SerializeField] private float alphaVisible = 1f;
    [SerializeField] private float alphaInvisible = 0.25f;

    private SpriteRenderer sr;
    private bool isVisible = true;
    private float timer = 0f;

    public void Awake()
    {
        base.Awake();
        sr = GetComponent<SpriteRenderer>();
    }

    public override void UpdateBehaviour()
    {
        timer += Time.deltaTime;

        if (isVisible && timer > visibleTime)
        {
            isVisible = false;
            timer = 0f;
        }
        else if (!isVisible && timer > invisibleTime)
        {
            isVisible = true;
            timer = 0f;
        }

        float targetAlpha = isVisible ? alphaVisible : alphaInvisible;
        Color c = sr.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, Time.deltaTime * 8f);
        sr.color = c;
    }
}