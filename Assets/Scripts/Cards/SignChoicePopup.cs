using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SignChoicePopup : MonoBehaviour
{
    [SerializeField] private SignDatabase database;
    [SerializeField] private SignCardUI[] cards;
    [SerializeField] private float appearTime = 0.25f;
    [SerializeField] private float hideTime = 0.25f;

    [Header("Sign Panel")]
    [SerializeField] private Transform signPanel;
    [SerializeField] private GameObject signSlotPrefab;
    [SerializeField] private float signFlySpeed = 600f;
    [SerializeField] private float signFlyScaleStart = 1f;
    [SerializeField] private float signFlyScaleEnd = 0.7f;
    [SerializeField] private float slotAppearTime = 0.2f;
    [SerializeField] private Canvas rootCanvas;

    [Header("Cooldown Cross")]
    [SerializeField] private Color cooldownCrossColor = new Color(1f, 0.05f, 0.35f, 0.9f);
    [SerializeField] private float cooldownCrossLineWidth = 8f;

    private bool choosing;

    void Awake()
    {
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void ShowRandomSigns(IReadOnlyList<SignEntry> unlockedSigns)
    {
        gameObject.SetActive(true);
        GameController.Instance.PauseGame("popup");
        choosing = true;

        List<SignEntry> available = new List<SignEntry>();
        foreach (SignEntry sign in database.signs)
            if (!unlockedSigns.Contains(sign))
                available.Add(sign);

        Shuffle(available);

        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].transform.localScale = Vector3.one;
            cards[i].ResetDissolve();

            if (i < available.Count && i < 3)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Set(available[i], this);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }

        StartCoroutine(Appear());
    }

    public void ChooseCard(SignCardUI selectedCard, SignEntry sign)
    {
        if (!choosing) return;
        choosing = false;

        GameController.Instance.AddUnlockedSign(sign);
        StartCoroutine(ChooseRoutine(selectedCard));
    }

    IEnumerator Appear()
    {
        float timer = 0f;
        while (timer < appearTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Sin(Mathf.Clamp01(timer / appearTime) * Mathf.PI * 0.5f);
            transform.localScale = Vector3.one * t;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    IEnumerator ChooseRoutine(SignCardUI selectedCard)
    {
        yield return selectedCard.BlinkFrame();

        // Запускаем полёт независимо от анимации скрытия
        StartCoroutine(FlySignToPanel(selectedCard));

        float timer = 0f;
        while (timer < hideTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / hideTime);

            foreach (SignCardUI card in cards)
            {
                if (card == selectedCard)
                    card.PlayDissolve(t);
                else
                    card.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
            }

            yield return null;
        }

        //gameObject.SetActive(false);
        GameController.Instance.ResumeGame("popup");
        transform.localScale = Vector3.zero;

        foreach (SignCardUI card in cards)
        {
            card.transform.localScale = Vector3.one;
            card.ResetDissolve();
        }
    }

    IEnumerator FlySignToPanel(SignCardUI selectedCard)
    {
        if (signPanel == null || signSlotPrefab == null || rootCanvas == null)
            yield break;

        // Спавним реальный слот в панели (пока невидимый)
        Debug.Log(signSlotPrefab);
        Debug.Log(signPanel);
        GameObject realSlot = Instantiate(signSlotPrefab, signPanel);
        realSlot.transform.localScale = Vector3.zero;

        UIGestureLine realGestureLine = realSlot.GetComponentInChildren<UIGestureLine>();

        // Спавним летящий дубликат на root canvas
        Debug.Log(signSlotPrefab);
        Debug.Log(rootCanvas.transform);
        GameObject flyingObj = Instantiate(signSlotPrefab, rootCanvas.transform);
        UIGestureLine flyingGestureLine = flyingObj.GetComponentInChildren<UIGestureLine>();
        RectTransform flyingRect = flyingObj.GetComponent<RectTransform>();

        // Стартовая позиция — позиция GestureLine выбранной карточки
        RectTransform sourceRect = selectedCard.GestureLine.GetComponent<RectTransform>();
        Vector3 startWorldPos = sourceRect.position + new Vector3(0,100,0);

        flyingRect.position = startWorldPos;
        flyingRect.sizeDelta = sourceRect.rect.size;

        // Устанавливаем жест на летящий объект
        if (flyingGestureLine != null && selectedCard.CurrentSign != null)
        {
            var gesture = PDollarGestureRecognizer.GestureIO
                .ReadGestureFromXML(selectedCard.CurrentSign.xmlFile.text);
            flyingGestureLine.SetGesture(gesture, true);
        }

        // Ждём один кадр чтобы GridLayout посчитал позицию слота
        yield return null;
        yield return null;

        RectTransform targetRect = realSlot.GetComponent<RectTransform>();


        // Полёт к слоту
        while (true)
        {
            Vector3 targetPos = targetRect.position;
            float dist = Vector3.Distance(flyingRect.position, targetPos);
            float step = signFlySpeed * Time.unscaledDeltaTime;

            // Прогресс полёта для масштаба (0=старт, 1=финиш)
            float totalDist = Vector3.Distance(startWorldPos, targetPos);
            float progress = totalDist > 0.01f
                ? 1f - Mathf.Clamp01(dist / totalDist)
                : 1f;

            float scale = Mathf.Lerp(signFlyScaleStart, signFlyScaleEnd, progress);
            flyingObj.transform.localScale = Vector3.one * scale;

            if (dist <= step)
            {
                Debug.Log("fly ");
                flyingRect.position = targetPos;
                break;
            }

            flyingRect.position = Vector3.MoveTowards(flyingRect.position, targetPos, step);
            yield return null;
        }

        Destroy(flyingObj);

        // Устанавливаем жест на реальный слот и показываем его
        if (realGestureLine != null && selectedCard.CurrentSign != null)
        {
            var gesture = PDollarGestureRecognizer.GestureIO
                .ReadGestureFromXML(selectedCard.CurrentSign.xmlFile.text);
            realGestureLine.SetGesture(gesture, true);
        }

        // Плавно показываем слот
        float timer = 0f;
        while (timer < slotAppearTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Sin(Mathf.Clamp01(timer / slotAppearTime) * Mathf.PI * 0.5f);
            realSlot.transform.localScale = Vector3.one * t;
            yield return null;
        }

        if (selectedCard.CurrentSign != null)
        {
            SignCooldownCrossUI cross = realSlot.GetComponent<SignCooldownCrossUI>();

            if (cross == null)
                cross = realSlot.AddComponent<SignCooldownCrossUI>();

            cross.Init(
                selectedCard.CurrentSign.name,
                cooldownCrossColor,
                cooldownCrossLineWidth
            );
        }

        realSlot.transform.localScale = Vector3.one;
    }

    void Shuffle(List<SignEntry> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
}