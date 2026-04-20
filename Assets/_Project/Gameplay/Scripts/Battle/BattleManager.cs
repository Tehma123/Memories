using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    private readonly struct EnemyResolutionResult
    {
        public EnemyResolutionResult(string encounterId, int sourceEnemyCount, EnemyData[] resolvedEnemies, bool usedFallback)
        {
            EncounterId = encounterId;
            SourceEnemyCount = sourceEnemyCount;
            ResolvedEnemies = resolvedEnemies;
            UsedFallback = usedFallback;
        }

        public string EncounterId { get; }
        public int SourceEnemyCount { get; }
        public EnemyData[] ResolvedEnemies { get; }
        public bool UsedFallback { get; }
    }

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

    [Header("Combat Flow")]
    [SerializeField] private bool freezeGameplayTimeOnPause = true;

    [Header("Victory Flow")]
    [SerializeField] private bool requestCheckpointSaveOnVictory = true;
    [SerializeField] private bool returnToExplorationOnVictory;
    [SerializeField] private string victorySceneName = string.Empty;
    [SerializeField] private string victoryEntryPointId = "Default";

    [Header("Defeat Flow")]
    [SerializeField] private string checkpointSceneName = string.Empty;
    [SerializeField] private string checkpointEntryPointId = "Default";
    [SerializeField] private string quitToMenuSceneName = "MainMenuScene";
    [SerializeField] private string quitToMenuEntryPointId = string.Empty;

    private readonly List<EnemyController> _activeEnemies = new List<EnemyController>();
    private EnemyData[] _runtimeEncounterEnemies = Array.Empty<EnemyData>();
    private EncounterPayload _runtimeEncounterPayload;
    private bool _hasRuntimeEncounterPayload;

    private EnemyData[] _lastEncounterEnemies = Array.Empty<EnemyData>();
    private EncounterPayload _lastEncounterPayload;
    private float _lastEncounterStartingMemory;
    private bool _hasEncounterStartMemorySnapshot;

    private BattleContext _context;
    private bool _encounterActive;
    private bool _isResolvingQueue;
    private bool _isWaitingForSkipDiscardSelection;
    private bool _isPaused;
    private float _cachedTimeScaleBeforePause = 1f;
    private int _currentEncounterSeed;
    private CombatFlowState _stateBeforePause = CombatFlowState.Init;

    public BattleContext CurrentContext => _context;
    public IReadOnlyList<EnemyController> ActiveEnemies => _activeEnemies;
    public bool IsEncounterActive => _encounterActive;
    public bool IsPaused => _isPaused;
    public CombatFlowState FlowState { get; private set; } = CombatFlowState.Init;
    public int CurrentEncounterSeed => _currentEncounterSeed;

    public event Action<BattleContext> OnEncounterStarted;
    public event Action<string, int, int> OnEncounterResolved;
    public event Action<int> OnTurnStarted;
    public event Action<int> OnTurnEnded;
    public event Action<bool> OnEncounterEnded;
    public event Action<CombatFlowState> OnCombatFlowStateChanged;
    public event Action OnCombatPaused;
    public event Action OnCombatResumed;
    public event Action OnCombatVictory;
    public event Action OnCombatDefeated;
    public event Action OnCombatRestarted;
    public event Action<string, string> OnCheckpointSaveRequested;

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

        EnemyData[] startupEnemies = _runtimeEncounterEnemies.Length > 0 ? _runtimeEncounterEnemies : encounterEnemies;
        EncounterPayload startupPayload = _hasRuntimeEncounterPayload ? _runtimeEncounterPayload : null;

        if (startupEnemies == null || startupEnemies.Length == 0)
        {
            Debug.LogWarning("BattleManager auto start is enabled, but no encounterEnemies are configured.");
            ClearRuntimeEncounterOverride();
            return;
        }

        StartEncounter(startupEnemies, startupPayload);
        ClearRuntimeEncounterOverride();
    }

    private void OnDisable()
    {
        if (endTurnButton != null)
        {
            endTurnButton.onClick.RemoveListener(HandleEndTurnButtonClicked);
        }

        if (_isPaused)
        {
            RestoreTimeScaleAfterPause();
            _isPaused = false;
        }
    }

    [ContextMenu("Start Configured Encounter")]
    private void StartConfiguredEncounter()
    {
        StartEncounter(encounterEnemies, null);
    }

    public void StartEncounter(EnemyData[] enemies)
    {
        StartEncounter(enemies, null);
    }

    public void StartEncounter(EnemyData[] enemies, EncounterPayload payload)
    {
        if (deckManager == null || actionQueue == null)
        {
            Debug.LogError("BattleManager missing DeckManager or ActionQueue reference.");
            UpdateEndTurnButtonState();
            return;
        }

        if (_isPaused)
        {
            RestoreTimeScaleAfterPause();
            _isPaused = false;
        }

        float startingMemorySnapshot = memoryManager != null ? memoryManager.CurrentMemoryPercent : 0f;

        ResetEncounterState();

        EnemyResolutionResult resolution = ResolveEncounterEnemies(enemies, payload);
        if (resolution.ResolvedEnemies.Length < CombatConfig.MinEnemySlots)
        {
            string unresolvedEncounterId = string.IsNullOrWhiteSpace(resolution.EncounterId)
                ? "unknown-encounter"
                : resolution.EncounterId;
            Debug.LogWarning($"BattleManager could not start encounter '{unresolvedEncounterId}' because no valid enemies were resolved.");
            return;
        }

        OnEncounterResolved?.Invoke(resolution.EncounterId, resolution.SourceEnemyCount, resolution.ResolvedEnemies.Length);

        if (resolution.UsedFallback)
        {
            string encounterId = string.IsNullOrWhiteSpace(resolution.EncounterId) ? "unknown-encounter" : resolution.EncounterId;
            Debug.LogWarning($"Encounter '{encounterId}' used fallback enemy configuration after payload resolution.");
        }

        SpawnEnemies(resolution.ResolvedEnemies);

        int encounterSeed = ResolveEncounterSeed(payload);
        _currentEncounterSeed = encounterSeed;
        _context = new BattleContext(this, deckManager, stateManager, memoryManager, playerObject, encounterSeed);
        deckManager.SetSeed(encounterSeed);

        _context.SetEnemies(_activeEnemies);
        actionQueue.SetContext(_context);

        deckManager.InitializeDeck();
        deckManager.DrawInitialHand();

        _encounterActive = true;
        _context.TurnNumber = 0;
        _context.IsPlayerTurn = true;
        _lastEncounterPayload = EncounterPayload.FromEnemyData(
            resolution.EncounterId,
            resolution.ResolvedEnemies,
            payload != null ? payload.EnemyLevel : 1,
            payload != null ? payload.SpawnPattern : string.Empty,
            encounterSeed);
        _lastEncounterEnemies = CopyEnemyArray(resolution.ResolvedEnemies);
        _lastEncounterStartingMemory = startingMemorySnapshot;
        _hasEncounterStartMemorySnapshot = memoryManager != null;

        SetFlowState(CombatFlowState.Init);
        OnEncounterStarted?.Invoke(_context);
        StartTurn();
        UpdateEndTurnButtonState();
    }

    public void StartTurn()
    {
        if (!_encounterActive || _context == null || _isPaused)
        {
            return;
        }

        if (TryHandleEncounterEndState())
        {
            return;
        }

        _context.TurnNumber++;
        _context.IsPlayerTurn = true;
        _isWaitingForSkipDiscardSelection = false;

        stateManager?.TickTurnStart();

        SetFlowState(CombatFlowState.PlayerTurn);
        OnTurnStarted?.Invoke(_context.TurnNumber);
        UpdateEndTurnButtonState();
    }

    public void EndTurn()
    {
        EndTurnInternal(true);
    }

    public void EndTurnAfterCardPlay()
    {
        EndTurnInternal(false);
    }

    private void EndTurnInternal(bool isSkipTurn)
    {
        if (!_encounterActive || _context == null || _isPaused)
        {
            return;
        }

        if (!_context.IsPlayerTurn)
        {
            return;
        }

        if (_isWaitingForSkipDiscardSelection)
        {
            return;
        }

        if (isSkipTurn)
        {
            if (deckManager != null && deckManager.TryBeginSkipTurnDraw(HandleSkipTurnDiscardResolved))
            {
                _isWaitingForSkipDiscardSelection = true;
                UpdateEndTurnButtonState();
                return;
            }

            deckManager?.DrawRandomBasicCards(1);
        }

        SetFlowState(CombatFlowState.Resolve);
        ResolveQueue();
        _context.IsPlayerTurn = false;
        UpdateEndTurnButtonState();

        if (TryHandleEncounterEndState())
        {
            return;
        }

        SetFlowState(CombatFlowState.EnemyTurn);
        RunEnemyTurn();
        stateManager?.TickTurnEnd();

        OnTurnEnded?.Invoke(_context.TurnNumber);

        if (TryHandleEncounterEndState())
        {
            return;
        }

        StartTurn();
    }

    private void HandleSkipTurnDiscardResolved()
    {
        _isWaitingForSkipDiscardSelection = false;

        if (!_encounterActive || _context == null || _isPaused || !_context.IsPlayerTurn)
        {
            UpdateEndTurnButtonState();
            return;
        }

        EndTurnInternal(false);
    }

    public void EnqueueAction(IBattleAction action)
    {
        if (!_encounterActive || action == null || _isPaused)
        {
            return;
        }

        actionQueue.Enqueue(action);
    }

    public void ResolveQueue()
    {
        if (!_encounterActive || _isResolvingQueue || _isPaused)
        {
            return;
        }

        _isResolvingQueue = true;

        while (actionQueue.HasPendingActions)
        {
            if (_isPaused)
            {
                break;
            }

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

    public bool SkipTurnToDraw()
    {
        if (!_encounterActive || _context == null || _isPaused || !_context.IsPlayerTurn)
        {
            return false;
        }

        EndTurn();
        return true;
    }

    public bool PauseCombat()
    {
        if (!_encounterActive || _isPaused || FlowState == CombatFlowState.Victory || FlowState == CombatFlowState.Defeated)
        {
            return false;
        }

        _isPaused = true;
        _stateBeforePause = FlowState;
        SetFlowState(CombatFlowState.Paused);

        if (freezeGameplayTimeOnPause)
        {
            _cachedTimeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
        }

        OnCombatPaused?.Invoke();
        UpdateEndTurnButtonState();
        return true;
    }

    public bool ResumeCombat()
    {
        if (!_isPaused)
        {
            return false;
        }

        RestoreTimeScaleAfterPause();
        _isPaused = false;

        if (_encounterActive)
        {
            CombatFlowState resumedState = _stateBeforePause;
            if (resumedState == CombatFlowState.Paused || resumedState == CombatFlowState.Init)
            {
                resumedState = _context != null && _context.IsPlayerTurn
                    ? CombatFlowState.PlayerTurn
                    : CombatFlowState.EnemyTurn;
            }

            SetFlowState(resumedState);
        }
        else
        {
            SetFlowState(CombatFlowState.Init);
        }

        OnCombatResumed?.Invoke();
        UpdateEndTurnButtonState();
        return true;
    }

    public bool RestartEncounter()
    {
        if (_lastEncounterEnemies == null || _lastEncounterEnemies.Length == 0)
        {
            Debug.LogWarning("BattleManager cannot restart because no encounter snapshot is available.");
            return false;
        }

        if (_isPaused)
        {
            RestoreTimeScaleAfterPause();
            _isPaused = false;
        }

        SetFlowState(CombatFlowState.Restarting);

        if (_hasEncounterStartMemorySnapshot && memoryManager != null)
        {
            memoryManager.SetMemory(_lastEncounterStartingMemory);
        }

        EnemyData[] restartEnemies = CopyEnemyArray(_lastEncounterEnemies);
        EncounterPayload restartPayload = _lastEncounterPayload?.Clone();

        StartEncounter(restartEnemies, restartPayload);
        OnCombatRestarted?.Invoke();
        return true;
    }

    public void LoadCheckpointAfterDefeat()
    {
        if (string.IsNullOrWhiteSpace(checkpointSceneName))
        {
            Debug.LogWarning("BattleManager has no checkpointSceneName configured for defeat recovery.");
            return;
        }

        SceneTransitionContext.LoadScene(checkpointSceneName, checkpointEntryPointId);
    }

    public void QuitToMenuAfterDefeat()
    {
        if (string.IsNullOrWhiteSpace(quitToMenuSceneName))
        {
            Debug.LogWarning("BattleManager has no quitToMenuSceneName configured for defeat recovery.");
            return;
        }

        SceneTransitionContext.LoadScene(quitToMenuSceneName, quitToMenuEntryPointId);
    }

    public void ConfigureRuntimeEncounter(EncounterPayload payload, EnemyData[] enemies)
    {
        _runtimeEncounterPayload = payload?.Clone();
        _hasRuntimeEncounterPayload = _runtimeEncounterPayload != null;
        _runtimeEncounterEnemies = CopyEnemyArray(enemies);
    }

    public EnemyData[] GetConfiguredEncounterEnemiesSnapshot()
    {
        return CopyEnemyArray(encounterEnemies);
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

        int maxSpawnCount = Mathf.Min(enemies.Length, CombatConfig.MaxEnemySlots);

        for (int i = 0; i < maxSpawnCount; i++)
        {
            EnemyData enemyData = enemies[i];
            if (enemyData == null)
            {
                continue;
            }

            Transform slot = i < enemySlots.Length && i < CombatConfig.MaxEnemySlots ? enemySlots[i] : null;
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
        GameObject enemyObject = new GameObject($"Enemy_{index}_{enemyData.displayName}", typeof(RectTransform), typeof(Image));
        Transform parent = slot != null ? slot : transform;
        enemyObject.transform.SetParent(parent, false);

        RectTransform rectTransform = enemyObject.GetComponent<RectTransform>();
        if (slot is RectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
        else
        {
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.sizeDelta = new Vector2(100f, 100f);
        }

        Image image = enemyObject.GetComponent<Image>();
        image.raycastTarget = true;
        image.preserveAspect = true;
        image.sprite = enemyData.enemySprite;

        if (image.sprite == null)
        {
            Debug.LogWarning($"Enemy '{enemyData.displayName}' has no sprite assigned. Set Enemy Sprite in EnemyData.");
        }

        return enemyObject.AddComponent<EnemyController>();
    }

    private void ResetEncounterState()
    {
        actionQueue?.Clear();
        actionQueue?.SetContext(null);
        stateManager?.ClearAllStates();
        _context = null;
        _encounterActive = false;
        _isResolvingQueue = false;
        _isWaitingForSkipDiscardSelection = false;

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

        if (_isPaused)
        {
            RestoreTimeScaleAfterPause();
            _isPaused = false;
        }

        _encounterActive = false;
        actionQueue?.Clear();
        _isWaitingForSkipDiscardSelection = false;

        if (playerWon)
        {
            SetFlowState(CombatFlowState.Victory);
            OnCombatVictory?.Invoke();
            RequestCheckpointSaveAfterVictory();

            if (returnToExplorationOnVictory && !string.IsNullOrWhiteSpace(victorySceneName))
            {
                SceneTransitionContext.LoadScene(victorySceneName, victoryEntryPointId);
            }
        }
        else
        {
            SetFlowState(CombatFlowState.Defeated);
            OnCombatDefeated?.Invoke();
        }

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

        bool canInteract = _encounterActive
            && !_isPaused
            && _context != null
            && _context.IsPlayerTurn
            && !_isResolvingQueue
            && !_isWaitingForSkipDiscardSelection
            && FlowState == CombatFlowState.PlayerTurn;
        endTurnButton.interactable = canInteract;
    }

    private EnemyResolutionResult ResolveEncounterEnemies(EnemyData[] requestedEnemies, EncounterPayload payload)
    {
        List<EnemyData> resolved = new List<EnemyData>(CombatConfig.MaxEnemySlots);
        bool usedFallback = false;

        int requestedCount = payload != null && payload.RequestedEnemyCount > 0
            ? payload.RequestedEnemyCount
            : CountValidEnemies(requestedEnemies);

        string encounterId = payload != null && !string.IsNullOrWhiteSpace(payload.EncounterId)
            ? payload.EncounterId
            : "default-encounter";

        AddEnemiesWithClamp(requestedEnemies, resolved);

        if (resolved.Count < CombatConfig.MinEnemySlots)
        {
            int previousCount = resolved.Count;
            AddEnemiesWithClamp(encounterEnemies, resolved);
            usedFallback = resolved.Count > previousCount;
        }

        if (requestedCount <= 0)
        {
            requestedCount = resolved.Count;
        }

        return new EnemyResolutionResult(encounterId, requestedCount, resolved.ToArray(), usedFallback);
    }

    private int ResolveEncounterSeed(EncounterPayload payload)
    {
        if (payload != null && payload.TryGetRngSeed(out int payloadSeed))
        {
            return payloadSeed;
        }

        if (useFixedSeed)
        {
            return fixedSeed;
        }

        return unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode());
    }

    private bool TryHandleEncounterEndState()
    {
        if (IsPlayerDefeated())
        {
            EndEncounterInternal(false);
            return true;
        }

        if (AreAllEnemiesDefeated())
        {
            EndEncounterInternal(true);
            return true;
        }

        return false;
    }

    private void SetFlowState(CombatFlowState nextState)
    {
        if (FlowState == nextState)
        {
            return;
        }

        FlowState = nextState;
        OnCombatFlowStateChanged?.Invoke(nextState);
    }

    private void RequestCheckpointSaveAfterVictory()
    {
        if (!requestCheckpointSaveOnVictory)
        {
            return;
        }

        string encounterId = _lastEncounterPayload != null && !string.IsNullOrWhiteSpace(_lastEncounterPayload.EncounterId)
            ? _lastEncounterPayload.EncounterId
            : "default-encounter";
        string checkpointId = $"battle_{encounterId}_victory";
        OnCheckpointSaveRequested?.Invoke(checkpointId, "Victory");
    }

    private void RestoreTimeScaleAfterPause()
    {
        if (!freezeGameplayTimeOnPause)
        {
            return;
        }

        float nextTimeScale = _cachedTimeScaleBeforePause <= 0f ? 1f : _cachedTimeScaleBeforePause;
        Time.timeScale = nextTimeScale;
        _cachedTimeScaleBeforePause = 1f;
    }

    private void ClearRuntimeEncounterOverride()
    {
        _runtimeEncounterEnemies = Array.Empty<EnemyData>();
        _runtimeEncounterPayload = null;
        _hasRuntimeEncounterPayload = false;
    }

    private static EnemyData[] CopyEnemyArray(IReadOnlyList<EnemyData> source)
    {
        if (source == null || source.Count == 0)
        {
            return Array.Empty<EnemyData>();
        }

        EnemyData[] copy = new EnemyData[source.Count];
        for (int i = 0; i < source.Count; i++)
        {
            copy[i] = source[i];
        }

        return copy;
    }

    private static int CountValidEnemies(IReadOnlyList<EnemyData> enemies)
    {
        if (enemies == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private static void AddEnemiesWithClamp(IReadOnlyList<EnemyData> source, IList<EnemyData> destination)
    {
        if (source == null || destination == null)
        {
            return;
        }

        for (int i = 0; i < source.Count && destination.Count < CombatConfig.MaxEnemySlots; i++)
        {
            EnemyData enemy = source[i];
            if (enemy != null)
            {
                destination.Add(enemy);
            }
        }
    }

    private void OnValidate()
    {
        victorySceneName = (victorySceneName ?? string.Empty).Trim();
        victoryEntryPointId = (victoryEntryPointId ?? string.Empty).Trim();
        checkpointSceneName = (checkpointSceneName ?? string.Empty).Trim();
        checkpointEntryPointId = (checkpointEntryPointId ?? string.Empty).Trim();
        quitToMenuSceneName = (quitToMenuSceneName ?? string.Empty).Trim();
        quitToMenuEntryPointId = (quitToMenuEntryPointId ?? string.Empty).Trim();
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
