using UnityEngine;

public class ShamanDialogue : MonoBehaviour, IInteractable
{
	 [SerializeField] private DialogueData dialogueData;

	 public void Interact()
	 {
		  if (dialogueData == null)
		  {
				Debug.LogWarning("NpcDialogueInteractable: dialogueData chưa được gán.");
				return;
		  }

		  if (DialogueManager.Instance == null)
		  {
				Debug.LogWarning("NpcDialogueInteractable: không tìm thấy DialogueManager trong scene.");
				return;
		  }

		  DialogueManager.Instance.StartDialogue(dialogueData);
	 }
}