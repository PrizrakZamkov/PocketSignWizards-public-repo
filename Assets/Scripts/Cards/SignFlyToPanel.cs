//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;

//public class SignFlyToPanel : MonoBehaviour
//{
//    private RectTransform rect;
//    private Canvas canvas;

//    void Awake()
//    {
//        rect = GetComponent<RectTransform>();
//        canvas = GetComponentInParent<Canvas>();
//    }

//    public void Fly(SignEntry sign)
//    {
//        StartCoroutine(FlyRoutine(sign));
//    }

//    IEnumerator FlyRoutine(SignEntry sign)
//    {
//        // 1. создаём слот
//        GameObject slot = Instantiate(
//            GameController.Instance.signSlotPrefab,
//            GameController.Instance.signPanel
//        );

//        RectTransform slotRect = slot.GetComponent<RectTransform>();

//        // важно: обновляем grid
//        LayoutRebuilder.ForceRebuildLayoutImmediate(
//            GameController.Instance.signPanel.GetComponent<RectTransform>()
//        );

//        // 2. позиция цели
//        Vector3 targetPos = slotRect.position;

//        // 3. отсоединяем знак (чтобы летел поверх UI)
//        rect.SetParent(canvas.transform);

//        Vector3 startPos = rect.position;

//        float t = 0f;

//        while (t < 1f)
//        {
//            t += Time.unscaledDeltaTime * GameController.Instance.signFlySpeed;

//            float eased = Mathf.SmoothStep(0f, 1f, t);

//            rect.position = Vector3.Lerp(startPos, targetPos, eased);
//            rect.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.6f, eased);

//            yield return null;
//        }

//        rect.position = targetPos;

//        // 4. переносим в слот
//        rect.SetParent(slotRect);
//        rect.localPosition = Vector3.zero;
//        rect.localScale = Vector3.one;

//        // 5. добавляем в игру
//        GameController.Instance.AddUnlockedSign(sign);

//    }
//}