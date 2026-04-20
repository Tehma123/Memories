using UnityEngine;
using System;

[DisallowMultipleComponent]
public class SceneExitInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string destinationSceneName = string.Empty;
    [SerializeField] private string destinationEntryPointId = "Default";
    [SerializeField] private bool blockRepeatedUse = true;

    [Header("Encounter Payload (Optional)")]
    [SerializeField] private bool includeEncounterPayload;
    [SerializeField] private string encounterId = string.Empty;
    [SerializeField] private EnemyData[] encounterEnemies = Array.Empty<EnemyData>();
    [SerializeField, Min(1)] private int encounterLevel = 1;
    [SerializeField] private string spawnPattern = string.Empty;
    [SerializeField] private bool overrideEncounterSeed;
    [SerializeField] private int encounterSeed;

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

        EncounterPayload payload = BuildEncounterPayload();
        SceneTransitionContext.LoadScene(destinationSceneName, destinationEntryPointId, payload);
    }

    private EncounterPayload BuildEncounterPayload()
    {
        if (!includeEncounterPayload)
        {
            return null;
        }

        int? seed = overrideEncounterSeed ? encounterSeed : (int?)null;
        return EncounterPayload.FromEnemyData(
            encounterId,
            encounterEnemies,
            encounterLevel,
            spawnPattern,
            seed);
    }

    private void OnValidate()
    {
        destinationSceneName = (destinationSceneName ?? string.Empty).Trim();
        destinationEntryPointId = (destinationEntryPointId ?? string.Empty).Trim();
        encounterId = (encounterId ?? string.Empty).Trim();
        spawnPattern = (spawnPattern ?? string.Empty).Trim();
        encounterLevel = Mathf.Max(1, encounterLevel);
    }
}
