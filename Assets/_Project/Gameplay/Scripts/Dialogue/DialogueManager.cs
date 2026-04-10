using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private bool pausePlayerDuringDialogue = true;
    [SerializeField] private float typewriterSpeed = 40f;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private StateManager stateManager;
    [SerializeField] private MemoryManager memoryManager;

    private readonly HashSet<string> _flags = new HashSet<string>();

    private DialogueData _activeDialogue;
    private DialogueNode _currentNode;
    private bool _isActive;
    private bool _nodeEventProcessed;

    public bool IsDialogueActive => _isActive;
    public float TypewriterSpeed => typewriterSpeed;

    public event Action<DialogueData> OnDialogueStarted;
    public event Action<DialogueNode> OnDialogueLineShown;
    public event Action<DialogueData> OnDialogueEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (playerInteraction == null)
        {
            playerInteraction = FindFirstObjectByType<PlayerInteraction>();
        }

        if (stateManager == null)
        {
            stateManager = FindFirstObjectByType<StateManager>();
        }

        if (memoryManager == null)
        {
            memoryManager = FindFirstObjectByType<MemoryManager>();
        }
    }

    public void StartDialogue(DialogueData dialogueData)
    {
        if (dialogueData == null)
        {
            return;
        }

        if (_isActive)
        {
            EndDialogue();
        }

        _activeDialogue = dialogueData;
        _isActive = true;
        SetPlayerInputEnabled(false);

        _currentNode = _activeDialogue.GetStartNode();
        if (_currentNode == null)
        {
            Debug.LogWarning($"Dialogue '{dialogueData.name}' has no valid start node.");
            EndDialogue();
            return;
        }

        _nodeEventProcessed = false;
        OnDialogueStarted?.Invoke(_activeDialogue);
        PresentCurrentNode();
    }

    public void ShowNextLine()
    {
        if (!_isActive || _currentNode == null)
        {
            return;
        }

        DispatchNodeEventIfNeeded();

        if (_currentNode.defaultNextNodeID < 0)
        {
            EndDialogue();
            return;
        }

        MoveToNode(_currentNode.defaultNextNodeID);
    }

    public void EndDialogue()
    {
        if (!_isActive)
        {
            return;
        }

        DialogueData endedDialogue = _activeDialogue;

        _isActive = false;
        _activeDialogue = null;
        _currentNode = null;
        _nodeEventProcessed = false;
        SetPlayerInputEnabled(true);

        OnDialogueEnded?.Invoke(endedDialogue);
    }

    public bool HasFlag(string flagId)
    {
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return false;
        }

        return _flags.Contains(flagId);
    }

    private void MoveToNode(int nodeID)
    {
        if (_activeDialogue == null)
        {
            EndDialogue();
            return;
        }

        DialogueNode nextNode = _activeDialogue.GetNodeByID(nodeID);
        if (nextNode == null)
        {
            EndDialogue();
            return;
        }

        _currentNode = nextNode;
        _nodeEventProcessed = false;
        PresentCurrentNode();
    }

    private void PresentCurrentNode()
    {
        if (_currentNode == null)
        {
            return;
        }

        string speaker = string.IsNullOrWhiteSpace(_currentNode.speakerName) ? "Narrator" : _currentNode.speakerName;
        Debug.Log($"[{speaker}] {_currentNode.textContent}");
        OnDialogueLineShown?.Invoke(_currentNode);
    }

    private void DispatchNodeEventIfNeeded()
    {
        if (_nodeEventProcessed || _currentNode == null)
        {
            return;
        }

        _nodeEventProcessed = true;
        DispatchEvent(_currentNode.triggerEvent, _currentNode.eventParam);
    }

    private void DispatchEvent(DialogueEvent dialogueEvent, string eventParam)
    {
        switch (dialogueEvent)
        {
            case DialogueEvent.None:
                break;

            case DialogueEvent.StartBattle:
                Debug.Log($"Dialogue requested battle start: {eventParam}");
                break;

            case DialogueEvent.UnlockMemoryFragment:
                if (memoryManager != null && !string.IsNullOrWhiteSpace(eventParam))
                {
                    memoryManager.UnlockFragment(eventParam);
                }
                break;

            case DialogueEvent.GiveCard:
                Debug.Log($"Dialogue requested card grant: {eventParam}");
                break;

            case DialogueEvent.SetFlag:
                if (!string.IsNullOrWhiteSpace(eventParam))
                {
                    _flags.Add(eventParam);
                    stateManager?.ApplyState(gameObject, eventParam, 1);
                }
                break;

            case DialogueEvent.TriggerVignette:
                Debug.Log($"Dialogue requested vignette trigger: {eventParam}");
                break;

            case DialogueEvent.LoadScene:
                if (!string.IsNullOrWhiteSpace(eventParam))
                {
                    SceneManager.LoadScene(eventParam);
                }
                break;

            default:
                Debug.LogWarning($"Unhandled dialogue event: {dialogueEvent}");
                break;
        }
    }

    private void SetPlayerInputEnabled(bool isEnabled)
    {
        if (!pausePlayerDuringDialogue)
        {
            return;
        }

        playerController?.SetMovementEnabled(isEnabled);
        playerInteraction?.SetInteractionEnabled(isEnabled);
    }
}
