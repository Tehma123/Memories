using UnityEngine;
using UnityEngine.SceneManagement;
public class MainMenuController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject button;

    public void Play()
    {
        SceneManager.LoadScene("ExplorationAct0");
    }

    public void Options()
    {
        Debug.Log("Options button clicked!");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
