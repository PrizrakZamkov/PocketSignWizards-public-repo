using UnityEngine;
using UnityEngine.UI;

public class PauseButton : MonoBehaviour
{
    [SerializeField] private GameObject pauseIcon;
    [SerializeField] private GameObject resumeIcon;
    [SerializeField] private Button button;

    private bool isPaused = false;
    private const string SOURCE = "player";

    void Update()
    {
        bool blockedByOther = GameController.Instance.IsGamePaused && !isPaused;
        button.interactable = !blockedByOther;
    }

    public void OnClick()
    {
        isPaused = !isPaused;

        if (isPaused)
            GameController.Instance.PauseGame(SOURCE);
        else
            GameController.Instance.ResumeGame(SOURCE);

        UpdateIcons();
    }

    void UpdateIcons()
    {
        pauseIcon.SetActive(!isPaused);
        resumeIcon.SetActive(isPaused);
    }
}