using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public class DeckManager : MonoBehaviour
{
    private const int RuleHandLimit = 5;

    [SerializeField] private List<CardData> startingDeck = new List<CardData>();
    [SerializeField, Min(1)] private int handSize = 5;
    [SerializeField] private int drawPerTurn = 0;

    [Header("Skip Turn Draw")]
    [SerializeField] private List<CardData> basicCardPool = new List<CardData>();
    [SerializeField] private bool discardRandomWhenHandIsFullOnSkipDraw = false;
    [SerializeField] private bool requireManualDiscardSelectionOnSkipDraw = true;

    [Header("Combat UI")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardHand;

    [Header("Card Presentation")]
    [SerializeField] private bool usePrefabPresentationOnly = true;
    [SerializeField] private bool keepEmptyHandSlots = true;
    [SerializeField] private Vector2 emptySlotSize = new Vector2(120f, 160f);

    [Header("Battle Wiring")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private BattleCardEffectPresenter cardEffectPresenter;

    private readonly List<CardData> _drawPile = new List<CardData>();
    private readonly List<CardData> _hand = new List<CardData>();
    private readonly List<CardData> _discardPile = new List<CardData>();
    private readonly List<CardData> _exilePile = new List<CardData>();
    private readonly List<CardData> _resolvedBasicPool = new List<CardData>(5);
    private readonly List<GameObject> _spawnedHandCards = new List<GameObject>();
    private readonly List<GameObject> _spawnedHandSlots = new List<GameObject>();

    private BattleCardView _armedCardView;
    private CardData _pendingTargetCardData;
    private bool _awaitingSkipTurnDiscardSelection;
    private Action _skipTurnDiscardResolvedCallback;
    private bool _battleEventsHooked;

    private System.Random _random = new System.Random();

    public IReadOnlyList<CardData> DrawPile => _drawPile;
    public IReadOnlyList<CardData> Hand => _hand;
    public IReadOnlyList<CardData> DiscardPile => _discardPile;
    public IReadOnlyList<CardData> ExilePile => _exilePile;
    public int HandLimit => GetHandLimit();

    public event Action OnDeckInitialized;
    public event Action<CardData> OnCardDrawn;
    public event Action<CardData> OnCardDiscarded;
    public event Action<CardData> OnCardExiled;
    public event Action OnHandChanged;
    public event Action OnDeckEmpty;

    public bool IsAwaitingSkipTurnDiscardSelection => _awaitingSkipTurnDiscardSelection;

    private void OnEnable()
    {
        ResolveRuntimeReferences();
        SubscribeBattleEvents();
        OnHandChanged += RebuildHandView;
    }

    private void OnDisable()
    {
        OnHandChanged -= RebuildHandView;
        UnsubscribeBattleEvents();
        ClearArmedCardSelection();
        ClearPendingEnemyTargetSelection(false);
        ClearSkipTurnDiscardSelectionState(false);
    }

    private void OnDestroy()
    {
        ClearSpawnedHandSlots();
        ClearSpawnedHandCards();
    }

    private void Awake()
    {
        ResolveRuntimeReferences();
    }

    public void SetSeed(int seed)
    {
        _random = new System.Random(seed);
    }

    public void InitializeDeck()
    {
        _drawPile.Clear();
        _hand.Clear();
        _discardPile.Clear();
        _exilePile.Clear();
        ClearArmedCardSelection();
        ClearPendingEnemyTargetSelection(false);
        ClearSkipTurnDiscardSelectionState(false);

        for (int i = 0; i < startingDeck.Count; i++)
        {
            CardData card = startingDeck[i];
            if (card != null)
            {
                _drawPile.Add(card);
            }
        }

        Shuffle(_drawPile);
        OnDeckInitialized?.Invoke();
        OnHandChanged?.Invoke();
    }

    public void DrawInitialHand()
    {
        DrawCards(GetHandLimit());
    }

    public void DrawForTurn()
    {
        DrawCards(Mathf.Max(0, drawPerTurn));
    }

    public bool TryBeginSkipTurnDraw(Action onSkipTurnResolved)
    {
        if (!requireManualDiscardSelectionOnSkipDraw || !IsHandFull())
        {
            return false;
        }

        if (!CanInteractWithCards())
        {
            return false;
        }

        if (_awaitingSkipTurnDiscardSelection)
        {
            return true;
        }

        _awaitingSkipTurnDiscardSelection = true;
        _skipTurnDiscardResolvedCallback = onSkipTurnResolved;
        ClearArmedCardSelection();
        ClearPendingEnemyTargetSelection(false);
        RefreshSpawnedCardInteractivity();

        Debug.Log("Hand is full. Select one card in hand to discard before drawing a basic card.");
        return true;
    }

    public int DrawRandomBasicCards(int count)
    {
        int requestedCount = Mathf.Max(0, count);
        if (requestedCount <= 0)
        {
            return 0;
        }

        IReadOnlyList<CardData> sourcePool = ResolveBasicCardPool();
        if (sourcePool == null || sourcePool.Count == 0)
        {
            Debug.LogWarning("DeckManager has no basic card pool configured for skip-turn draw.");
            return 0;
        }

        int handLimit = GetHandLimit();
        int drawn = 0;
        bool handChanged = false;

        for (int i = 0; i < requestedCount; i++)
        {
            if (_hand.Count >= handLimit)
            {
                if (!discardRandomWhenHandIsFullOnSkipDraw || !DiscardOneRandomFromHandSilently())
                {
                    break;
                }

                handChanged = true;
            }

            CardData randomBasic = PickRandomValidCard(sourcePool);
            if (randomBasic == null)
            {
                Debug.LogWarning("DeckManager basic card pool does not contain any valid card entries.");
                break;
            }

            _hand.Add(randomBasic);
            drawn++;
            handChanged = true;
            OnCardDrawn?.Invoke(randomBasic);
        }

        if (handChanged)
        {
            OnHandChanged?.Invoke();
        }

        return drawn;
    }

    public int DrawCards(int count)
    {
        int drawn = 0;
        int handLimit = GetHandLimit();

        for (int i = 0; i < count; i++)
        {
            if (_hand.Count >= handLimit)
            {
                break;
            }

            CardData card = DrawSingle();
            if (card == null)
            {
                break;
            }

            _hand.Add(card);
            drawn++;
            OnCardDrawn?.Invoke(card);
        }

        if (drawn > 0)
        {
            OnHandChanged?.Invoke();
        }

        return drawn;
    }

    public void DiscardCard(CardData card)
    {
        if (card == null)
        {
            return;
        }

        if (_hand.Remove(card))
        {
            _discardPile.Add(card);
            OnCardDiscarded?.Invoke(card);
            OnHandChanged?.Invoke();
            return;
        }

        if (_drawPile.Remove(card))
        {
            _discardPile.Add(card);
            OnCardDiscarded?.Invoke(card);
            return;
        }

        if (_exilePile.Remove(card))
        {
            _discardPile.Add(card);
            OnCardDiscarded?.Invoke(card);
        }
    }

    public void ExileCard(CardData card)
    {
        if (card == null)
        {
            return;
        }

        bool removedFromHand = _hand.Remove(card);
        bool removed = removedFromHand || _drawPile.Remove(card) || _discardPile.Remove(card);
        if (!removed)
        {
            return;
        }

        _exilePile.Add(card);
        OnCardExiled?.Invoke(card);

        if (removedFromHand)
        {
            OnHandChanged?.Invoke();
        }
    }

    public void ShuffleDiscardIntoDeck()
    {
        if (_discardPile.Count == 0)
        {
            return;
        }

        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle(_drawPile);
    }

    public int DiscardRandomFromHand(int count)
    {
        int amount = Mathf.Min(Mathf.Max(0, count), _hand.Count);
        int discarded = 0;

        for (int i = 0; i < amount; i++)
        {
            int index = _random.Next(0, _hand.Count);
            CardData card = _hand[index];
            _hand.RemoveAt(index);
            _discardPile.Add(card);
            discarded++;
            OnCardDiscarded?.Invoke(card);
        }

        if (discarded > 0)
        {
            OnHandChanged?.Invoke();
        }

        return discarded;
    }

    public int ExileRandomFromHand(int count)
    {
        int amount = Mathf.Min(Mathf.Max(0, count), _hand.Count);
        int exiled = 0;

        for (int i = 0; i < amount; i++)
        {
            int index = _random.Next(0, _hand.Count);
            CardData card = _hand[index];
            _hand.RemoveAt(index);
            _exilePile.Add(card);
            exiled++;
            OnCardExiled?.Invoke(card);
        }

        if (exiled > 0)
        {
            OnHandChanged?.Invoke();
        }

        return exiled;
    }

    public int ReturnFromDiscardToHand(int count)
    {
        int amount = Mathf.Min(Mathf.Max(0, count), _discardPile.Count);
        int returned = 0;

        for (int i = 0; i < amount; i++)
        {
            int index = _random.Next(0, _discardPile.Count);
            CardData card = _discardPile[index];
            _discardPile.RemoveAt(index);
            _hand.Add(card);
            returned++;
        }

        if (returned > 0)
        {
            OnHandChanged?.Invoke();
        }

        return returned;
    }

    public bool TryMoveCardFromHandToDiscard(CardData card)
    {
        if (card == null || !_hand.Remove(card))
        {
            return false;
        }

        _discardPile.Add(card);
        OnCardDiscarded?.Invoke(card);
        OnHandChanged?.Invoke();
        return true;
    }

    public void SetCardHandRoot(Transform handRoot)
    {
        cardHand = handRoot;
        RebuildHandView();
    }

    public void HandleCardClicked(BattleCardView cardView)
    {
        if (cardView == null || cardView.CardData == null)
        {
            return;
        }

        if (_awaitingSkipTurnDiscardSelection)
        {
            if (!CanInteractWithCards())
            {
                return;
            }

            ResolveSkipTurnDiscardSelection(cardView.CardData);
            return;
        }

        if (!CanInteractWithCards())
        {
            return;
        }

        if (!HasCardInHand(cardView.CardData))
        {
            if (_pendingTargetCardData == cardView.CardData)
            {
                ClearPendingEnemyTargetSelection();
            }

            cardView.SetArmed(false);
            if (_armedCardView == cardView)
            {
                _armedCardView = null;
            }

            return;
        }

        if (_armedCardView != null && _armedCardView != cardView)
        {
            _armedCardView.SetArmed(false);
        }

        if (_armedCardView != cardView)
        {
            _armedCardView = cardView;
            _armedCardView.SetArmed(true);
            ClearPendingEnemyTargetSelection();
            RefreshEnemyTargetIndicators();
            return;
        }

        if (CardRequiresSingleEnemyTarget(cardView.CardData))
        {
            if (_pendingTargetCardData == cardView.CardData)
            {
                ClearPendingEnemyTargetSelection();
                _armedCardView.SetArmed(false);
                _armedCardView = null;
                RefreshSpawnedCardInteractivity();
                return;
            }

            BeginEnemyTargetSelection(cardView);
            return;
        }

        _armedCardView.SetArmed(false);
        _armedCardView = null;
        ClearPendingEnemyTargetSelection();

        TryPlayCardFromHand(cardView.CardData, null);
        RefreshSpawnedCardInteractivity();
    }

    public void HandleEnemyClicked(EnemyController enemy)
    {
        if (enemy == null || !enemy.IsAlive)
        {
            return;
        }

        if (!CanInteractWithCards())
        {
            return;
        }

        if (_pendingTargetCardData == null)
        {
            return;
        }

        if (!HasCardInHand(_pendingTargetCardData))
        {
            ClearPendingEnemyTargetSelection();
            RefreshSpawnedCardInteractivity();
            return;
        }

        CardData cardToPlay = _pendingTargetCardData;
        ClearPendingEnemyTargetSelection(false);

        if (_armedCardView != null)
        {
            _armedCardView.SetArmed(false);
            _armedCardView = null;
        }

        TryPlayCardFromHand(cardToPlay, enemy.gameObject);
        RefreshSpawnedCardInteractivity();
    }

    public bool HasCardInHand(CardData cardData)
    {
        return cardData != null && _hand.Contains(cardData);
    }

    public bool IsHandFull()
    {
        return _hand.Count >= GetHandLimit();
    }

    private CardData DrawSingle()
    {
        if (_drawPile.Count == 0)
        {
            ShuffleDiscardIntoDeck();
        }

        if (_drawPile.Count == 0)
        {
            OnDeckEmpty?.Invoke();
            return null;
        }

        int topIndex = _drawPile.Count - 1;
        CardData card = _drawPile[topIndex];
        _drawPile.RemoveAt(topIndex);
        return card;
    }

    private void Shuffle(List<CardData> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int swapIndex = _random.Next(0, i + 1);
            (cards[i], cards[swapIndex]) = (cards[swapIndex], cards[i]);
        }
    }

    private void RebuildHandView()
    {
        if (cardHand == null)
        {
            return;
        }

        ClearArmedCardSelection();
        ClearPendingEnemyTargetSelection(false);
        ClearSpawnedHandCards();
        ClearSpawnedHandSlots();

        int slotCount = keepEmptyHandSlots ? GetHandLimit() : _hand.Count;
        slotCount = Mathf.Max(slotCount, _hand.Count);

        for (int i = 0; i < slotCount; i++)
        {
            Transform cardParent = cardHand;
            if (keepEmptyHandSlots)
            {
                GameObject slotObject = CreateHandSlot(i);
                _spawnedHandSlots.Add(slotObject);
                cardParent = slotObject.transform;
            }

            if (i >= _hand.Count)
            {
                continue;
            }

            CardData card = _hand[i];
            if (card == null)
            {
                continue;
            }

            GameObject prefabToSpawn = ResolvePrefabForCard(card);
            if (prefabToSpawn == null)
            {
                continue;
            }

            GameObject cardInstance = Instantiate(prefabToSpawn, cardParent);
            cardInstance.name = $"Card_{card.displayName}_{i + 1}";
            BindCardPrefab(cardInstance, card);
            BindCardInteraction(cardInstance, card);
            _spawnedHandCards.Add(cardInstance);
        }

        RefreshSpawnedCardInteractivity();
    }

    private GameObject ResolvePrefabForCard(CardData cardData)
    {
        if (cardData != null && cardData.cardPrefabOverride != null)
        {
            return cardData.cardPrefabOverride;
        }

        return cardPrefab;
    }

    private void ClearSpawnedHandCards()
    {
        for (int i = 0; i < _spawnedHandCards.Count; i++)
        {
            GameObject card = _spawnedHandCards[i];
            if (card == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(card);
            }
            else
            {
                DestroyImmediate(card);
            }
        }

        _spawnedHandCards.Clear();
    }

    private void ClearSpawnedHandSlots()
    {
        for (int i = 0; i < _spawnedHandSlots.Count; i++)
        {
            GameObject slot = _spawnedHandSlots[i];
            if (slot == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(slot);
            }
            else
            {
                DestroyImmediate(slot);
            }
        }

        _spawnedHandSlots.Clear();
    }

    private GameObject CreateHandSlot(int index)
    {
        GameObject slotObject = new GameObject($"CardSlot_{index + 1}", typeof(RectTransform), typeof(LayoutElement));
        slotObject.transform.SetParent(cardHand, false);

        LayoutElement layoutElement = slotObject.GetComponent<LayoutElement>();
        layoutElement.minWidth = emptySlotSize.x;
        layoutElement.preferredWidth = emptySlotSize.x;
        layoutElement.minHeight = emptySlotSize.y;
        layoutElement.preferredHeight = emptySlotSize.y;
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;

        return slotObject;
    }

    private void BindCardInteraction(GameObject cardInstance, CardData cardData)
    {
        if (cardInstance == null || cardData == null)
        {
            return;
        }

        BattleCardView cardView = cardInstance.GetComponent<BattleCardView>();
        if (cardView == null)
        {
            cardView = cardInstance.AddComponent<BattleCardView>();
        }

        cardView.Initialize(this, cardData);
    }

    private bool TryPlayCardFromHand(CardData cardData, GameObject explicitTarget)
    {
        if (cardData == null || !HasCardInHand(cardData))
        {
            return false;
        }

        ResolveRuntimeReferences();
        if (battleManager == null)
        {
            Debug.LogWarning("DeckManager cannot play cards because BattleManager is missing.");
            return false;
        }

        BattleContext context = battleManager.CurrentContext;
        if (context == null || !battleManager.IsEncounterActive || battleManager.IsPaused || !context.IsPlayerTurn)
        {
            return false;
        }

        int maxMemory = context.MemoryManager != null ? Mathf.RoundToInt(context.MemoryManager.MaxMemoryPercent) : 100;
        int memoryCost = cardData.GetMemoryCost(maxMemory);

        if (context.MemoryManager != null && !context.MemoryManager.CanSpend(memoryCost))
        {
            Debug.Log($"Not enough Memory to arm card '{cardData.displayName}'.");
            return false;
        }

        PlayCardAction action = new PlayCardAction();
        GameObject resolvedTarget = ResolveActionTarget(context, cardData, explicitTarget);
        action.Setup(cardData, context.PlayerObject, resolvedTarget);

        battleManager.EnqueueAction(action);
        battleManager.ResolveQueue();

        if (!action.WasResolvedSuccessfully)
        {
            return false;
        }

        cardEffectPresenter?.PlayForCard(cardData);
        battleManager.EndTurnAfterCardPlay();
        return true;
    }

    private bool CanInteractWithCards()
    {
        ResolveRuntimeReferences();
        if (battleManager == null)
        {
            return false;
        }

        BattleContext context = battleManager.CurrentContext;
        return battleManager.IsEncounterActive
            && !battleManager.IsPaused
            && context != null
            && context.IsPlayerTurn;
    }

    private void RefreshSpawnedCardInteractivity()
    {
        bool canInteract = CanInteractWithCards();
        for (int i = 0; i < _spawnedHandCards.Count; i++)
        {
            GameObject cardObject = _spawnedHandCards[i];
            if (cardObject == null)
            {
                continue;
            }

            BattleCardView cardView = cardObject.GetComponent<BattleCardView>();
            if (cardView != null)
            {
                cardView.SetInteractable(canInteract);
            }
        }

        RefreshEnemyTargetIndicators();
    }

    private void ClearArmedCardSelection()
    {
        if (_armedCardView != null)
        {
            _armedCardView.SetArmed(false);
        }

        _armedCardView = null;
    }

    private int GetHandLimit()
    {
        return Mathf.Clamp(handSize, 1, RuleHandLimit);
    }

    private void BeginEnemyTargetSelection(BattleCardView cardView)
    {
        if (cardView == null || cardView.CardData == null)
        {
            return;
        }

        EnemyController onlyAliveEnemy = GetOnlyAliveEnemy();
        if (onlyAliveEnemy != null)
        {
            CardData cardToPlay = cardView.CardData;
            _armedCardView?.SetArmed(false);
            _armedCardView = null;
            ClearPendingEnemyTargetSelection(false);
            TryPlayCardFromHand(cardToPlay, onlyAliveEnemy.gameObject);
            RefreshSpawnedCardInteractivity();
            return;
        }

        _pendingTargetCardData = cardView.CardData;
        RefreshEnemyTargetIndicators();
        Debug.Log($"Card '{_pendingTargetCardData.displayName}' is waiting for an enemy target.");
    }

    private void ClearPendingEnemyTargetSelection(bool refreshIndicators = true)
    {
        _pendingTargetCardData = null;
        if (refreshIndicators)
        {
            RefreshEnemyTargetIndicators();
        }
    }

    private void ResolveSkipTurnDiscardSelection(CardData cardData)
    {
        if (!_awaitingSkipTurnDiscardSelection || cardData == null)
        {
            return;
        }

        if (!HasCardInHand(cardData))
        {
            return;
        }

        _hand.Remove(cardData);
        _discardPile.Add(cardData);
        OnCardDiscarded?.Invoke(cardData);

        Action resolvedCallback = _skipTurnDiscardResolvedCallback;
        _awaitingSkipTurnDiscardSelection = false;
        _skipTurnDiscardResolvedCallback = null;

        int drawn = DrawRandomBasicCards(1);
        if (drawn <= 0)
        {
            OnHandChanged?.Invoke();
        }

        RefreshSpawnedCardInteractivity();
        resolvedCallback?.Invoke();
    }

    private void ClearSkipTurnDiscardSelectionState(bool invokeCallback)
    {
        if (!_awaitingSkipTurnDiscardSelection && _skipTurnDiscardResolvedCallback == null)
        {
            return;
        }

        Action resolvedCallback = _skipTurnDiscardResolvedCallback;
        _awaitingSkipTurnDiscardSelection = false;
        _skipTurnDiscardResolvedCallback = null;

        if (invokeCallback)
        {
            resolvedCallback?.Invoke();
        }
    }

    private void RefreshEnemyTargetIndicators()
    {
        ResolveRuntimeReferences();
        if (battleManager == null)
        {
            return;
        }

        BattleContext context = battleManager.CurrentContext;
        if (context == null)
        {
            return;
        }

        bool canSelectEnemyTarget = battleManager.IsEncounterActive
            && !battleManager.IsPaused
            && context.IsPlayerTurn
            && _pendingTargetCardData != null
            && CardRequiresSingleEnemyTarget(_pendingTargetCardData)
            && HasCardInHand(_pendingTargetCardData);

        IReadOnlyList<EnemyController> enemies = context.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            EnemyController enemy = enemies[i];
            if (enemy == null)
            {
                continue;
            }

            enemy.SetTargetSelectionState(canSelectEnemyTarget && enemy.IsAlive);
        }
    }

    private bool CardRequiresSingleEnemyTarget(CardData cardData)
    {
        if (cardData == null || cardData.effects == null)
        {
            return false;
        }

        for (int i = 0; i < cardData.effects.Count; i++)
        {
            EffectData effect = cardData.effects[i];
            if (effect == null)
            {
                continue;
            }

            if (effect.effectType == EffectType.Damage && effect.targetScope == TargetScope.Single)
            {
                return true;
            }
        }

        return false;
    }

    private GameObject ResolveActionTarget(BattleContext context, CardData cardData, GameObject explicitTarget)
    {
        if (explicitTarget != null)
        {
            return explicitTarget;
        }

        if (!CardRequiresSingleEnemyTarget(cardData))
        {
            return null;
        }

        EnemyController targetEnemy = context.GetPrimaryAliveEnemy();
        return targetEnemy != null ? targetEnemy.gameObject : null;
    }

    private EnemyController GetOnlyAliveEnemy()
    {
        ResolveRuntimeReferences();
        if (battleManager == null)
        {
            return null;
        }

        BattleContext context = battleManager.CurrentContext;
        if (context == null)
        {
            return null;
        }

        IReadOnlyList<EnemyController> aliveEnemies = context.GetAliveEnemies();
        return aliveEnemies.Count == 1 ? aliveEnemies[0] : null;
    }

    private IReadOnlyList<CardData> ResolveBasicCardPool()
    {
        _resolvedBasicPool.Clear();

        IReadOnlyList<CardData> sourcePool = basicCardPool != null && basicCardPool.Count > 0
            ? basicCardPool
            : startingDeck;

        if (sourcePool == null || sourcePool.Count == 0)
        {
            return _resolvedBasicPool;
        }

        int maxBasicCards = Mathf.Min(5, sourcePool.Count);
        for (int i = 0; i < maxBasicCards; i++)
        {
            CardData card = sourcePool[i];
            if (card != null)
            {
                _resolvedBasicPool.Add(card);
            }
        }

        return _resolvedBasicPool;
    }

    private bool DiscardOneRandomFromHandSilently()
    {
        if (_hand.Count <= 0)
        {
            return false;
        }

        int randomIndex = _random.Next(0, _hand.Count);
        CardData discardedCard = _hand[randomIndex];
        _hand.RemoveAt(randomIndex);
        _discardPile.Add(discardedCard);
        OnCardDiscarded?.Invoke(discardedCard);
        return true;
    }

    private CardData PickRandomValidCard(IReadOnlyList<CardData> sourcePool)
    {
        if (sourcePool == null || sourcePool.Count <= 0)
        {
            return null;
        }

        int attempts = sourcePool.Count;
        for (int i = 0; i < attempts; i++)
        {
            CardData candidate = sourcePool[_random.Next(0, sourcePool.Count)];
            if (candidate != null)
            {
                return candidate;
            }
        }

        for (int i = 0; i < sourcePool.Count; i++)
        {
            if (sourcePool[i] != null)
            {
                return sourcePool[i];
            }
        }

        return null;
    }

    private void ResolveRuntimeReferences()
    {
        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
        }

        if (cardEffectPresenter == null)
        {
            cardEffectPresenter = FindFirstObjectByType<BattleCardEffectPresenter>();
            if (cardEffectPresenter == null)
            {
                GameObject presenterObject = new GameObject(nameof(BattleCardEffectPresenter));
                cardEffectPresenter = presenterObject.AddComponent<BattleCardEffectPresenter>();
            }
        }

        if (isActiveAndEnabled)
        {
            SubscribeBattleEvents();
        }
    }

    private void SubscribeBattleEvents()
    {
        if (_battleEventsHooked)
        {
            return;
        }

        if (battleManager == null)
        {
            return;
        }

        battleManager.OnTurnStarted += HandleTurnStarted;
        battleManager.OnTurnEnded += HandleTurnEnded;
        battleManager.OnEncounterEnded += HandleEncounterEnded;
        _battleEventsHooked = true;
    }

    private void UnsubscribeBattleEvents()
    {
        if (!_battleEventsHooked || battleManager == null)
        {
            return;
        }

        battleManager.OnTurnStarted -= HandleTurnStarted;
        battleManager.OnTurnEnded -= HandleTurnEnded;
        battleManager.OnEncounterEnded -= HandleEncounterEnded;
        _battleEventsHooked = false;
    }

    private void HandleTurnStarted(int turnNumber)
    {
        RefreshSpawnedCardInteractivity();
    }

    private void HandleTurnEnded(int turnNumber)
    {
        ClearArmedCardSelection();
        ClearPendingEnemyTargetSelection(false);
        ClearSkipTurnDiscardSelectionState(false);
        RefreshSpawnedCardInteractivity();
    }

    private void HandleEncounterEnded(bool playerWon)
    {
        ClearArmedCardSelection();
        ClearPendingEnemyTargetSelection();
        ClearSkipTurnDiscardSelectionState(false);
        RefreshSpawnedCardInteractivity();
    }

    private void BindCardPrefab(GameObject cardInstance, CardData cardData)
    {
        if (cardInstance == null || cardData == null)
        {
            return;
        }

        // Keep all visuals/text authored directly in the prefab when enabled.
        if (usePrefabPresentationOnly)
        {
            return;
        }

        TMP_Text costLabel = FindFirstNamedComponent<TMP_Text>(
            cardInstance.transform,
            "CostLabel",
            "Cost",
            "ManaCost");
        if (costLabel != null)
        {
            costLabel.text = $"{Mathf.Clamp(cardData.costPercentage, 0, 100)}%";
        }

        TMP_Text cardName = FindFirstNamedComponent<TMP_Text>(
            cardInstance.transform,
            "CardName",
            "Name",
            "Title");
        if (cardName != null)
        {
            cardName.text = string.IsNullOrWhiteSpace(cardData.displayName) ? "Card" : cardData.displayName;
        }

        TMP_Text flavorText = FindFirstNamedComponent<TMP_Text>(
            cardInstance.transform,
            "FlavorText",
            "Description",
            "BodyText");
        if (flavorText != null)
        {
            flavorText.text = cardData.flavorText ?? string.Empty;
        }

        Transform selectionHighlight = FindFirstNamedTransform(
            cardInstance.transform,
            "SelectionHighlight",
            "Highlight",
            "Selected");
        if (selectionHighlight != null)
        {
            selectionHighlight.gameObject.SetActive(false);
        }
    }

    private static T FindFirstNamedComponent<T>(Transform root, params string[] objectNames) where T : Component
    {
        if (root == null || objectNames == null)
        {
            return null;
        }

        for (int i = 0; i < objectNames.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(objectNames[i]))
            {
                continue;
            }

            T component = FindNamedComponent<T>(root, objectNames[i]);
            if (component != null)
            {
                return component;
            }
        }

        return null;
    }

    private static Transform FindFirstNamedTransform(Transform root, params string[] objectNames)
    {
        if (root == null || objectNames == null)
        {
            return null;
        }

        for (int i = 0; i < objectNames.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(objectNames[i]))
            {
                continue;
            }

            Transform found = FindNamedTransform(root, objectNames[i]);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static T FindNamedComponent<T>(Transform root, string objectName) where T : Component
    {
        Transform namedTransform = FindNamedTransform(root, objectName);
        if (namedTransform != null && namedTransform.TryGetComponent(out T component))
        {
            return component;
        }

        T[] candidates = root.GetComponentsInChildren<T>(true);
        for (int i = 0; i < candidates.Length; i++)
        {
            if (string.Equals(candidates[i].name, objectName, StringComparison.OrdinalIgnoreCase))
            {
                return candidates[i];
            }
        }

        return null;
    }

    private static Transform FindNamedTransform(Transform root, string objectName)
    {
        if (root == null || string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        if (string.Equals(root.name, objectName, StringComparison.OrdinalIgnoreCase))
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            Transform found = FindNamedTransform(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private void OnValidate()
    {
        handSize = Mathf.Clamp(handSize, 1, RuleHandLimit);
        drawPerTurn = Mathf.Max(0, drawPerTurn);
        emptySlotSize.x = Mathf.Max(1f, emptySlotSize.x);
        emptySlotSize.y = Mathf.Max(1f, emptySlotSize.y);

        if (requireManualDiscardSelectionOnSkipDraw)
        {
            discardRandomWhenHandIsFullOnSkipDraw = false;
        }
    }
}
