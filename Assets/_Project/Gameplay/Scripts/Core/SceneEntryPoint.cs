using UnityEngine;

[DisallowMultipleComponent]
public class SceneEntryPoint : MonoBehaviour
{
    [SerializeField] private string entryPointId = "Default";
    [SerializeField] private bool useAsFallback;

    public string EntryPointId => entryPointId;
    public bool UseAsFallback => useAsFallback;

    private void OnValidate()
    {
        entryPointId = (entryPointId ?? string.Empty).Trim();
    }
}
