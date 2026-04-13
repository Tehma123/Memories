using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private List<CardData> startingDeck = new List<CardData>();
    [SerializeField] private int handSize = 5;
    [SerializeField] private int drawPerTurn = 1;

    [Header("Combat UI")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardHand;

    private readonly List<CardData> _drawPile = new List<CardData>();
    private readonly List<CardData> _hand = new List<CardData>();
    private readonly List<CardData> _discardPile = new List<CardData>();
    private readonly List<CardData> _exilePile = new List<CardData>();
    private readonly List<GameObject> _spawnedHandCards = new List<GameObject>();

    private System.Random _random = new System.Random();

    public IReadOnlyList<CardData> DrawPile => _drawPile;
    public IReadOnlyList<CardData> Hand => _hand;
    public IReadOnlyList<CardData> DiscardPile => _discardPile;
    public IReadOnlyList<CardData> ExilePile => _exilePile;

    public event Action OnDeckInitialized;
    public event Action<CardData> OnCardDrawn;
    public event Action<CardData> OnCardDiscarded;
    public event Action<CardData> OnCardExiled;
    public event Action OnHandChanged;
    public event Action OnDeckEmpty;

    private void OnEnable()
    {
        OnHandChanged += RebuildHandView;
    }

    private void OnDisable()
    {
        OnHandChanged -= RebuildHandView;
    }

    private void OnDestroy()
    {
        ClearSpawnedHandCards();
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
        DrawCards(Mathf.Max(1, handSize));
    }

    public void DrawForTurn()
    {
        DrawCards(Mathf.Max(0, drawPerTurn));
    }

    public int DrawCards(int count)
    {
        int drawn = 0;
        for (int i = 0; i < count; i++)
        {
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
        if (cardPrefab == null || cardHand == null)
        {
            return;
        }

        ClearSpawnedHandCards();

        for (int i = 0; i < _hand.Count; i++)
        {
            CardData card = _hand[i];
            if (card == null)
            {
                continue;
            }

            GameObject cardInstance = Instantiate(cardPrefab, cardHand);
            cardInstance.name = $"Card_{card.displayName}_{i + 1}";
            BindCardPrefab(cardInstance, card);
            _spawnedHandCards.Add(cardInstance);
        }
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

    private static void BindCardPrefab(GameObject cardInstance, CardData cardData)
    {
        if (cardInstance == null || cardData == null)
        {
            return;
        }

        Image backgroundImage = FindNamedComponent<Image>(cardInstance.transform, "CardBackground");
        if (backgroundImage != null && cardData.sprite1Bit != null)
        {
            backgroundImage.sprite = cardData.sprite1Bit;
        }

        Image cardArt = FindNamedComponent<Image>(cardInstance.transform, "CardArt");
        if (cardArt != null)
        {
            cardArt.sprite = cardData.sprite1Bit;
            cardArt.enabled = cardData.sprite1Bit != null;
        }

        TMP_Text costLabel = FindNamedComponent<TMP_Text>(cardInstance.transform, "CostLabel");
        if (costLabel != null)
        {
            costLabel.text = $"{Mathf.Clamp(cardData.costPercentage, 0, 100)}%";
        }

        TMP_Text cardName = FindNamedComponent<TMP_Text>(cardInstance.transform, "CardName");
        if (cardName != null)
        {
            cardName.text = string.IsNullOrWhiteSpace(cardData.displayName) ? "Card" : cardData.displayName;
        }

        TMP_Text flavorText = FindNamedComponent<TMP_Text>(cardInstance.transform, "FlavorText");
        if (flavorText != null)
        {
            flavorText.text = cardData.flavorText ?? string.Empty;
        }

        Transform selectionHighlight = FindNamedTransform(cardInstance.transform, "SelectionHighlight");
        if (selectionHighlight != null)
        {
            selectionHighlight.gameObject.SetActive(false);
        }
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
}
