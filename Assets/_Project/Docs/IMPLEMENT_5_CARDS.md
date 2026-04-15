# Implement 5 Cards (CardData + Prefabs + DeckManager)

## Goal
Use the 5 card prefabs in Assets/_Project/Gameplay/Prefabs/CardPrefabs with CardData ScriptableObjects, then load them through DeckManager in CombatScene.

## Current Project Status
- Existing card prefabs:
  - euphoria_card.prefab
  - nostalgia_card.prefab
  - pain_card.prefab
  - void_card.prefab
  - wraith_card.prefab
- Existing CardData assets in Assets/_Project/Gameplay/Data/Cards:
  - Void.asset

## 1) Create CardData Assets
In Unity Editor:
1. Open folder Assets/_Project/Gameplay/Data/Cards.
2. Right click -> Create -> Memories -> Data -> Card.
3. Create these assets:
   - Euphoria.asset
   - Nostalgia.asset
   - Pain.asset
   - Void.asset (already exists)
   - Wrath.asset

## 2) Fill CardData Fields
For each CardData asset, set:
- Card ID: unique string (example: euphoria, nostalgia, pain, void, wrath)
- Display Name: player-facing card name
- Type: Attack / Support / Skill / Control / Utility
- Presentation -> Card Prefab Override: assign matching prefab
- Cost Percentage: memory cost in percent
- Effects: add one or more effect entries
- Flavor Text: optional
- Sprite 1 Bit: matching card art sprite

Suggested mapping:

| CardData Asset | Display Name | Prefab Override | Type | Cost % | Effects (suggested) |
|---|---|---|---|---:|---|
| Euphoria.asset | Euphoria | euphoria_card.prefab | Support | 0 | HealMemory: flatValue 20 |
| Nostalgia.asset | Nostalgia | nostalgia_card.prefab | Utility | 5 | ReturnFromDiscard: flatValue 1 |
| Pain.asset | Pain | pain_card.prefab | Attack | 15 | Damage: flatValue 30, target Single |
| Void.asset | Void | void_card.prefab | Control | 10 | ApplyStatus: statusId Lost, duration 1, target AllEnemies |
| Wrath.asset | Wrath | wraith_card.prefab | Skill | 25 | Damage: flatValue 10, target AllEnemies |

Notes:
- Cost Percentage is percentage of Max Memory. With Max Memory = 100, cost value equals the percentage number.
- If you want multi-effect cards, add multiple entries in Effects.

## 3) Setup DeckManager in CombatScene
1. Select CombatManagers object.
2. In DeckManager:
   - Card Hand -> drag Canvas/MiddleTier/CardHand
   - Starting Deck -> add your 5 CardData assets
   - Card Prefab -> optional fallback prefab (used when Card Prefab Override is empty)

How card spawning works now:
- If CardData.Card Prefab Override is assigned, DeckManager instantiates that prefab.
- If not assigned, DeckManager uses DeckManager.Card Prefab as fallback.

## 4) Add Card Copies to Deck
To have multiple copies of a card, add the same CardData asset multiple times in Starting Deck.

Example 10-card starting deck:
- 2x Euphoria
- 2x Nostalgia
- 2x Pain
- 2x Void
- 2x Wrath

## 5) Play Mode Validation
1. Press Play in CombatScene.
2. Confirm cards appear under CardHand.
3. Confirm name and cost text updates from CardData.
4. Play cards and verify effects update battle state.
5. End turn and verify turn loop continues.

## Troubleshooting
- Card not visible:
  - Card Prefab Override missing and DeckManager.Card Prefab is also empty.
  - CardHand reference missing in DeckManager.
- Wrong card text:
  - Check Display Name and Cost Percentage in CardData.
- Partial text binding on prefab:
  - Current DeckManager supports both naming styles for labels:
    - CardName and CostLabel
    - Name and Cost
- Effect does nothing:
  - Check Effect Type, Flat Value, Target Scope, and required fields like statusId.
