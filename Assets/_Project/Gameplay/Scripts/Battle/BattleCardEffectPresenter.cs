using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class BattleCardEffectPresenter : MonoBehaviour
{
    [Header("Optional UI Bindings")]
    [SerializeField] private RectTransform overlayRoot;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] private Vector2 flyInOffset = new Vector2(0f, -70f);
    [SerializeField] private Vector2 flyOutOffset = new Vector2(0f, 70f);
    [SerializeField] private float flyInDuration = 0.2f;
    [SerializeField] private float holdDuration = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private LeanTweenType flyInEase = LeanTweenType.easeOutQuad;
    [SerializeField] private LeanTweenType fadeOutEase = LeanTweenType.easeInQuad;

    [Header("Typewriter")]
    [SerializeField] private bool enableTypewriter = true;
    [SerializeField, Min(1f)] private float typewriterCharsPerSecond = 30f;
    [SerializeField] private float commaPause = 0.03f;
    [SerializeField] private float sentencePause = 0.08f;
    [SerializeField] private DialogueUIManager dialogueUIManager;

    private readonly Queue<string> _lineQueue = new Queue<string>();
    private Coroutine _playRoutine;
    private Vector2 _restPosition;

    public void PlayForCard(CardData cardData)
    {
        if (cardData == null)
        {
            return;
        }

        string line = cardData.GetRandomCombatEffectLine();
        PlayLine(line);
    }

    public void PlayLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        if (!EnsureUi())
        {
            return;
        }

        _lineQueue.Enqueue(line.Trim());

        if (_playRoutine == null)
        {
            _playRoutine = StartCoroutine(PlayQueueRoutine());
        }
    }

    private void OnDisable()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        if (overlayRoot != null)
        {
            overlayRoot.gameObject.SetActive(false);
        }

        _lineQueue.Clear();
    }

    private IEnumerator PlayQueueRoutine()
    {
        while (_lineQueue.Count > 0)
        {
            string line = _lineQueue.Dequeue();
            yield return PlaySingleLine(line);
        }

        _playRoutine = null;
    }

    private IEnumerator PlaySingleLine(string line)
    {
        if (overlayRoot == null || effectText == null)
        {
            yield break;
        }

        overlayRoot.gameObject.SetActive(true);

        Vector2 startPosition = _restPosition + flyInOffset;
        Vector2 centerPosition = _restPosition;
        Vector2 endPosition = _restPosition + flyOutOffset;

        overlayRoot.anchoredPosition = startPosition;
        SetOverlayAlpha(0f);

        yield return AnimateInWithTypewriter(line, startPosition, centerPosition);

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        yield return AnimateOut(centerPosition, endPosition);
        overlayRoot.gameObject.SetActive(false);
    }

    private IEnumerator AnimateOut(Vector2 fromPosition, Vector2 toPosition)
    {
        float duration = Mathf.Max(0f, fadeOutDuration);
        if (duration <= 0f)
        {
            overlayRoot.anchoredPosition = toPosition;
            SetOverlayAlpha(0f);

            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float linearT = Mathf.Clamp01(elapsed / duration);
            float easedT = EvaluateEase(fadeOutEase, linearT);

            overlayRoot.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, easedT);
            SetOverlayAlpha(Mathf.LerpUnclamped(1f, 0f, easedT));

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        overlayRoot.anchoredPosition = toPosition;
        SetOverlayAlpha(0f);
    }

    private IEnumerator AnimateInWithTypewriter(string fullLine, Vector2 startPosition, Vector2 centerPosition)
    {
        if (effectText == null)
        {
            yield break;
        }

        effectText.text = fullLine ?? string.Empty;
        effectText.maxVisibleCharacters = 0;
        effectText.ForceMeshUpdate();

        int visibleCharacterCount = effectText.textInfo.characterCount;
        bool shouldType = enableTypewriter && typewriterCharsPerSecond > 0f && visibleCharacterCount > 0;
        if (!shouldType)
        {
            effectText.maxVisibleCharacters = int.MaxValue;
        }

        float duration = Mathf.Max(0f, flyInDuration);
        float elapsed = 0f;
        float baseDelay = shouldType ? 1f / typewriterCharsPerSecond : 0f;
        float delayRemaining = 0f;
        int visibleIndex = 0;

        while (true)
        {
            float linearT = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float easedT = EvaluateEase(flyInEase, linearT);
            overlayRoot.anchoredPosition = Vector2.LerpUnclamped(startPosition, centerPosition, easedT);
            SetOverlayAlpha(easedT);

            if (shouldType)
            {
                delayRemaining -= Time.unscaledDeltaTime;
                while (delayRemaining <= 0f && visibleIndex < visibleCharacterCount)
                {
                    effectText.maxVisibleCharacters = visibleIndex + 1;

                    char visibleCharacter = effectText.textInfo.characterInfo[visibleIndex].character;
                    PlayTypewriterTick(visibleCharacter);
                    visibleIndex++;

                    float characterDelay = GetTypewriterDelay(visibleCharacter, baseDelay);
                    if (characterDelay > 0f)
                    {
                        delayRemaining += characterDelay;
                        break;
                    }
                }

                if (visibleIndex >= visibleCharacterCount)
                {
                    effectText.maxVisibleCharacters = int.MaxValue;
                    shouldType = false;
                }
            }

            bool tweenDone = duration <= 0f || elapsed >= duration;
            if (tweenDone && !shouldType)
            {
                break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        overlayRoot.anchoredPosition = centerPosition;
        SetOverlayAlpha(1f);

        effectText.maxVisibleCharacters = int.MaxValue;
    }

    private float GetTypewriterDelay(char visibleCharacter, float baseDelay)
    {
        float delay = baseDelay;

        if (visibleCharacter == ',')
        {
            delay += Mathf.Max(0f, commaPause);
            return delay;
        }

        if (visibleCharacter == '.' || visibleCharacter == '!' || visibleCharacter == '?')
        {
            delay += Mathf.Max(0f, sentencePause);
        }

        return delay;
    }

    private void PlayTypewriterTick(char visibleCharacter)
    {
        if (dialogueUIManager == null)
        {
            dialogueUIManager = FindFirstObjectByType<DialogueUIManager>();
        }

        dialogueUIManager?.PlaySharedTypewriterTick(visibleCharacter);
    }

    private float EvaluateEase(LeanTweenType ease, float t)
    {
        float clampedT = Mathf.Clamp01(t);

        switch (ease)
        {
            case LeanTweenType.easeInQuad:
                return clampedT * clampedT;

            case LeanTweenType.easeOutQuad:
                return 1f - ((1f - clampedT) * (1f - clampedT));

            case LeanTweenType.easeInOutSine:
                return 0.5f - (Mathf.Cos(Mathf.PI * clampedT) * 0.5f);

            case LeanTweenType.easeInOutQuad:
                return clampedT < 0.5f
                    ? 2f * clampedT * clampedT
                    : 1f - (Mathf.Pow(-2f * clampedT + 2f, 2f) * 0.5f);

            default:
                return clampedT;
        }
    }

    private void SetOverlayAlpha(float alpha)
    {
        float clampedAlpha = Mathf.Clamp01(alpha);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = clampedAlpha;
        }
    }

    private bool EnsureUi()
    {
        if (overlayRoot != null && effectText != null)
        {
            EnsureCanvasGroup();
            _restPosition = overlayRoot.anchoredPosition;
            return true;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("BattleCardEffectPresenter could not find a Canvas to render effect lines.");
            return false;
        }

        GameObject overlayObject = new GameObject("CardEffectOverlay", typeof(RectTransform), typeof(CanvasGroup));
        overlayRoot = overlayObject.GetComponent<RectTransform>();
        overlayRoot.SetParent(canvas.transform, false);
        overlayRoot.anchorMin = new Vector2(0.2f, 0.64f);
        overlayRoot.anchorMax = new Vector2(0.8f, 0.84f);
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;
        overlayRoot.anchoredPosition = Vector2.zero;

        GameObject textObject = new GameObject("CardEffectText", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(overlayRoot, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        effectText = textObject.GetComponent<TextMeshProUGUI>();
        effectText.text = string.Empty;
        effectText.enableWordWrapping = true;
        effectText.alignment = TextAlignmentOptions.Center;
        effectText.fontSize = 34f;
        effectText.color = Color.white;

        EnsureCanvasGroup();
        _restPosition = overlayRoot.anchoredPosition;
        overlayRoot.gameObject.SetActive(false);
        return true;
    }

    private void EnsureCanvasGroup()
    {
        if (overlayRoot == null)
        {
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = overlayRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = overlayRoot.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

}