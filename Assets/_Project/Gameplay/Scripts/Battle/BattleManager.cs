using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private ActionQueue actionQueue;
    [SerializeField] private StateManager stateManager;
    [SerializeField] private MemoryManager memoryManager;
    [SerializeField] private GameObject playerObject;
    [SerializeField] private Transform[] enemySlots = Array.Empty<Transform>();
    [SerializeField] private EnemyData[] encounterEnemies = Array.Empty<EnemyData>();
    [SerializeField] private bool autoStartEncounterOnStart = true;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int fixedSeed;

    private readonly List<EnemyController> _activeEnemies = new List<EnemyController>();
    private BattleContext _context;
    private bool _encounterActive;
    private bool _isResolvingQueue;

    public BattleContext CurrentContext => _context;
    public IReadOnlyList<EnemyController> ActiveEnemies => _activeEnemies;
    public bool IsEncounterActive => _encounterActive;

    public event Action<BattleContext> OnEncounterStarted;
    public event Action<int> OnTurnStarted;
    public event Action<int> OnTurnEnded;
    public event Action<bool> OnEncounterEnded;

    private void Awake()
    {
        if (deckManager == null)
        {
            deckManager = FindFirstObjectByType<DeckManager>();
        }

        if (actionQueue == null)
        {
            actionQueue = FindFirstObjectByType<ActionQueue>();
        }

        if (stateManager == null)
        {
            stateManager = FindFirstObjectByType<StateManager>();
        }

        if (memoryManager == null)
        {
            memoryManager = FindFirstObjectByType<MemoryManager>();
        }

        if (endTurnButton == null)
        {
            endTurnButton = FindEndTurnButton();
        }
    }

    private void OnEnable()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(HandleEndTurnButtonClicked);
        }
    }

    private void Start()
    {
        UpdateEndTurnButtonState();

        if (!autoStartEncounterOnStart)
        {
            return;
        }

        if (encounterEnemies == null || encounterEnemies.Length == 0)
        {
            Debug.LogWarning("BattleManager auto start is enabled, but no encounterEnemies are configured.");
            return;
        }

        StartEncounter(encounterEnemies);
    }

    private void OnDisable()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveListener(HandleEndTurnButtonClicked);
        }
    }

    [ContextMenu("Start Configured Encounter")]
    private void StartConfiguredEncounter()
    {
        StartEncounter(encounterEnemies);
    }

    public void StartEncounter(EnemyData[] enemies)
    {
        if (deckManager == null || actionQueue == null)
        {
            Debug.LogError("BattleManager missing DeckManager or ActionQueue reference.");
            UpdateEndTurnButtonState();
            return;
        }

        ResetEncounterState();
        SpawnEnemies(enemies);

        int? seed = useFixedSeed ? fixedSeed : null;
        _context = new BattleContext(this, deckManager, stateManager, memoryManager, playerObject, seed);
        if (useFixedSeed)
        {
            deckManager.SetSeed(fixedSeed);
        }

        _context.SetEnemies(_activeEnemies);
        actionQueue.SetContext(_context);

        deckManager.InitializeDeck();
        deckManager.DrawInitialHand();

        _encounterActive = true;
        _context.TurnNumber = 0;
        _context.IsPlayerTurn = true;

        OnEncounterStarted?.Invoke(_context);
        StartTurn();
        UpdateEndTurnButtonState();
    }

    public void StartTurn()
    {
        if (!_encounterActive || _context == null)
        {
            return;
        }

        if (IsPlayerDefeated())
        {
            EndEncounterInternal(false);
            return;
        }

        if (AreAllEnemiesDefeated())
        {
            EndEncounterInternal(true);
            return;
        }

        _context.TurnNumber++;
        _context.IsPlayerTurn = true;

        stateManager?.TickTurnStart();
        deckManager.DrawForTurn();

        OnTurnStarted?.Invoke(_context.TurnNumber);
        UpdateEndTurnButtonState();
    }

    public void EndTurn()
    {
        if (!_encounterActive || _context == null)
        {
            return;
        }

        if (!_context.IsPlayerTurn)
        {
            return;
        }

        ResolveQueue();
        _context.IsPlayerTurn = false;
        UpdateEndTurnButtonState();

        RunEnemyTurn();
        stateManager?.TickTurnEnd();

        OnTurnEnded?.Invoke(_context.TurnNumber);

        if (IsPlayerDefeated())
        {
            EndEncounterInternal(false);
            return;
        }

        if (AreAllEnemiesDefeated())
        {
            EndEncounterInternal(true);
            return;
        }

        StartTurn();
    }

    public void EnqueueAction(IBattleAction action)
    {
        if (!_encounterActive || action == null)
        {
            return;
        }

        actionQueue.Enqueue(action);
    }

    public void ResolveQueue()
    {
        if (!_encounterActive || _isResolvingQueue)
        {
            return;
        }

        _isResolvingQueue = true;

        while (actionQueue.HasPendingActions)
        {
            actionQueue.ProcessNext();
        }

        _isResolvingQueue = false;
        UpdateEndTurnButtonState();
    }

    public void EndEncounter()
    {
        bool playerWon = AreAllEnemiesDefeated() && !IsPlayerDefeated();
        EndEncounterInternal(playerWon);
    }

    private void RunEnemyTurn()
    {
        for (int i = 0; i < _activeEnemies.Count; i++)
        {
            EnemyController enemy = _activeEnemies[i];
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            enemy.TakeTurn();
        }
    }

    private bool IsPlayerDefeated()
    {
        return memoryManager != null && memoryManager.CurrentMemoryPercent <= 0f;
    }

    private bool AreAllEnemiesDefeated()
    {
        if (_activeEnemies.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < _activeEnemies.Count; i++)
        {
            if (_activeEnemies[i] != null && _activeEnemies[i].IsAlive)
            {
                return false;
            }
        }

        return true;
    }

    private void SpawnEnemies(EnemyData[] enemies)
    {
        if (enemies == null || enemies.Length == 0)
        {
            return;
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyData enemyData = enemies[i];
            if (enemyData == null)
            {
                continue;
            }

            Transform slot = i < enemySlots.Length ? enemySlots[i] : null;
            EnemyController controller = CreateEnemyController(enemyData, i, slot);

            if (controller == null)
            {
                continue;
            }

            controller.Initialize(enemyData);
            _activeEnemies.Add(controller);
        }
    }

    private EnemyController CreateEnemyController(EnemyData enemyData, int index, Transform slot)
    {
        if (enemyData.enemyPrefab != null)
        {
            Vector3 spawnPosition = slot != null ? slot.position : transform.position + Vector3.right * (index * 2f);
            Quaternion spawnRotation = slot != null ? slot.rotation : Quaternion.identity;
            Transform parent = slot != null ? slot : transform;

            GameObject spawnedEnemy = Instantiate(enemyData.enemyPrefab, spawnPosition, spawnRotation, parent);
            EnemyController controller = spawnedEnemy.GetComponent<EnemyController>();
            if (controller == null)
            {
                controller = spawnedEnemy.AddComponent<EnemyController>();
            }

            return controller;
        }

        GameObject fallback = new GameObject($"Enemy_{index}_{enemyData.displayName}");
        if (slot != null)
        {
            fallback.transform.SetPositionAndRotation(slot.position, slot.rotation);
            fallback.transform.SetParent(slot, true);
        }
        else
        {
            fallback.transform.position = transform.position + Vector3.right * (index * 2f);
            fallback.transform.SetParent(transform, true);
        }

        return fallback.AddComponent<EnemyController>();
    }

    private void ResetEncounterState()
    {
        actionQueue?.Clear();
        _context = null;
        _encounterActive = false;
        _isResolvingQueue = false;

        for (int i = 0; i < _activeEnemies.Count; i++)
        {
            if (_activeEnemies[i] != null)
            {
                Destroy(_activeEnemies[i].gameObject);
            }
        }

        _activeEnemies.Clear();
        UpdateEndTurnButtonState();
    }

    private void EndEncounterInternal(bool playerWon)
    {
        if (!_encounterActive)
        {
            return;
        }

        _encounterActive = false;
        actionQueue?.Clear();
        OnEncounterEnded?.Invoke(playerWon);
        UpdateEndTurnButtonState();
    }

    private void HandleEndTurnButtonClicked()
    {
        EndTurn();
    }

    private void UpdateEndTurnButtonState()
    {
        if (endTurnButton == null)
        {
            return;
        }

        bool canInteract = _encounterActive && _context != null && _context.IsPlayerTurn && !_isResolvingQueue;
        endTurnButton.interactable = canInteract;
    }

    private static Button FindEndTurnButton()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (string.Equals(buttons[i].name, "EndTurnButton", StringComparison.OrdinalIgnoreCase))
            {
                return buttons[i];
            }
        }

        return null;
    }
}
