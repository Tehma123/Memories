using UnityEngine;
using UnityEngine.EventSystems;
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

        _owner.HandleCardClicked(this);
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
}