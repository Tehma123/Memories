using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections;

public class EnemyController : MonoBehaviour, IDamageable, IPointerClickHandler
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private int currentHealth;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private DeckManager deckManager;

    [Header("Targeting Debug")]
    [SerializeField] private bool debugHealthLogs = true;
    [SerializeField] private Color targetableTint = new Color(1f, 0.82f, 0.82f, 1f);
    [SerializeField, Range(0f, 1f)] private float targetOutlineAlpha = 0.85f;

    [Header("Defeat Visual")]
    [SerializeField, Min(0f)] private float defeatFadeDuration = 0.3f;
    [SerializeField, Range(10f, 100f)] private float defeatTargetScalePercent = 64f;
    [SerializeField, Min(0f)] private float defeatJitterPixels = 4f;
    [SerializeField] private bool hideEnemyOnDefeat = true;



    private Image _image;
    private Outline _outline;
    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;
    private Color _defaultImageColor = Color.white;
    private Vector3 _aliveScale = Vector3.one;
    private Vector2 _aliveAnchoredPosition = Vector2.zero;
    private Coroutine _defeatPresentationRoutine;

    public EnemyData Data => enemyData;
    public int CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0;

    public event Action<EnemyController, int, int> OnHealthChanged;
    public event Action<EnemyController> OnDefeated;

    private void Awake()
    {
        _image = GetComponent<Image>();
        if (_image != null)
        {
            _defaultImageColor = _image.color;
        }

        _rectTransform = transform as RectTransform;
        if (_rectTransform != null)
        {
            _aliveScale = _rectTransform.localScale;
            _aliveAnchoredPosition = _rectTransform.anchoredPosition;
        }

        _canvasGroup = GetComponent<CanvasGroup>();

        _outline = GetComponent<Outline>();
        if (_outline != null)
        {
            Color color = _outline.effectColor;
            color.a = 0f;
            _outline.effectColor = color;
        }

        if (enemyData != null && currentHealth <= 0)
        {
            currentHealth = Mathf.Max(1, enemyData.maxHealth);
        }

        ResetAliveVisualState();
        SetTargetSelectionState(false);
    }

    public void Initialize(EnemyData enemyData)
    {
        this.enemyData = enemyData;

        if (_defeatPresentationRoutine != null)
        {
            StopCoroutine(_defeatPresentationRoutine);
            _defeatPresentationRoutine = null;
        }

        currentHealth = this.enemyData != null ? Mathf.Max(1, this.enemyData.maxHealth) : 1;
        ResetAliveVisualState();
        NotifyHealthChanged();
        SetTargetSelectionState(false);
    }

    private void OnDisable()
    {
        if (_defeatPresentationRoutine == null)
        {
            return;
        }

        StopCoroutine(_defeatPresentationRoutine);
        _defeatPresentationRoutine = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsAlive)
        {
            return;
        }

        deckManager?.HandleEnemyClicked(this);
    }

    public void SetRuntimeReferences(BattleManager battleManagerReference, DeckManager deckManagerReference)
    {
        battleManager = battleManagerReference;
        deckManager = deckManagerReference;
    }

    public void SetTargetSelectionState(bool isTargetable)
    {
        bool canBeTargeted = isTargetable && IsAlive;

        if (_image != null)
        {
            _image.color = canBeTargeted ? targetableTint : _defaultImageColor;
        }

        if (_outline != null)
        {
            Color color = _outline.effectColor;
            color.a = canBeTargeted ? targetOutlineAlpha : 0f;
            _outline.effectColor = color;
        }
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        NotifyHealthChanged();

        if (debugHealthLogs)
        {
            Debug.Log($"Enemy '{GetEnemyDebugName()}' took {amount} damage ({previousHealth} -> {currentHealth}).");
        }

        if (currentHealth <= 0)
        {
            HandleDefeat();
        }
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        int maxHealth = enemyData != null ? Mathf.Max(1, enemyData.maxHealth) : 1;
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        NotifyHealthChanged();

        if (debugHealthLogs)
        {
            Debug.Log($"Enemy '{GetEnemyDebugName()}' healed {amount} HP ({previousHealth} -> {currentHealth}).");
        }
    }

    public bool TakeTurn()
    {
        if (!IsAlive || battleManager == null)
        {
            return false;
        }

        BattleContext context = battleManager.CurrentContext;
        if (context == null)
        {
            return false;
        }

        EnemyMoveData move = enemyData != null ? enemyData.GetRandomMove(context.Random) : null;
        if (move == null)
        {
            context.MemoryManager?.ChangeMemory(-1f);
            return false;
        }

        int value = move.GetRoll(context.Random);
        bool isAttackMove = move.moveType == EnemyMoveType.Attack || move.moveType == EnemyMoveType.AttackMemory;

        switch (move.moveType)
        {
            case EnemyMoveType.Attack:
            case EnemyMoveType.AttackMemory:
                context.MemoryManager?.ChangeMemory(-Mathf.Max(0, value));
                break;

            case EnemyMoveType.HealSelf:
                Heal(Mathf.Max(0, value));
                break;

            case EnemyMoveType.ApplyStateToPlayer:
                if (context.PlayerObject != null && !string.IsNullOrWhiteSpace(move.statusId))
                {
                    context.StateManager?.ApplyState(context.PlayerObject, move.statusId, Mathf.Max(1, move.durationTurns));
                }
                break;

            case EnemyMoveType.BuffSelf:
                if (!string.IsNullOrWhiteSpace(move.statusId))
                {
                    context.StateManager?.ApplyState(gameObject, move.statusId, Mathf.Max(1, move.durationTurns));
                }
                break;
        }

        Debug.Log($"Enemy '{name}' used move '{move.displayName}' (value {value}).");
        return isAttackMove;
    }

    private void NotifyHealthChanged()
    {
        int maxHealth = enemyData != null ? Mathf.Max(1, enemyData.maxHealth) : 1;
        OnHealthChanged?.Invoke(this, currentHealth, maxHealth);

        if (debugHealthLogs)
        {
            Debug.Log($"[Enemy HP] {GetEnemyDebugName()}: {currentHealth}/{maxHealth}");
        }
    }

    private void HandleDefeat()
    {
        SetTargetSelectionState(false);
        OnDefeated?.Invoke(this);

        if (_defeatPresentationRoutine != null)
        {
            StopCoroutine(_defeatPresentationRoutine);
            _defeatPresentationRoutine = null;
        }

        if (isActiveAndEnabled)
        {
            _defeatPresentationRoutine = StartCoroutine(PlayDefeatPresentationRoutine());
        }
        else
        {
            ApplyDefeatedVisualState();
        }

        Debug.Log($"Enemy '{name}' was defeated.");
    }

    private IEnumerator PlayDefeatPresentationRoutine()
    {
        EnsureCanvasGroup();
        SetRaycastState(false);

        RectTransform rect = _rectTransform != null ? _rectTransform : transform as RectTransform;
        Vector3 startScale = rect != null ? rect.localScale : transform.localScale;
        Vector2 startAnchoredPosition = rect != null ? rect.anchoredPosition : Vector2.zero;

        float duration = Mathf.Max(0f, defeatFadeDuration);
        if (duration <= 0f)
        {
            ApplyDefeatedVisualState();
            _defeatPresentationRoutine = null;
            yield break;
        }

        float elapsed = 0f;
        float targetScaleFactor = Mathf.Clamp01(defeatTargetScalePercent / 100f);

        while (elapsed < duration)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = 1f - ((1f - t) * (1f - t));

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f - easedT;
            }

            float scaleFactor = Mathf.LerpUnclamped(1f, targetScaleFactor, easedT);
            if (rect != null)
            {
                rect.localScale = startScale * scaleFactor;
                Vector2 jitter = UnityEngine.Random.insideUnitCircle * defeatJitterPixels * (1f - t);
                rect.anchoredPosition = startAnchoredPosition + jitter;
            }
            else
            {
                transform.localScale = startScale * scaleFactor;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (rect != null)
        {
            rect.localScale = startScale * targetScaleFactor;
            rect.anchoredPosition = startAnchoredPosition;
        }
        else
        {
            transform.localScale = startScale * targetScaleFactor;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
        }

        ApplyDefeatedVisualState();
        _defeatPresentationRoutine = null;
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup != null)
        {
            return;
        }

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void ApplyDefeatedVisualState()
    {
        if (_outline != null)
        {
            Color outlineColor = _outline.effectColor;
            outlineColor.a = 0f;
            _outline.effectColor = outlineColor;
        }

        if (_image != null)
        {
            Color imageColor = _image.color;
            imageColor.a = 0f;
            _image.color = imageColor;
        }

        SetRaycastState(false);

        if (hideEnemyOnDefeat)
        {
            gameObject.SetActive(false);
        }
    }

    private void ResetAliveVisualState()
    {
        if (_rectTransform != null)
        {
            _rectTransform.localScale = _aliveScale;
            _rectTransform.anchoredPosition = _aliveAnchoredPosition;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
        }

        if (_image != null)
        {
            _image.color = _defaultImageColor;
        }

        SetRaycastState(true);
    }

    private void SetRaycastState(bool canInteract)
    {
        if (_image != null)
        {
            _image.raycastTarget = canInteract;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.interactable = canInteract;
            _canvasGroup.blocksRaycasts = canInteract;
        }
    }

    private string GetEnemyDebugName()
    {
        if (enemyData != null && !string.IsNullOrWhiteSpace(enemyData.displayName))
        {
            return enemyData.displayName;
        }

        return name;
    }
}
