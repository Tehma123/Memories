using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject button;
    [SerializeField] private string playSceneName = "ExplorationAct0";
    [SerializeField] private string playSceneEntryPointId = string.Empty;

    public void Play()
    {
        SceneTransitionContext.LoadScene(playSceneName, playSceneEntryPointId);
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
