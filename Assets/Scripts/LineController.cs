using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PDollarGestureRecognizer
{
    public class LineController : MonoBehaviour
    {
        private int drawingFingerId = -1;
        public Transform gestureOnScreenPrefab;

        [SerializeField] private float percentageOfDrawAreaInWindow = 1f; // 0.8f

        [Header("Drawing")]
        public bool canDrawNewGestureBeforePreviousDisappear = true;
        public int  sortingOrderWhileDrawing  = 10;
        public int  sortingOrderAfterRecognize = 1;

        [Header("Line Look")]
        public float lineWidth      = 0.018f;
        public int   capVertices    = 16;
        public int   cornerVertices = 16;

        [Header("Smoothing")]
        public float minScreenDistance = 2f;
        public int   samplesPerSegment = 10;

        [Header("Disappear")]
        public float disappearDelay        = 5f;
        public float dissolveDuration      = 1f;
        public float dissolveWrongDuration = 1f;
        public int   minPointsToRecognize  = 5;

        private List<Gesture>  trainingSet  = new();
        private List<Vector2>  rawScreenPoints = new();

        private Rect drawArea;
        private LineRenderer currentGestureLineRenderer;
        private List<LineRenderer> gestureLinesRenderer = new();

        private bool isDrawingGesture    = false;
        private int  disappearingGestures = 0;

        private PlayerAttack playerAttack;
        private SpellSystem  spellSystem;

        [Header("Line Colors")]
        [SerializeField] private Color drawingColor       = Color.white;
        [SerializeField] private Color activatedColor     = new Color(0.35f, 0.8f, 1f, 1f);
        [SerializeField] private float activatedColorPower = 1f;

        void Start()
        {
            playerAttack = PlayerAttack.Instance;
            spellSystem  = SpellSystem.Instance;

            drawArea = new Rect(0, Screen.height / 5, Screen.width, Screen.height - Screen.height / 5);
            drawArea = new Rect(0, Screen.height * (1 - percentageOfDrawAreaInWindow), Screen.width, Screen.height * percentageOfDrawAreaInWindow);

            TextAsset[] gesturesXml = Resources.LoadAll<TextAsset>("GestureSet/10-stylus-MEDIUM/");
            foreach (TextAsset g in gesturesXml)
                trainingSet.Add(GestureIO.ReadGestureFromXML(g.text));

            string[] filePaths = Directory.GetFiles(Application.persistentDataPath, "*.xml");
            foreach (string fp in filePaths)
                trainingSet.Add(GestureIO.ReadGestureFromFile(fp));
        }

        void Update()
        {
            if (Time.timeScale == 0f)
            {
                CancelCurrentGesture();
                return;
            }
            if (Input.touchCount > 0) HandleTouch();
            else                      HandleMouse();
        }

        void CancelCurrentGesture()
        {
            if (!isDrawingGesture) return;
            isDrawingGesture = false;
            drawingFingerId  = -1;
            rawScreenPoints.Clear();
            if (currentGestureLineRenderer != null)
                Destroy(currentGestureLineRenderer.gameObject);
            currentGestureLineRenderer = null;
        }

        void HandleTouch()
        {
            if (drawingFingerId == -1)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.phase == TouchPhase.Began && drawArea.Contains(t.position))
                    {
                        if (!canDrawNewGestureBeforePreviousDisappear && disappearingGestures > 0) return;
                        drawingFingerId = t.fingerId;
                        StartGesture(t.position);
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch t = Input.GetTouch(i);
                    if (t.fingerId != drawingFingerId) continue;

                    if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                        AddRawPoint(t.position);

                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        FinishGesture();
                        drawingFingerId = -1;
                    }
                    break;
                }
            }
        }

        void HandleMouse()
        {
            Vector2 pos = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))  TryStartGesture(pos);
            if (Input.GetMouseButton(0) && isDrawingGesture) AddRawPoint(pos);
            if (Input.GetMouseButtonUp(0) && isDrawingGesture) FinishGesture();
        }

        void TryStartGesture(Vector2 screenPos)
        {
            isDrawingGesture = false;
            if (!canDrawNewGestureBeforePreviousDisappear && disappearingGestures > 0) return;
            if (!drawArea.Contains(screenPos)) return;
            StartGesture(screenPos);
        }

        void StartGesture(Vector2 screenPos)
        {
            isDrawingGesture = true;
            rawScreenPoints.Clear();
            gestureLinesRenderer.Clear();

            Transform tmpGesture = Instantiate(gestureOnScreenPrefab, transform.position, transform.rotation);
            currentGestureLineRenderer = tmpGesture.GetComponent<LineRenderer>();

            currentGestureLineRenderer.sortingOrder     = sortingOrderWhileDrawing;
            currentGestureLineRenderer.startWidth       = lineWidth;
            currentGestureLineRenderer.endWidth         = lineWidth;
            currentGestureLineRenderer.numCapVertices   = capVertices;
            currentGestureLineRenderer.numCornerVertices = cornerVertices;
            currentGestureLineRenderer.useWorldSpace    = true;

            currentGestureLineRenderer.material = new Material(currentGestureLineRenderer.material);
            currentGestureLineRenderer.material.SetFloat("_DissolveAmount", 0f);
            currentGestureLineRenderer.material.SetColor("_Color", drawingColor); // рисуем белым

            gestureLinesRenderer.Add(currentGestureLineRenderer);
            AddRawPoint(screenPos, true);
        }

        void AddRawPoint(Vector2 screenPos, bool force = false)
        {
            if (!force && rawScreenPoints.Count > 0)
            {
                if (Vector2.Distance(rawScreenPoints[^1], screenPos) < minScreenDistance) return;
            }
            rawScreenPoints.Add(screenPos);
            RedrawSmoothLine();
        }

        void RedrawSmoothLine()
        {
            if (currentGestureLineRenderer == null) return;
            List<Vector2> smoothPoints = BuildSmoothScreenPoints();
            currentGestureLineRenderer.positionCount = smoothPoints.Count;
            for (int i = 0; i < smoothPoints.Count; i++)
            {
                Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(smoothPoints[i].x, smoothPoints[i].y, 10f));
                currentGestureLineRenderer.SetPosition(i, world);
            }
        }

        List<Vector2> BuildSmoothScreenPoints()
        {
            var result = new List<Vector2>();
            if (rawScreenPoints.Count == 0) return result;
            if (rawScreenPoints.Count < 4)  { result.AddRange(rawScreenPoints); return result; }

            for (int i = 0; i < rawScreenPoints.Count - 1; i++)
            {
                Vector2 p0 = i == 0 ? rawScreenPoints[i] : rawScreenPoints[i - 1];
                Vector2 p1 = rawScreenPoints[i];
                Vector2 p2 = rawScreenPoints[i + 1];
                Vector2 p3 = i + 2 < rawScreenPoints.Count ? rawScreenPoints[i + 2] : p2;

                for (int j = 0; j < samplesPerSegment; j++)
                    result.Add(CatmullRom(p0, p1, p2, p3, j / (float)samplesPerSegment));
            }
            result.Add(rawScreenPoints[^1]);
            return result;
        }

        Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t +
                           (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
                           (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        void FinishGesture()
        {
            isDrawingGesture = false;
            bool activated   = false;

            List<Vector2> smoothPoints = BuildSmoothScreenPoints();

            if (smoothPoints.Count >= minPointsToRecognize && trainingSet.Count > 0)
            {
                Point[] points = new Point[smoothPoints.Count];
                for (int i = 0; i < smoothPoints.Count; i++)
                    points[i] = new Point(smoothPoints[i].x, -smoothPoints[i].y, 0);

                Gesture candidate = new Gesture(points);
                Result  result    = PointCloudRecognizer.Classify(candidate, trainingSet.ToArray());

                Debug.Log($"Recognized: {result.GestureClass}  score: {result.Score:F2}");

                bool enoughScore = result.Score >= playerAttack.minPercentageForAttack;

                // ── Сначала пробуем SpellSystem, потом стандартный PlayerAttack ──
                bool handledBySpell = false;
                Debug.Log(result.GestureClass + " " + spellSystem.HasSpell(result.GestureClass) + " in spell system");

                if (enoughScore && spellSystem != null && spellSystem.HasSpell(result.GestureClass)
                            && GameController.Instance.IsAttackAvailable(result.GestureClass))
                {
                    handledBySpell = spellSystem.TryCast(result.GestureClass, smoothPoints.ToArray());
                    activated      = handledBySpell;

                    // Цвет линии — цвет заклинания
                    if (handledBySpell)
                    {
                        Color spellColor = spellSystem.GetSpellColor(result.GestureClass) * activatedColorPower;
                        foreach (var lr in gestureLinesRenderer)
                        {
                            lr.sortingOrder = sortingOrderAfterRecognize;
                            lr.material?.SetColor("_Color", spellColor);
                        }
                    }
                }

                if (!handledBySpell && false) // убрал на время потом верну если надо
                {
                    // Стандартный удар по линии (оригинальный PlayerAttack)
                    playerAttack?.Attack(result.GestureClass, result.Score, smoothPoints.ToArray());

                    if (enoughScore && GameController.Instance.IsAttackAvailable(result.GestureClass))
                    {
                        activated = true;
                        Color col = activatedColor * activatedColorPower;
                        foreach (var lr in gestureLinesRenderer)
                        {
                            lr.sortingOrder = sortingOrderAfterRecognize;
                            lr.material?.SetColor("_Color", col);
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Gesture too short");
            }

            StartCoroutine(DisappearGesture(new List<LineRenderer>(gestureLinesRenderer), activated));
        }

        IEnumerator DisappearGesture(List<LineRenderer> lines, bool isLineCorrect = true)
        {
            disappearingGestures++;
            yield return new WaitForSecondsRealtime(disappearDelay);

            float time     = 0f;
            float duration = isLineCorrect ? dissolveDuration : dissolveWrongDuration;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float value = Mathf.Clamp01(time / duration);
                foreach (var line in lines)
                    if (line != null && line.material != null)
                        line.material.SetFloat("_DissolveAmount", value);
                yield return null;
            }

            foreach (var line in lines)
                if (line != null) Destroy(line.gameObject);

            disappearingGestures--;
        }
    }
}
