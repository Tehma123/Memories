using UnityEngine;
using System;
using System.Collections.Generic;

public class ActionQueue : MonoBehaviour
{
    private readonly Queue<IBattleAction> _pendingActions = new Queue<IBattleAction>();
    private BattleContext _context;

    public int Count => _pendingActions.Count;
    public bool HasPendingActions => _pendingActions.Count > 0;

    public event Action<IBattleAction> OnActionEnqueued;
    public event Action<IBattleAction> OnActionResolved;
    public event Action OnQueueCleared;

    public void SetContext(BattleContext context)
    {
        _context = context;
    }

    public void Enqueue(IBattleAction action)
    {
        if (action == null)
        {
            return;
        }

        _pendingActions.Enqueue(action);
        OnActionEnqueued?.Invoke(action);
    }

    public void ProcessNext()
    {
        if (_pendingActions.Count == 0)
        {
            return;
        }

        if (_context == null)
        {
            Debug.LogWarning("ActionQueue cannot process without a BattleContext.");
            return;
        }

        IBattleAction action = _pendingActions.Dequeue();
        action.Resolve(_context);
        OnActionResolved?.Invoke(action);
    }

    public void Clear()
    {
        if (_pendingActions.Count == 0)
        {
            return;
        }

        _pendingActions.Clear();
        OnQueueCleared?.Invoke();
    }
}
