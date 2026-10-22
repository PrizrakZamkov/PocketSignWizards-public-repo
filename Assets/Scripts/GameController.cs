using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class GameController : MonoBehaviour
{
    public static GameController Instance;

    [Header("Enemy Prefabs")]
    [SerializeField] private EnemyDatabase enemyDatabase;
    [SerializeField] private EnemySpawnDirector spawnDirector;

    [Header("XP UI")]
    [SerializeField] private Transform xpTarget;
    [SerializeField] private Image fakeCircleImage;
    [SerializeField] private Image realCircleImage;
    [SerializeField] private int[] xpGoals = { 10, 20, 40 };
    [SerializeField] private float fillEffectTime = 0.25f;
    [SerializeField] private float clearEffectTime = 0.5f;
    [SerializeField] private TMP_Text levelText;

    [Header("Signs")]
    [SerializeField] private SignChoicePopup signChoicePopup;
    [SerializeField] private SignDatabase signDatabase;
    [SerializeField] private bool isAllSignsAllowed = false;
    [SerializeField] private List<int> defaultSignsID = new List<int>();

    [Header("Audio")]
    [SerializeField] private AudioClip levelUpClip; // Assign this in the Inspector
    [SerializeField] private AudioSource audioSource; // Optional: if you want to control volume/pitch

    private int currentLevel = 1;
    private Transform[] enemySpawnpoints;
    private Dictionary<string, GameObject> enemyPrefabMap;
    private LevelData level;

    private Material fakeCircleMaterial;

    private int currentXP;
    private int xpGoalIndex;
    private bool filling;

    private readonly List<SignEntry> unlockedSigns = new List<SignEntry>();

    public Transform XPTarget => xpTarget;
    public IReadOnlyList<SignEntry> UnlockedSigns => unlockedSigns;

    private readonly HashSet<string> pauseSources = new HashSet<string>();
    internal readonly object signPanel;
    [SerializeField] private CameraShake cameraShake;
    public void PauseGame(string source = "default")
    {
        pauseSources.Add(source);
        spawnDirector?.StopSpawning();
        Time.timeScale = 0f;
    }

    public void ResumeGame(string source = "default")
    {
        pauseSources.Remove(source);
        spawnDirector?.StartSpawning();
        if (pauseSources.Count == 0)
            Time.timeScale = 1f;
    }

    public bool IsPausedBy(string source) => pauseSources.Contains(source);
    public bool IsGamePaused => pauseSources.Count > 0;
    private int CurrentXpGoal
    {
        get
        {
            if (xpGoals != null && xpGoals.Length > 0)
            {
                if (xpGoalIndex < xpGoals.Length)
                    return xpGoals[xpGoalIndex];

                int goal = xpGoals[xpGoals.Length - 1];

                for (int i = xpGoals.Length; i <= xpGoalIndex; i++)
                    goal *= 2;

                return goal;
            }

            return 10;
        }
    }

    void Awake()
    {
        Instance = this;
        BuildEnemyDatabase();
    }

    void Start()
    {
        SetupXPCollectorUI();
        AddDefaultSigns();
        UpdateLevelUI();
        if (spawnDirector != null)
            spawnDirector.StartSpawning();
    }

    void SetupXPCollectorUI()
    {

        fakeCircleMaterial = Instantiate(fakeCircleImage.material);
        fakeCircleImage.material = fakeCircleMaterial;

        fakeCircleMaterial.SetFloat("_DissolveAmount", 1f);
        fakeCircleImage.gameObject.SetActive(false);

        realCircleImage.transform.localScale = Vector3.zero;
        fakeCircleImage.transform.localScale = Vector3.zero;

    }

    void AddDefaultSigns()
    {
        if (signDatabase == null || signDatabase.signs == null)
            return;

        if (isAllSignsAllowed)
        {
            foreach (var s in signDatabase.signs)
            {
                if (!defaultSignsID.Contains(s.id))
                {
                    defaultSignsID.Add(s.id);
                }
            }
        }

        foreach (var s in signDatabase.signs)
        {
            if (defaultSignsID.Contains(s.id))
            {
                if (!unlockedSigns.Contains(s))
                {
                    unlockedSigns.Add(s);
                    Debug.Log("Default sign added: " + s.name);
                }
            }
        }
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;
    }

    public void AddUnlockedSign(SignEntry sign)
    {
        if (!unlockedSigns.Contains(sign))
            unlockedSigns.Add(sign);

        Debug.Log("Chosen sign: " + sign.name);
    }

    void BuildEnemyDatabase()
    {
        enemyPrefabMap = new Dictionary<string, GameObject>();

        if (enemyDatabase == null || enemyDatabase.enemies == null)
        {
            Debug.LogError("EnemyDatabase not assigned");
            return;
        }

        foreach (EnemyPrefabEntry entry in enemyDatabase.enemies)
        {
            if (entry == null || entry.prefab == null || string.IsNullOrEmpty(entry.name))
                continue;

            enemyPrefabMap[entry.name] = entry.prefab;
        }

        Debug.Log("Enemy prefabs loaded: " + enemyPrefabMap.Count);
    }



    Vector3 GetRandomPointAround(Vector3 center, float radius)
    {
        Vector2 offset = Random.insideUnitCircle * radius;
        return center + new Vector3(offset.x, offset.y, 0f);
    }

    public void AddXP(int amount)
    {
        currentXP += amount;

        if (!filling)
            StartCoroutine(FillXP());
    }

    private IEnumerator FillXP()
    {
        filling = true;

        while (true)
        {
            float targetFill = Mathf.Clamp01((float)currentXP / CurrentXpGoal);
            float currentFill = realCircleImage.transform.localScale.x;

            if (Mathf.Abs(targetFill - currentFill) < 0.001f)
                break;

            fakeCircleImage.transform.localScale = Vector3.one * targetFill;
            fakeCircleImage.gameObject.SetActive(true);
            fakeCircleMaterial.SetFloat("_DissolveAmount", 1f);

            float timer = 0f;

            while (timer < fillEffectTime)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / fillEffectTime);

                fakeCircleMaterial.SetFloat("_DissolveAmount", Mathf.Lerp(1f, 0f, t));

                yield return null;
            }

            fakeCircleMaterial.SetFloat("_DissolveAmount", 0f);

            timer = 0f;

            while (timer < fillEffectTime)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / fillEffectTime);
                t = Mathf.Sin(t * Mathf.PI * 0.5f);

                float newFill = Mathf.Lerp(currentFill, targetFill, t);
                realCircleImage.transform.localScale = Vector3.one * newFill;

                yield return null;
            }

            realCircleImage.transform.localScale = Vector3.one * targetFill;

            if (currentXP >= CurrentXpGoal)
            {
                Debug.Log("√отово!");
                currentLevel++;
                UpdateLevelUI();
                //AudioSource.PlayClipAtPoint(levelUpClip, Vector3.zero);
                StartCoroutine(LevelUpEffect());

                currentXP = 0;
                xpGoalIndex++;

                realCircleImage.transform.localScale = Vector3.zero;

                fakeCircleImage.transform.localScale = Vector3.one;
                fakeCircleImage.gameObject.SetActive(true);

                timer = 0f;

                while (timer < clearEffectTime)
                {
                    timer += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(timer / clearEffectTime);

                    fakeCircleMaterial.SetFloat("_DissolveAmount", Mathf.Lerp(0f, 1f, t));

                    yield return null;
                }

                fakeCircleMaterial.SetFloat("_DissolveAmount", 1f);
                fakeCircleImage.gameObject.SetActive(false);
                fakeCircleImage.transform.localScale = Vector3.zero;

                if (signChoicePopup != null && unlockedSigns.Count != signDatabase.signs.Length)
                {
                    audioSource.PlayOneShot(levelUpClip); // SOUND

                    cameraShake.StopShake();
                    PauseGame();
                    signChoicePopup.ShowRandomSigns(unlockedSigns);
                }

                break;
            }

            fakeCircleMaterial.SetFloat("_DissolveAmount", 1f);
            fakeCircleImage.gameObject.SetActive(false);

            yield return null;
        }

        filling = false;
    }

    
    public bool IsAttackAvailable(string attackName)
    {
        foreach (SignEntry sign in unlockedSigns)
        { 
            if (attackName == sign.name) return true;
        }
        return false;
    }
    void UpdateLevelUI()
    {
        if (levelText != null)
            levelText.text = "Level " + currentLevel;
    }
    IEnumerator LevelUpEffect()
    {
        if (levelText == null) yield break;

        Transform t = levelText.transform;

        Vector3 original = Vector3.one;
        Vector3 target = Vector3.one * 1.5f;

        float time = 0f;
        float duration = 0.15f;

        // увеличиваем
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float tLerp = time / duration;
            t.localScale = Vector3.Lerp(original, target, tLerp);
            yield return null;
        }

        time = 0f;

        // возвращаем обратно
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float tLerp = time / duration;
            t.localScale = Vector3.Lerp(target, original, tLerp);
            yield return null;
        }

        t.localScale = original;
    }
























}




