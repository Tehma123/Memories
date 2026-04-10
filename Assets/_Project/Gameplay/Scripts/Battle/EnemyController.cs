using UnityEngine;
using System;

public class EnemyController : MonoBehaviour, IDamageable
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private int currentHealth;
    [SerializeField] private BattleManager battleManager;

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

        if (enemyData != null && currentHealth <= 0)
        {
            currentHealth = Mathf.Max(1, enemyData.maxHealth);
        }
    }

    public void Initialize(EnemyData enemyData)
    {
        this.enemyData = enemyData;
        currentHealth = this.enemyData != null ? Mathf.Max(1, this.enemyData.maxHealth) : 1;
        NotifyHealthChanged();
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);
        NotifyHealthChanged();

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
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        NotifyHealthChanged();
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
    }

    private void HandleDefeat()
    {
        OnDefeated?.Invoke(this);
        Debug.Log($"Enemy '{name}' was defeated.");
    }
}
