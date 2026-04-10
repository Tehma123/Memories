using UnityEngine;
using UnityEngine.SceneManagement;
public class MemoryShop : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueData shopDialogue;

    public void Interact()
    {
        if (shopDialogue != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(shopDialogue);
            return;
        }

        Debug.Log("Memory shop interaction triggered.");
        SceneManager.LoadScene("MemoryShopScene");
    }
}