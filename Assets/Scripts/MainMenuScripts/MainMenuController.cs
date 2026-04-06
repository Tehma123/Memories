using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject button;

    private void Play()
    {
        Debug.Log("Play button clicked!");
    }

    private void Options()
    {
        Debug.Log("Options button clicked!");
    }

    private void Quit()
    {
        Debug.Log("Quit button clicked!");
    }
}
