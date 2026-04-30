using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public class SceneTransitionFader : MonoBehaviour
{
    private const string RootObjectName = "SceneTransitionFader";
    private const string CanvasObjectName = "FadeCanvas";
    private const string OverlayObjectName = "FadeOverlay";

    private static SceneTransitionFader _instance;

    [Header("Fade Settings")]
    [SerializeField] private float fadeOutDuration = 0.2f;
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private int sortingOrder = 32767;
    [SerializeField] private bool fadeInOnStartup;

    private Canvas _canvas;
    private CanvasGroup _canvasGroup;
    private Image _overlayImage;
    private bool _isTransitioning;
    private bool _hasPlayedStartupFade;

    public static bool IsTransitioning => _instance != null && _instance._isTransitioning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstance();
    }

    public static bool TryLoadSceneWithFade(string destinationSceneName)
    {
        if (string.IsNullOrWhiteSpace(destinationSceneName))
        {
            return false;
        }

        SceneTransitionFader instance = EnsureInstance();
        if (instance == null)
        {
            return false;
        }

        instance.BeginTransition(destinationSceneName.Trim());
        return true;
    }

    private static SceneTransitionFader EnsureInstance()
    {
        if (_instance != null)
        {
            return _instance;
        }

        _instance = FindFirstObjectByType<SceneTransitionFader>();
        if (_instance != null)
        {
            return _instance;
        }

        GameObject root = new GameObject(RootObjectName);
        _instance = root.AddComponent<SceneTransitionFader>();
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureOverlay();
        SetAlpha(0f, blockRaycasts: false);
    }

    private IEnumerator Start()
    {
        if (_hasPlayedStartupFade || !fadeInOnStartup)
        {
            yield break;
        }

        _hasPlayedStartupFade = true;
        SetAlpha(1f, blockRaycasts: true);
        yield return FadeTo(0f, fadeInDuration, keepRaycastBlock: false);
    }

    private void BeginTransition(string destinationSceneName)
    {
        if (_isTransitioning)
        {
            return;
        }

        StartCoroutine(TransitionRoutine(destinationSceneName));
    }

    private IEnumerator TransitionRoutine(string destinationSceneName)
    {
        _isTransitioning = true;
        EnsureOverlay();

        SetAlpha(_canvasGroup != null ? _canvasGroup.alpha : 0f, blockRaycasts: true);
        yield return FadeTo(1f, fadeOutDuration, keepRaycastBlock: true);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(destinationSceneName, LoadSceneMode.Single);
        if (loadOperation == null)
        {
            _isTransitioning = false;
            SetAlpha(0f, blockRaycasts: false);
            yield break;
        }

        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return null;

        var smoothFollow = FindFirstObjectByType<CameraSmoothFollow>();
        smoothFollow?.SnapImmediate();

        yield return null;

        yield return FadeTo(0f, fadeInDuration, keepRaycastBlock: false);

        _isTransitioning = false;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration, bool keepRaycastBlock)
    {
        if (_canvasGroup == null)
        {
            yield break;
        }

        float startAlpha = _canvasGroup.alpha;
        float normalizedDuration = Mathf.Max(0f, duration);

        if (normalizedDuration <= 0.0001f)
        {
            SetAlpha(targetAlpha, keepRaycastBlock);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < normalizedDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / normalizedDuration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(alpha, blockRaycasts: true);
            yield return null;
        }

        SetAlpha(targetAlpha, keepRaycastBlock);
    }

    private void EnsureOverlay()
    {
        if (_canvas != null && _canvasGroup != null && _overlayImage != null)
        {
            _canvas.sortingOrder = sortingOrder;
            _overlayImage.color = fadeColor;
            return;
        }

        Transform canvasTransform = transform.Find(CanvasObjectName);
        if (canvasTransform == null)
        {
            GameObject canvasObject = new GameObject(
                CanvasObjectName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CanvasGroup)
            );

            canvasObject.transform.SetParent(transform, false);
            canvasTransform = canvasObject.transform;
        }

        _canvas = canvasTransform.GetComponent<Canvas>();
        CanvasScaler scaler = canvasTransform.GetComponent<CanvasScaler>();
        GraphicRaycaster raycaster = canvasTransform.GetComponent<GraphicRaycaster>();
        _canvasGroup = canvasTransform.GetComponent<CanvasGroup>();

        if (_canvas != null)
        {
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = sortingOrder;
        }

        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (raycaster != null)
        {
            raycaster.ignoreReversedGraphics = true;
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = canvasTransform.gameObject.AddComponent<CanvasGroup>();
        }

        Transform overlayTransform = canvasTransform.Find(OverlayObjectName);
        if (overlayTransform == null)
        {
            GameObject overlayObject = new GameObject(OverlayObjectName, typeof(RectTransform), typeof(Image));
            overlayObject.transform.SetParent(canvasTransform, false);

            RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;

            overlayTransform = overlayObject.transform;
        }

        _overlayImage = overlayTransform.GetComponent<Image>();
        if (_overlayImage == null)
        {
            _overlayImage = overlayTransform.gameObject.AddComponent<Image>();
        }

        _overlayImage.color = fadeColor;
        _overlayImage.raycastTarget = false;

        SetAlpha(0f, blockRaycasts: false);
    }

    private void SetAlpha(float alpha, bool blockRaycasts)
    {
        if (_canvasGroup == null)
        {
            return;
        }

        _canvasGroup.alpha = Mathf.Clamp01(alpha);
        _canvasGroup.blocksRaycasts = blockRaycasts;
        _canvasGroup.interactable = blockRaycasts;
    }

    private void OnValidate()
    {
        fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        fadeInDuration = Mathf.Max(0f, fadeInDuration);
    }
}
