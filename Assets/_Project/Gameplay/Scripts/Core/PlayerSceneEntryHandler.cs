using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlayerSceneEntryHandler : MonoBehaviour
{
    [SerializeField] private bool useFallbackEntryWhenNoPendingTransition = false;
    [SerializeField] private string fallbackEntryPointId = string.Empty;

    private Rigidbody2D _rigidbody2D;
    private bool _entryApplied;

    private void Awake()
    {
        _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        TryApplyEntryPoint();
    }

    public void TryApplyEntryPoint()
    {
        if (_entryApplied)
        {
            return;
        }

        string requestedEntryPointId;
        bool hasRequestedEntry = SceneTransitionContext.TryConsumeEntryPointForActiveScene(
            SceneManager.GetActiveScene().name,
            out requestedEntryPointId
        );

        if (!hasRequestedEntry && !useFallbackEntryWhenNoPendingTransition)
        {
            return;
        }

        string targetEntryId = hasRequestedEntry ? requestedEntryPointId : fallbackEntryPointId;

        SceneEntryPoint spawnPoint = FindEntryPoint(targetEntryId);
        if (spawnPoint == null)
        {
            spawnPoint = FindFallbackEntryPoint();
        }

        if (spawnPoint == null)
        {
            return;
        }

        Vector3 nextPosition = spawnPoint.transform.position;
        nextPosition.z = transform.position.z;
        transform.position = nextPosition;

        if (_rigidbody2D != null)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
        }

        _entryApplied = true;
    }

    private SceneEntryPoint FindEntryPoint(string entryPointId)
    {
        if (string.IsNullOrWhiteSpace(entryPointId))
        {
            return null;
        }

        SceneEntryPoint[] candidates = FindObjectsByType<SceneEntryPoint>(FindObjectsSortMode.None);
        for (int i = 0; i < candidates.Length; i++)
        {
            SceneEntryPoint candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            if (string.Equals(candidate.EntryPointId, entryPointId, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    private SceneEntryPoint FindFallbackEntryPoint()
    {
        SceneEntryPoint[] candidates = FindObjectsByType<SceneEntryPoint>(FindObjectsSortMode.None);
        for (int i = 0; i < candidates.Length; i++)
        {
            SceneEntryPoint candidate = candidates[i];
            if (candidate != null && candidate.UseAsFallback)
            {
                return candidate;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        fallbackEntryPointId = (fallbackEntryPointId ?? string.Empty).Trim();
    }
}
