using UnityEngine;

[DisallowMultipleComponent]
public class VignetteInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private VignetteData vignetteData;

    public virtual void Interact()
    {
        VignetteManager vignetteManager = VignetteManager.Instance;
        if (vignetteManager == null)
        {
            Debug.LogWarning($"{nameof(VignetteInteractable)} on '{name}' could not find {nameof(VignetteManager)} in scene.");
            return;
        }

        if (vignetteData == null)
        {
            Debug.LogWarning($"{nameof(VignetteInteractable)} on '{name}' has no vignette data assigned.");
            return;
        }

        vignetteManager.Show(vignetteData);
    }
}
