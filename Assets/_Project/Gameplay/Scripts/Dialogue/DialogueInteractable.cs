using UnityEngine;

[DisallowMultipleComponent]
public class DialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Post Reward Dialogue (Optional)")]
    [SerializeField] private DialogueData postRewardDialogueData;
    [SerializeField] private string rewardSequenceId = string.Empty;
    [SerializeField] private bool lockToPostRewardDialogue = true;

    private bool _postRewardDialogueLocked;

    public virtual void Interact()
    {
        DialogueManager dialogueManager = DialogueManager.Instance;
        if (dialogueManager == null)
        {
            Debug.LogWarning($"{nameof(DialogueInteractable)} on '{name}' could not find {nameof(DialogueManager)} in scene.");
            return;
        }

        DialogueData selectedDialogue = ResolveDialogue(dialogueManager);
        if (selectedDialogue == null)
        {
            Debug.LogWarning($"{nameof(DialogueInteractable)} on '{name}' has no dialogue assigned.");
            return;
        }

        dialogueManager.StartDialogue(selectedDialogue);
    }

    protected virtual DialogueData ResolveDialogue(DialogueManager dialogueManager)
    {
        if (_postRewardDialogueLocked && postRewardDialogueData != null)
        {
            return postRewardDialogueData;
        }

        if (postRewardDialogueData == null || string.IsNullOrWhiteSpace(rewardSequenceId))
        {
            return dialogueData;
        }

        if (!dialogueManager.IsCardRewardSequenceCompleted(rewardSequenceId))
        {
            return dialogueData;
        }

        if (lockToPostRewardDialogue)
        {
            _postRewardDialogueLocked = true;
        }

        return postRewardDialogueData;
    }
}
