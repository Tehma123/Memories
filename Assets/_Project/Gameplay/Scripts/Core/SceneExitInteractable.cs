using UnityEngine;

[DisallowMultipleComponent]
public class SceneExitInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string destinationSceneName = string.Empty;
    [SerializeField] private string destinationEntryPointId = "Default";
    [SerializeField] private bool blockRepeatedUse = true;

    private bool _hasBeenUsed;

    private void OnEnable()
    {
        _hasBeenUsed = false;
    }

    public void Interact()
    {
        if (blockRepeatedUse && _hasBeenUsed)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(destinationSceneName))
        {
            Debug.LogWarning($"{nameof(SceneExitInteractable)} on '{name}' has no destination scene set.");
            return;
        }

        _hasBeenUsed = true;
        SceneTransitionContext.LoadScene(destinationSceneName, destinationEntryPointId);
    }

    private void OnValidate()
    {
        destinationSceneName = (destinationSceneName ?? string.Empty).Trim();
        destinationEntryPointId = (destinationEntryPointId ?? string.Empty).Trim();
    }
}
