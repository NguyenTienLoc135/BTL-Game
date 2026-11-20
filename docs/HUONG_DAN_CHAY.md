# Hướng dẫn chạy Cờ Tỷ Phú RPG

Tài liệu này dành cho cả nhóm: lấy code, mở project, chơi thử, dựng lại Scene, build, chạy test và các lỗi hay gặp.
Luật chơi nằm ở [GAME_RULES.md](GAME_RULES.md); phân công và ranh giới module nằm ở [TEAM_TASKS.md](TEAM_TASKS.md).

## 1. Yêu cầu

| Thứ | Yêu cầu |
|---|---|
| Unity | **6000.6.4f1** (đúng phiên bản trong `ProjectSettings/ProjectVersion.txt`), pipeline URP có sẵn trong project |
| Git LFS | **Bắt buộc.** Model `.fbx` và âm thanh `.ogg` nằm trong LFS |
| Hệ điều hành | Windows (môi trường chính) |
| Package | Không cần cài thêm; mọi package đã nằm trong `Packages/manifest.json` |

## 2. Lấy code (luôn pull `develop` trước khi làm tiếp)

Lần đầu:

```bash
git lfs install
git clone https://github.com/NguyenTienLoc135/BTL-Game.git
cd BTL-Game
git lfs pull
```

Mỗi lần bắt đầu làm việc:

```bash
git fetch origin
git switch develop
git pull --ff-only origin develop
git lfs pull
git switch -c ten-nhanh-cua-ban      # hoặc quay lại nhánh cá nhân rồi: git merge develop
```

Quy tắc nhóm: làm trên nhánh cá nhân, đưa vào `develop` bằng Pull Request (`develop` bị khóa, không push thẳng).
Chỉ giữ **một** thư mục project; không tạo bản sao thứ hai của project (kể cả trong `.claude/worktrees`).

## 3. Mở project lần đầu

1. Unity Hub → **Add** → chọn thư mục gốc `BTL-Game` → mở bằng Unity 6000.6.4f1.
2. Lần mở đầu Unity import lại toàn bộ (`Library` không nằm trong Git) — mất vài phút, bình thường.
3. Đợi Console hết biên dịch. Không được có lỗi đỏ.

## 4. Chạy game trong Unity

1. Mở `Assets/_Game/Scenes/MainMenu.unity` (build index 0) và nhấn **Play**.
2. **Bắt đầu chơi** → chọn **Chơi với Bot** hoặc **Chơi với người**.
3. Màn thiết lập: chọn số người (2–4), thời gian (30/45/60 phút hoặc Vô hạn), nghề của từng người (được trùng nghề);
   bấm vào thẻ người chơi để xem Passive / Active / Ultimate / Weakness. Người đi trước do Core chọn ngẫu nhiên.
4. **Bắt đầu trận** → Scene `Gameplay` (build index 1). Core (`UnityMatchHost`) tạo trận từ cấu hình vừa chọn.

Trong trận:

| Nút | Việc |
|---|---|
| Xúc xắc | Tung xúc xắc / xác nhận / kết thúc lượt (theo quyền hợp lệ do Core báo) |
| Kỹ năng | Dùng Active hoặc Ultimate hợp lệ |
| Tài sản | Xem tài sản; nút **Bán tài sản** bán tự nguyện khi được phép |
| Thẻ | Xem thẻ đang giữ và trạng thái |
| Menu | Tạm dừng: Tiếp tục, Cài đặt âm thanh, Về menu chính |

Cài đặt âm thanh: kéo thanh chỉ để nghe thử; chỉ nút **Lưu** mới ghi. Đóng popup mà chưa Lưu thì hoàn về giá trị đã lưu.
Mở thẳng Scene `Gameplay` rồi Play sẽ tự quay về `MainMenu` (không tự chọn roster).

## 5. Dựng lại Scene

Hai Scene `MainMenu` và `Gameplay` do builder Editor sinh ra, **không sửa Scene bằng tay** (sửa tay sẽ mất khi dựng lại):

- Menu **Tools ▸ CoTyPhu ▸ Build All Screens** — dựng lại cả hai Scene.
- Hoặc dựng riêng: **Build MainMenu Scene** / **Build Gameplay Scene**.
- Muốn đổi giao diện/bàn cờ: sửa builder rồi dựng lại.

Code liên quan:

| Phần | Vị trí |
|---|---|
| Builder và công cụ dựng UI/bàn cờ | `Assets/_Game/Editor/Presentation/` (asmdef `BtlGame.Presentation.Editor`) |
| View/controller giao diện chạy lúc Play | `Assets/_Game/Scripts/Presentation/` (asmdef `BtlGame.Presentation`) |
| Nối Core ↔ giao diện | `UnityMatchHost` + `IGameplayUi` (`Scripts/Core`, `Scripts/UI`) |
| Màu/tên ô bàn cờ | `BoardVisualCatalog` (một nguồn duy nhất) |

> **Không chạy** menu cũ **BTL Game ▸ Connect Team Gameplay** và `PackageBuilder` — chúng dựng Scene theo bản cũ và sẽ ghi đè hai Scene hiện tại.

## 6. Build bản Windows

Menu **Tools ▸ CoTyPhu ▸ Build Playtest (Windows)** → `Builds/Playtest/CoTyPhuRPG.exe` (thư mục `Builds` đã nằm trong `.gitignore`).

Dòng lệnh (đóng Unity trước, vì một project chỉ mở được ở một tiến trình):

```bash
"C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -nographics -projectPath <thu-muc-project> -logFile build.log -executeMethod BTLGame.EditorTools.BuildPlaytest.Build
```

## 7. Chạy test

Trong Unity: **Window ▸ General ▸ Test Runner** → **EditMode** → Run All (assembly `BtlGame.Core.Tests`).

Dòng lệnh:

```bash
"C:/Program Files/Unity/Hub/Editor/6000.6.4f1/Editor/Unity.exe" -batchmode -nographics -projectPath <thu-muc-project> -runTests -testPlatform EditMode -testResults ket-qua.xml -logFile test.log
```

Bộ test hiện có 239 test (luật, AI, thẻ, tích hợp). Trong đó `TeamIntegrationTests.ActualScenesStartFromMenuAndCompleteAHumanMovementWithUi`
chạy luồng thật: MainMenu → Chơi với người → Bắt đầu trận → Gameplay → tung xúc xắc → xác nhận → quân di chuyển xong.

## 8. Lỗi thường gặp

| Hiện tượng | Nguyên nhân / cách xử lý |
|---|---|
| Model nhà/nhân vật không hiện hoặc hồng | Thiếu Git LFS: chạy `git lfs install` rồi `git lfs pull`, đóng và mở lại Unity |
| Lỗi biên dịch ngay lần mở đầu | Sai phiên bản Unity (phải 6000.6.4f1) hoặc `Library` hỏng: đóng Unity, xóa thư mục `Library`, mở lại |
| Menu **Tools ▸ CoTyPhu** không thấy | Đang có lỗi biên dịch: sửa lỗi Console trước |
| Nút bấm không phản hồi khi Play | Scene thiếu EventSystem (do sửa tay): dựng lại bằng **Build All Screens** |
| Play `Gameplay` bị quay về menu | Đúng thiết kế: phải bắt đầu từ `MainMenu` để có cấu hình trận |
| Git báo xung đột ở `.unity`/`.prefab` | Hạn chế hai người cùng sửa Scene; với Scene sinh bởi builder thì lấy bản `develop` rồi dựng lại |
| Cảnh báo mạng/chứng chỉ của Unity (Unity Connect 403, TLS) | Không ảnh hưởng game, có thể bỏ qua |

## 9. Quy ước khi làm việc

- Đọc [AGENTS.md](../AGENTS.md) và `docs/GAME_RULES.md` trước khi sửa logic. ID ô 0–31 là cố định.
- Không sửa `Assets/ThirdParty/`; asset tùy biến đặt dưới `Assets/_Game`. Commit file `.meta` cùng asset.
- Trước khi mở PR: biên dịch sạch và chạy bộ test EditMode.
