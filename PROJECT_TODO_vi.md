TODO DỰ ÁN — Memories (triển khai hoàn chỉnh & hướng dẫn scene)

## Mục lục
- Tổng quan
- Nhóm ưu tiên
- Dữ liệu lõi (ScriptableObjects)
  - CardData
  - EnemyData
  - DialogueData
  - VignetteData
- Scene (yêu cầu & cách sử dụng)
  - ExplorationScene
  - BattleScene
  - Shared UI Scene
- Hệ thống lõi & Scripts
- UI / Hiệu ứng / Âm thanh
- Kiểm thử & Công cụ
- Hoàn thiện, Localization & Accessibility
- Checklist build & phát hành
- Cách dùng file TODO

## Tổng quan
- Mục tiêu: Danh sách công việc ưu tiên, có thể thực hiện cho toàn bộ dự án. Dùng như runbook cho dev: các SO/asset cần tạo, nội dung mỗi scene, script cần gắn, và tham số công khai cần điều chỉnh.

## Nhóm ưu tiên
1. Dữ liệu lõi & hệ thống
2. Scene & prefab
3. UI / VFX / Audio
4. Kiểm thử, hoàn thiện & phát hành

---

## Dữ liệu lõi (ScriptableObjects)
- `CardData` (SO)
  - Trường: `id`, `name`, `CardType`(enum), `costPercent`, `effectType`, `power`, `statusApplied`, `sprite1bit`, `flavorText`.
  - Công dụng: Tạo một asset SO cho mỗi lá bài. `DeckManager` sẽ load các SO này khi bắt đầu trận.
  - Mặc định: Tạo 5 lá khởi đầu (Pain, Void, Euphoria, Wrath, Nostalgia).

- `EnemyData` (SO)
  - Trường: `id`, `displayName`, `maxHP`, `moves` (list tham chiếu tới `MoveData` SO), `startStates`, `sprite1bit`.
  - Công dụng: `BattleManager` spawn prefab kẻ địch và gán SO này.

- `DialogueData` / `DialogueNode` (SO)
  - Trường: nodes: speaker, text, portraitRef, audioCue, choices, flagsToSet, nextNode.
  - Công dụng: Prefab NPC tham chiếu `DialogueData` SO; `DialogueManager` sẽ chạy hội thoại.

- `VignetteData` (SO)
  - Trường: `vignetteType` (Newspaper/Scratch/Terminal), `textLines`, `displayEffects`, `audioCue`, `revealMemoryId`, `replayable`.
  - Công dụng: Prefab `Paper` tham chiếu SO này; `VignetteManager` hiển thị khi người chơi tương tác.

---

Scene (yêu cầu & cách sử dụng)

1) `ExplorationScene` (thế giới chính)
- Phải có:
  - Prefab Player với `PlayerController`, `PlayerInteraction`, và tham chiếu tới `MemoryManager`.
  - Điểm spawn tương tác: NPC, Paper, cửa hàng (Are you lost?), trigger khởi battle.
  - Object quản lý sự kiện: `GameManagers` root chứa `MemoryManager`, `DialogueManager`, `VignetteManager`, `StateManager` (kiểu singleton trong scene).
- Prefab cần chuẩn bị:
  - NPC prefab: Sprite, `Collider2D` (isTrigger), script implement `IInteractable`, tham chiếu `DialogueData` SO.
  - Paper prefab: Sprite, `Collider2D`, `IInteractable`, tham chiếu `VignetteData` SO.
- Cách dùng:
  - Gắn `IInteractable` cho các prefab và cấu hình `interactKey` trong `PlayerInteraction` (mặc định `F`).
  - Với vignette một lần, đặt `replayable=false` để `VignetteManager` vô hiệu hóa collider sau khi hiển thị.

2) `BattleScene` (overlay additive)
- Phải có:
  - `BattleManager` root (1 instance khi scene load).
  - Prefab `UICanvas_Battle` với layout 3 tầng: Tầng enemy, tầng bài (hand), tầng player (avatar + thanh Memory).
  - `DeckManager` (per-player), `StateManager`, `CardInputHandler` (xử lý chọn/ra bài).
  - Vị trí spawn kẻ địch (enemy slots) để spawn prefab kẻ địch theo `EnemyData` SO.
- Cách dùng:
  - Khi gặp enemy trong `ExplorationScene`, gọi `SceneManager.LoadSceneAsync("BattleScene", LoadSceneMode.Additive)` và truyền `BattleContext` (refs enemy SO, vị trí player) tới `BattleManager`.
  - Sau khi battle kết thúc, `BattleManager` gọi UnloadScene và trả lại quyền điều khiển cho player.

3) Shared UI Scene (tùy chọn)
- Đặt AudioManager, canvas chính (menu), và LocalizationManager trong scene này; load 1 lần khi game start.

---

Hệ thống lõi & Scripts

- `PlayerController`
  - Trách nhiệm: di chuyển (Input System), trạng thái animation, public `Transform` để hệ thống khác tham chiếu.
  - Gắn: Player prefab root.
  - Tham số công khai: `moveSpeed`, `collisionLayerMask`, `lookDirection`.

- `PlayerInteraction`
  - Trách nhiệm: raycast/trigger để tìm `IInteractable` gần nhất, hiển thị prompt, gọi `Interact()` khi nhấn phím.
  - Gắn: Player prefab.
  - Tham số: `interactKey` (mặc định `KeyCode.F`), `interactionRange`.

- `MemoryManager`
  - Trách nhiệm: quản lý `memoryPercent` (0–100), áp dụng tăng/giảm, kích hoạt `GameOver` ở 0.
  - Gắn: GameManagers root.
  - Tham số: `startingMemory`, event hooks: `OnMemoryChanged`, `OnMemoryDepleted`.

- `DeckManager`
  - Trách nhiệm: giữ deck, Draw/Shuffle/Discard/Exile, instantiate UI card từ `CardData`.
  - Gắn: `BattleManager` hoặc object player trong battle scene.
  - Tham số: `handSize` (mặc định 5), `drawDelay`, refs tới card prefab và danh sách SO thẻ.

- `BattleManager`
  - Trách nhiệm: vòng luân phiên turn, thực thi hiệu ứng bài, giao tiếp với `StateManager` và `MemoryManager`.
  - Gắn: BattleScene root.
  - Tham số: `turnTimer`, `enemySlots` (transforms), `playerSlot`.

- `StateManager`
  - Trách nhiệm: apply/expire buff/debuff; tra cứu trạng thái (ví dụ "U buồn").
  - API công khai: `ApplyState(target, stateId, duration)`, `HasState(target, stateId)`.

- `DialogueManager`
  - Trách nhiệm: chạy `DialogueData` SO, hiển thị UI, xử lý lựa chọn và side-effect (set flags, trao item).
  - Gắn: GameManagers root.
  - Tham số: `typewriterSpeed`, `skipKey`.

- `VignetteManager`
  - Trách nhiệm: hiển thị `VignetteData` với hiệu ứng, thông báo `MemoryManager` nếu có fragment được mở khóa.
  - Gắn: GameManagers root.
  - Tham số: `instantText` toggle (tùy chọn accessibility), intensity của hiệu ứng.

- `EnemyAI` / `EnemyController`
  - Trách nhiệm: chọn move từ `EnemyData.moves`, tương tác với `BattleManager` để thực thi.
  - Gắn: Enemy prefab.
  - Tham số: `aiDifficulty`, `aggressiveness`.

---

UI / Hiệu ứng / Âm thanh

- UI chiến đấu:
  - Card prefab: gắn script `CardView` (bind tới `CardData`) với trường công khai: `artImage`, `costText`, `nameText`, `onPlay` event.
  - Thanh Memory: bind tới `MemoryManager.OnMemoryChanged` để cập nhật giá trị và màu ở ngưỡng.

- VFX:
  - Chuẩn bị prefab hoặc component cho: screen shake, flash/invert shader, dithering noise overlay, glitch shader.
  - Expose intensity (float) để designer tinh chỉnh theo event.

- Audio:
  - Bank SFX: typewriter, static, play card, attack, boss reveal.
  - `AudioManager` cung cấp `PlaySFX(name)` và control âm nhạc.

---

Kiểm thử & Công cụ

- Unit tests (Editor, NUnit):
  - Test `DeckManager` cho logic draw/discard/exile.
  - Test `MemoryManager` cho biên giới (0/100 trigger).
  - Test `StateManager` cho cơ chế apply/expire.

- Công cụ debug:
  - `DebugUI` in-editor: spawn enemy, grant memory, reveal vignette, fast-forward turns.

---

Hoàn thiện, Localization & Accessibility

- Localization: lưu chuỗi trong bảng localization; `DialogueData` nodes tham chiếu key localization.
- Accessibility: `VignetteManager` hỗ trợ instant text và chế độ high-contrast; cho phép remap phím.
- Hiệu năng: dùng atlas/sprite sheet cho art 1-bit; pool UI card để giảm GC.

---

Checklist build & phát hành

- Kiểm tra các scene có trong `Build Settings`.
- Chạy test tự động và sửa lỗi.
- Tạo build release (Windows) và smoke test luồng battle + giới hạn memory.

---

Cách dùng file TODO này
- Theo nhóm ưu tiên mà làm; cập nhật trạng thái trong project tracker hoặc chỉnh `PROJECT_TODO_vi.md`.
- Dùng `GameManagers` root để wire các singleton scene; kéo thả SO vào public fields trong Inspector.
- Khi thêm thẻ hoặc kẻ địch mới: tạo SO asset, rồi thêm vào pool (mảng SO) mà `DeckManager` / `BattleManager` dùng.

Nếu bạn muốn, tôi có thể:
- Ghi đè `Specification.md` bằng phiên bản này,
- Sinh danh sách task CSV (Trello/Jira),
- Tạo template unit test cho `DeckManager` / `MemoryManager`.
