using UnityEngine;
using System.Collections.Generic;

public class PlayCardAction : IBattleAction
{
    private CardData _cardData;
    private GameObject _source;
    private GameObject _target;

    public void Setup(CardData cardData, GameObject source, GameObject target)
    {
        _cardData = cardData;
        _source = source;
        _target = target;
    }

    public void Resolve(BattleContext context)
    {
        if (context == null || _cardData == null)
        {
            return;
        }

        int maxMemory = context.MemoryManager != null ? Mathf.RoundToInt(context.MemoryManager.MaxMemoryPercent) : 100;
        int memoryCost = _cardData.GetMemoryCost(maxMemory);
        if (context.MemoryManager != null && !context.MemoryManager.TrySpend(memoryCost))
        {
            Debug.Log($"Not enough Memory to play card '{_cardData.displayName}'.");
            return;
        }

        if (_cardData.effects == null)
        {
            return;
        }

        for (int i = 0; i < _cardData.effects.Count; i++)
        {
            EffectData effect = _cardData.effects[i];
            if (effect == null)
            {
                continue;
            }

            ResolveEffect(context, effect);
        }
    }

    private void ResolveEffect(BattleContext context, EffectData effect)
    {
        int value = effect.GetResolvedValue();
        List<GameObject> targets = ResolveTargets(context, effect.targetScope);

        switch (effect.effectType)
        {
            case EffectType.Damage:
                for (int i = 0; i < targets.Count; i++)
                {
                    IDamageable damageable = targets[i].GetComponent<IDamageable>();
                    damageable?.TakeDamage(Mathf.Max(0, value));
                }
                break;

            case EffectType.HealMemory:
                context.MemoryManager?.ChangeMemory(Mathf.Max(0, value));
                break;

            case EffectType.Buff:
            case EffectType.Debuff:
            case EffectType.ApplyStatus:
                if (string.IsNullOrWhiteSpace(effect.statusId))
                {
                    break;
                }

                int duration = Mathf.Max(1, effect.durationTurns);
                for (int i = 0; i < targets.Count; i++)
                {
                    context.StateManager?.ApplyState(targets[i], effect.statusId, duration);
                }
                break;

            case EffectType.Draw:
                context.DeckManager?.DrawCards(Mathf.Max(1, value));
                break;

            case EffectType.Discard:
                context.DeckManager?.DiscardRandomFromHand(Mathf.Max(1, value));
                break;

            case EffectType.Exile:
                context.DeckManager?.ExileRandomFromHand(Mathf.Max(1, value));
                break;

            case EffectType.ReturnFromDiscard:
                context.DeckManager?.ReturnFromDiscardToHand(Mathf.Max(1, value));
                break;
        }
    }

    private List<GameObject> ResolveTargets(BattleContext context, TargetScope targetScope)
    {
        List<GameObject> targets = new List<GameObject>();

        switch (targetScope)
        {
            case TargetScope.Single:
                if (_target != null)
                {
                    targets.Add(_target);
                    return targets;
                }

                EnemyController primaryEnemy = context.GetPrimaryAliveEnemy();
                if (primaryEnemy != null)
                {
                    targets.Add(primaryEnemy.gameObject);
                }
                break;

            case TargetScope.AllEnemies:
                IReadOnlyList<EnemyController> aliveEnemies = context.GetAliveEnemies();
                for (int i = 0; i < aliveEnemies.Count; i++)
                {
                    targets.Add(aliveEnemies[i].gameObject);
                }
                break;

            case TargetScope.Self:
            case TargetScope.Ally:
                if (_source != null)
                {
                    targets.Add(_source);
                }
                else if (context.PlayerObject != null)
                {
                    targets.Add(context.PlayerObject);
                }
                break;
        }

        return targets;
    }
}
