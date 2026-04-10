using System;
using System.Collections.Generic;
using UnityEngine;

public class StateManager : MonoBehaviour
{
    [Serializable]
    private class TimedState
    {
        public string stateId;
        public int turnsRemaining;
    }

    private class StateBucket
    {
        public GameObject target;
        public readonly List<TimedState> states = new List<TimedState>();
    }

    private readonly Dictionary<int, StateBucket> _statesByTargetId = new Dictionary<int, StateBucket>();

    public event Action<GameObject, string, int> OnStateApplied;
    public event Action<GameObject, string> OnStateRemoved;

    public void ApplyState(GameObject target, string stateId, int durationTurns)
    {
        if (target == null || string.IsNullOrWhiteSpace(stateId))
        {
            return;
        }

        int normalizedDuration = Mathf.Max(1, durationTurns);
        StateBucket bucket = GetOrCreateBucket(target);

        for (int i = 0; i < bucket.states.Count; i++)
        {
            if (bucket.states[i].stateId == stateId)
            {
                bucket.states[i].turnsRemaining = Mathf.Max(bucket.states[i].turnsRemaining, normalizedDuration);
                OnStateApplied?.Invoke(target, stateId, bucket.states[i].turnsRemaining);
                return;
            }
        }

        bucket.states.Add(new TimedState
        {
            stateId = stateId,
            turnsRemaining = normalizedDuration
        });

        OnStateApplied?.Invoke(target, stateId, normalizedDuration);
    }

    public void RemoveState(GameObject target, string stateId)
    {
        if (target == null || string.IsNullOrWhiteSpace(stateId))
        {
            return;
        }

        int targetId = target.GetInstanceID();
        if (!_statesByTargetId.TryGetValue(targetId, out StateBucket bucket))
        {
            return;
        }

        for (int i = bucket.states.Count - 1; i >= 0; i--)
        {
            if (bucket.states[i].stateId != stateId)
            {
                continue;
            }

            bucket.states.RemoveAt(i);
            OnStateRemoved?.Invoke(target, stateId);
            break;
        }

        if (bucket.states.Count == 0)
        {
            _statesByTargetId.Remove(targetId);
        }
    }

    public bool HasState(GameObject target, string stateId)
    {
        if (target == null || string.IsNullOrWhiteSpace(stateId))
        {
            return false;
        }

        int targetId = target.GetInstanceID();
        if (!_statesByTargetId.TryGetValue(targetId, out StateBucket bucket))
        {
            return false;
        }

        for (int i = 0; i < bucket.states.Count; i++)
        {
            if (bucket.states[i].stateId == stateId)
            {
                return true;
            }
        }

        return false;
    }

    public int GetRemainingTurns(GameObject target, string stateId)
    {
        if (target == null || string.IsNullOrWhiteSpace(stateId))
        {
            return 0;
        }

        int targetId = target.GetInstanceID();
        if (!_statesByTargetId.TryGetValue(targetId, out StateBucket bucket))
        {
            return 0;
        }

        for (int i = 0; i < bucket.states.Count; i++)
        {
            if (bucket.states[i].stateId == stateId)
            {
                return bucket.states[i].turnsRemaining;
            }
        }

        return 0;
    }

    public void TickTurnStart()
    {
        RemoveDestroyedTargets();
    }

    public void TickTurnEnd()
    {
        RemoveDestroyedTargets();

        List<int> targetKeys = new List<int>(_statesByTargetId.Keys);
        for (int i = 0; i < targetKeys.Count; i++)
        {
            if (!_statesByTargetId.TryGetValue(targetKeys[i], out StateBucket bucket))
            {
                continue;
            }

            for (int j = bucket.states.Count - 1; j >= 0; j--)
            {
                bucket.states[j].turnsRemaining--;

                if (bucket.states[j].turnsRemaining > 0)
                {
                    continue;
                }

                string removedStateId = bucket.states[j].stateId;
                bucket.states.RemoveAt(j);
                OnStateRemoved?.Invoke(bucket.target, removedStateId);
            }

            if (bucket.states.Count == 0)
            {
                _statesByTargetId.Remove(targetKeys[i]);
            }
        }
    }

    public void ClearAllStates()
    {
        _statesByTargetId.Clear();
    }

    private StateBucket GetOrCreateBucket(GameObject target)
    {
        int targetId = target.GetInstanceID();
        if (_statesByTargetId.TryGetValue(targetId, out StateBucket existingBucket))
        {
            return existingBucket;
        }

        StateBucket bucket = new StateBucket { target = target };
        _statesByTargetId[targetId] = bucket;
        return bucket;
    }

    private void RemoveDestroyedTargets()
    {
        List<int> keys = new List<int>(_statesByTargetId.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            StateBucket bucket = _statesByTargetId[keys[i]];
            if (bucket.target != null)
            {
                continue;
            }

            _statesByTargetId.Remove(keys[i]);
        }
    }
}
