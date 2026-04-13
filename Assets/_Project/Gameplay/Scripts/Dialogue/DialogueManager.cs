using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Serializable]
    private class CardRewardEntry
    {
        public Sprite overrideSprite;

        [TextArea(2, 4)]
        public string caption;
    }

    [Serializable]
    private class CardRewardSequence
    {
        public string sequenceId = string.Empty;
        public List<CardRewardEntry> cards = new List<CardRewardEntry>();
    }

    public static DialogueManager Instance { get; private set; }

    [SerializeField] private bool pausePlayerDuringDialogue = true;
    [SerializeField] private float typewriterSpeed = 20f;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private StateManager stateManager;
    [SerializeField] private MemoryManager memoryManager;

    [Header("Card Reward Event")]
    [SerializeField] private CardRewardPresenter cardRewardPresenter;
    [SerializeField] private List<CardRewardSequence> cardRewardSequences = new List<CardRewardSequence>();

    private readonly HashSet<string> _flags = new HashSet<string>();
    private readonly HashSet<string> _completedCardRewardSequences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private DialogueData _activeDialogue;
    private DialogueNode _currentNode;
    private bool _isActive;
    private bool _nodeEventProcessed;
    private Coroutine _advanceNodeCoroutine;

    public bool IsDialogueActive => _isActive;
    public DialogueNode CurrentNode => _currentNode;
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

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
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

        if (cardRewardPresenter == null)
        {
            cardRewardPresenter = FindFirstObjectByType<CardRewardPresenter>();
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
        _advanceNodeCoroutine = null;
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
        if (!_isActive || _currentNode == null || _advanceNodeCoroutine != null)
        {
            return;
        }

        _advanceNodeCoroutine = StartCoroutine(AdvanceCurrentNodeCoroutine());
    }

    public void EndDialogue()
    {
        if (!_isActive)
        {
            return;
        }

        if (_advanceNodeCoroutine != null)
        {
            StopCoroutine(_advanceNodeCoroutine);
            _advanceNodeCoroutine = null;
        }

        DialogueData endedDialogue = _activeDialogue;

        _isActive = false;
        _activeDialogue = null;
        _currentNode = null;
        _nodeEventProcessed = false;

        if (cardRewardPresenter != null)
        {
            cardRewardPresenter.StopSequence();
        }

        SetPlayerInputEnabled(true);

        OnDialogueEnded?.Invoke(endedDialogue);
    }

    private IEnumerator AdvanceCurrentNodeCoroutine()
    {
        if (!_nodeEventProcessed && _currentNode != null)
        {
            _nodeEventProcessed = true;
            yield return DispatchEventRoutine(_currentNode.triggerEvent, _currentNode.eventParam);
        }

        _advanceNodeCoroutine = null;

        if (!_isActive || _currentNode == null)
        {
            yield break;
        }

        if (_currentNode.defaultNextNodeID < 0)
        {
            EndDialogue();
            yield break;
        }

        MoveToNode(_currentNode.defaultNextNodeID);
    }

    public bool HasFlag(string flagId)
    {
        if (string.IsNullOrWhiteSpace(flagId))
        {
            return false;
        }

        return _flags.Contains(flagId);
    }

    public bool IsCardRewardSequenceCompleted(string sequenceId)
    {
        if (string.IsNullOrWhiteSpace(sequenceId))
        {
            return false;
        }

        return _completedCardRewardSequences.Contains(sequenceId.Trim());
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

    private IEnumerator DispatchEventRoutine(DialogueEvent dialogueEvent, string eventParam)
    {
        switch (dialogueEvent)
        {
            case DialogueEvent.None:
                yield break;

            case DialogueEvent.StartBattle:
                Debug.Log($"Dialogue requested battle start: {eventParam}");
                yield break;

            case DialogueEvent.UnlockMemoryFragment:
                if (memoryManager != null && !string.IsNullOrWhiteSpace(eventParam))
                {
                    memoryManager.UnlockFragment(eventParam);
                }
                yield break;

            case DialogueEvent.GiveCard:
                yield return HandleGiveCardEvent(eventParam);
                yield break;

            case DialogueEvent.SetFlag:
                if (!string.IsNullOrWhiteSpace(eventParam))
                {
                    _flags.Add(eventParam);
                    stateManager?.ApplyState(gameObject, eventParam, 1);
                }
                yield break;

            case DialogueEvent.TriggerVignette:
                Debug.Log($"Dialogue requested vignette trigger: {eventParam}");
                yield break;

            case DialogueEvent.LoadScene:
                if (SceneTransitionContext.TryParseSceneAndEntry(eventParam, out string sceneName, out string entryPointId))
                {
                    SceneTransitionContext.LoadScene(sceneName, entryPointId);
                }
                else
                {
                    Debug.LogWarning("LoadScene event requires a scene name. Use 'SceneName' or 'SceneName|EntryPointId'.");
                }
                yield break;

            default:
                Debug.LogWarning($"Unhandled dialogue event: {dialogueEvent}");
                yield break;
        }
    }

    private IEnumerator HandleGiveCardEvent(string eventParam)
    {
        string sequenceId = string.IsNullOrWhiteSpace(eventParam) ? string.Empty : eventParam.Trim();
        if (string.IsNullOrWhiteSpace(sequenceId))
        {
            Debug.LogWarning("GiveCard event has no sequence id. Set eventParam to a reward sequence id.");
            yield break;
        }

        CardRewardSequence sequence = FindCardRewardSequence(sequenceId);
        if (sequence == null || sequence.cards == null || sequence.cards.Count == 0)
        {
            Debug.LogWarning($"No card reward sequence found for id '{sequenceId}'.");
            yield break;
        }

        List<CardRewardPresentationData> presentationCards = new List<CardRewardPresentationData>(sequence.cards.Count);

        for (int i = 0; i < sequence.cards.Count; i++)
        {
            CardRewardEntry entry = sequence.cards[i];
            if (entry == null)
            {
                continue;
            }

            Sprite sprite = entry.overrideSprite;
            string caption = entry.caption;

            presentationCards.Add(new CardRewardPresentationData(sprite, caption));
        }

        if (cardRewardPresenter == null)
        {
            cardRewardPresenter = FindFirstObjectByType<CardRewardPresenter>();
        }

        if (cardRewardPresenter == null || presentationCards.Count == 0)
        {
            yield break;
        }

        bool sequenceCompleted = false;
        cardRewardPresenter.PlaySequence(presentationCards, () => sequenceCompleted = true);

        while (!sequenceCompleted)
        {
            yield return null;
        }

        _completedCardRewardSequences.Add(sequenceId);
    }

    private CardRewardSequence FindCardRewardSequence(string sequenceId)
    {
        if (string.IsNullOrWhiteSpace(sequenceId) || cardRewardSequences == null)
        {
            return null;
        }

        for (int i = 0; i < cardRewardSequences.Count; i++)
        {
            CardRewardSequence candidate = cardRewardSequences[i];
            if (candidate == null)
            {
                continue;
            }

            if (string.Equals(candidate.sequenceId, sequenceId, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private void SetPlayerInputEnabled(bool isEnabled)
    {
        if (!pausePlayerDuringDialogue)
        {
            return;
        }

        playerMovement?.SetMovementEnabled(isEnabled);
        playerInteraction?.SetInteractionEnabled(isEnabled);
    }
}
