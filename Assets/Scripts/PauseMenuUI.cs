using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Одна кнопка — открывает и закрывает меню паузы.
/// Работает с AudioManager (SetMusic / SetSFX / SetUI)
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("Кнопка паузы")]
    [SerializeField] private Button pauseButton;

    [Header("Панель паузы")]
    [SerializeField] private GameObject pausePanel;

    [Header("Аудио toggles")]
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Toggle uiToggle; // optional

    // ─────────────────────────────────────────

    private void Start()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(ToggleMenu);

        if (musicToggle != null)
            musicToggle.onValueChanged.AddListener(OnMusicToggle);

        if (sfxToggle != null)
            sfxToggle.onValueChanged.AddListener(OnSFXToggle);

        if (uiToggle != null)
            uiToggle.onValueChanged.AddListener(OnUIToggle);

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    // ─────────────────────────────────────────

    public void ToggleMenu()
    {
        if (pausePanel == null) return;

        bool willOpen = !pausePanel.activeSelf;

        if (willOpen)
            SyncToggles(); // обновляем состояние перед открытием

        pausePanel.SetActive(willOpen);

        // если нужна пауза игры — раскомментируй:
        // Time.timeScale = willOpen ? 0f : 1f;
    }

    // ─────────────────────────────────────────
    // Toggles

    private void OnMusicToggle(bool isOn)
    {
        AudioManager.Instance?.SetMusic(isOn);
    }

    private void OnSFXToggle(bool isOn)
    {
        AudioManager.Instance?.SetSFX(isOn);
    }

    private void OnUIToggle(bool isOn)
    {
        AudioManager.Instance?.SetUI(isOn);
    }

    // ─────────────────────────────────────────

    private void SyncToggles()
    {
        var am = AudioManager.Instance;
        if (am == null) return;

        if (musicToggle != null)
            musicToggle.SetIsOnWithoutNotify(am.MusicOn);

        if (sfxToggle != null)
            sfxToggle.SetIsOnWithoutNotify(am.SFXOn);

        if (uiToggle != null)
            uiToggle.SetIsOnWithoutNotify(am.UIOn);
    }
}