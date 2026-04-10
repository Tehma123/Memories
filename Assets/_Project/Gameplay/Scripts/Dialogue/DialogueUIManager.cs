using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIManager : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private PortraitManager portraitManager;

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image portraitImage;

    [Header("Behavior")]
    [SerializeField] private bool hidePanelOnStart = true;

    private bool _subscribed;

    private void Awake()
    {
        ResolveReferences();

        if (hidePanelOnStart)
        {
            SetPanelVisible(false);
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    public void OnNextPressed()
    {
        ResolveReferences();
        dialogueManager?.ShowNextLine();
    }

    public void OnClosePressed()
    {
        ResolveReferences();
        dialogueManager?.EndDialogue();
    }

    private void HandleDialogueStarted(DialogueData _)
    {
        SetPanelVisible(true);
    }

    private void HandleDialogueLineShown(DialogueNode node)
    {
        if (node == null)
        {
            return;
        }

        if (speakerText != null)
        {
            speakerText.text = string.IsNullOrWhiteSpace(node.speakerName) ? "Narrator" : node.speakerName;
        }

        if (dialogueText != null)
        {
            dialogueText.text = node.textContent;
        }

        if (portraitImage != null)
        {
            if (portraitManager != null)
            {
                portraitManager.ApplyPortrait(portraitImage, node.portraitId);
            }
            else
            {
                portraitImage.sprite = null;
                portraitImage.enabled = false;
            }
        }
    }

    private void HandleDialogueEnded(DialogueData _)
    {
        ClearText();
        SetPanelVisible(false);
    }

    private void ResolveReferences()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        }

        if (portraitManager == null)
        {
            portraitManager = FindFirstObjectByType<PortraitManager>();
        }
    }

    private void Subscribe()
    {
        if (_subscribed || dialogueManager == null)
        {
            return;
        }

        dialogueManager.OnDialogueStarted += HandleDialogueStarted;
        dialogueManager.OnDialogueLineShown += HandleDialogueLineShown;
        dialogueManager.OnDialogueEnded += HandleDialogueEnded;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || dialogueManager == null)
        {
            return;
        }

        dialogueManager.OnDialogueStarted -= HandleDialogueStarted;
        dialogueManager.OnDialogueLineShown -= HandleDialogueLineShown;
        dialogueManager.OnDialogueEnded -= HandleDialogueEnded;
        _subscribed = false;
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(isVisible);
        }
    }

    private void ClearText()
    {
        if (speakerText != null)
        {
            speakerText.text = string.Empty;
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }
    }
}