using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

public class MemoryManager : MonoBehaviour
{
    [SerializeField] private float startingMemoryPercent = 100f;
    [SerializeField] private float maxMemoryPercent = 100f;

    [Header("Combat UI")]
    [SerializeField] private RectTransform memBarFill;
    [SerializeField] private TMP_Text memBarLabel;

    private readonly HashSet<string> _unlockedFragments = new HashSet<string>();
    private bool _gameOverHandled;

    public float CurrentMemoryPercent { get; private set; }
    public float MaxMemoryPercent => Mathf.Max(0f, maxMemoryPercent);

    public event Action<float> OnMemoryChanged;
    public event Action OnMemoryDepleted;
    public event Action<string> OnMemoryFragmentUnlocked;

    public void Initialize()
    {
        _gameOverHandled = false;
        _unlockedFragments.Clear();
        SetMemory(startingMemoryPercent);
    }

    private void Awake()
    {
        ResolveCombatUiReferences();
        Initialize();
    }

    public void SetMemory(float valuePercent)
    {
        float clamped = Mathf.Clamp(valuePercent, 0f, Mathf.Max(0f, maxMemoryPercent));
        if (Mathf.Approximately(CurrentMemoryPercent, clamped))
        {
            UpdateCombatUi();
            return;
        }

        CurrentMemoryPercent = clamped;
        OnMemoryChanged?.Invoke(CurrentMemoryPercent);
        UpdateCombatUi();

        if (CurrentMemoryPercent <= 0f)
        {
            HandleGameOver();
        }
    }

    public void ChangeMemory(float deltaPercent)
    {
        SetMemory(CurrentMemoryPercent + deltaPercent);
    }

    public bool CanSpend(float amountPercent)
    {
        return CurrentMemoryPercent >= Mathf.Max(0f, amountPercent);
    }

    public bool TrySpend(float amountPercent)
    {
        float spendAmount = Mathf.Max(0f, amountPercent);
        if (!CanSpend(spendAmount))
        {
            return false;
        }

        ChangeMemory(-spendAmount);
        return true;
    }

    public bool UnlockFragment(string fragmentId)
    {
        if (string.IsNullOrWhiteSpace(fragmentId))
        {
            return false;
        }

        bool isNewFragment = _unlockedFragments.Add(fragmentId);
        if (isNewFragment)
        {
            OnMemoryFragmentUnlocked?.Invoke(fragmentId);
        }

        return isNewFragment;
    }

    public bool HasFragment(string fragmentId)
    {
        if (string.IsNullOrWhiteSpace(fragmentId))
        {
            return false;
        }

        return _unlockedFragments.Contains(fragmentId);
    }

    public IReadOnlyCollection<string> GetUnlockedFragments()
    {
        return _unlockedFragments;
    }

    public void HandleGameOver()
    {
        if (_gameOverHandled)
        {
            return;
        }

        _gameOverHandled = true;
        OnMemoryDepleted?.Invoke();
        Debug.LogWarning("Memory depleted. Game over flow should be handled by listeners.");
    }

    private void ResolveCombatUiReferences()
    {
        if (memBarFill == null)
        {
            memBarFill = FindRectTransformByName("MemBar_Fill");
        }

        if (memBarLabel == null)
        {
            memBarLabel = FindTextByName("MemBar_Label");
        }
    }

    private void UpdateCombatUi()
    {
        float max = MaxMemoryPercent;
        float normalized = max <= 0f ? 0f : Mathf.Clamp01(CurrentMemoryPercent / max);

        if (memBarFill != null)
        {
            Vector2 anchorMax = memBarFill.anchorMax;
            anchorMax.x = normalized;
            memBarFill.anchorMax = anchorMax;
        }

        if (memBarLabel != null)
        {
            memBarLabel.text = $"{Mathf.RoundToInt(CurrentMemoryPercent)}/{Mathf.RoundToInt(max)}";
        }
    }

    private static RectTransform FindRectTransformByName(string objectName)
    {
        RectTransform[] rects = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < rects.Length; i++)
        {
            if (string.Equals(rects[i].name, objectName, StringComparison.OrdinalIgnoreCase))
            {
                return rects[i];
            }
        }

        return null;
    }

    private static TMP_Text FindTextByName(string objectName)
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (string.Equals(texts[i].name, objectName, StringComparison.OrdinalIgnoreCase))
            {
                return texts[i];
            }
        }

        return null;
    }
}
