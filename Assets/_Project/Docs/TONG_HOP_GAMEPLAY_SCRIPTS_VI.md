# Tổng Hợp Giải Thích Code Gameplay Scripts

Ngày cập nhật: 2026-04-14  
Phạm vi: 31 script C# trong `Assets/_Project/Gameplay/Scripts`  
Không bao gồm script thư viện bên thứ ba trong `Assets/LeanTween` và `Assets/TextMesh Pro`.

Tài liệu bổ sung:
1. Flow Mermaid: `Assets/_Project/Docs/GAMEPLAY_FLOW_MERMAID_VI.md`
2. Chi tiết theo hàm: `Assets/_Project/Docs/CHI_TIET_HAM_GAMEPLAY_SCRIPTS_VI.md`

## 1. Bức tranh tổng quan

Dự án được chia theo 6 nhóm script:

1. `Interfaces`: Hợp đồng hành vi chung (`IInteractable`, `IDamageable`, `IBattleAction`).
2. `Data`: ScriptableObject chứa dữ liệu thiết kế (`CardData`, `EnemyData`, `DialogueData`, `VignetteData`).
3. `Core`: Vận hành nền tảng (di chuyển, tương tác, bộ nhớ, trạng thái, chuyển cảnh, camera).
4. `Battle`: Chiến đấu theo lượt (bộ bài, hàng đợi, bối cảnh, AI kẻ thù, hành động thẻ, luồng trận đấu).
5. `Dialogue`: Hội thoại nhanh + kích hoạt sự kiện + thưởng thẻ.
6. `Vignette`: Trình chiếu đoạn tư sự ngắn với hiệu ứng.

## 2. Flow tổng thể của game

### 2.1 Flow khám phá và chuyển cảnh

1. Thực đơn chính gọi `MainMenuController.Play()`.
2. `SceneTransitionContext.LoadScene(scene, entryPointId)` lưu điểm đến.
3. Cảnh mới tải xong, `PlayerSceneEntryHandler` tìm `SceneEntryPoint` trùng `entryPointId` và đặt vị trí người chơi.
4. Trong cảnh, `PlayerMovement` nhận đầu vào di chuyển; `CameraSmoothFollow` điều chỉnh độ dịch chuyển camera theo hướng trái/phải.
5. Khi gặp cổng/cửa:
   - Tương tác tay: `SceneExitInteractable.Interact()`.
   - Tự động qua biên màn hình: `SceneEdgeAutoPortal`.
6. Chuyển sang cảnh tiếp theo qua `SceneTransitionContext` (có thể kèm `SceneTransitionFader` để mờ).

### 2.2 Flow tương tác và hội thoại

1. Người chơi vào vùng kích hoạt của đối tượng có `IInteractable`.
2. Nhấn phím tương tác, `PlayerMovement` hoặc `PlayerInteraction` gọi `Interact()`.
3. Nếu là NPC: `DialogueInteractable` gọi `DialogueManager.StartDialogue(dialogueData)`.
4. `DialogueUIManager` hiển thị văn bản (máy đánh máy), nhấn phím để next/skip.
5. Mỗi nút có thể kích hoạt sự kiện:
   - `StartBattle`, `UnlockMemoryFragment`, `GiveCard`, `SetFlag`, `TriggerVignette`, `LoadScene`.

### 2.3 Flow chiến đấu theo lượt

1. `BattleManager.StartEncounter(enemies)` tạo cuộc gặp, sinh kẻ thù, tạo `BattleContext`.
2. `DeckManager.InitializeDeck()` và rút bài tay đầu.
3. Bắt đầu lượt người chơi:
   - `StateManager.TickTurnStart()` cập nhật buff/debuff.
   - Rút bài mới.
4. Người chơi chơi bài:
   - Tạo `PlayCardAction`.
   - Đưa vào `ActionQueue`.
5. Kết thúc lượt:
   - `BattleManager.ResolveQueue()` xử lý từng hành động.
   - Kẻ thù `TakeTurn()` theo nước đi ngẫu nhiên từ `EnemyData`.
   - `StateManager.TickTurnEnd()` giảm thời gian trạng thái.
6. Kiểm tra thắng/thua:
   - Thắng khi tất cả kẻ thù chết.
   - Thua khi Bộ nhớ về 0 (theo cách game hiện tại đang dùng).

## 3. Giải thích từng script

## 3.1 Interfaces

### `Interfaces/IInteractable.cs`
- Vai trò: Hợp đồng cho mọi đối tượng có thể tương tác.
- Method chính: `Interact()`.
- Script liên quan: `PlayerMovement`, `PlayerInteraction`, `DialogueInteractable`, `MemoryShop`, `SceneExitInteractable`.
- Ví dụ: Nhấn F trước cửa, script của đối tượng được gọi `Interact()` để chuyển cảnh.

### `Interfaces/IDamageable.cs`
- Vai trò: Hợp đồng cho đối tượng nhận sát thương/hồi phục.
- Method chính: `TakeDamage(int)`, `Heal(int)`.
- Script liên quan: `EnemyController`, `PlayCardAction`.
- Ví dụ: Thẻ sát thương gọi `TakeDamage(5)` lên kẻ thù.

### `Interfaces/IBattleAction.cs`
- Vai trò: Hợp đồng cho hành động có thể xử lý trong hàng đợi trận đấu.
- Method chính: `Resolve(BattleContext)`.
- Script liên quan: `ActionQueue`, `BattleManager`, `PlayCardAction`.
- Ví dụ: Mỗi lá bài được chơi được đóng gói thành 1 `IBattleAction` và giải quyết lúc End Turn.

## 3.2 Data

### `Data/CardData.cs`
- Vai trò: ScriptableObject mô tả lá bài.
- Dữ liệu chính: ID, tên, loại thẻ, chi phí theo %, danh sách hiệu ứng, text hương vị, sprite.
- Logic chính: Tính chi phí bộ nhớ dựa trên `% * maxMemory`.
- Script liên quan: `DeckManager`, `PlayCardAction`.
- Ví dụ: Thẻ chi phí 20%, hiệu ứng Sát thương 5 + Rút 1.

### `Data/EnemyData.cs`
- Vai trò: ScriptableObject mô tả kẻ thù và bộ nước đi.
- Dữ liệu chính: maxHealth, tính hung hăng, danh sách `EnemyMoveData`.
- Logic chính: Chọn nước đi ngẫu nhiên theo cấu hình.
- Script liên quan: `EnemyController`, `BattleManager`.
- Ví dụ: Kẻ thù có nước đi `AttackMemory` gây mất 1-3 bộ nhớ mỗi lượt.

### `Data/DialogueData.cs`
- Vai trò: ScriptableObject mô tả cây hội thoại.
- Dữ liệu chính: `startNodeID`, danh sách nút, sự kiện trên mỗi nút.
- Logic chính: Tìm nút theo ID, lấy nút bắt đầu.
- Script liên quan: `DialogueManager`, `DialogueInteractable`.
- Ví dụ: Nút 1 kích hoạt `StartBattle`, nút 2 kích hoạt `GiveCard`.

### `Data/VignetteData.cs`
- Vai trò: ScriptableObject cho đoạn vignette ngắn.
- Dữ liệu chính: loại vignette, dòng văn bản, cờ hiệu ứng, tín hiệu âm thanh, mở khóa đoạn, có thể phát lại.
- Logic chính: Kiểm tra có hiệu ứng nào đang bật qua bit flag.
- Script liên quan: `VignetteManager`, `DialogueManager`, `MemoryManager`.
- Ví dụ: Vignette báo chí hiển thị 3 dòng văn bản và mở khóa 1 đoạn.

## 3.3 Core

### `Core/StateManager.cs`
- Vai trò: Quản lý trạng thái tạm thời (buff/debuff) theo lượt.
- Dữ liệu chính: Dictionary mục tiêu -> danh sách trạng thái có thời hạn.
- Logic chính: `ApplyState`, `HasState`, `TickTurnStart`, `TickTurnEnd`, xóa trạng thái hết hạn.
- Script liên quan: `BattleManager`, `EnemyController`, `PlayCardAction`.
- Ví dụ: Kẻ thù buff trong 2 lượt, mỗi End Turn giảm 1 và tự xóa khi về 0.

### `Core/PlayerMovement.cs`
- Vai trò: Điều khiển di chuyển người chơi và kích hoạt tương tác gần.
- Dữ liệu chính: `Rigidbody2D`, `speed`, vectơ di chuyển, đối tượng có tương tác hiện tại.
- Logic chính: `OnMove`, `OnInteract`, bật/tắt di chuyển.
- Script liên quan: `CameraSmoothFollow`, `IInteractable` implementations.
- Ví dụ: Người chơi vào vùng kích hoạt NPC, nhấn F để mở hội thoại.

### `Core/PlayerInteraction.cs`
- Vai trò: Hệ thống tương tác theo danh sách mục tiêu/chồng chéo (bổ sung hoặc lưu giữ).
- Dữ liệu chính: Danh sách có tương tác trong phạm vi.
- Logic chính: `TryInteract` ưu tiên mục tiêu gần nhất.
- Script liên quan: `IInteractable` implementations.
- Ví dụ: Nếu không dùng kích hoạt currentTarget từ `PlayerMovement`, vẫn có thể tìm NPC gần nhất để tương tác.

### `Core/MemoryManager.cs`
- Vai trò: Quản lý tài nguyên Bộ nhớ và đoạn đã mở khóa.
- Dữ liệu chính: CurrentMemoryPercent, MaxMemoryPercent, bộ đoạn IDs, thanh giao diện bộ nhớ.
- Logic chính: `SetMemory`, `ChangeMemory`, `TrySpend`, `UnlockFragment`, sự kiện thay đổi/cạn kiệt bộ nhớ.
- Script liên quan: `PlayCardAction`, `EnemyController`, `DialogueManager`, `VignetteManager`.
- Ví dụ: Người chơi chơi thẻ 25% -> Bộ nhớ giảm từ 100% còn 75%.

### `Core/MainMenuController.cs`
- Vai trò: Điều khiển các nút trực quan chính.
- Logic chính: `Play`, `Options`, `Quit`.
- Script liên quan: `SceneTransitionContext`.
- Ví dụ: Bấm Chơi vào cảnh đầu với điểm vào được cấu hình.

### `Core/SceneTransitionContext.cs`
- Vai trò: Lưu thông tin cảnh đích + điểm vào khi chuyển cảnh.
- Dữ liệu chính: tên cảnh, ID điểm vào, cờ đang chờ.
- Logic chính: `LoadScene`, `TryConsumeEntryPointForActiveScene`, phân tích cú pháp chuỗi cảnh|điểm.
- Script liên quan: `MainMenuController`, `SceneExitInteractable`, `SceneEdgeAutoPortal`, `PlayerSceneEntryHandler`.
- Ví dụ: Từ cảnh A sang cảnh B với điểm vào `north_gate`.

### `Core/SceneEntryPoint.cs`
- Vai trò: Đánh dấu điểm sinh trong cảnh.
- Dữ liệu chính: `entryPointId`, cờ không có sẵn.
- Script liên quan: `PlayerSceneEntryHandler`.
- Ví dụ: Cảnh có 3 điểm vào, script này gán ID cho mỗi transform.

### `Core/PlayerSceneEntryHandler.cs`
- Vai trò: Đặt người chơi vào đúng điểm vào khi cảnh tải.
- Logic chính: Đọc chuyển tiếp đang chờ, tìm `SceneEntryPoint` trùng ID, không có sẵn nếu cần.
- Script liên quan: `SceneTransitionContext`, `SceneEntryPoint`.
- Ví dụ: Qua cửa bên trái bản đồ cũ, sang bản mới sinh ở `from_left`.

### `Core/SceneExitInteractable.cs`
- Vai trò: Cổng/cửa chuyển cảnh qua hành động tương tác.
- Dữ liệu chính: tên cảnh đích, điểm vào đích.
- Logic chính: `Interact()` gọi `SceneTransitionContext.LoadScene(...)`.
- Script liên quan: `PlayerMovement`, `PlayerInteraction`.
- Ví dụ: Nhấn F trước cửa nhà -> tải cảnh nội thất nhà.

### `Core/SceneEdgeAutoPortal.cs`
- Vai trò: Chuyển cảnh tự động khi người chơi vượt biên trái/phải.
- Dữ liệu chính: x biên trái/phải, đích cho mỗi bên, độ trễ, kiểm tra đầu vào hướng ra.
- Logic chính: Theo dõi vị trí người chơi theo khung hình và kích hoạt chuyển đổi khi đạt điều kiện.
- Script liên quan: `SceneTransitionContext`.
- Ví dụ: Đi qua mép phải bản đồ -> tự động sang bản bên cạnh.

### `Core/SceneTransitionFader.cs`
- Vai trò: Hiệu ứng mờ đến/ra khi chuyển cảnh (singleton bền vững).
- Dữ liệu chính: fadeOutDuration, fadeInDuration, fadeColor, thứ tự sắp xếp.
- Logic chính: Tạo canvas phủ, mờ ra -> tải cảnh -> mờ vào.
- Script liên quan: các hệ thống gọi tải cảnh.
- Ví dụ: Chuyển cảnh không bị cứng, màn hình đen nhanh 0.2s rồi sáng lại.

### `Core/CameraSmoothFollow.cs`
- Vai trò: Dịch độ dịch chuyển camera mềm theo hướng di chuyển (nhìn trước).
- Dữ liệu chính: offsetAmount, smoothTime, tham chiếu camera Cinemachine.
- Logic chính: `UpdateCameraDirection(moveX)` và tween độ dịch chuyển bằng LeanTween.
- Script liên quan: `PlayerMovement`.
- Ví dụ: Người chơi di sang phải, camera sẽ tuyến tính về phía trước để nhìn xa hơn.

## 3.4 Battle

### `Battle/BattleManager.cs`
- Vai trò: Điều phối toàn bộ trận đấu theo lượt.
- Dữ liệu chính: quản lý bộ bài/hành động/trạng thái/bộ nhớ, người chơi, khe kẻ thù, bối cảnh, danh sách kẻ thù sống.
- Logic chính: `StartEncounter`, `StartTurn`, `EndTurn`, `ResolveQueue`, kiểm tra thắng/thua.
- Script liên quan: tất cả script trận đấu + `MemoryManager`, `StateManager`.
- Ví dụ: Người chơi chơi 2 thẻ, bấm End Turn, hàng đợi giải quyết hết, đến lượt kẻ thù, qua lượt mới.

### `Battle/BattleContext.cs`
- Vai trò: Gói trạng thái trận đấu để hành động truy cập thông tin cần thiết.
- Dữ liệu chính: tham chiếu quản lý, đối tượng người chơi, ngẫu nhiên, danh sách kẻ thù, số lượt.
- Logic chính: trợ giúp lấy danh sách kẻ thù còn sống, mục tiêu chính kẻ thù.
- Script liên quan: `PlayCardAction`, `ActionQueue`, `BattleManager`.
- Ví dụ: Hành động sát thương tất cả kẻ thù lấy mục tiêu từ `GetAliveEnemies()`.

### `Battle/ActionQueue.cs`
- Vai trò: Hàng đợi FIFO cho các hành động trận đấu.
- Dữ liệu chính: Queue `IBattleAction`, bối cảnh.
- Logic chính: `Enqueue`, `ProcessNext`, `Clear`, sự kiện hành động xếp hàng/giải quyết.
- Script liên quan: `BattleManager`, `PlayCardAction`.
- Ví dụ: Chơi 3 thẻ trong lượt -> hàng đợi có 3 hành động và giải quyết lần lượt lúc End Turn.

### `Battle/DeckManager.cs`
- Vai trò: Quản lý bộ ứng dụng, tay, loại bỏ, lưu trữ.
- Dữ liệu chính: startingDeck, các danh sách bộ, handSize, drawPerTurn, ngẫu nhiên.
- Logic chính: xáo trộn, rút thẻ, loại bỏ/lưu trữ, trả thẻ từ loại bỏ, xây dựng lại chế độ xem tay.
- Script liên quan: `BattleManager`, `PlayCardAction`.
- Ví dụ: Hết bộ ứng dụng thi xáo trộn loại bỏ rồi rút tiếp.

### `Battle/EnemyController.cs`
- Vai trò: Điều khiển 1 kẻ thù cụ thể, thực hiện `IDamageable`.
- Dữ liệu chính: `EnemyData`, sức khỏe hiện tại, ref `BattleManager`.
- Logic chính: `Initialize`, `TakeDamage`, `Heal`, `TakeTurn` theo loại nước đi.
- Script liên quan: `BattleManager`, `MemoryManager`, `StateManager`.
- Ví dụ: Kẻ thù dùng nước đi AttackMemory làm người chơi mất 2% bộ nhớ.

### `Battle/PlayCardAction.cs`
- Vai trò: Xử lý logic khi 1 lá bài được chơi.
- Dữ liệu chính: dữ liệu thẻ, nguồn, mục tiêu.
- Logic chính: `Resolve` -> tính chi phí -> `TrySpend` -> lặp qua từng hiệu ứng và áp dụng.
- Hỗ trợ hiệu ứng: Sát thương, HealMemory, Buff/Debuff/Status, Rút, Loại bỏ, Lưu trữ, ReturnFromDiscard.
- Script liên quan: `MemoryManager`, `DeckManager`, `StateManager`, `EnemyController`, `BattleContext`.
- Ví dụ: Thẻ A gây 6 sát thương tất cả kẻ thù + rút 1 thẻ.

## 3.5 Dialogue

### `Dialogue/DialogueManager.cs`
- Vai trò: Máy trạng thái hội thoại và xử lý kích hoạt sự kiện trong nút.
- Dữ liệu chính: hội thoại hoạt động, nút hiện tại, cờ, tiến trình trình tự thưởng thẻ.
- Logic chính: `StartDialogue`, `ShowNextLine`, `EndDialogue`, xử lý sự kiện theo nút.
- Script liên quan: `DialogueUIManager`, `BattleManager`, `MemoryManager`, `VignetteManager`, `SceneTransitionContext`.
- Ví dụ: Nút có sự kiện `StartBattle`, vừa next dòng là kích hoạt trận đấu.

### `Dialogue/DialogueUIManager.cs`
- Vai trò: Điều khiển bảng hội thoại và hiệu ứng máy đánh máy.
- Dữ liệu chính: tham chiếu bảng/diễn viên/văn bản, tốc độ máy đánh máy, phím next, âm thanh tick.
- Logic chính: nhận đầu vào để bỏ qua hoặc sang dòng tiếp, xử lý tạm dừng câu theo dấu chấm/phẩy.
- Script liên quan: `DialogueManager`.
- Ví dụ: Câu hội thoại dài chạy từng ký tự, nhấn F để hiển thị đầy đủ ngay lập tức.

### `Dialogue/DialogueInteractable.cs`
- Vai trò: NPC có hội thoại, có logic chuyển sang hội thoại sau thưởng.
- Dữ liệu chính: hội thoại đầu, hội thoại sau thưởng, rewardSequenceId, chế độ khóa.
- Logic chính: `Interact` + `ResolveDialogue` để chọn bộ hội thoại phù hợp.
- Script liên quan: `DialogueManager`.
- Ví dụ: Lần đầu nói chuyện để nhận thẻ, lần sau NPC nói nhanh theo script sau thưởng.

### `Dialogue/CardRewardPresenter.cs`
- Vai trò: Trình bày thưởng thẻ có hoạt ảnh.
- Dữ liệu chính: tham chiếu giao diện phủ/thẻ, thời gian bay vào/ra, xếp chồng, máy đánh máy chú thích.
- Logic chính: `PlaySequence(...)` chạy từng thẻ theo chuỗi: bay vào -> giữ -> văn bản -> bay ra.
- Script liên quan: `DialogueManager` (hoặc luồng thưởng sau hội thoại).
- Ví dụ: Kết thúc hội thoại, 2 thẻ mới hiển thị lần lượt với chú thích.

### `Dialogue/MemoryShop.cs`
- Vai trò: Điểm tương tác cửa hàng.
- Logic chính: `Interact` ưu tiên mở `shopDialogue`; nếu không có thi tải cảnh dự phòng.
- Script liên quan: `DialogueManager`, `SceneTransitionContext`.
- Ví dụ: Người chơi tương tác ki-ốt cửa hàng, mở hội thoại mua vật phẩm.

## 3.6 Vignette

### `Vignette/VignetteManager.cs`
- Vai trò: Hiển thị vignette theo dòng văn bản và hiệu ứng.
- Dữ liệu chính: vignette hoạt động, chỉ số dòng, bộ một lần đã hiển thị, ref `MemoryManager`.
- Logic chính: `Show`, `ShowNextLine`, `Skip`, `Close`, bỏ qua vignette không thể phát lại đã xem.
- Script liên quan: `DialogueManager`, `VignetteData`, `MemoryManager`.
- Ví dụ: Kích hoạt vignette sau nút hội thoại, đọc hết 3 dòng thi đóng và mở khóa đoạn.

## 4. Ví dụ end-to-end cụ thể

### Tình huống: Gặp NPC, mở trận đấu, thắng trận, nhận thưởng

1. Người chơi di chuyển trong cảnh (`PlayerMovement`) và camera độ dịch chuyển theo hướng (`CameraSmoothFollow`).
2. Người chơi chạm vào kích hoạt NPC (`DialogueInteractable`) và nhấn F.
3. `DialogueManager.StartDialogue()` bắt đầu hội thoại; `DialogueUIManager` hiển thị văn bản máy đánh máy.
4. Đến nút có sự kiện `StartBattle`, `BattleManager.StartEncounter()` được gọi.
5. `DeckManager` xáo trộn bài + rút tay đầu; `BattleManager.StartTurn()` vào lượt người chơi.
6. Người chơi chơi thẻ:
   - Tạo `PlayCardAction`.
   - Đưa vào `ActionQueue`.
7. Bấm End Turn:
   - `BattleManager.ResolveQueue()` xử lý hiệu ứng thẻ.
   - Kẻ thù `TakeTurn()` gây ảnh hưởng lên Bộ nhớ hoặc trạng thái.
8. Lặp đến khi tất cả kẻ thù bị hạ:
   - `BattleManager` kết thúc cuộc gặp với kết quả thắng.
9. Chuỗi thưởng thẻ hiển thị qua `CardRewardPresenter`.
10. Lần tương tác sau, `DialogueInteractable` có thể chuyển sang hội thoại sau thưởng.

## 5. Ghi chú quan trọng khi bảo trì

1. Hiện tại tồn tại 2 hướng xử lý tương tác (`PlayerMovement` và `PlayerInteraction`). Nên thống nhất để tránh xung đột đầu vào.
2. Bộ nhớ hiện đang vừa đóng vai trò tài nguyên cho thẻ, vừa ảnh hưởng đến điều kiện thua. Cần giữ quy tắc này nếu là quan trọng trong thiết kế.
3. Khi thêm thẻ mới:
   - Thêm `CardData`.
   - Đảm bảo `PlayCardAction` hỗ trợ đúng mục tiêu/hiệu ứng mong muốn.
   - Kiểm tra cân bằng với `MemoryManager.TrySpend`.
4. Khi thêm cảnh mới:
   - Đặt đầy đủ `SceneEntryPoint`.
   - Cấu hình đúng `destinationSceneName` và `destinationEntryPointId`.
5. Khi thêm sự kiện hội thoại mới:
   - Mở rộng enum/xử lý sự kiện trong `DialogueManager`.
   - Xác nhận sự kiện không vô tình chạy lặp khi next dòng.

## 6. Danh sách script để đối chiếu nhanh

1. `Battle/ActionQueue.cs`
2. `Battle/BattleContext.cs`
3. `Battle/BattleManager.cs`
4. `Battle/DeckManager.cs`
5. `Battle/EnemyController.cs`
6. `Battle/PlayCardAction.cs`
7. `Core/CameraSmoothFollow.cs`
8. `Core/MainMenuController.cs`
9. `Core/MemoryManager.cs`
10. `Core/PlayerInteraction.cs`
11. `Core/PlayerMovement.cs`
12. `Core/PlayerSceneEntryHandler.cs`
13. `Core/SceneEdgeAutoPortal.cs`
14. `Core/SceneEntryPoint.cs`
15. `Core/SceneExitInteractable.cs`
16. `Core/SceneTransitionContext.cs`
17. `Core/SceneTransitionFader.cs`
18. `Core/StateManager.cs`
19. `Data/CardData.cs`
20. `Data/DialogueData.cs`
21. `Data/EnemyData.cs`
22. `Data/VignetteData.cs`
23. `Dialogue/CardRewardPresenter.cs`
24. `Dialogue/DialogueInteractable.cs`
25. `Dialogue/DialogueManager.cs`
26. `Dialogue/DialogueUIManager.cs`
27. `Dialogue/MemoryShop.cs`
28. `Interfaces/IBattleAction.cs`
29. `Interfaces/IDamageable.cs`
30. `Interfaces/IInteractable.cs`
31. `Vignette/VignetteManager.cs`