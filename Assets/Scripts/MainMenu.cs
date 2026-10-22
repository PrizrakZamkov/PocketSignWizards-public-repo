using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private int gameSceneNum = 1;
    void Start()
    {
        
    }

    public void StartGame()
    {
        //Debug.Log("Start level: " + PlanetSnapScroll.Instance.SelectedIndex);
        SceneManager.LoadScene(gameSceneNum);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
