using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueUIManager : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Behavior")]
    [SerializeField] private bool hidePanelOnStart = true;
    [SerializeField] private bool autoCreateRuntimeUIIfMissing = true;
    [SerializeField] private Key nextLineKey = Key.F;
    [SerializeField] private float sentencePunctuationPause = 0.08f;
    [SerializeField] private float commaPunctuationPause = 0.04f;

    [Header("Audio")]
    [SerializeField] private AudioSource typewriterAudioSource;
    [SerializeField] private AudioClip typewriterTickClip;
    [SerializeField] [Range(0f, 1f)] private float typewriterTickVolume = 0.35f;

    private bool _subscribed;
    private bool _hideWithCanvasGroup;
    private CanvasGroup _panelCanvasGroup;
    private bool _runtimeUICreated;
    private bool _missingUIWarningLogged;
    private Coroutine _typewriterCoroutine;
    private string _currentLineText = string.Empty;
    private bool _isTypewriterRunning;
    private bool _waitForAdvanceKeyRelease;

    private void Awake()
    {
        ResolveReferences();
        EnsureUIBindings();
        ConfigurePanelVisibilityStrategy();

        if (hidePanelOnStart)
        {
            SetPanelVisible(false);
        }
    }

    private void OnEnable()
    {
        ResolveReferences();
        EnsureUIBindings();
        ConfigurePanelVisibilityStrategy();
        Subscribe();
        SyncWithDialogueState();
    }

    private void OnDisable()
    {
        StopTypewriterAnimation(false);
        Unsubscribe();
    }

    private void Update()
    {
        if (!_subscribed)
        {
            ResolveReferences();
            EnsureUIBindings();
            Subscribe();
            if (_subscribed)
            {
                SyncWithDialogueState();
            }
        }

        HandleAdvanceInput();
    }

    public void OnNextPressed()
    {
        ResolveReferences();
        if (TryCompleteCurrentLine())
        {
            return;
        }

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
        _waitForAdvanceKeyRelease = IsAdvanceKeyPressed();
    }

    private void HandleDialogueLineShown(DialogueNode node)
    {
        if (node == null)
        {
            return;
        }

        EnsureUIBindings();

        SetPanelVisible(true);

        if (speakerText != null)
        {
            speakerText.text = string.IsNullOrWhiteSpace(node.speakerName) ? "Narrator" : node.speakerName;
        }

        StartTypewriterAnimation(node.textContent);
    }

    private void HandleDialogueEnded(DialogueData _)
    {
        StopTypewriterAnimation(false);
        _waitForAdvanceKeyRelease = false;
        ClearText();
        SetPanelVisible(false);
    }

    private void ResolveReferences()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        }

        if (typewriterTickClip != null && typewriterAudioSource == null)
        {
            typewriterAudioSource = GetComponent<AudioSource>();
            if (typewriterAudioSource == null)
            {
                typewriterAudioSource = gameObject.AddComponent<AudioSource>();
                typewriterAudioSource.playOnAwake = false;
                typewriterAudioSource.loop = false;
                typewriterAudioSource.spatialBlend = 0f;
            }
        }
    }

    private void EnsureUIBindings()
    {
        if (dialoguePanel != null)
        {
            if (speakerText == null || dialogueText == null)
            {
                TMP_Text[] texts = dialoguePanel.GetComponentsInChildren<TMP_Text>(true);
                if (texts.Length > 0)
                {
                    if (speakerText == null)
                    {
                        speakerText = texts[0];
                    }

                    if (dialogueText == null)
                    {
                        dialogueText = texts.Length > 1 ? texts[1] : texts[0];
                    }
                }
            }
        }

        if (dialoguePanel != null && dialogueText != null)
        {
            return;
        }

        if (autoCreateRuntimeUIIfMissing)
        {
            CreateRuntimeUIIfNeeded();
        }

        if ((dialoguePanel == null || dialogueText == null) && !_missingUIWarningLogged)
        {
            Debug.LogWarning("DialogueUIManager is missing UI references. Assign dialoguePanel and dialogueText in Inspector, or keep autoCreateRuntimeUIIfMissing enabled.");
            _missingUIWarningLogged = true;
        }
    }

    private void CreateRuntimeUIIfNeeded()
    {
        if (_runtimeUICreated || (dialoguePanel != null && dialogueText != null))
        {
            return;
        }

        GameObject canvasObject = new GameObject("RuntimeDialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject("DialoguePanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.05f, 0.03f);
        panelRect.anchorMax = new Vector2(0.95f, 0.3f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.78f);

        GameObject speakerObject = new GameObject("SpeakerText", typeof(RectTransform), typeof(TextMeshProUGUI));
        speakerObject.transform.SetParent(panelObject.transform, false);
        RectTransform speakerRect = speakerObject.GetComponent<RectTransform>();
        speakerRect.anchorMin = new Vector2(0.03f, 0.68f);
        speakerRect.anchorMax = new Vector2(0.97f, 0.96f);
        speakerRect.offsetMin = Vector2.zero;
        speakerRect.offsetMax = Vector2.zero;

        TextMeshProUGUI speakerLabel = speakerObject.GetComponent<TextMeshProUGUI>();
        ConfigureRuntimeText(speakerLabel, 34f, FontStyles.Bold);

        GameObject dialogueObject = new GameObject("DialogueText", typeof(RectTransform), typeof(TextMeshProUGUI));
        dialogueObject.transform.SetParent(panelObject.transform, false);
        RectTransform dialogueRect = dialogueObject.GetComponent<RectTransform>();
        dialogueRect.anchorMin = new Vector2(0.03f, 0.08f);
        dialogueRect.anchorMax = new Vector2(0.97f, 0.7f);
        dialogueRect.offsetMin = Vector2.zero;
        dialogueRect.offsetMax = Vector2.zero;

        TextMeshProUGUI dialogueLabel = dialogueObject.GetComponent<TextMeshProUGUI>();
        ConfigureRuntimeText(dialogueLabel, 28f, FontStyles.Normal);
        dialogueLabel.textWrappingMode = TextWrappingModes.Normal;

        dialoguePanel = panelObject;
        speakerText = speakerLabel;
        dialogueText = dialogueLabel;

        _runtimeUICreated = true;
    }

    private static void ConfigureRuntimeText(TextMeshProUGUI textComponent, float fontSize, FontStyles fontStyle)
    {
        textComponent.text = string.Empty;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.TopLeft;

        if (TMP_Settings.defaultFontAsset != null)
        {
            textComponent.font = TMP_Settings.defaultFontAsset;
        }
    }

    private void ConfigurePanelVisibilityStrategy()
    {
        _hideWithCanvasGroup = false;
        _panelCanvasGroup = null;

        if (dialoguePanel == null)
        {
            return;
        }

        if (dialoguePanel == gameObject || transform.IsChildOf(dialoguePanel.transform))
        {
            _hideWithCanvasGroup = true;
            _panelCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
            if (_panelCanvasGroup == null)
            {
                _panelCanvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
            }

            if (!dialoguePanel.activeSelf)
            {
                dialoguePanel.SetActive(true);
            }
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
        if (dialoguePanel == null)
        {
            return;
        }

        if (_hideWithCanvasGroup && _panelCanvasGroup != null)
        {
            _panelCanvasGroup.alpha = isVisible ? 1f : 0f;
            _panelCanvasGroup.interactable = isVisible;
            _panelCanvasGroup.blocksRaycasts = isVisible;
            return;
        }

        dialoguePanel.SetActive(isVisible);
    }

    private void SyncWithDialogueState()
    {
        if (dialogueManager == null || !dialogueManager.IsDialogueActive)
        {
            return;
        }

        SetPanelVisible(true);

        if (dialogueManager.CurrentNode != null)
        {
            HandleDialogueLineShown(dialogueManager.CurrentNode);
        }
    }

    private void ClearText()
    {
        StopTypewriterAnimation(false);
        _currentLineText = string.Empty;

        if (speakerText != null)
        {
            speakerText.text = string.Empty;
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }
    }

    private void HandleAdvanceInput()
    {
        if (dialogueManager == null || !dialogueManager.IsDialogueActive)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        var keyControl = Keyboard.current[nextLineKey];
        if (_waitForAdvanceKeyRelease)
        {
            if (keyControl != null && !keyControl.isPressed)
            {
                _waitForAdvanceKeyRelease = false;
            }

            return;
        }

        if (keyControl != null && keyControl.wasPressedThisFrame)
        {
            OnNextPressed();
        }
    }

    private bool IsAdvanceKeyPressed()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        var keyControl = Keyboard.current[nextLineKey];
        return keyControl != null && keyControl.isPressed;
    }

    private bool TryCompleteCurrentLine()
    {
        if (!_isTypewriterRunning)
        {
            return false;
        }

        StopTypewriterAnimation(true);
        return true;
    }

    private void StartTypewriterAnimation(string lineText)
    {
        _currentLineText = lineText ?? string.Empty;
        StopTypewriterAnimation(false);

        if (dialogueText == null)
        {
            return;
        }

        _isTypewriterRunning = true;
        _typewriterCoroutine = StartCoroutine(TypewriterCoroutine(_currentLineText));
    }

    private void StopTypewriterAnimation(bool showCurrentLine)
    {
        if (_typewriterCoroutine != null)
        {
            StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = null;
        }

        _isTypewriterRunning = false;

        if (dialogueText == null)
        {
            return;
        }

        if (showCurrentLine)
        {
            dialogueText.text = _currentLineText;
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    private IEnumerator TypewriterCoroutine(string fullText)
    {
        if (dialogueText == null)
        {
            _isTypewriterRunning = false;
            _typewriterCoroutine = null;
            yield break;
        }

        float charsPerSecond = dialogueManager != null ? dialogueManager.TypewriterSpeed : 40f;
        if (charsPerSecond <= 0f)
        {
            dialogueText.text = fullText;
            dialogueText.maxVisibleCharacters = int.MaxValue;
            _isTypewriterRunning = false;
            _typewriterCoroutine = null;
            yield break;
        }

        dialogueText.text = fullText;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int visibleCharacterCount = dialogueText.textInfo.characterCount;
        if (visibleCharacterCount <= 0)
        {
            _isTypewriterRunning = false;
            _typewriterCoroutine = null;
            yield break;
        }

        float baseCharacterDelay = 1f / charsPerSecond;

        for (int i = 0; i < visibleCharacterCount; i++)
        {
            dialogueText.maxVisibleCharacters = i + 1;

            char visibleCharacter = dialogueText.textInfo.characterInfo[i].character;
            PlayTypewriterTick(visibleCharacter);

            float delay = GetCharacterDelay(visibleCharacter, baseCharacterDelay);
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }
            else
            {
                yield return null;
            }
        }

        dialogueText.maxVisibleCharacters = int.MaxValue;
        _isTypewriterRunning = false;
        _typewriterCoroutine = null;
    }

    private float GetCharacterDelay(char visibleCharacter, float baseCharacterDelay)
    {
        float delay = baseCharacterDelay;

        if (visibleCharacter == ',')
        {
            delay += Mathf.Max(0f, commaPunctuationPause);
            return delay;
        }

        if (visibleCharacter == '.' || visibleCharacter == '!' || visibleCharacter == '?')
        {
            delay += Mathf.Max(0f, sentencePunctuationPause);
        }

        return delay;
    }

    private void PlayTypewriterTick(char visibleCharacter)
    {
        if (char.IsWhiteSpace(visibleCharacter))
        {
            return;
        }

        if (typewriterAudioSource == null || typewriterTickClip == null)
        {
            return;
        }

        typewriterAudioSource.PlayOneShot(typewriterTickClip, typewriterTickVolume);
    }
}