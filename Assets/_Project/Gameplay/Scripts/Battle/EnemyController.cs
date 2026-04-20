using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

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

    private Image _image;
    private Outline _outline;
    private Color _defaultImageColor = Color.white;

    public EnemyData Data => enemyData;
    public int CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0;

    public event Action<EnemyController, int, int> OnHealthChanged;
    public event Action<EnemyController> OnDefeated;

    private void Awake()
    {
        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
        }

        if (deckManager == null)
        {
            deckManager = FindFirstObjectByType<DeckManager>();
        }

        _image = GetComponent<Image>();
        if (_image != null)
        {
            _defaultImageColor = _image.color;
        }

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

        SetTargetSelectionState(false);
    }

    public void Initialize(EnemyData enemyData)
    {
        this.enemyData = enemyData;
        currentHealth = this.enemyData != null ? Mathf.Max(1, this.enemyData.maxHealth) : 1;
        NotifyHealthChanged();
        SetTargetSelectionState(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsAlive)
        {
            return;
        }

        if (deckManager == null)
        {
            deckManager = FindFirstObjectByType<DeckManager>();
        }

        deckManager?.HandleEnemyClicked(this);
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

    public void TakeTurn()
    {
        if (!IsAlive || battleManager == null)
        {
            return;
        }

        BattleContext context = battleManager.CurrentContext;
        if (context == null)
        {
            return;
        }

        EnemyMoveData move = enemyData != null ? enemyData.GetRandomMove(context.Random) : null;
        if (move == null)
        {
            context.MemoryManager?.ChangeMemory(-1f);
            return;
        }

        int value = move.GetRoll(context.Random);

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
        Debug.Log($"Enemy '{name}' was defeated.");
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
