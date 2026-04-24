using UnityEngine;
using System;

[DisallowMultipleComponent]
public class SceneExitInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string destinationSceneName = string.Empty;
    [SerializeField] private string destinationEntryPointId = "Default";
    [SerializeField] private bool blockRepeatedUse = true;

    [Header("Pre-Transition Dialogue (Optional)")]
    [SerializeField] private DialogueData preTransitionDialogue;

    [Header("Encounter Payload (Optional)")]
    [SerializeField] private bool includeEncounterPayload;
    [SerializeField] private string encounterId = string.Empty;
    [SerializeField] private EnemyData[] encounterEnemies = Array.Empty<EnemyData>();
    [SerializeField, Min(1)] private int encounterLevel = 1;
    [SerializeField] private string spawnPattern = string.Empty;
    [SerializeField] private bool overrideEncounterSeed;
    [SerializeField] private int encounterSeed;

    private bool _hasBeenUsed;
    private bool _isWaitingForDialogueEnd;

    private void OnEnable()
    {
        _hasBeenUsed = false;
        _isWaitingForDialogueEnd = false;
    }

    private void OnDisable()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= HandleDialogueEnded;
        }

        _isWaitingForDialogueEnd = false;
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

        if (TryStartPreTransitionDialogue())
        {
            return;
        }

        TransitionToDestination();
    }

    private bool TryStartPreTransitionDialogue()
    {
        if (preTransitionDialogue == null)
        {
            return false;
        }

        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogueManager == null)
        {
            Debug.LogWarning($"{nameof(SceneExitInteractable)} on '{name}' could not find {nameof(DialogueManager)}. Skipping pre-transition dialogue.");
            return false;
        }

        if (_isWaitingForDialogueEnd)
        {
            return true;
        }

        _hasBeenUsed = true;
        _isWaitingForDialogueEnd = true;
        dialogueManager.OnDialogueEnded -= HandleDialogueEnded;
        dialogueManager.OnDialogueEnded += HandleDialogueEnded;
        dialogueManager.StartDialogue(preTransitionDialogue);
        return true;
    }

    private void HandleDialogueEnded(DialogueData endedDialogue)
    {
        if (!_isWaitingForDialogueEnd)
        {
            return;
        }

        if (endedDialogue != preTransitionDialogue)
        {
            return;
        }

        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueEnded -= HandleDialogueEnded;
        }

        _isWaitingForDialogueEnd = false;
        TransitionToDestination();
    }

    private void TransitionToDestination()
    {
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
