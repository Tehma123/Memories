using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject button;

    public void Play()
    {
        Debug.Log("Play button clicked!");
    }

    public void Options()
    {
        Debug.Log("Options button clicked!");
    }

    public void Quit()
    {
        Debug.Log("Quit button clicked!");
    }
}
