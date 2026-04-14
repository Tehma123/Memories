# Luồng Gameplay dùng Mermaid

Ngày cập nhật: 2026-04-14  
Phạm vi: `Assets/_Project/Gameplay/Scripts`

Tài liệu này tập trung vào luồng và quan hệ giữa các hệ thống, để bạn nhìn nhanh đường đi dữ liệu và chuỗi gọi hàm.

## 1) Kiến trúc tổng quan

```mermaid
graph TD
    PM[PlayerMovement] --> PI[PlayerInteraction]
    PM --> CAM[CameraSmoothFollow]
    PI --> II[IInteractable]

    II --> DI[DialogueInteractable]
    II --> SEI[SceneExitInteractable]
    II --> SHOP[MemoryShop]

    DI --> DM[DialogueManager]
    SHOP --> DM
    DM --> DUI[DialogueUIManager]
    DM --> CRP[CardRewardPresenter]
    DM --> VM[VignetteManager]
    DM --> MM[MemoryManager]
    DM --> STM[StateManager]
    DM --> STC[SceneTransitionContext]
    DM --> BM[BattleManager]

    BM --> BC[BattleContext]
    BM --> AQ[ActionQueue]
    BM --> DK[DeckManager]
    BM --> EC[EnemyController]
    BM --> MM
    BM --> STM

    AQ --> PCA[PlayCardAction]
    PCA --> BC
    PCA --> DK
    PCA --> MM
    PCA --> STM
    PCA --> IDMG[IDamageable]
    EC --> IDMG

    STC --> STF[SceneTransitionFader]
    STF --> PSL[PlayerSceneEntryHandler]
    PSL --> SEP[SceneEntryPoint]
    PM --> SAP[SceneEdgeAutoPortal]
    SAP --> STC

    CD[CardData] --> DK
    CD --> PCA
    DD[DialogueData] --> DM
    ED[EnemyData] --> BM
    ED --> EC
    VD[VignetteData] --> VM
```

## 2) Chuỗi chính: Khám phá → Hội thoại → Chiến đấu → Phần thưởng → Cảnh

```mermaid
sequenceDiagram
    autonumber
    participant P as Player
    participant PM as PlayerMovement
    participant PI as PlayerInteraction
    participant NPC as DialogueInteractable
    participant DM as DialogueManager
    participant DUI as DialogueUIManager
    participant BM as BattleManager
    participant AQ as ActionQueue
    participant PCA as PlayCardAction
    participant EC as EnemyController
    participant MM as MemoryManager
    participant STC as SceneTransitionContext
    participant STF as SceneTransitionFader
    participant PSL as PlayerSceneEntryHandler

    P->>PM: Di chuyển
    PM->>PI: Có thể tương tác khi vào tầm
    P->>PM: Nhấn nút tương tác
    PM->>NPC: Interact()
    NPC->>DM: StartDialogue(dialogueData)
    DM->>DUI: OnDialogueStarted / OnDialogueLineShown
    P->>DUI: Dòng tiếp theo
    DUI->>DM: ShowNextLine()

    alt Nút kích hoạt StartBattle
        DM->>BM: StartEncounter(enemies)
        BM->>BM: StartTurn()
        P->>BM: EnqueueAction(chơi thẻ)
        BM->>AQ: Enqueue(action)
        P->>BM: EndTurn()
        BM->>AQ: ResolveQueue()
        AQ->>PCA: Resolve(context)
        PCA->>MM: TrySpend(cost)
        PCA->>EC: TakeDamage(...) hoặc hiệu ứng
        BM->>EC: TakeTurn()
        EC->>MM: ChangeMemory(-value)
        BM->>BM: Kiểm tra chiến thắng/thua
    end

    alt Nút kích hoạt GiveCard
        DM->>DM: HandleGiveCardEvent()
        DM->>DUI: Vào luồng trình bày phần thưởng
    end

    alt Nút kích hoạt LoadScene
        DM->>STC: LoadScene(scene, entry)
        STC->>STF: TryLoadSceneWithFade(scene)
        STF->>PSL: Sau khi cảnh mới tải
        PSL->>PSL: TryApplyEntryPoint()
    end
```

## 3) Biểu đồ luồng theo giai đoạn chiến đấu

```mermaid
flowchart TD
    A[StartEncounter] --> B[Sinh kẻ thù + Khởi tạo bộ bài + Rút bài]
    B --> C[StartTurn]
    C --> D[Người chơi chơi thẻ -> EnqueueAction]
    D --> E{Kết thúc lượt?}
    E -- Chưa --> D
    E -- Rồi --> F[ResolveQueue]
    F --> G[Lượt kẻ thù]
    G --> H[TickTurnEnd trạng thái]
    H --> I{Người chơi bị đánh bại?}
    I -- Có --> L[Gặp gỡ kết thúc: thua]
    I -- Không --> J{Tất cả kẻ thù bị đánh bại?}
    J -- Có --> K[Gặp gỡ kết thúc: thắng]
    J -- Không --> C
```

## 4) Ghi chú đọc biểu đồ

1. `SceneTransitionContext` là điểm vào chung cho chuyển cảnh, có quan hệ chặt chẽ với `SceneTransitionFader` và `PlayerSceneEntryHandler`.
2. `BattleContext` là một tham chiếu được truyền vào hành động khi giải quyết, để tránh hành động phải tìm quản lý ở nhiều nơi.
3. `PlayCardAction` là trung tâm xử lý hiệu ứng thẻ; được cung cấp bởi `ActionQueue` và `BattleManager`.
4. `MemoryManager` đang là tài nguyên liên tục: được sử dụng trong chi phí thẻ, tấn công kẻ thù bộ nhớ, khóa mở hội thoại mảnh.
