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
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform overlayRoot;
    [SerializeField] private RectTransform cardRoot;
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text captionText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Behavior")]
    [SerializeField] private bool autoCreateRuntimeUIIfMissing = true;
    [SerializeField] private bool hideOnStart = true;

    [Header("Animation")]
    [SerializeField] private Vector2 flyInOffset = new Vector2(0f, -260f);
    [SerializeField] private Vector2 fadeOutOffset = new Vector2(0f, 80f);
    [SerializeField] private float flyInDuration = 0.45f;
    [SerializeField] private float holdDuration = 1.4f;
    [SerializeField] private float fadeOutDuration = 0.35f;
    [SerializeField] private float pauseBetweenCards = 0.2f;
    [SerializeField] [Range(0.1f, 1.5f)] private float startScale = 0.85f;

    private Coroutine _sequenceCoroutine;
    private Vector2 _restCardPosition;

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

        cardImage.sprite = card.Sprite;
        cardImage.preserveAspect = true;
        cardImage.enabled = card.Sprite != null;

        if (captionText != null)
        {
            bool hasCaption = !string.IsNullOrWhiteSpace(caption);
            captionText.gameObject.SetActive(hasCaption);
            captionText.text = caption;
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

        float elapsed = 0f;
        while (elapsed < flyInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flyInDuration);

            float moveEase = EaseOutBack(t);
            float fadeEase = EaseOutSine(t);

            cardRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, moveEase);
            cardRoot.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, 1f, moveEase);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = fadeEase;
            }

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

        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            float eased = EaseInSine(t);

            cardRoot.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.LerpUnclamped(1f, 0f, eased);
            }

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

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private void EnsureBindings()
    {
        if (overlayRoot != null && cardRoot != null && cardImage != null)
        {
            EnsureCanvasGroup();
            _restCardPosition = cardRoot.anchoredPosition;
            return;
        }

        if (!autoCreateRuntimeUIIfMissing)
        {
            return;
        }

        CreateRuntimeUIIfMissing();
        EnsureCanvasGroup();

        if (cardRoot != null)
        {
            _restCardPosition = cardRoot.anchoredPosition;
        }
    }

    private void CreateRuntimeUIIfMissing()
    {
        if (rootCanvas == null)
        {
            GameObject canvasObject = new GameObject("RuntimeCardRewardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rootCanvas = canvasObject.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 700;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (overlayRoot == null)
        {
            GameObject overlayObject = new GameObject("CardRewardOverlay", typeof(RectTransform), typeof(CanvasGroup));
            overlayObject.transform.SetParent(rootCanvas.transform, false);
            overlayRoot = overlayObject.GetComponent<RectTransform>();
            overlayRoot.anchorMin = Vector2.zero;
            overlayRoot.anchorMax = Vector2.one;
            overlayRoot.offsetMin = Vector2.zero;
            overlayRoot.offsetMax = Vector2.zero;
            canvasGroup = overlayObject.GetComponent<CanvasGroup>();
        }

        if (cardRoot == null)
        {
            GameObject cardObject = new GameObject("CardImageRoot", typeof(RectTransform), typeof(Image));
            cardObject.transform.SetParent(overlayRoot, false);
            cardRoot = cardObject.GetComponent<RectTransform>();
            cardRoot.anchorMin = new Vector2(0.5f, 0.56f);
            cardRoot.anchorMax = new Vector2(0.5f, 0.56f);
            cardRoot.pivot = new Vector2(0.5f, 0.5f);
            cardRoot.sizeDelta = new Vector2(360f, 520f);
            cardRoot.anchoredPosition = Vector2.zero;

            cardImage = cardObject.GetComponent<Image>();
            cardImage.color = Color.white;
            cardImage.preserveAspect = true;
        }

        if (captionText == null)
        {
            GameObject captionObject = new GameObject("CardCaption", typeof(RectTransform), typeof(TextMeshProUGUI));
            captionObject.transform.SetParent(overlayRoot, false);

            RectTransform captionRect = captionObject.GetComponent<RectTransform>();
            captionRect.anchorMin = new Vector2(0.08f, 0.08f);
            captionRect.anchorMax = new Vector2(0.92f, 0.2f);
            captionRect.offsetMin = Vector2.zero;
            captionRect.offsetMax = Vector2.zero;

            TextMeshProUGUI runtimeCaption = captionObject.GetComponent<TextMeshProUGUI>();
            runtimeCaption.text = string.Empty;
            runtimeCaption.fontSize = 34f;
            runtimeCaption.color = Color.white;
            runtimeCaption.alignment = TextAlignmentOptions.Center;
            runtimeCaption.textWrappingMode = TextWrappingModes.Normal;

            if (TMP_Settings.defaultFontAsset != null)
            {
                runtimeCaption.font = TMP_Settings.defaultFontAsset;
            }

            captionText = runtimeCaption;
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

    private static float EaseOutSine(float t)
    {
        return Mathf.Sin(t * Mathf.PI * 0.5f);
    }

    private static float EaseInSine(float t)
    {
        return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
    }

    private static float EaseOutBack(float t)
    {
        const float overshoot = 1.70158f;
        float x = t - 1f;
        return 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
    }
}
