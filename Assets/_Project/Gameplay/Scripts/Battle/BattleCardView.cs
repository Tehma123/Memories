using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BattleCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField, Range(0f, 1f)] private float hoverLighten = 0.2f;
    [SerializeField, Range(0f, 1f)] private float armedLighten = 0.35f;
    [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.6f;
    [SerializeField, Min(1f)] private float hoverScalePercent = 103f;
    [SerializeField, Min(1f)] private float armedScalePercent = 106f;
    [SerializeField, Min(0f)] private float scaleTweenDuration = 0.08f;

    [Header("Played Card Dissolve")]
    [SerializeField, Min(0f)] private float dissolveDuration = 0.32f;
    [SerializeField, Range(10f, 100f)] private float dissolveTargetScalePercent = 72f;
    [SerializeField, Min(0f)] private float dissolveJitterPixels = 6f;
    [SerializeField, Min(0f)] private float slotCaptionHoldDuration = 0.45f;
    [SerializeField, Min(1f)] private float slotCaptionCharsPerSecond = 26f;
    [SerializeField, Min(8f)] private float slotCaptionFontSize = 18f;
    [SerializeField] private bool slotCaptionBold = false;
    [SerializeField] private TMP_FontAsset slotCaptionFont;

    [Header("Inspector Actions")]
    [SerializeField] private UnityEvent onPointerClicked;
    [SerializeField] private UnityEvent onDissolveStarted;
    [SerializeField] private UnityEvent onDissolveCompleted;

    private DeckManager _owner;
    private CardData _cardData;
    private Image _rootImage;
    private Outline _outline;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;

    private Color _baseColor = Color.white;
    private Vector3 _baseScale = Vector3.one;
    private bool _isHovered;
    private bool _isArmed;
    private bool _isInteractable = true;
    private int _activeScaleTweenId = -1;

    public CardData CardData => _cardData;

    public void Initialize(DeckManager owner, CardData cardData)
    {
        _owner = owner;
        _cardData = cardData;

        _rectTransform = transform as RectTransform;
        _rootImage = GetComponent<Image>();
        _outline = GetComponent<Outline>();
        _canvasGroup = GetComponent<CanvasGroup>();

        if (_rootImage != null)
        {
            _baseColor = _rootImage.color;
        }

        if (_rectTransform != null)
        {
            _baseScale = _rectTransform.localScale;
        }

        _isHovered = false;
        _isArmed = false;
        ApplyVisualState();
    }

    public void SetArmed(bool isArmed)
    {
        _isArmed = isArmed;
        ApplyVisualState();
    }

    public void SetInteractable(bool isInteractable)
    {
        _isInteractable = isInteractable;
        ApplyVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        ApplyVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        ApplyVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_isInteractable || _owner == null)
        {
            return;
        }

        onPointerClicked?.Invoke();
        _owner.HandleCardClicked(this);
    }

    public IEnumerator PlayDissolveAndShowSlotText(string caption)
    {
        onDissolveStarted?.Invoke();
        CancelScaleTween();
        _isInteractable = false;

        CanvasGroup workingCanvasGroup = _canvasGroup;
        if (workingCanvasGroup == null)
        {
            workingCanvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (workingCanvasGroup == null)
            {
                workingCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _canvasGroup = workingCanvasGroup;
        }

        RectTransform rect = _rectTransform != null ? _rectTransform : transform as RectTransform;
        Vector3 startScale = rect != null ? rect.localScale : Vector3.one;
        Vector2 startAnchoredPosition = rect != null ? rect.anchoredPosition : Vector2.zero;

        float duration = Mathf.Max(0f, dissolveDuration);
        if (duration <= 0f)
        {
            workingCanvasGroup.alpha = 0f;
            ToggleCardGraphics(false);
        }
        else
        {
            float elapsed = 0f;
            float targetScaleFactor = Mathf.Clamp01(dissolveTargetScalePercent / 100f);

            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = 1f - ((1f - t) * (1f - t));
                workingCanvasGroup.alpha = 1f - easedT;

                if (rect != null)
                {
                    float scaleFactor = Mathf.LerpUnclamped(1f, targetScaleFactor, easedT);
                    rect.localScale = startScale * scaleFactor;

                    Vector2 jitter = Random.insideUnitCircle * dissolveJitterPixels * (1f - t);
                    rect.anchoredPosition = startAnchoredPosition + jitter;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            workingCanvasGroup.alpha = 0f;
            if (rect != null)
            {
                rect.localScale = startScale * Mathf.Clamp01(dissolveTargetScalePercent / 100f);
                rect.anchoredPosition = startAnchoredPosition;
            }

            ToggleCardGraphics(false);
        }

        if (string.IsNullOrWhiteSpace(caption))
        {
            onDissolveCompleted?.Invoke();
            yield break;
        }

        Transform slotRoot = transform.parent;
        if (slotRoot == null)
        {
            onDissolveCompleted?.Invoke();
            yield break;
        }

        GameObject captionObject = new GameObject("PlayedCardCaption", typeof(RectTransform), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        RectTransform captionRect = captionObject.GetComponent<RectTransform>();
        captionRect.SetParent(slotRoot, false);
        captionRect.anchorMin = Vector2.zero;
        captionRect.anchorMax = Vector2.one;
        captionRect.offsetMin = Vector2.zero;
        captionRect.offsetMax = Vector2.zero;

        CanvasGroup captionCanvasGroup = captionObject.GetComponent<CanvasGroup>();
        captionCanvasGroup.alpha = 1f;

        TextMeshProUGUI captionLabel = captionObject.GetComponent<TextMeshProUGUI>();
        captionLabel.text = caption.Trim();
        captionLabel.maxVisibleCharacters = 0;
        captionLabel.alignment = TextAlignmentOptions.Center;
        captionLabel.fontSize = slotCaptionFontSize;
        captionLabel.fontStyle = slotCaptionBold ? FontStyles.Bold : FontStyles.Normal;
        if (slotCaptionFont != null)
        {
            captionLabel.font = slotCaptionFont;
        }

        captionLabel.textWrappingMode = TextWrappingModes.Normal;
        captionLabel.color = Color.white;
        captionLabel.raycastTarget = false;
        captionLabel.ForceMeshUpdate();

        int visibleCharacterCount = captionLabel.textInfo.characterCount;
        if (visibleCharacterCount <= 0)
        {
            visibleCharacterCount = captionLabel.text.Length;
        }

        if (visibleCharacterCount <= 0)
        {
            if (Application.isPlaying)
            {
                Destroy(captionObject);
            }
            else
            {
                DestroyImmediate(captionObject);
            }

            onDissolveCompleted?.Invoke();
            yield break;
        }

        float characterDelay = 1f / Mathf.Max(1f, slotCaptionCharsPerSecond);
        for (int i = 0; i < visibleCharacterCount; i++)
        {
            captionLabel.maxVisibleCharacters = i + 1;
            yield return new WaitForSecondsRealtime(characterDelay);
        }

        if (slotCaptionHoldDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(slotCaptionHoldDuration);
        }

        float fadeElapsed = 0f;
        const float captionFadeDuration = 0.15f;
        while (fadeElapsed < captionFadeDuration)
        {
            float t = Mathf.Clamp01(fadeElapsed / captionFadeDuration);
            captionCanvasGroup.alpha = 1f - t;
            fadeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (Application.isPlaying)
        {
            Destroy(captionObject);
        }
        else
        {
            DestroyImmediate(captionObject);
        }

        onDissolveCompleted?.Invoke();
    }

    private void OnDisable()
    {
        CancelScaleTween();
    }

    private void OnDestroy()
    {
        CancelScaleTween();
    }

    private void ApplyVisualState()
    {
        float lighten = _isArmed ? armedLighten : (_isHovered ? hoverLighten : 0f);

        if (_rootImage != null)
        {
            _rootImage.color = Color.Lerp(_baseColor, Color.white, Mathf.Clamp01(lighten));
        }

        if (_outline != null)
        {
            Color color = _outline.effectColor;
            color.a = _isArmed ? 0.9f : (_isHovered ? 0.6f : 0.35f);
            _outline.effectColor = color;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = _isInteractable ? 1f : Mathf.Clamp01(disabledAlpha);
        }

        if (_rectTransform != null)
        {
            float targetScalePercent = _isArmed ? armedScalePercent : (_isHovered ? hoverScalePercent : 100f);
            Vector3 targetScale = _baseScale * (targetScalePercent / 100f);

            if (!_isInteractable)
            {
                targetScale = _baseScale;
            }

            if (scaleTweenDuration <= 0f)
            {
                _rectTransform.localScale = targetScale;
                return;
            }

            CancelScaleTween();
            _activeScaleTweenId = LeanTween.scale(_rectTransform, targetScale, scaleTweenDuration)
                .setEase(LeanTweenType.easeOutQuad)
                .setIgnoreTimeScale(true)
                .setOnComplete(() => _activeScaleTweenId = -1)
                .id;
        }
    }

    private void CancelScaleTween()
    {
        if (_activeScaleTweenId < 0)
        {
            return;
        }

        LeanTween.cancel(_activeScaleTweenId);
        _activeScaleTweenId = -1;
    }

    private void ToggleCardGraphics(bool isVisible)
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
            {
                continue;
            }

            if (graphic.transform.IsChildOf(transform))
            {
                graphic.enabled = isVisible;
            }
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = isVisible;
            _canvasGroup.interactable = isVisible;
        }
    }
}