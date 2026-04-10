using UnityEngine;
using System;
using System.Collections.Generic;

public class VignetteManager : MonoBehaviour
{
    public static VignetteManager Instance { get; private set; }

    [SerializeField] private bool instantText;
    [SerializeField] private MemoryManager memoryManager;

    private readonly HashSet<string> _shownOneShotVignettes = new HashSet<string>();
    private VignetteData _activeVignette;
    private int _currentLineIndex = -1;
    private bool _isShowing;

    public bool IsShowing => _isShowing;

    public event Action<VignetteData> OnVignetteTriggered;
    public event Action<string, int, int> OnVignetteLineShown;
    public event Action<VignetteData> OnVignetteClosed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (memoryManager == null)
        {
            memoryManager = FindFirstObjectByType<MemoryManager>();
        }
    }

    public void Show(VignetteData vignetteData)
    {
        if (vignetteData == null)
        {
            return;
        }

        string vignetteId = string.IsNullOrWhiteSpace(vignetteData.vignetteID) ? vignetteData.name : vignetteData.vignetteID;
        if (!vignetteData.replayable && _shownOneShotVignettes.Contains(vignetteId))
        {
            return;
        }

        if (!vignetteData.replayable)
        {
            _shownOneShotVignettes.Add(vignetteId);
        }

        _activeVignette = vignetteData;
        _currentLineIndex = 0;
        _isShowing = true;

        if (!string.IsNullOrWhiteSpace(vignetteData.revealMemoryFragment) && memoryManager != null)
        {
            memoryManager.UnlockFragment(vignetteData.revealMemoryFragment);
        }

        OnVignetteTriggered?.Invoke(vignetteData);
        DisplayCurrentLine();
    }

    public void Skip()
    {
        if (!_isShowing)
        {
            return;
        }

        int lineCount = _activeVignette != null && _activeVignette.textLines != null ? _activeVignette.textLines.Length : 0;
        if (lineCount == 0)
        {
            Close();
            return;
        }

        if (instantText)
        {
            _currentLineIndex = lineCount - 1;
            DisplayCurrentLine();
            Close();
            return;
        }

        ShowNextLine();
    }

    public void Close()
    {
        if (!_isShowing)
        {
            return;
        }

        VignetteData closedVignette = _activeVignette;
        _activeVignette = null;
        _currentLineIndex = -1;
        _isShowing = false;

        OnVignetteClosed?.Invoke(closedVignette);
    }

    public void ShowNextLine()
    {
        if (!_isShowing || _activeVignette == null)
        {
            return;
        }

        int lineCount = _activeVignette.textLines != null ? _activeVignette.textLines.Length : 0;
        if (lineCount == 0)
        {
            Close();
            return;
        }

        _currentLineIndex++;
        if (_currentLineIndex >= lineCount)
        {
            Close();
            return;
        }

        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        if (!_isShowing || _activeVignette == null)
        {
            return;
        }

        int lineCount = _activeVignette.textLines != null ? _activeVignette.textLines.Length : 0;
        if (lineCount == 0)
        {
            Close();
            return;
        }

        _currentLineIndex = Mathf.Clamp(_currentLineIndex, 0, lineCount - 1);
        string line = _activeVignette.textLines[_currentLineIndex];

        Debug.Log($"[Vignette] {line}");
        OnVignetteLineShown?.Invoke(line, _currentLineIndex, lineCount);
    }
}
