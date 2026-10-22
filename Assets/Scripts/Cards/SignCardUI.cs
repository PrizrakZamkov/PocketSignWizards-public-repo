using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PDollarGestureRecognizer;
using UnityEngine.EventSystems;
public class SignCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UIGestureLine gestureLine;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image cardImage;
    [SerializeField] private GameObject blinkFrame;

    [SerializeField] private GameObject hoverFrame;
    [SerializeField] private float hoverAppearTime = 0.15f;

    private Coroutine hoverRoutine;

    private SignEntry currentSign;
    private SignChoicePopup popup;
    private Material materialInstance;

    public UIGestureLine GestureLine => gestureLine;
    public SignEntry CurrentSign => currentSign;


    void Awake()
    {
        if (cardImage != null)
        {
            materialInstance = Instantiate(cardImage.material);
            cardImage.material = materialInstance;
            ResetDissolve();
        }
    }
    public IEnumerator BlinkFrame()
    {
        if (blinkFrame == null)
            yield break;

        for (int i = 0; i < 2; i++)
        {
            blinkFrame.SetActive(true);
            yield return new WaitForSecondsRealtime(0.08f);

            blinkFrame.SetActive(false);
            yield return new WaitForSecondsRealtime(0.08f);
        }
    }
    public void Set(SignEntry sign, SignChoicePopup owner)
    {
        currentSign = sign;
        popup = owner;

        descriptionText.text = sign.description;

        ResetDissolve();
        StartCoroutine(SetGestureNextFrame(sign));
    }

    IEnumerator SetGestureNextFrame(SignEntry sign)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();

        Gesture gesture = GestureIO.ReadGestureFromXML(sign.xmlFile.text);
        gestureLine.SetGesture(gesture, true);
    }

    public void OnClick()
    {
        popup.ChooseCard(this, currentSign);
    }

    public void PlayDissolve(float t)
    {
        if (materialInstance != null)
            materialInstance.SetFloat("_DissolveAmount", t);
    }

    public void ResetDissolve()
    {
        if (materialInstance != null)
            materialInstance.SetFloat("_DissolveAmount", 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverFrame == null) return;

        if (hoverRoutine != null)
            StopCoroutine(hoverRoutine);

        hoverRoutine = StartCoroutine(HoverAppear());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverFrame == null) return;

        if (hoverRoutine != null)
            StopCoroutine(hoverRoutine);

        hoverRoutine = StartCoroutine(HoverDisappear());
    }


    IEnumerator HoverAppear()
    {
        hoverFrame.SetActive(true);

        float timer = 0f;

        while (timer < hoverAppearTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / hoverAppearTime);

            SetHoverDissolve(1f - t);

            yield return null;
        }

        SetHoverDissolve(0f);
    }

    IEnumerator HoverDisappear()
    {
        float timer = 0f;

        while (timer < hoverAppearTime)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / hoverAppearTime);

            SetHoverDissolve(t);

            yield return null;
        }

        SetHoverDissolve(1f);
        hoverFrame.SetActive(false);
    }

    private Material hoverMaterial;

    void Start()
    {
        if (hoverFrame != null)
        {
            var img = hoverFrame.GetComponent<UnityEngine.UI.Image>();

            if (img != null)
            {
                hoverMaterial = Instantiate(img.material);
                img.material = hoverMaterial;

                hoverMaterial.SetFloat("_DissolveAmount", 1f);
                hoverFrame.SetActive(false);
            }
        }
    }

    void SetHoverDissolve(float value)
    {
        if (hoverMaterial != null)
            hoverMaterial.SetFloat("_DissolveAmount", value);
    }
}