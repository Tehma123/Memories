using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransitionContext
{
    public const char EntryPointDelimiter = '|';

    private static string _destinationSceneName = string.Empty;
    private static string _destinationEntryPointId = string.Empty;
    private static bool _hasPendingTransition;

    private static string _encounterPayloadSceneName = string.Empty;
    private static EncounterPayload _encounterPayload;
    private static bool _hasPendingEncounterPayload;

    public static void LoadScene(
        string destinationSceneName,
        string destinationEntryPointId = "",
        EncounterPayload encounterPayload = null)
    {
        if (string.IsNullOrWhiteSpace(destinationSceneName))
        {
            Debug.LogWarning($"{nameof(SceneTransitionContext)} cannot load an empty scene name.");
            return;
        }

        SetPendingTransition(destinationSceneName, destinationEntryPointId);
        SetPendingEncounterPayload(destinationSceneName, encounterPayload);

        if (!SceneTransitionFader.TryLoadSceneWithFade(destinationSceneName))
        {
            SceneManager.LoadScene(destinationSceneName);
        }
    }

    public static void SetPendingTransition(string destinationSceneName, string destinationEntryPointId = "")
    {
        _destinationSceneName = (destinationSceneName ?? string.Empty).Trim();
        _destinationEntryPointId = (destinationEntryPointId ?? string.Empty).Trim();
        _hasPendingTransition = !string.IsNullOrWhiteSpace(_destinationSceneName);
    }

    public static void ClearPendingTransition()
    {
        _destinationSceneName = string.Empty;
        _destinationEntryPointId = string.Empty;
        _hasPendingTransition = false;
    }

    public static bool TryConsumeEntryPointForActiveScene(string activeSceneName, out string entryPointId)
    {
        entryPointId = string.Empty;

        if (!_hasPendingTransition)
        {
            return false;
        }

        if (!string.Equals(_destinationSceneName, activeSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        entryPointId = _destinationEntryPointId;
        ClearPendingTransition();
        return true;
    }

    public static void SetPendingEncounterPayload(string destinationSceneName, EncounterPayload payload)
    {
        if (payload == null || string.IsNullOrWhiteSpace(destinationSceneName))
        {
            ClearPendingEncounterPayload();
            return;
        }

        _encounterPayloadSceneName = destinationSceneName.Trim();
        _encounterPayload = payload.Clone();
        _hasPendingEncounterPayload = true;
    }

    public static void ClearPendingEncounterPayload()
    {
        _encounterPayloadSceneName = string.Empty;
        _encounterPayload = null;
        _hasPendingEncounterPayload = false;
    }

    public static bool TryConsumeEncounterPayloadForActiveScene(string activeSceneName, out EncounterPayload payload)
    {
        payload = null;

        if (!_hasPendingEncounterPayload)
        {
            return false;
        }

        if (!string.Equals(_encounterPayloadSceneName, activeSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        payload = _encounterPayload?.Clone();
        ClearPendingEncounterPayload();
        return payload != null;
    }

    public static bool TryParseSceneAndEntry(string rawValue, out string sceneName, out string entryPointId)
    {
        sceneName = string.Empty;
        entryPointId = string.Empty;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        string trimmed = rawValue.Trim();
        int delimiterIndex = trimmed.IndexOf(EntryPointDelimiter);

        if (delimiterIndex < 0)
        {
            sceneName = trimmed;
            return !string.IsNullOrWhiteSpace(sceneName);
        }

        sceneName = trimmed.Substring(0, delimiterIndex).Trim();
        entryPointId = trimmed.Substring(delimiterIndex + 1).Trim();

        return !string.IsNullOrWhiteSpace(sceneName);
    }
}
