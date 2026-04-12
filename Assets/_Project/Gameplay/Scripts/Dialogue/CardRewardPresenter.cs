using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CardRewardPresentationData
{
    public CardRewardPresentationData(Sprite sprite, string caption)
    {
        Sprite = sprite;
        Caption = caption ?? string.Empty;
    }

    public Sprite Sprite { get; }
    public string Caption { get; }
}

public class CardRewardPresenter : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform overlayRoot;
    [SerializeField] private RectTransform cardRoot;
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text captionText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private DialogueUIManager dialogueUIManager;

    [Header("Behavior")]
    [SerializeField] private bool hideOnStart = true;

    [Header("Animation")]
    [SerializeField] private Vector2 flyInOffset = new Vector2(0f, -260f);
    [SerializeField] private Vector2 fadeOutOffset = new Vector2(0f, 80f);
    [SerializeField] private float flyInDuration = 0.45f;
    [SerializeField] private float holdDuration = 1.4f;
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private float pauseBetweenCards = 0.2f;
    [SerializeField] [Range(0.1f, 1.5f)] private float startScale = 0.85f;

    [Header("LeanTween Easing")]
    [SerializeField] private LeanTweenType flyInEase = LeanTweenType.easeInOutSine;
    [SerializeField] private LeanTweenType fadeOutEase = LeanTweenType.easeInOutSine;

    [Header("Caption Typewriter")]
    [SerializeField] private bool enableCaptionTypewriter = true;
    [SerializeField] [Min(0f)] private float captionCharsPerSecond = 20f;
    [SerializeField] [Min(0f)] private float captionSentencePause = 0.08f;
    [SerializeField] [Min(0f)] private float captionCommaPause = 0.04f;

    private Coroutine _sequenceCoroutine;
    private Vector2 _restCardPosition;
    private bool _missingReferencesWarningLogged;
    private int _activeTweenId = -1;

    public bool IsPlaying => _sequenceCoroutine != null;

    public event Action OnSequenceCompleted;

    private void Awake()
    {
        EnsureBindings();

        if (hideOnStart)
        {
            SetVisible(false);
        }
    }

    private void OnDisable()
    {
        StopCurrentSequence();
        SetVisible(false);
    }

    public void PlaySequence(IReadOnlyList<CardRewardPresentationData> cards, Action onCompleted = null)
    {
        if (cards == null || cards.Count == 0)
        {
            onCompleted?.Invoke();
            OnSequenceCompleted?.Invoke();
            return;
        }

        EnsureBindings();
        if (overlayRoot == null || cardRoot == null || cardImage == null)
        {
            onCompleted?.Invoke();
            OnSequenceCompleted?.Invoke();
            return;
        }

        StopCurrentSequence();
        _sequenceCoroutine = StartCoroutine(PlaySequenceRoutine(cards, onCompleted));
    }

    public void StopSequence()
    {
        StopCurrentSequence();
        SetVisible(false);
    }

    private IEnumerator PlaySequenceRoutine(IReadOnlyList<CardRewardPresentationData> cards, Action onCompleted)
    {
        SetVisible(true);

        for (int i = 0; i < cards.Count; i++)
        {
            CardRewardPresentationData card = cards[i];
            if (card == null)
            {
                continue;
            }

            yield return AnimateSingleCard(card);
        }

        SetVisible(false);
        _sequenceCoroutine = null;

        onCompleted?.Invoke();
        OnSequenceCompleted?.Invoke();
    }

    private IEnumerator AnimateSingleCard(CardRewardPresentationData card)
    {
        string caption = card.Caption ?? string.Empty;
        bool hasCaption = false;

        cardImage.sprite = card.Sprite;
        cardImage.preserveAspect = true;
        cardImage.enabled = card.Sprite != null;

        if (captionText != null)
        {
            hasCaption = !string.IsNullOrWhiteSpace(caption);
            captionText.gameObject.SetActive(hasCaption);
            captionText.text = hasCaption ? caption : string.Empty;
            captionText.maxVisibleCharacters = hasCaption ? 0 : int.MaxValue;
        }

        Vector2 startPosition = _restCardPosition + flyInOffset;
        Vector2 endPosition = _restCardPosition;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        cardRoot.anchoredPosition = startPosition;
        cardRoot.localScale = Vector3.one * startScale;

        yield return AnimateIn(startPosition, endPosition);

        if (hasCaption)
        {
            yield return TypeCaption(caption);
        }

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdDuration);
        }

        yield return AnimateOut(endPosition, endPosition + fadeOutOffset);

        if (pauseBetweenCards > 0f)
        {
            yield return new WaitForSecondsRealtime(pauseBetweenCards);
        }
    }

    private IEnumerator AnimateIn(Vector2 from, Vector2 to)
    {
        if (flyInDuration <= 0f)
        {
            cardRoot.anchoredPosition = to;
            cardRoot.localScale = Vector3.one;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            yield break;
        }

        bool tweenCompleted = false;
        CancelActiveTween();

        _activeTweenId = LeanTween.value(gameObject, 0f, 1f, flyInDuration)
            .setEase(flyInEase)
            .setIgnoreTimeScale(true)
            .setOnUpdate((float progress) =>
            {
                cardRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, progress);
                cardRoot.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, progress);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = progress;
                }
            })
            .setOnComplete(() =>
            {
                tweenCompleted = true;
                _activeTweenId = -1;
            })
            .id;

        while (!tweenCompleted)
        {
            yield return null;
        }

        cardRoot.anchoredPosition = to;
        cardRoot.localScale = Vector3.one;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    private IEnumerator AnimateOut(Vector2 from, Vector2 to)
    {
        if (fadeOutDuration <= 0f)
        {
            cardRoot.anchoredPosition = to;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            yield break;
        }

        bool tweenCompleted = false;
        CancelActiveTween();

        _activeTweenId = LeanTween.value(gameObject, 0f, 1f, fadeOutDuration)
            .setEase(fadeOutEase)
            .setIgnoreTimeScale(true)
            .setOnUpdate((float progress) =>
            {
                cardRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, progress);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.LerpUnclamped(1f, 0f, progress);
                }
            })
            .setOnComplete(() =>
            {
                tweenCompleted = true;
                _activeTweenId = -1;
            })
            .id;

        while (!tweenCompleted)
        {
            yield return null;
        }

        cardRoot.anchoredPosition = to;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void StopCurrentSequence()
    {
        if (_sequenceCoroutine != null)
        {
            StopCoroutine(_sequenceCoroutine);
            _sequenceCoroutine = null;
        }

        CancelActiveTween();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void CancelActiveTween()
    {
        if (_activeTweenId < 0)
        {
            return;
        }

        LeanTween.cancel(_activeTweenId);
        _activeTweenId = -1;
    }

    private void EnsureBindings()
    {
        if (overlayRoot != null && cardRoot != null && cardImage != null)
        {
            EnsureCanvasGroup();
            _restCardPosition = cardRoot.anchoredPosition;
            return;
        }

        if (!_missingReferencesWarningLogged)
        {
            Debug.LogWarning("CardRewardPresenter is missing required UI references. Assign Overlay Root, Card Root, and Card Image in the Inspector.");
            _missingReferencesWarningLogged = true;
        }
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

    private void SetVisible(bool isVisible)
    {
        if (overlayRoot == null)
        {
            return;
        }

        overlayRoot.gameObject.SetActive(isVisible);
        if (!isVisible && canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private IEnumerator TypeCaption(string fullCaption)
    {
        if (captionText == null)
        {
            yield break;
        }

        captionText.text = fullCaption ?? string.Empty;
        captionText.maxVisibleCharacters = 0;
        captionText.ForceMeshUpdate();

        int visibleCharacterCount = captionText.textInfo.characterCount;
        if (!enableCaptionTypewriter || captionCharsPerSecond <= 0f || visibleCharacterCount <= 0)
        {
            captionText.maxVisibleCharacters = int.MaxValue;
            yield break;
        }

        float baseCharacterDelay = 1f / captionCharsPerSecond;

        for (int i = 0; i < visibleCharacterCount; i++)
        {
            captionText.maxVisibleCharacters = i + 1;

            char visibleCharacter = captionText.textInfo.characterInfo[i].character;
            PlayCaptionTick(visibleCharacter);
            float delay = GetCaptionCharacterDelay(visibleCharacter, baseCharacterDelay);
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }
            else
            {
                yield return null;
            }
        }

        captionText.maxVisibleCharacters = int.MaxValue;
    }

    private float GetCaptionCharacterDelay(char visibleCharacter, float baseCharacterDelay)
    {
        float delay = baseCharacterDelay;

        if (visibleCharacter == ',')
        {
            delay += captionCommaPause;
            return delay;
        }

        if (visibleCharacter == '.' || visibleCharacter == '!' || visibleCharacter == '?')
        {
            delay += captionSentencePause;
        }

        return delay;
    }

    private void PlayCaptionTick(char visibleCharacter)
    {
        if (dialogueUIManager == null)
        {
            dialogueUIManager = FindFirstObjectByType<DialogueUIManager>();
        }

        dialogueUIManager?.PlaySharedTypewriterTick(visibleCharacter);
    }
}
