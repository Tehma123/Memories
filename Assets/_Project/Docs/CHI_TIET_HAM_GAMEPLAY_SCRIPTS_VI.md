# Chi tiết hàm quan trọng của gameplay scripts

Ngày cập nhật: 2026-04-14  
Phạm vi: 31 scripts trong `Assets/_Project/Gameplay/Scripts`

Mục tiêu tài liệu:
1. Giải thích hàm nào quan trọng nhất trong mỗi script.
2. Hàm đó làm gì, tác động gì, gọi đến hệ thống nào.
3. Giúp debug nhanh khi có lỗi theo luồng runtime.

## 1) Interfaces

### `Interfaces/IInteractable.cs`
- Kiểu: Interface
- Hàm:
  - `void Interact()`
    - Định nghĩa hợp đồng tương tác cho đối tượng trong world.
    - Được gọi bởi `PlayerMovement` hoặc `PlayerInteraction`.

### `Interfaces/IDamageable.cs`
- Kiểu: Interface
- Hàm:
  - `void TakeDamage(int amount)`
    - Nhận sát thương từ card/enemy attack.
  - `void Heal(int amount)`
    - Hồi phục cho target.

### `Interfaces/IBattleAction.cs`
- Kiểu: Interface
- Hàm:
  - `void Resolve(BattleContext context)`
    - Action trong queue phải tự resolve dựa trên context battle.

## 2) Data

### `Data/CardData.cs`
- Kiểu: ScriptableObject + nested `EffectData`
- Hàm:
  - `int GetMemoryCost(int maxMemory)`
    - Tính chi phí Memory theo phần trăm card cost.
    - Dùng khi `PlayCardAction.Resolve()` trước khi apply effects.
  - `int EffectData.GetResolvedValue()`
    - Trả về giá trị effect thực tế.
    - Thường được dùng để lấy damage/heal/draw amount.

### `Data/DialogueData.cs`
- Kiểu: ScriptableObject + nested `DialogueNode`
- Hàm:
  - `DialogueNode GetNodeByID(int nodeID)`
    - Tìm node theo ID trong danh sách nodes.
  - `DialogueNode GetStartNode()`
    - Lấy node bắt đầu theo `startNodeID`, fallback theo cấu hình.

### `Data/EnemyData.cs`
- Kiểu: ScriptableObject + nested `EnemyMoveData`
- Hàm:
  - `EnemyMoveData GetRandomMove(System.Random random)`
    - Chọn move cho enemy turn.
  - `int EnemyMoveData.GetRoll(System.Random random)`
    - Roll giá trị trong khoảng min/max của move.

### `Data/VignetteData.cs`
- Kiểu: ScriptableObject
- Hàm:
  - `bool HasEffect(VignetteEffect effect)`
    - Check bit-flag để xác định vignette có hiệu ứng nào.

## 3) Core

### `Core/StateManager.cs`
- Vai trò: Quản lý states có thời hạn theo turn.
- Hàm quan trọng:
  - `void ApplyState(GameObject target, string stateId, int durationTurns)`
    - Thêm state mới hoặc refresh state cũ cho target.
    - Phát event `OnStateApplied`.
  - `void RemoveState(GameObject target, string stateId)`
    - Xóa state cụ thể và phát `OnStateRemoved`.
  - `bool HasState(GameObject target, string stateId)`
    - Kiểm tra target có state không.
  - `int GetRemainingTurns(GameObject target, string stateId)`
    - Trả về số turn còn lại của state.
  - `void TickTurnStart()`
    - Dọn dẹp target đã bị destroy.
  - `void TickTurnEnd()`
    - Giảm thời gian state mỗi cuối turn và xóa state hết hạn.
  - `void ClearAllStates()`
    - Xóa toàn bộ state buckets.
  - `StateBucket GetOrCreateBucket(GameObject target)`
    - Tạo bucket state nếu target chưa có.
  - `void RemoveDestroyedTargets()`
    - Tránh memory leak references đến objects đã mất.

### `Core/MemoryManager.cs`
- Vai trò: Quản lý Memory và fragments.
- Lifecycle:
  - `Awake()`
    - Resolve refs, setup UI combat, initialize giá trị ban đầu.
- Hàm quan trọng:
  - `void Initialize()`
    - Reset memory + fragments cho phiên mới.
  - `void SetMemory(float valuePercent)`
    - Đặt trực tiếp giá trị memory (có clamp).
    - Trigger event `OnMemoryChanged`; nếu <=0 thì game over flow.
  - `void ChangeMemory(float deltaPercent)`
    - Tăng/giảm memory theo delta.
  - `bool CanSpend(float amountPercent)`
    - Check khả năng chi memory.
  - `bool TrySpend(float amountPercent)`
    - Nếu đủ memory thì trừ và trả true; ngược lại false.
  - `bool UnlockFragment(string fragmentId)`
    - Mở khóa fragment mới; trigger `OnMemoryFragmentUnlocked`.
  - `bool HasFragment(string fragmentId)`
    - Kiểm tra fragment đã mở khóa hay chưa.
  - `IReadOnlyCollection<string> GetUnlockedFragments()`
    - Lấy snapshot danh sách fragments.
  - `void HandleGameOver()`
    - Trigger `OnMemoryDepleted` một lần.
  - `void ResolveCombatUiReferences()`
    - Tìm refs UI runtime để cập nhật thanh Memory.
  - `void UpdateCombatUi()`
    - Đồng bộ fill/text theo memory hiện tại.

### `Core/PlayerMovement.cs`
- Vai trò: Input movement + trigger interact gần.
- Lifecycle:
  - `Awake()`
    - Cache Rigidbody2D, optional refs.
  - `FixedUpdate()`
    - Apply velocity theo vector movement khi cho phép di chuyển.
- Input/interaction:
  - `void OnMove(InputValue inputValue)`
    - Nhận input di chuyển; gửi hướng cho camera follow.
  - `void OnInteract(InputValue value)`
    - Gọi `Interact()` lên target đang trong tầm (nếu có).
  - `void SetMovementEnabled(bool isEnabled)`
    - Bật/tắt movement khi vào dialogue/battle.
- Trigger hooks:
  - `void OnTriggerEnter2D(Collider2D collision)`
    - Cache interactable target.
  - `void OnTriggerExit2D(Collider2D collision)`
    - Clear target khi rời trigger.

### `Core/PlayerInteraction.cs`
- Vai trò: Hệ thống interact theo list target + overlap fallback.
- Lifecycle:
  - `Awake()` / `Initialize()`
    - Khởi tạo list targets trong tầm.
- Hàm quan trọng:
  - `void OnInteract(InputValue inputValue)`
    - Nhận input interact.
  - `void SetInteractionEnabled(bool isEnabled)`
    - Bật/tắt khả năng interact.
  - `void TryInteract()`
    - Chọn target ưu tiên gần nhất và gọi `Interact()`.
  - `IInteractable GetNearestKnownTarget()`
    - Lấy target từ list trigger đã cache.
  - `IInteractable FindNearestByOverlap()`
    - Fallback overlap search khi list trigger không đúng.
- Trigger hooks:
  - `OnTriggerEnter2D` / `OnTriggerExit2D`
    - Thêm/xóa interactables trong list.

### `Core/CameraSmoothFollow.cs`
- Vai trò: Đổi camera offset mềm theo hướng di.
- Hàm:
  - `void SetTracking(bool track)`
    - Bật/tắt tracking camera.
  - `void UpdateCameraDirection(float moveX)`
    - Tính target offset X theo hướng di chuyển.
  - `void StartOffsetTween(float targetX)`
    - Tween offset bằng LeanTween.

### `Core/MainMenuController.cs`
- Hàm:
  - `void Play()`
    - Start game, gọi scene transition đến scene chơi.
  - `void Options()`
    - Hook cho options (hiển thị placeholder nếu chưa custom).
  - `void Quit()`
    - Thoát game/app.

### `Core/SceneTransitionContext.cs`
- Kiểu: static context utility
- Hàm:
  - `void LoadScene(string destinationSceneName, string destinationEntryPointId = "")`
    - Đặt pending transition và gọi fader load scene.
  - `void SetPendingTransition(string destinationSceneName, string destinationEntryPointId = "")`
    - Lưu scene/entry point đích.
  - `void ClearPendingTransition()`
    - Xóa pending state.
  - `bool TryConsumeEntryPointForActiveScene(string activeSceneName, out string entryPointId)`
    - One-shot consume entry point cho scene vừa vào.
  - `bool TryParseSceneAndEntry(string rawValue, out string sceneName, out string entryPointId)`
    - Parse format `SceneName|EntryPoint`.

### `Core/SceneTransitionFader.cs`
- Vai trò: Fade đến/ra khi load scene.
- Lifecycle + bootstrap:
  - `Bootstrap()`
    - Đảm bảo singleton được tạo từ sớm.
  - `Awake()`
    - Khởi tạo overlay + singleton logic.
  - `Start()`
    - Có thể fade-in startup.
- Hàm quan trọng:
  - `bool TryLoadSceneWithFade(string destinationSceneName)`
    - Entry point để bắt đầu transition coroutine.
  - `void BeginTransition(string destinationSceneName)`
    - Guard để tránh transition trùng lặp.
  - `IEnumerator TransitionRoutine(string destinationSceneName)`
    - Fade out -> load scene -> fade in.
  - `IEnumerator FadeTo(float targetAlpha, float duration, bool keepRaycastBlock)`
    - Nội suy alpha overlay theo thời gian.
  - `void EnsureOverlay()`
    - Tạo/liên kết overlay UI nếu thiếu.
  - `void SetAlpha(float alpha, bool blockRaycasts)`
    - Apply alpha và block raycast.

### `Core/PlayerSceneEntryHandler.cs`
- Vai trò: Đặt vị trí player theo entry point scene mới.
- Lifecycle:
  - `Awake()`
  - `Start()` -> thử apply entry point.
- Hàm quan trọng:
  - `void TryApplyEntryPoint()`
    - Consume pending transition; tìm entry point trùng ID hoặc fallback.
    - Đặt transform/velocity của player.
  - `SceneEntryPoint FindEntryPoint(string entryPointId)`
    - Tìm ID đúng trong scene.
  - `SceneEntryPoint FindFallbackEntryPoint()`
    - Tìm điểm fallback.
  - `OnValidate()`
    - Trợ giúp editor validation.

### `Core/SceneEntryPoint.cs`
- Vai trò: Marker component cho điểm vào.
- Hàm:
  - `OnValidate()`
    - Validation editor cho ID/fallback setup.

### `Core/SceneExitInteractable.cs`
- Vai trò: Cửa/portal manual transition.
- Hàm:
  - `OnEnable()`
    - Reset trạng thái use flag.
  - `void Interact()`
    - Gọi `SceneTransitionContext.LoadScene` với destination config.
  - `OnValidate()`
    - Validation editor fields.

### `Core/SceneEdgeAutoPortal.cs`
- Vai trò: Auto transition khi vượt mép map.
- Lifecycle:
  - `Awake()`
  - `OnEnable()`
  - `Update()`
- Hàm quan trọng:
  - `Update()`
    - Detect crossing edge, check outward movement, trigger transition.
  - `bool DidCrossLeftEdge(float previousX, float currentX)`
  - `bool DidCrossRightEdge(float previousX, float currentX)`
  - `bool IsMovingOutward(bool isLeftEdge)`
    - Đảm bảo user đang di ra ngoài mép nếu mode này bật.
  - `bool TryTransition(string destinationSceneName, string destinationEntryPointId, string edgeName)`
    - Gọi scene transition, có guard chông repeat.
  - `void TryResolvePlayerReferences()`
    - Tìm player transform/movement nếu refs chưa set.
  - `OnValidate()`

## 4) Battle

### `Battle/BattleContext.cs`
- Vai trò: Container state để resolve action.
- Hàm:
  - `BattleContext(...)`
    - Constructor nhận refs managers + random seed.
  - `void SetEnemies(IEnumerable<EnemyController> enemies)`
    - Set danh sách enemies runtime.
  - `IReadOnlyList<EnemyController> GetAliveEnemies()`
    - Filter enemy còn sống.
  - `EnemyController GetPrimaryAliveEnemy()`
    - Lấy enemy đầu tiên còn sống.
  - `void SetRandomSeed(int seed)`
    - Đặt random để reproducible test.

### `Battle/ActionQueue.cs`
- Vai trò: Queue FIFO cho battle actions.
- Hàm:
  - `void SetContext(BattleContext context)`
    - Gán context để action resolve.
  - `void Enqueue(IBattleAction action)`
    - Thêm action vào queue; fire `OnActionEnqueued`.
  - `void ProcessNext()`
    - Pop action đầu và gọi `Resolve(context)`.
  - `void Clear()`
    - Xóa tất cả pending actions; fire `OnQueueCleared` nếu cần.

### `Battle/BattleManager.cs`
- Vai trò: Điều phối encounter và turn loop.
- Lifecycle:
  - `Awake()`
    - Resolve refs managers + End Turn button.
  - `OnEnable()` / `OnDisable()`
    - Đăng ký/hủy callback nút End Turn.
  - `Start()`
    - Có thể auto-start encounter theo config.
- Hàm quan trọng:
  - `void StartConfiguredEncounter()`
    - Khởi động encounter từ cấu hình scene.
  - `void StartEncounter(EnemyData[] enemies)`
    - Reset state, spawn enemies, tạo context, init deck, draw hand, vào turn 1.
  - `void StartTurn()`
    - Tick start-turn, draw-for-turn, cập nhật input turn.
  - `void EndTurn()`
    - Resolve queue, enemy turns, tick end-turn, check win/lose.
  - `void EnqueueAction(IBattleAction action)`
    - Add action khi player chơi card.
  - `void ResolveQueue()`
    - Xử lý đến khi queue rỗng.
  - `void EndEncounter()`
    - Đóng battle theo trạng thái hiện tại.
  - `void RunEnemyTurn()`
    - Mỗi enemy còn sống tự chạy `TakeTurn()`.
  - `bool IsPlayerDefeated()`
    - Rule lose dựa trên memory.
  - `bool AreAllEnemiesDefeated()`
    - Rule win dựa trên HP enemies.
  - `void SpawnEnemies(EnemyData[] enemies)`
    - Tạo enemy gameobjects/controllers.
  - `EnemyController CreateEnemyController(...)`
    - Tạo và initialize 1 enemy tại slot.
  - `void ResetEncounterState()`
    - Dọn state encounter trước khi start mới.
  - `void EndEncounterInternal(bool playerWon)`
    - Fire event kết quả và clear queue.
  - `void HandleEndTurnButtonClicked()`
    - Callback UI.
  - `void UpdateEndTurnButtonState()`
    - Enable button đúng lúc.
  - `Button FindEndTurnButton()`
    - Tìm button nếu chưa assign.

### `Battle/DeckManager.cs`
- Vai trò: Quản lý draw pile, hand, discard, exile.
- Lifecycle:
  - `OnEnable()` / `OnDisable()` / `OnDestroy()`
    - Đăng ký hand change và clear spawned UI cards.
- Hàm quan trọng:
  - `void SetSeed(int seed)`
    - Seed random để test reproducible.
  - `void InitializeDeck()`
    - Reset piles, copy starting deck vào draw pile, shuffle.
  - `void DrawInitialHand()`
    - Rút hand ban đầu.
  - `void DrawForTurn()`
    - Rút bài đầu mỗi turn.
  - `int DrawCards(int count)`
    - Rút N lá; auto shuffle discard vào draw khi cần.
  - `void DiscardCard(CardData card)`
  - `void ExileCard(CardData card)`
  - `void ShuffleDiscardIntoDeck()`
  - `int DiscardRandomFromHand(int count)`
  - `int ExileRandomFromHand(int count)`
  - `int ReturnFromDiscardToHand(int count)`
  - `bool TryMoveCardFromHandToDiscard(CardData card)`
    - Các hàm trên phục vụ effect card và quản lý piles.
  - `void SetCardHandRoot(Transform handRoot)`
    - Gán root UI để render hand.
  - `CardData DrawSingle()`
    - Rút 1 lá core internal.
  - `void Shuffle(List<CardData> cards)`
    - Fisher-Yates style xáo list.
  - `void RebuildHandView()`
    - Build lại card UI theo hand hiện tại.
  - `void ClearSpawnedHandCards()`
    - Destroy card UIs đã tạo.
  - `void BindCardPrefab(GameObject cardInstance, CardData cardData)`
    - Bind text/image cho prefab card.
  - `T FindNamedComponent<T>(Transform root, string objectName)`
  - `Transform FindNamedTransform(Transform root, string objectName)`
    - Helpers tìm node con trong prefab.

### `Battle/EnemyController.cs`
- Vai trò: Runtime enemy + AI turn + IDamageable.
- Lifecycle:
  - `Awake()`
    - Resolve refs + init máu nếu có data.
- Hàm quan trọng:
  - `void Initialize(EnemyData enemyData)`
    - Gán dữ liệu enemy và reset HP.
  - `void TakeDamage(int amount)`
    - Trừ HP, fire health changed, defeat nếu <=0.
  - `void Heal(int amount)`
    - Tăng HP tới đa max.
  - `void TakeTurn()`
    - Chọn move từ EnemyData, roll value, apply effect lên player/self.
  - `void NotifyHealthChanged()`
    - Fire event cập nhật HP.
  - `void HandleDefeat()`
    - Fire event defeated và xử lý cleanup cần thiết.

### `Battle/PlayCardAction.cs`
- Vai trò: 1 battle action đại diện cho việc chơi card.
- Hàm:
  - `void Setup(CardData cardData, GameObject source, GameObject target)`
    - Nạp input trước khi enqueue.
  - `void Resolve(BattleContext context)`
    - Tính cost, trừ memory, lặp qua từng effect để apply.
  - `void ResolveEffect(BattleContext context, EffectData effect)`
    - Switch theo effect type: damage/heal/status/draw/discard/exile/return.
  - `List<GameObject> ResolveTargets(BattleContext context, TargetScope targetScope)`
    - Chọn danh sách target theo scope.

## 5) Dialogue

### `Dialogue/DialogueManager.cs`
- Vai trò: Điều phối state machine hội thoại + dispatch event của node.
- Lifecycle:
  - `Awake()`
    - Singleton setup, resolve refs managers + UI presenter.
- Hàm quan trọng:
  - `void StartDialogue(DialogueData dialogueData)`
    - Bắt đầu session hội thoại, khóa input player, đến node đầu.
  - `void ShowNextLine()`
    - Advance node tiếp theo thông qua coroutine.
  - `void EndDialogue()`
    - Kết session, mở khóa input player, fire ended event.
  - `IEnumerator AdvanceCurrentNodeCoroutine()`
    - Xử lý event của node hiện tại rồi move node tiếp.
  - `IEnumerator DispatchEventRoutine(DialogueEvent dialogueEvent, string eventParam)`
    - Router sự kiện: StartBattle, UnlockFragment, GiveCard, SetFlag, TriggerVignette, LoadScene.
  - `IEnumerator HandleGiveCardEvent(string eventParam)`
    - Tìm sequence reward và gọi presenter.
  - `CardRewardSequence FindCardRewardSequence(string sequenceId)`
    - Resolve sequence trong config.
  - `bool HasFlag(string flagId)`
    - Query narrative flag.
  - `bool IsCardRewardSequenceCompleted(string sequenceId)`
    - Check sequence reward đã xong.
  - `void MoveToNode(int nodeID)`
    - Chuyển pointer node.
  - `void PresentCurrentNode()`
    - Phát event line shown cho UI layer.
  - `void ResolveNarrativeUiReferences()`
    - Tìm refs text runtime nếu cần.
  - `TMP_Text FindTextByName(string objectName)`
    - Helper tìm text object.
  - `void SetPlayerInputEnabled(bool isEnabled)`
    - Bật/tắt movement + interaction trong dialogue.

### `Dialogue/DialogueUIManager.cs`
- Vai trò: Hiện panel dialogue + typewriter + input next.
- Lifecycle:
  - `Awake()`, `OnEnable()`, `OnDisable()`, `Update()`
- Hàm input/UI:
  - `void OnNextPressed()`
    - Nếu đang typewriter thì complete line; nếu không thì next node.
  - `void OnClosePressed()`
    - Đóng dialogue ngay.
  - `void HandleDialogueStarted(DialogueData _)`
  - `void HandleDialogueLineShown(DialogueNode node)`
  - `void HandleDialogueEnded(DialogueData _)`
    - Trio handlers đồng bộ panel + text.
  - `void HandleAdvanceInput()`
    - Poll key để advance.
  - `bool IsAdvanceKeyPressed()`
    - Check key mapping hiện tại.
  - `bool TryCompleteCurrentLine()`
    - Skip typewriter và show full line.
- Hàm typewriter:
  - `void StartTypewriterAnimation(string lineText)`
  - `void StopTypewriterAnimation(bool showCurrentLine)`
  - `IEnumerator TypewriterCoroutine(string fullText)`
  - `float GetCharacterDelay(char visibleCharacter, float baseCharacterDelay)`
  - `void PlaySharedTypewriterTick(char visibleCharacter)`
  - `void PlayTypewriterTick(char visibleCharacter)`
    - Điều khiển text reveal + audio tick theo ký tự.
- Hàm binding/runtime UI:
  - `void ResolveReferences()`
  - `void EnsureUIBindings()`
  - `void CreateRuntimeUIIfNeeded()`
  - `void ConfigureRuntimeText(...)`
  - `void ConfigurePanelVisibilityStrategy()`
  - `void Subscribe()` / `void Unsubscribe()`
  - `void SetPanelVisible(bool isVisible)`
  - `void SyncWithDialogueState()`
  - `void ClearText()`

### `Dialogue/DialogueInteractable.cs`
- Vai trò: NPC endpoint của IInteractable.
- Hàm:
  - `virtual void Interact()`
    - Gọi DialogueManager để mở dialogue được resolve.
  - `virtual DialogueData ResolveDialogue(DialogueManager dialogueManager)`
    - Chọn dialogue nào sẽ chạy (before/after reward từ sequence state).

### `Dialogue/CardRewardPresenter.cs`
- Vai trò: Trình bày card reward animation.
- Lifecycle:
  - `Awake()`
  - `OnDisable()`
- Hàm public:
  - `CardRewardPresentationData(Sprite sprite, string caption)`
    - Data object cho 1 card frame.
  - `void PlaySequence(IReadOnlyList<CardRewardPresentationData> cards, Action onCompleted = null)`
    - Bắt đầu sequence animation.
  - `void StopSequence()`
    - Dừng sequence đang chạy.
- Hàm coroutine/animation:
  - `IEnumerator PlaySequenceRoutine(...)`
  - `IEnumerator AnimateSingleCard(CardRewardPresentationData card)`
  - `IEnumerator AnimateIn(Vector2 from, Vector2 to)`
  - `IEnumerator AnimateOut(Vector2 from, Vector2 to)`
    - Điều khiển fly in/out + hold/fade.
- Hàm helper:
  - `void StopCurrentSequence()`
  - `void CancelActiveTween()`
  - `void EnsureBindings()`
  - `void EnsureCanvasGroup()`
  - `void SetVisible(bool isVisible)`
- Hàm caption typewriter:
  - `IEnumerator TypeCaption(string fullCaption)`
  - `float GetCaptionCharacterDelay(char visibleCharacter, float baseCharacterDelay)`
  - `void PlayCaptionTick(char visibleCharacter)`

### `Dialogue/MemoryShop.cs`
- Vai trò: Shop interact point.
- Hàm:
  - `void Interact()`
    - Nếu có `shopDialogue` thì mở dialogue, nếu không thì fallback load scene.

## 6) Vignette

### `Vignette/VignetteManager.cs`
- Vai trò: Show vignette lines + unlock fragment.
- Lifecycle:
  - `Awake()`
    - Singleton + resolve refs.
- Hàm quan trọng:
  - `void Show(VignetteData vignetteData)`
    - Bắt đầu vignette, skip nếu one-shot đã xem.
    - Có thể unlock memory fragment theo data.
  - `void Skip()`
    - Nhảy nhanh qua line/đến cuối.
  - `void ShowNextLine()`
    - Sang line tiếp theo, hết line thì đóng.
  - `void Close()`
    - Kết thúc vignette session.
  - `void DisplayCurrentLine()`
    - Phát event hiển động text hiện tại.

## 7) Điều hướng debug nhanh theo symptom

1. Bấm interact không mở NPC:
   - Kiểm tra `PlayerMovement.OnInteract`, `PlayerInteraction.TryInteract`, `DialogueInteractable.Interact`.
2. Chơi card không trừ memory hoặc không gây damage:
   - Kiểm tra `PlayCardAction.Resolve`, `CardData.GetMemoryCost`, `MemoryManager.TrySpend`, `ResolveTargets`.
3. End Turn không chạy enemy:
   - Kiểm tra `BattleManager.EndTurn`, `RunEnemyTurn`, `EnemyController.TakeTurn`.
4. Chuyển scene sai vị trí spawn:
   - Kiểm tra `SceneTransitionContext.SetPendingTransition`, `TryConsumeEntryPointForActiveScene`, `PlayerSceneEntryHandler.TryApplyEntryPoint`.
5. Dialogue không next line:
   - Kiểm tra `DialogueUIManager.OnNextPressed`, `TryCompleteCurrentLine`, `DialogueManager.ShowNextLine`.

## 8) Ghi chú maintain

1. Hiện có 2 nhánh xử lý interact (`PlayerMovement` và `PlayerInteraction`), cần thống nhất để tránh duplicate input.
2. Event dispatch trong `DialogueManager` là điểm mở rộng chính khi thêm narrative features mới.
3. `PlayCardAction.ResolveEffect` là điểm mở rộng chính khi thêm effect type mới.
4. `SceneTransitionContext` + `PlayerSceneEntryHandler` là cặp đôi bắt buộc dùng đồng bộ khi thêm map mới.