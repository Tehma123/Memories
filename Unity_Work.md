# Hướng dẫn tạo Dialogue mẫu (Unity Editor + VS Code)

Tài liệu này hướng dẫn từng bước tạo một dialogue mẫu đơn giản trong project Memories.
Mục tiêu: nhấn phím tương tác với NPC, mở dialogue, và chuyển line theo luồng tuyến tính.

---

## 0) Điều kiện trước khi bắt đầu

Cần có sẵn các script sau trong project:
- `DialogueData`
- `DialogueManager`
- `DialogueUIManager`
- `PortraitManager`
- `IInteractable`
- `PlayerInteraction`

Nếu đã làm theo bộ khung hiện tại trong project thì bạn đã có sẵn các script này.

---

## 1) VS Code: Tạo script NPC tương tác để mở dialogue

1. Mở VS Code tại thư mục project Unity.
2. Tạo file mới:
	`Assets/_Project/Gameplay/Scripts/Dialogue/NpcDialogueInteractable.cs`
3. Paste code sau:

```csharp
using UnityEngine;

public class NpcDialogueInteractable : MonoBehaviour, IInteractable
{
	 [SerializeField] private DialogueData dialogueData;

	 public void Interact()
	 {
		  if (dialogueData == null)
		  {
				Debug.LogWarning("NpcDialogueInteractable: dialogueData chưa được gán.");
				return;
		  }

		  if (DialogueManager.Instance == null)
		  {
				Debug.LogWarning("NpcDialogueInteractable: không tìm thấy DialogueManager trong scene.");
				return;
		  }

		  DialogueManager.Instance.StartDialogue(dialogueData);
	 }
}
```

4. Save file, quay lại Unity để Unity compile script.

---

## 2) VS Code: Tạo script debug để next line (tạm thời)

Nếu bạn chưa có UI button cho dialogue, tạo script debug input để test nhanh:

1. Tạo file mới:
	`Assets/_Project/Gameplay/Scripts/Dialogue/DialogueDebugInput.cs`
2. Paste code sau:

```csharp
using UnityEngine;

public class DialogueDebugInput : MonoBehaviour
{
	 private void Update()
	 {
		  if (DialogueManager.Instance == null)
		  {
				return;
		  }

		  if (Input.GetKeyDown(KeyCode.Space))
		  {
				DialogueManager.Instance.ShowNextLine();
		  }
	 }
}
```

3. Save file và đợi Unity compile xong.

---

## 3) Unity Editor: Tạo DialogueData asset mẫu

1. Trong Project window, vào:
	`Assets/_Project/Gameplay/Data/Dialogue`
2. Right click -> `Create > Memories > Data > Dialogue`
3. Đặt tên asset: `DLG_NPC_Intro`
4. Chọn asset `DLG_NPC_Intro` và setup:
	- `dialogueID`: `npc_intro_001`
	- `startNodeID`: `0`
	- `nodes`: size = `3`

5. Cấu hình node 0:
	- `nodeID`: `0`
	- `speakerName`: `NPC`
	- `textContent`: `Chào mừng đến với ký ức của tôi.`
	- `defaultNextNodeID`: `1`

6. Cấu hình node 1:
	- `nodeID`: `1`
	- `speakerName`: `NPC`
	- `textContent`: `Hãy nghe tiếp câu chuyện này.`
	- `defaultNextNodeID`: `2`

7. Cấu hình node 2:
	- `nodeID`: `2`
	- `speakerName`: `NPC`
	- `textContent`: `Ký ức này đã được mở khóa.`
	- `triggerEvent`: `UnlockMemoryFragment`
	- `eventParam`: `fragment_intro_001`
	- `defaultNextNodeID`: `-1`

---

## 4) Unity Editor: Đặt DialogueManager trong scene

1. Trong scene (vd: `ExplorationAct0`), tạo empty object tên `GameManagers` nếu chưa có.
2. Add component `DialogueManager` vào `GameManagers`.
3. Gán reference nếu cần:
	- `playerController`
	- `playerInteraction`
	- `stateManager`
	- `memoryManager`

Gợi ý: nếu để trống, script có cơ chế tìm tự động khi Awake.

4. Tạo object `PortraitManager` và add component `PortraitManager`.
	- Khai báo danh sách `portraitId -> Sprite` trong inspector.
	- Có thể gán `defaultPortrait` để fallback khi `portraitId` không tồn tại.

5. Tạo object `DialogueUI` và add component `DialogueUIManager`.
	- Gán `dialoguePanel`, `speakerText`, `dialogueText`, `portraitImage`.
	- Gán reference đến `DialogueManager` và `PortraitManager` (hoặc để script tự tìm).

---

## 5) Unity Editor: Tạo NPC và gán script tương tác

1. Tạo GameObject mới tên `NPC_Intro`.
2. Add component:
	- `SpriteRenderer` (chọn sprite bất kỳ để test)
	- `Collider2D` (vd `BoxCollider2D`) và bật `Is Trigger`
	- `NpcDialogueInteractable`
3. Trong inspector của `NpcDialogueInteractable`, kéo asset `DLG_NPC_Intro` vào ô `dialogueData`.

Lưu ý:
- Player cần có collider + `PlayerInteraction` để bắt trigger interact.
- Khoảng cách và layer phải cho phép PlayerInteraction tìm thấy IInteractable.

---

## 6) Unity Editor: Gán script debug input (nếu chưa có UI)

1. Tạo empty object tên `DialogueDebug`.
2. Add component `DialogueDebugInput`.
3. Mục đích:
	- `Space`: next line

---

## 7) Play test từng bước

1. Bấm Play.
2. Di chuyển player lại gần NPC.
3. Nhấn phím tương tác (theo setup hiện tại, thử `F`).
4. Kiểm tra Console:
	- Line node 0 hiện ra.
5. Nhấn `Space` -> sang node 1.
6. Nhấn `Space` -> sang node 2.
7. Nhấn `Space` -> kết thúc dialogue.

Kỳ vọng:
- Dialogue bắt đầu đúng.
- Chuyển node đúng theo `defaultNextNodeID`.
- Event `UnlockMemoryFragment` được gọi tại node 2.

---

## 8) Lỗi thường gặp và cách sửa nhanh

1. Không mở dialogue khi nhấn interact:
	- Kiểm tra NPC có `Collider2D` + `Is Trigger`.
	- Kiểm tra player có `PlayerInteraction`.
	- Kiểm tra NPC có script `NpcDialogueInteractable`.

2. Báo lỗi không tìm thấy DialogueManager:
	- Thêm `DialogueManager` vào scene.
	- Kiểm tra object có đang Active.

3. Nhập Space không tác dụng:
	- Kiểm tra `DialogueDebugInput` đã gán vào object active.
	- Kiểm tra game window đang focus khi bấm phím.

4. Dialogue kết thúc sớm:
	- Kiểm tra `defaultNextNodeID` của từng node.
	- Node kết thúc thì để `-1`.

---

## 9) Bước tiếp theo sau khi demo chạy

1. Dùng UI button gọi `DialogueUIManager.OnNextPressed()` thay cho `DialogueDebugInput`.
2. Hoàn thiện style cho panel thoại + portrait.
3. Tạo thêm `DialogueData` cho nhiều NPC.
4. Dùng `triggerEvent` để mở battle, load scene, hoặc set flag.

