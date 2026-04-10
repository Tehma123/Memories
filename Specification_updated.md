*** Specifications — Memories (restructured) ***

## Tóm tắt
- Thể loại: Game 1-bit, turn-based card combat lồng trong exploration.
- Chủ đề: Hồi tưởng, tội lỗi, tự nhận thức và cú twist bi thảm.

## Mục tiêu tài liệu
- Tạo bản tổng hợp rõ ràng cho đội phát triển: cốt truyện, gameplay, UI, dữ liệu (SO), và các scripts lõi.

---

## Thiết bị & Nền tảng: PC (Windows/Mac)

## Engine: Unity 2D (URP, C#)

##  Độ phân giải : 1920x1080 (16:9)

## Hiệu năng mục tiêu (fps, memory): 60 fps ổn định trên phần cứng tầm trung; tối ưu hóa để giữ memory footprint thấp (dưới 500MB).

## Lưu Trữ & Save:
- Save system dựa trên JSON hoặc binary serialization, lưu trữ tiến trình, deck, unlocked memories.
- Checkpoint tại các act transitions và sau mỗi battle.

## Điều khiển & Input:
- Bàn phím: F để tương tác, A/D để di chuyển.
## Luồng trò chơi (Game Flow)

**Premise:** Người chơi là một linh hồn tỉnh dậy, vào cửa hàng "Are you lost?" và nhận 5 lá bài từ một bà đồng. Trò chơi kết hợp thám hiểm (exploration) và combat thẻ bài (battle).

**Giai đoạn chính (3 act structure):**

1) **Giai đoạn 1 — Khu Phố Của Sự Chối Bỏ (The Denial)**
   - Bối cảnh: Phố 1-bit, neon hư ảo, quái vật mang hình hồ sơ, đồng hồ.
   - NPC tiêu biểu: **Kẻ Nịnh Bợ (The Bartender)** — đại diện cho ngụy biện.
   - Lối chơi: Người chơi sử dụng trạng thái **GIẬN DỮ** để tiêu diệt kẻ thù.
   - Ký ức thu được: Hình ảnh dưới mưa, hộp quà sinh nhật.

2) **Giai đoạn 2 — Căn Nhà Ngột Ngạt (The Fractured Reality)**
   - Bối cảnh: Trước cửa nhà, đồ đạc bay, tường dán chữ.
   - NPC/Kịch bản: **Chú Chó Bông Rách**, **Người Phụ Nữ Sau Cánh Cửa**.
   - Lối chơi: Dùng **HỤT HẪNG (Void)**, đối mặt sự thật về hành vi bạo lực.
   - Ký ức thu được: Hộp quà bị đập, cảnh bạo hành.

3) **Giai đoạn 3 — Phán Xét Cuối Cùng (The Basement of Truth)**
   - Bối cảnh: Hầm tối, tấm gương phản chiếu.
   - Boss: **Bản Ngã Bạo Chúa (Tyrant Ego)** — dùng lá bài cảm xúc mạnh hơn.
   - Kết: Reveal self-harm / suicide twist; gia đình được giải thoát.

---

## Truyện ngụ ngôn (Vignettes)

- Ba đoạn văn ngắn dùng để tăng chiều sâu cảm xúc, mỗi đoạn có hiệu ứng 1-bit riêng:
  - "Mặt Trời Khát Khao": text-box, climax glitch + static noise.
  - "Câu Chuyện Của Bé": chữ trên tường, từ bị strikethrough/biến dạng.
  - "Người Thợ Mộc": CRT terminal, dòng lỗi lặp vô hạn, ép người chơi thoát.

---

## Hệ thống thẻ (Cards)

Mỗi thẻ là một `ScriptableObject` (`CardData`) chứa: `CardType`, `Cost` (ký ức), `Effect`, `FlavorText`, `Sprite1bit`.

- Thẻ mẫu:
  - **ĐAU THƯƠNG (Pain)** — Type: Attack; Cost: 15; Effect: 30 DMG (x2 nếu target có "U buồn"); Art: giọt lệ + kiếm gãy.
  - **HỤT HẪNG (Void)** — Type: Control; Cost: 10; Effect: Enemy "Lost" (skip next turn); Player: no draw next turn.
  - **VUI VẺ (Euphoria)** — Type: Support; Cost: 0 (special); Effect: Restore 20 Memory; Exile after use.
  - **GIẬN DỮ (Wrath)** — Type: Skill; Cost: 25; Effect: 10 AoE DMG; Buff +5 attack to cards in hand.
  - **HOÀI NIỆM (Nostalgia)** — Type: Utility; Cost: 5; Effect: Return 1 card from Discard to hand.

---

## Cấu trúc màn hình chiến đấu (Battle Scene Layout)

- **3 tầng dọc:**
  - Tầng trên: Enemy sprites + narrative text-box.
  - Tầng giữa: Card area (5 cards visible).
  - Tầng dưới: Player avatar + Memory bar (HP/energy).

---

## Core mechanics

- **Memory ()**: Tài nguyên chính — HP & Mana. 0 = Game Over.
- **Action-Driven States**: Dùng thẻ áp ngay trạng thái (buff/debuff, skip draw).
- **DeckManager**: Draw, Discard, Exile logic.
- **StateManager**: Quản lý trạng thái kéo dài.

---

## Visual & Audio

- Rung (shake), Flash/Invert, Dithering noise, Glitch khi Memory thấp.
- Mid-battle dialogue: enemy lines xen kẽ lượt đánh.

---

## Narrative integration

- Flavor Text cho mỗi thẻ.
- Combat events unlock reveals / memory fragments.

---

## Dialogue System (NPC <-> Player)

- **Overview:** Hệ thống dialogue xử lý hội thoại giữa NPC và người chơi trong exploration và các đoạn mid-battle dialogue.
- **DialogueData (SO):** mỗi đoạn hội thoại là một ScriptableObject chứa: list of `DialogueNode` (speaker, text, portrait), trigger event, flags, và `defaultNextNodeID` để đi tuyến tính.
- **DialogueManager responsibilities:**
  - Queue và hiển thị lines với typewriter effect.
  - Chuyển node theo `defaultNextNodeID`, set/check flags, trigger game events (unlock memory, start battle, give item).
  - Pause player movement/input while dialogue active; resume after.
  - Integrate with `StateManager` / `MemoryManager` to modify states or reveal fragments.
- **DialogueUIManager responsibilities:** subscribe `OnDialogueStarted` / `OnDialogueLineShown` / `OnDialogueEnded`, cập nhật speaker + text, xử lý Next/Close button.
- **PortraitManager responsibilities:** quản lý mapping `portraitId -> Sprite`, fallback portrait, và apply ảnh vào UI.
- **UI:** nameplate, portrait, text box, skip/next controls, optional subtitles.
- **Integration:** NPC prefabs implement `IInteractable` và reference a `DialogueData` SO. When `Interact()` called, the `DialogueManager` runs the dialogue.

```csharp
public interface IInteractable { void Interact(); }
public class DialogueManager { void StartDialogue(DialogueData d); }
```

---

## Paper Interaction — Vignette Display (when picking up notes)

- **Overview:** Khi người chơi tương tác với tờ giấy / note trên map, hiện một vignette (ngụ ngôn) dạng text-box overlay với hiệu ứng 1-bit đặc trưng.
- **VignetteData (SO):** chứa: `VignetteType` (Newspaper / Scratch / Terminal), `TextLines`, `DisplayEffects` (glitch, static, scroll), `AudioCue`, `RevealMemoryFragment` (optional), `Replayable` flag.
- **Pickup behaviour:** `Paper` prefab implements `IInteractable`; on `Interact()` it calls `VignetteManager.Show(vignetteSO)`.
- **VignetteManager responsibilities:**
  - Render text with appropriate effect (typewriter, slow-scroll, auto-scroll loop for error lines).
  - Apply visual effects (glitch, invert, shake) and audio (static, typewriter sounds).
  - If `RevealMemoryFragment` present, notify `MemoryManager` to add fragment and mark progress.
  - Respect `Replayable` flag — if false, mark the item as collected and disable future interaction.
- **UI considerations:** full-screen modal overlay with close/skip controls; supports accessibility (instant text option).

---

## Scene Loading

- Use additive loading:
  - `ExplorationScene` — world.
  - `BattleScene` — loaded additively on encounter; unload after.

---

## Interfaces & Data

- `IInteractable`:
```csharp
public interface IInteractable { void Interact(); }
```
- `IDamageable` for battle entities.
- ScriptableObjects: `CardData`, `EnemyData`.

---

## Core scripts (overview)

- `PlayerController` (movement, input)
- `PlayerInteraction` (detect/interact IInteractable)
- `MemoryManager` (manage Memory, GameOver)
- `BattleManager` (turn flow, execute card effects)
- `DeckManager` (draw/discard/exile)
- `StateManager` (buff/debuff lifecycle)

---

## Combat use-cases (UC-01..UC-09) and Workflows (WF-01..WF-07)

- **Mục tiêu:** Chuẩn hoá các use-case người chơi (UC-xx) và workflow mã (WF-xx) để dễ tham chiếu trong thiết kế và implementation.

- **Use-cases (UC):**
  - **UC-01 — Play card (single-target):** chọn thẻ -> chọn mục tiêu -> xác nhận -> consume Memory -> enqueue `PlayCardAction`.
  - **UC-02 — Play card (multi-target / AoE):** chọn thẻ -> preview ảnh hưởng -> confirm -> apply effect cho tất cả target.
  - **UC-03 — Pass / End turn early:** người chơi chủ động kết thúc pha hành động; trigger end-turn effects.
  - **UC-04 — Use non-card ability / item:** dùng ability/item (consume Memory hoặc enter cooldown), có thể ảnh hưởng draw/turn.
  - **UC-05 — Play instant / reaction card:** phản ứng ngay giữa pipeline (interrupt/counter) theo priority rules.
  - **UC-06 — Target selection cancel:** hủy chọn mục tiêu trước confirm; UI trả về state trước đó (Memory không thay đổi).
  - **UC-07 — Exile / One-shot card (e.g., `Euphoria`):** sau resolve, chuyển thẻ tới Exile; loại khỏi deck/discard.
  - **UC-08 — Play with insufficient Memory:** ngăn hành động, hiển thị feedback (shake + tooltip); đề xuất phương án (discard/choose khác).
  - **UC-09 — Auto-resolve on timeout (optional):** nếu bật timer, mặc định pass hoặc play preselected action khi timeout.

- **Edge-case handlers (tham khảo cho từng UC):**
  - Nếu target biến mất trước resolve (chết/di chuyển) -> áp dụng qui tắc retarget/skip (UI/Rule: retarget nearest alive hoặc no-op).
  - Nếu Memory giảm xuống <=0 giữa resolve -> hoàn tất resolution hiện tại, sau đó enqueue `GameOver` xử lý.
  - Draw khi deck rỗng -> `DeckManager` reshuffle discard -> nếu vẫn rỗng -> fire `OnDeckEmpty`.

- **Workflows (WF):**
  - **WF-01 — Encounter initialization (BattleManager.StartEncounter):** tạo `BattleContext` (deck snapshot, enemies, RNG seed), load UI, `DeckManager.DrawInitialHand()`, apply initial states.
  - **WF-02 — Turn loop (StartTurn → PlayerActionPhase → ResolvePhase → EnemyTurn → EndTurn):**
    - `StartTurn()`: fire `OnTurnStarted`, apply start-of-turn effects, `DeckManager.DrawForTurn()`.
    - `PlayerActionPhase()`: enable input; on `PlayCard` tạo `PlayCardAction` và `ActionQueue.Enqueue()`; validate costs on select + confirm.
    - `ResolvePhase()`: `ActionQueue.ProcessNext()` sequentially; mỗi `IBattleAction.Resolve(ctx)` post events tới `EventBus`.
    - `EnemyTurn()`: AI tạo `IBattleAction` cho enemy; resolve bằng cùng pipeline.
    - `EndTurn()`: fire `OnTurnEnd`, giảm durations, apply end-turn triggers.
  - **WF-03 — Resolution guarantees & determinism:** mọi thay đổi qua methods chuẩn (`MemoryManager.Change`, `StateManager.Apply`, `DeckManager.Move`); mọi resolver nhận `BattleContext` và dùng RNG seed.
  - **WF-04 — Action model & interfaces:** định nghĩa `IBattleAction` (IEnumerator Resolve(BattleContext)), `PlayCardAction`, `ActionQueue` — giúp tách logic và animation.
  - **WF-05 — Event system:** `EventBus` với events typed (`DamageEvent`, `HealEvent`, `StateAppliedEvent`, `CardPlayedEvent`); subscribers gồm `UI`, `StateManager`, `DeckManager`, `Log`.
  - **WF-06 — Coroutines & animations:** mỗi `Resolve()` yield để cho animations; BattleManager chờ coroutine hoàn thành; hỗ trợ fast-forward/skip.
  - **WF-07 — Failure & recoverability:** catch exceptions, log `BattleContext` snapshot, show error modal, allow resume hoặc abort; persist minimal state mỗi turn để restore.

---

## Schema cho Save System (JSON)

- **Mục tiêu:** Chuẩn hoá format save để tương thích lâu dài (versioning), dễ debug, và đủ dữ liệu để restore exploration hoặc battle.
- **Định dạng:** JSON text (`.sav.json`) theo schema version.
- **Quy ước:**
  - `schemaVersion`: tăng khi thay đổi cấu trúc dữ liệu.
  - `gameVersion`: version build game để hỗ trợ migration.
  - `cardID`, `enemyID`, `sceneName`, `checkpointID`: dùng ID/string ổn định, không phụ thuộc index runtime.
  - `costPercentage` của card được tính theo **MaxMemory** để chi phí nhất quán.

```json
{
  "schemaVersion": 1,
  "saveId": "slot-01",
  "gameVersion": "0.1.0",
  "createdAtUtc": "2026-04-10T12:00:00Z",
  "updatedAtUtc": "2026-04-10T12:34:56Z",

  "gameState": "Exploration",
  "act": 1,
  "sceneName": "ExplorationScene_Act1",
  "checkpointId": "act1_after_battle_02",

  "player": {
    "memoryCurrent": 68,
    "memoryMax": 100,
    "position": { "x": 12.4, "y": -3.1 },
    "facing": "Right",
    "statusEffects": [
      { "statusId": "WrathBuff", "stacks": 1, "remainingTurns": 2 }
    ]
  },

  "deck": {
    "drawPile": ["CARD_PAIN", "CARD_VOID"],
    "hand": ["CARD_NOSTALGIA", "CARD_WRATH"],
    "discardPile": ["CARD_PAIN"],
    "exilePile": ["CARD_EUPHORIA"]
  },

  "narrative": {
    "unlockedMemoryFragments": ["frag_rain_01", "frag_birthday_box"],
    "dialogueFlags": {
      "met_bartender": true,
      "revealed_door_woman": false
    },
    "seenVignettes": ["vignette_sun", "vignette_child_story"]
  },

  "battleSnapshot": {
    "isInBattle": false,
    "turnNumber": 0,
    "rngSeed": 0,
    "enemies": []
  },

  "meta": {
    "playTimeSeconds": 4520,
    "lastManualSaveUtc": "2026-04-10T12:30:00Z",
    "lastAutoSaveUtc": "2026-04-10T12:34:56Z"
  }
}
```

- **Save triggers đề xuất:**
  - Auto-save khi chuyển act, kết thúc battle, hoàn tất vignette quan trọng.
  - Manual save tại safe points trong exploration.
  - Không cho save giữa animation resolve; chỉ save tại điểm state ổn định.

---

## Event Bus — Định nghĩa chi tiết Events

- **Quy tắc chung:**
  - Event name dùng thì quá khứ hoặc completed action (`OnCardPlayed`, `OnDamageDealt`).
  - Event payload phải đủ dữ liệu cho UI + log + analytics, không buộc subscriber truy vấn ngược runtime state.
  - Event dispatch theo thứ tự resolve trong `ActionQueue` để giữ determinism.

### Combat Events

1. **OnCardPlayed**
   - **Khi phát sinh:** ngay sau khi cost hợp lệ, card được confirm play và đưa vào resolve pipeline.
   - **Publisher:** `BattleManager` / `ActionQueue`.
   - **Payload:**
     - `string battleId`
     - `int turnNumber`
     - `string actorId`
     - `string cardId`
     - `int memoryCostPaid`
     - `List<string> targetIds`
     - `bool isReaction`

2. **OnDamageDealt**
   - **Khi phát sinh:** mỗi lần damage final được commit lên target.
   - **Publisher:** `DamageResolver`.
   - **Payload:**
     - `string battleId`
     - `int turnNumber`
     - `string sourceId`
     - `string targetId`
     - `int baseDamage`
     - `float totalMultiplier`
     - `int finalDamage`
     - `bool isCritical`
     - `string damageType`

3. **OnMemoryChanged**
   - **Khi phát sinh:** mọi thay đổi Memory (cost card, heal, self-damage, scripted event).
   - **Publisher:** `MemoryManager`.
   - **Payload:**
     - `string ownerId`
     - `int oldValue`
     - `int newValue`
     - `int delta`
     - `string reason` (`CardCost`, `Damage`, `Heal`, `Scripted`)
     - `string sourceId`

4. **OnTurnStarted**
   - **Khi phát sinh:** bắt đầu turn của player hoặc enemy, trước draw/start-of-turn effects.
   - **Publisher:** `BattleManager`.
   - **Payload:**
     - `string battleId`
     - `int turnNumber`
     - `string side` (`Player` hoặc `Enemy`)
     - `int memoryAtTurnStart`

### Narrative Events

1. **OnDialogueStarted**
   - **Khi phát sinh:** `DialogueManager.StartDialogue()` được gọi thành công.
   - **Payload:** `string dialogueId`, `int startNodeId`, `string npcId`, `string sceneName`.

2. **OnDialogueEnded**
   - **Khi phát sinh:** dialogue kết thúc tự nhiên hoặc do skip.
   - **Payload:** `string dialogueId`, `int endNodeId`, `string endReason` (`Completed`, `Skipped`, `Interrupted`), `Dictionary<string, bool> updatedFlags`.

3. **OnVignetteTriggered**
   - **Khi phát sinh:** người chơi tương tác paper/note và vignette bắt đầu hiển thị.
   - **Payload:** `string vignetteId`, `string vignetteType`, `bool replayable`, `string sourceInteractableId`.

### System Events

1. **OnGameStateChanged**
   - **Khi phát sinh:** state machine chuyển giữa `Exploration`, `Dialogue`, `Battle`, `Vignette`.
   - **Payload:** `string previousState`, `string newState`, `string reason`, `string contextId`.

2. **OnSaveTriggered**
   - **Khi phát sinh:** bắt đầu tiến trình save (manual/auto/checkpoint).
   - **Payload:** `string saveId`, `string triggerType`, `string gameState`, `string checkpointId`, `string requestedBy`.

---

## ScriptableObjects — Cấu trúc chi tiết (Field Level)

### CardData SO

```csharp
public enum CardType
{
    Attack,
    Support,
    Skill,
    Control,
    Utility
}

public enum EffectType
{
    Damage,
    HealMemory,
    Buff,
    Debuff,
    ApplyStatus,
    Draw,
    Discard,
    Exile,
    ReturnFromDiscard
}

[System.Serializable]
public class EffectData
{
    public EffectType effectType;
    public int flatValue;
    public float multiplier;
    public string statusId;
    public int durationTurns;
    public TargetScope targetScope; // Single, AllEnemies, Self, Ally
    public string conditionKey;      // Ví dụ: "TargetHas_UBuon"
}

[CreateAssetMenu(menuName = "Memories/Data/CardData")]
public class CardData : ScriptableObject
{
    public string cardID;            // ID duy nhất để save/load và truy vấn
    public string displayName;       // Tên hiển thị trên UI
    public CardType type;            // Attack, Support, ...
    [Range(0, 100)]
    public int costPercentage;       // % Memory tiêu tốn theo MaxMemory
    public List<EffectData> effects; // Danh sách effect resolver sẽ chạy theo thứ tự
    [TextArea]
    public string flavorText;        // Đoạn dẫn chuyện 1-bit
    public Sprite sprite1Bit;        // Minh hoạ card
}
```

- **Quy tắc cost:**
  - `memoryCost = ceil(playerMaxMemory * costPercentage / 100)`
  - Nếu `memoryCurrent < memoryCost` -> block play, fire feedback UI.

### DialogueNode (trong DialogueData SO)

```csharp
public enum DialogueEvent
{
    None,
    StartBattle,
    UnlockMemoryFragment,
    GiveCard,
    SetFlag,
    TriggerVignette,
    LoadScene
}

[System.Serializable]
public class DialogueNode
{
    public int nodeID;               // Định danh node
    public string speakerName;       // Tên speaker
    [TextArea]
    public string textContent;       // Nội dung thoại
    public DialogueEvent triggerEvent; // Event bắn khi line kết thúc
    public string eventParam;        // Ví dụ: battleId/flagKey/vignetteId
    public string portraitId;        // Key portrait
    public int defaultNextNodeID;
}
```

- `triggerEvent` chạy tại cuối node hoặc trước chuyển node kế tiếp (configurable).

---

## State Machine — Logic trạng thái Game

```csharp
public enum GameState
{
    Exploration,
    Dialogue,
    Battle,
    Vignette
}
```

- **Exploration State**
  - Cho phép: di chuyển (`A/D`), tương tác (`F`), trigger encounter.
  - Khoá: mọi input combat/card.

- **Dialogue State**
  - Cho phép: `F` để next line (hoặc key tương đương UI).
  - Khoá: movement, card input, world interaction khác.

- **Battle State**
  - Cho phép: chọn card, chọn target, end turn, reaction theo rule.
  - Khoá: movement exploration và interact world object.

- **Vignette State**
  - Cho phép: next/skip/close vignette overlay.
  - Khoá: toàn bộ input world + battle.

- **State transitions chuẩn:**
  - `Exploration -> Dialogue` (interact NPC)
  - `Exploration -> Battle` (encounter/script)
  - `Exploration -> Vignette` (interact paper/note)
  - `Dialogue -> Exploration` (dialogue end)
  - `Dialogue -> Battle` (`DialogueEvent.StartBattle`)
  - `Vignette -> Exploration` (close overlay)
  - `Battle -> Exploration` (battle end)

- **Triển khai khuyến nghị:**
  - `GameStateMachine` trung tâm, mọi manager đăng ký callback `OnEnter/OnExit`.
  - Fire `OnGameStateChanged` mỗi transition để UI, input layer, audio layer sync.
  - Có thể dùng state stack cho modal (`Exploration` base + push `Dialogue`/`Vignette`).

---

## Namespace & Project Organization

- **Namespace quy chuẩn:**
  - `Memories.Core`: `GameManager`, `GameStateMachine`, `StateManager`, `MemoryManager`.
  - `Memories.Combat`: `DeckManager`, `BattleManager`, `CardResolver`, `ActionQueue`.
  - `Memories.Narrative`: `DialogueManager`, `VignetteManager`, narrative triggers.
  - `Memories.Data`: ScriptableObjects, DTO/save wrappers, enums dùng chung.

- **Cấu trúc thư mục đề xuất (khớp layout hiện tại):**

```text
Assets/_Project/Gameplay/Scripts/
  Core/            -> namespace Memories.Core
  Combat/          -> namespace Memories.Combat
  Narrative/       -> namespace Memories.Narrative
  Data/            -> namespace Memories.Data
  Interfaces/      -> namespace Memories.Core.Interfaces hoặc Memories.Shared
```

- **Quy tắc đặt tên:**
  - Class manager: hậu tố `Manager`.
  - SO data: hậu tố `Data`.
  - Event payload: hậu tố `Event`.
  - Enum dùng chung đặt tại `Memories.Data.Enums`.

---

## Math Formulas — Damage Calculation

- **Mục tiêu:** rõ ràng, cân bằng được, deterministic theo seed.

### Công thức khuyến nghị

Gọi:
- $B$: base damage từ card/effect.
- $A_f$: tổng flat bonus từ attacker buffs.
- $M_a$: attacker multiplier (ví dụ Wrath, card synergy).
- $M_t$: target taken-damage multiplier (ví dụ Vulnerable/Guard).
- $R$: defense mitigation theo công thức mềm.
- $V$: random variance deterministic từ seed (khuyên dùng $0.95 \le V \le 1.05$).
- $S$: flat shield của target.

$$
R = \frac{Defense}{Defense + K} \quad (K = 100 \text{ mặc định})
$$

$$
Raw = (B + A_f) \times M_a \times M_t \times (1 - R) \times V
$$

$$
FinalDamage = \max\left(1, \text{round}(Raw) - S\right)
$$

### Rule notes

- Crit (nếu có): nhân vào $M_a$ trước bước mitigation.
- Hiệu ứng đặc biệt card (ví dụ Pain x2 khi target có trạng thái "U buồn") được cộng vào $M_a$.
- Clamp toàn bộ multiplier trong khoảng hợp lệ để tránh overflow/one-shot ngoài ý muốn.
- Emit `OnDamageDealt` với cả `baseDamage`, `totalMultiplier`, `finalDamage` để debug balance.

### Ví dụ nhanh

- Card Pain: $B=30$, attacker buff +10% -> $M_a=1.1$, target Vulnerable +25% -> $M_t=1.25$, defense=50, $K=100$, variance $V=1.0$, shield $S=2$.

$$
R = 50/(50+100)=0.333
$$

$$
Raw = 30 \times 1.1 \times 1.25 \times (1-0.333) \approx 27.5
$$

$$
FinalDamage = \max(1, round(27.5)-2)=26
$$

---

## Implementation notes & next steps

- Create SO samples for the 5 cards.
- Build UI prefabs for 3-tier battle layout.
- Add unit tests for `DeckManager` and `MemoryManager`.

---

## Options (I can do next)

- Thêm mục lục (TOC) ở đầu file.
- Tách phần scripts thành tài liệu tham chiếu riêng.
- Xuất PDF/Google Doc để review.
