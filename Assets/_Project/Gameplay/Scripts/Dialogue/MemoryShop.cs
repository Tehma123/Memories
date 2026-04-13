using UnityEngine;

public class MemoryShop : MonoBehaviour, IInteractable
{
    [SerializeField] private DialogueData shopDialogue;
    [SerializeField] private string fallbackSceneName = "MemoryShopScene";
    [SerializeField] private string fallbackEntryPointId = string.Empty;

    public void Interact()
    {
        if (shopDialogue != null && DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(shopDialogue);
            return;
        }

        Debug.Log("Memory shop interaction triggered.");
        SceneTransitionContext.LoadScene(fallbackSceneName, fallbackEntryPointId);
    }
}