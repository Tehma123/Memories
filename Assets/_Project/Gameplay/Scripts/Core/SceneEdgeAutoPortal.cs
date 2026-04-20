using UnityEngine;
using System;

[DisallowMultipleComponent]
public class SceneEdgeAutoPortal : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float activationDelaySeconds = 0.2f;
    [SerializeField] private bool requireOutwardInput = false;

    [Header("Left Edge")]
    [SerializeField] private bool enableLeftEdge = true;
    [SerializeField] private float leftEdgeX = -10f;
    [SerializeField] private string leftDestinationSceneName = string.Empty;
    [SerializeField] private string leftDestinationEntryPointId = "Default";

    [Header("Right Edge")]
    [SerializeField] private bool enableRightEdge = true;
    [SerializeField] private float rightEdgeX = 10f;
    [SerializeField] private string rightDestinationSceneName = string.Empty;
    [SerializeField] private string rightDestinationEntryPointId = "Default";

    [Header("Encounter Payload (Optional)")]
    [SerializeField] private bool includeEncounterPayload;
    [SerializeField] private string encounterId = string.Empty;
    [SerializeField] private EnemyData[] encounterEnemies = Array.Empty<EnemyData>();
    [SerializeField, Min(1)] private int encounterLevel = 1;
    [SerializeField] private string spawnPattern = string.Empty;
    [SerializeField] private bool overrideEncounterSeed;
    [SerializeField] private int encounterSeed;

    private float _previousPlayerX;
    private bool _hasPreviousPosition;
    private bool _hasTriggeredTransition;

    private void Awake()
    {
        TryResolvePlayerReferences();
    }

    private void OnEnable()
    {
        _hasPreviousPosition = false;
        _hasTriggeredTransition = false;
    }

    private void Update()
    {
        if (_hasTriggeredTransition)
        {
            return;
        }

        if (Time.timeSinceLevelLoad < Mathf.Max(0f, activationDelaySeconds))
        {
            return;
        }

        if (playerTransform == null)
        {
            TryResolvePlayerReferences();
            if (playerTransform == null)
            {
                return;
            }
        }

        float currentPlayerX = playerTransform.position.x;

        if (!_hasPreviousPosition)
        {
            _previousPlayerX = currentPlayerX;
            _hasPreviousPosition = true;
            return;
        }

        if (enableLeftEdge && DidCrossLeftEdge(_previousPlayerX, currentPlayerX) && IsMovingOutward(isLeftEdge: true))
        {
            if (TryTransition(leftDestinationSceneName, leftDestinationEntryPointId, "left"))
            {
                return;
            }
        }

        if (enableRightEdge && DidCrossRightEdge(_previousPlayerX, currentPlayerX) && IsMovingOutward(isLeftEdge: false))
        {
            if (TryTransition(rightDestinationSceneName, rightDestinationEntryPointId, "right"))
            {
                return;
            }
        }

        _previousPlayerX = currentPlayerX;
    }

    private bool DidCrossLeftEdge(float previousX, float currentX)
    {
        return previousX > leftEdgeX && currentX <= leftEdgeX;
    }

    private bool DidCrossRightEdge(float previousX, float currentX)
    {
        return previousX < rightEdgeX && currentX >= rightEdgeX;
    }

    private bool IsMovingOutward(bool isLeftEdge)
    {
        if (!requireOutwardInput || playerMovement == null)
        {
            return true;
        }

        float horizontalInput = playerMovement.MoveInput.x;
        return isLeftEdge ? horizontalInput < -0.01f : horizontalInput > 0.01f;
    }

    private bool TryTransition(string destinationSceneName, string destinationEntryPointId, string edgeName)
    {
        if (string.IsNullOrWhiteSpace(destinationSceneName))
        {
            Debug.LogWarning($"{nameof(SceneEdgeAutoPortal)} on '{name}' has no {edgeName} destination scene set.");
            return false;
        }

        _hasTriggeredTransition = true;
        SceneTransitionContext.LoadScene(destinationSceneName, destinationEntryPointId, BuildEncounterPayload());
        return true;
    }

    private EncounterPayload BuildEncounterPayload()
    {
        if (!includeEncounterPayload)
        {
            return null;
        }

        int? seed = overrideEncounterSeed ? encounterSeed : (int?)null;
        return EncounterPayload.FromEnemyData(
            encounterId,
            encounterEnemies,
            encounterLevel,
            spawnPattern,
            seed);
    }

    private void TryResolvePlayerReferences()
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (playerTransform != null)
        {
            return;
        }

        if (playerMovement != null)
        {
            playerTransform = playerMovement.transform;
        }
    }

    private void OnValidate()
    {
        leftDestinationSceneName = (leftDestinationSceneName ?? string.Empty).Trim();
        leftDestinationEntryPointId = (leftDestinationEntryPointId ?? string.Empty).Trim();
        rightDestinationSceneName = (rightDestinationSceneName ?? string.Empty).Trim();
        rightDestinationEntryPointId = (rightDestinationEntryPointId ?? string.Empty).Trim();
        encounterId = (encounterId ?? string.Empty).Trim();
        spawnPattern = (spawnPattern ?? string.Empty).Trim();
        encounterLevel = Mathf.Max(1, encounterLevel);
    }
}
