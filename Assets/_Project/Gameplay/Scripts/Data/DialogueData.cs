using System.Collections.Generic;
using UnityEngine;

public enum DialogueEvent
{
	None,
	StartBattle,
	UnlockMemoryFragment,
	GiveCard,
	SetFlag,
	TriggerVignette,
	LoadScene
}

[System.Serializable]
public class DialogueNode
{
	public int nodeID = 0;
	public string speakerName = "Narrator";

	[TextArea(2, 6)]
	public string textContent = string.Empty;

	public DialogueEvent triggerEvent = DialogueEvent.None;
	public string eventParam = string.Empty;
	public string portraitId = string.Empty;
	public int defaultNextNodeID = -1;
}

[CreateAssetMenu(fileName = "DialogueData", menuName = "Memories/Data/Dialogue")]
public class DialogueData : ScriptableObject
{
	public string dialogueID = string.Empty;
	public int startNodeID = 0;
	public List<DialogueNode> nodes = new List<DialogueNode>();

	public DialogueNode GetNodeByID(int nodeID)
	{
		if (nodes == null)
		{
			return null;
		}

		for (int i = 0; i < nodes.Count; i++)
		{
			if (nodes[i] != null && nodes[i].nodeID == nodeID)
			{
				return nodes[i];
			}
		}

		return null;
	}

	public DialogueNode GetStartNode()
	{
		DialogueNode startNode = GetNodeByID(startNodeID);
		if (startNode != null)
		{
			return startNode;
		}

		if (nodes == null || nodes.Count == 0)
		{
			return null;
		}

		return nodes[0];
	}
}
