# AGENTS.md — Cờ Tỷ Phú RPG (Unity 6 / C#)

> Áp dụng cho **Codex, Google Antigravity và các coding agent** khi làm việc trong repository này. Đặt file tại **gốc Git repository** `BTL-Game/AGENTS.md`. Đây là chỉ dẫn dự án, KHÔNG phải yêu cầu tự động triển khai toàn bộ game mỗi lần agent được mở.

## 0. NGUỒN SỰ THẬT BẮT BUỘC

- **Đọc `docs/GAME_RULES.md` trước khi viết/sửa bất kỳ gameplay logic, dữ liệu, UI có tính luật, AI hoặc bài test nào.** Khi chỉ làm một tính năng, đọc đầy đủ mục tương ứng và các luật liên quan (lượt, tiền, sở hữu, kỹ năng, thẻ, thắng/thua).
- Thứ tự ưu tiên gameplay: **(1) quyết định mới được người dùng phê duyệt rõ ràng → (2) Word gốc cho luật không bị quyết định mới thay đổi → (3) `docs/GAME_RULES.md` → (4) tài liệu kỹ thuật khác**. Không áp lại Word để phủ quyết một ngoại lệ mới đã được phê duyệt.
- Word thực tế: [docs/references/GAME_DESIGN_DOCUMENT.docx](docs/references/GAME_DESIGN_DOCUMENT.docx), trước đây được nhắc bằng tên `Bao_cao_Co_Ty_Phu_RPG_Giu_Nguyen_Muc_Luc_Goc(2).docx`. Khi đọc phải trích xuất nội dung thật; không sửa Word đã nộp. Quyết định mới và khác biệt với Word ghi tại [docs/DECISIONS.md](docs/DECISIONS.md).
- Ngày 2026-11-06, người dùng phê duyệt **Q04=A, Q05=A, Q06=A, Q09=A, Q11=A, Q13=A, Q20=B và tù tối đa 3 lượt riêng**. Đây là luật đã duyệt; các đề xuất kiến trúc A01–A07 không tự chuyển sang được phê duyệt.
- Đọc [docs/TEAM_TASKS.md](docs/TEAM_TASKS.md) trước khi triển khai: Nguyễn Xuân Hoàng Lộc—A Core Gameplay & Economy; Hồ Đức Mạnh—B Board 3D & Visual; Đinh Phương Ly—C UI/UX & Event Cards; Nguyễn Tiến Lộc—D Characters, Skills & AI. Người phụ trách A/B được người dùng đổi ngày 2026-11-14 (F02 trong DECISIONS); phạm vi từng gói giữ nguyên. Không tự sửa module/Scene người khác khi chưa được phân công; shared contract phải được các bên liên quan thống nhất.
- Không lấy luật Monopoly truyền thống, suy đoán, hay code cũ làm lý do thay đổi luật đã chốt. **Không tự chỉnh tài liệu luật để hợp thức hóa code.** Nếu thấy mâu thuẫn/thiếu quy định: ghi rõ vấn đề và xin quyết định người dùng **trước khi triển khai nhánh hành vi chưa xác định**; phần khác vẫn được triển khai bình thường.
- Với yêu cầu code cụ thể: thực hiện ĐÚNG PHẠM VI yêu cầu; không tự xây nguyên game, tự thêm hệ thống hoặc refactor lan rộng.

## 1. DỰ ÁN VÀ PHẠM VI CỐ ĐỊNH

- Unity **6.6 — 6000.6.4f1**, C#, **Universal Render Pipeline (URP)**, PC/Windows là môi trường chính. Kiểm tra `ProjectSettings/ProjectVersion.txt` trước khi thay đổi điều kiện tương thích. Không tự chuyển sang Built-in/HDRP.
- Board game chiến thuật theo lượt, **bàn cờ 3D Low Poly**, camera nhìn chéo **cố định**, **UI 2D điều khiển bằng chuột/nút**.
- Tối đa **4 người**, gồm AI và/hoặc nhiều người lần lượt trên **cùng một thiết bị**. **KHÔNG** multiplayer online, lobby/networking, tài khoản, cloud, save/load giữa trận.
- Thời gian trận do người chơi chọn: **30/45/60 phút hoặc vô hạn**. **32 ô, 20 thành phố, 4 bãi biển, 8 ô chức năng; 6 nghề nghiệp, 14 thẻ sự kiện**.
- **Không** thêm skin, hệ thống nhiệm vụ tuần, cửa hàng, tiền xu, nạp tiền, cấp nhân vật, cây kỹ năng, giao dịch BĐS thỏa thuận trực tiếp hay tính năng khác ngoài báo cáo.
- Hoàn thiện game **không chỉ là viết C#**: còn tạo data asset, Scene, Prefab, UI, audio/VFX, kết nối Inspector, trải nghiệm chơi, kiểm thử và build. Không tuyên bố tính năng hoạt động nếu mới tạo script mà chưa nối vào Unity.

## 2. LUẬT BẤT BIẾN — KIỂM TRA TRƯỚC KHI LẬP TRÌNH

- Người chơi bắt đầu **300.000**; qua/dừng ô xuất phát đúng luật nhận **100.000**; hai xúc xắc 6 mặt; **đôi được tung tiếp không giới hạn trong CÙNG lượt riêng**, KHÔNG tính lượt riêng mới cho cooldown, duration hoặc vòng chung. Ngoại lệ đã duyệt: vào tù hủy quyền gieo thêm và kết thúc lượt; hết giờ không mở lần gieo mới.
- 20 TP dùng giá **Nhà 3 tham chiếu 100.000–800.000** theo từng TP; cấp **Chưa sở hữu (riêng) → Cọc (40%) → Nhà 1 (60%) → Nhà 2 (80%) → Nhà 3 (100%) → Khách sạn (120%)**. Mua lần đầu tối đa Nhà 3; muốn lên Khách sạn theo luật quay lại ô (trừ kỹ năng Kỹ Sư).
- Thuê gốc TP = `5% × Giá Nhà 3 × hệ số theo cấp [1.5, 2.5, 4, 6, 8]`. Tiền thuê cộng các % tăng/giảm **đại số trên thuê gốc**, sau đó mới áp dụng nhân đôi, cuối cùng làm tròn xuống.
- Độc quyền trọn một nước: +20% thuê TP của nước; **độc quyền trọn 3 nước bất kỳ thắng ngay**. Chọn ngẫu nhiên **3 thành phố lễ hội cố định +50% thuê** đầu trận. Lễ hội tạm phí 200.000: +50% thuê **một TP của mình trong 2 lượt riêng** (không phải biển; không chồng nhiều lễ hội tạm trên cùng TP).
- Biển tại **4/14/18/25**, giá **100k/130k/160k/190k**, thuê trên MỖI biển theo số biển cùng chủ: **1→50k, 2→100k, 3→150k; 4→thắng ngay**. Không nâng cấp, không cưỡng chế, không bị thẻ phá hoại.
- Cưỡng chế thường: dẫm TP đối thủ Cọc–Nhà3, **trả thuê trước**, được chọn mua **200% giá niêm yết hiện tại**; Nhà Đầu Tư Ultimate mua từ xa **150%**, không trả thuê. Cấm cưỡng chế Khách sạn/biển.
- Vốn đầu tư là **tiền THỰC đã chi**, theo bậc; giảm giá/free upgrade phải phản ánh đúng vốn. Bán ngân hàng = **50% vốn thực còn lại**, Động đất hạ 1 bậc Nhà 1–3 và mất vốn bậc ấy, thẻ Phá tài sản hoàn **100% vốn còn lại** rồi trả TP về ngân hàng.
- Thuế ô **30** và thẻ Nộp phạt thuế = **20% (tiền mặt + tổng giá niêm yết hiện tại BĐS)** tại lúc phát sinh, không tính lại khi thanh lý. Passive Luật Sư giảm **25% chỉ thuế từ thẻ**, không giảm thuế ô 30.
- Thiếu tiền cho nghĩa vụ **bắt buộc** phải cho thanh lý rồi mới phá sản; tài sản còn lại (kể cả biển/khách sạn) về ngân hàng. Mua, nâng, lễ hội, dịch chuyển là phí tự chọn chỉ khi đủ tiền.
- Nhà tù ô **8**: dừng vào bị giam, đi ngang không. **Tối đa 3 lượt riêng ở tù; lượt bị bắt không tính**. Mỗi lượt chọn tung thử / trả 200k / thẻ Ra tù. Lượt tù thứ 3 tung không đôi: bắt buộc trả 200k, được thanh lý; vẫn thiếu thì phá sản. Trả thành công thì thả cuối lượt, **không di chuyển thêm**, lượt riêng kế tiếp mới đi. Thoát sớm hoặc tung đôi ra tù vẫn theo luật thường; vẫn thu thuê khi bị giam.
- Q04: kỹ năng chủ động và bán tự nguyện ở điểm dừng hợp lệ trước gieo hoặc sau xử lý xong ô; không giữa animation/giao dịch. Lawyer Active phản ứng trước tiền thuê của chính mình; thanh lý bắt buộc được mở cả ngoài lượt. Strategist Ultimate thay một lần gieo đang được phép, không tạo đôi.
- Q05: Ultimate Doanh Nhân có hiệu lực ngay, phủ lượt đối thủ và **hết ngay trước lượt riêng tiếp theo của người dùng**, không kéo dài vì đôi.
- Q06: áp hiệu ứng có sẵn trước; nghĩa vụ bằng 0 không tiêu thêm tài nguyên; nghĩa vụ dương chỉ chọn tối đa **một** biện pháp miễn/giảm chủ động.
- Q09: nghĩa vụ đến hạn → thanh lý/phá sản nếu cần → thắng tức thời → cập nhật mốc lượt/cooldown/effect → lãi Banker nếu hết vòng → chuyển lượt hoặc kết quả hết giờ. Dừng ngay nếu đã thắng tức thời.
- Q13: tiền dùng số nguyên; floor thanh toán cuối cho nghiệp vụ chưa có quy tắc riêng; phân bổ vốn theo phần lẻ lớn nhất, phá hòa bằng thứ tự bậc cố định, bảo toàn tổng vốn thực. Thứ tự phá hòa cụ thể còn P01 trong DECISIONS.
- Q20: hết giờ phải hoàn tất **toàn bộ lần dừng và chuỗi phát sinh trực tiếp**, gồm popup mua/bỏ qua/cưỡng chế/dịch chuyển/lễ hội; không gieo mới hoặc mở kỹ năng chủ động không liên quan. Popup chờ quyết định pause cho cả Human/AI; sau đó xử lý Q09 rồi xếp hạng. Ô **24** dịch chuyển 100k; ô **16** lễ hội 200k; giữ chống lặp dịch chuyển.
- Chi tiết **từng ô, từng giá, đủ 14 thẻ, 6 nhân vật (Passive/Active/Ultimate/Weakness, cooldown), thời hạn hiệu ứng, điều kiện thắng** xem `docs/GAME_RULES.md`. Không hardcode số theo trí nhớ hay dựa trên ví dụ ở phần này.

## 3. CẤU TRÚC VÀ RANH GIỚI TRÁCH NHIỆM

```text
Assets/_Game/
  Scenes/                 # MainMenu.unity, Gameplay.unity
  Scripts/
    Core/                 # Trận, trạng thái, lượt, vòng, thời gian, xử lý thắng
    Board/                # 32 ô, xúc xắc, di chuyển, dừng ô
    Properties/           # Giá, sở hữu, đầu tư, nâng cấp, thuê, bán, thanh lý
    Characters/           # Người chơi, nghề nghiệp, trạng thái
    Skills/               # Passive/Active/Ultimate, cooldown, duration
    Cards/                # 14 thẻ và trạng thái thẻ giữ
    AI/                   # Quyết định của máy qua cùng gameplay API
    UI/                   # View/controller Unity UI, popup
  Prefabs/Board|Characters|Buildings|UI/
  Materials/ Data/Board|Properties|Characters|Skills|Cards/
  Audio/ Art/Icons|Sprites/
Assets/ThirdParty/
  Kenney/                 # 14 gói nguồn đã import
  Fonts/BeVietnamPro/     # font + OFL
Packages/ ProjectSettings/
```

- **`Assets/ThirdParty/` là nguồn tài nguyên:** không đổi tên, di chuyển, xóa, chỉnh trực tiếp model/material gốc khi không được yêu cầu. Tạo Prefab/Material do nhóm tùy biến dưới `Assets/_Game`.
- Unity `.meta` và GUID là quan trọng; **KHÔNG tự tạo/đổi GUID, xóa `.meta`, sao chép `.meta` của asset khác**. Nếu cần thêm/move tài nguyên, ưu tiên thao tác qua Unity Editor; với script ngoài Unity phải đảm bảo GUID ổn định và để Unity tạo `.meta` mới một lần.
- Chỉ sử dụng những package đã có trừ khi được đồng ý thêm dependency. Không mặc định tích hợp DOTween, Cinemachine, Addressables, networking, DI framework hoặc thư viện khác.
- Scene đã cấu hình: `MainMenu` **build index 0**, `Gameplay` **build index 1**; `SampleScene` tắt. Không thay đổi tùy ý.
- Tài nguyên Kenney theo giấy phép **CC0**, font Be Vietnam Pro theo **OFL**; giữ file giấy phép. FBX đã được chọn cho model 3D; không nhập thêm OBJ/GLB trùng model. URP đã kiểm tra sơ bộ CityCommercial; không suy luận tất cả model đã được thử.

## 4. HƯỚNG DẪN KIẾN TRÚC & C#

1. **Tách model luật chơi (plain C#) khỏi MonoBehaviour/Unity UI** để kiểm thử được các công thức, điều kiện và chuyển trạng thái. Không làm mọi thứ trong một `GameManager` khổng lồ; không overengineer thành hàng chục service/abstraction khi chưa cần.
2. **Một nguồn sự thật cho MatchState:** tiền, chủ đất, cấp công trình, sổ vốn theo bậc, vị trí, tù, thẻ, cooldown, hiệu ứng, thời gian, trạng thái thắng. UI/AI chỉ đọc trạng thái và phát lệnh hợp lệ; tránh hai nguồn trạng thái song song.
3. Logic xử lý hành động phải có **kiểm tra điều kiện → tính kết quả → áp dụng một lần → thông báo UI**, tránh nhân đôi tiền, kích hoạt thẻ khi bất hợp lệ, và trigger lặp ngoài ý muốn. Ưu tiên các thao tác nhất quán/atomic tại một ranh giới rõ ràng.
4. Ưu tiên `enum`/ID ổn định cho loại ô, cấp nhà, card, nghề nghiệp, trạng thái; **ID ô 0–31 là cố định**, không dùng chuỗi tên hiển thị làm ID. TP17 không có trong mã hiển thị của báo cáo nhưng dữ liệu nội bộ phải có đủ 20 TP.
5. Tiền lưu bằng **kiểu số nguyên** (ví dụ `long`) để tránh sai số float. Với %, nhân hệ số bằng số nguyên/hệ phân số và **làm tròn xuống ở kết quả cuối theo tài liệu**. Không trừ tiền trước khi xác nhận hành động hợp lệ.
6. Dữ liệu cố định (32 ô, 20 TP giá, 14 thẻ, 6 role) đặt tập trung, có thể dùng ScriptableObject hoặc dữ liệu C# đơn giản nếu hợp lý. **Không tạo hai bản cấu hình luật cạnh tranh nhau**. Runtime mutable state tách khỏi static config.
7. AI phải đi qua **cùng rules engine/action validation** với người chơi, không bypass giá tiền, cooldown hay điều kiện thẻ. AI dùng heuristics đủ cho đồ án, không thêm ML.
8. `MonoBehaviour` chỉ điều phối Unity lifecycle, binding, input và presentation; không giấu công thức tiền trong `Update()` hoặc các nút UI. Dùng event/callback hợp lý, tránh gọi `FindObjectOfType` liên tục mỗi frame.
9. Khi thay đổi Scene/Prefab/Material yêu cầu xác nhận trực quan trong Unity, **không tự khẳng định build/Console sạch** nếu chưa chạy Unity. Khi tạo script cần nói rõ GameObject nào phải attach và dữ liệu Inspector nào phải gán.
10. Đặt tên C# rõ nghĩa: lớp/phương thức public `PascalCase`, field private `_camelCase`, hằng `PascalCase`, một type chính/file khi phù hợp. Chú thích cho **luật khó**, không spam comment hiển nhiên. Giao diện người dùng hiển thị tiếng Việt có dấu với Be Vietnam Pro khi khả thi.
11. **Gói A/Core là nơi duy nhất được trực tiếp ghi trạng thái gameplay có thẩm quyền.** B chỉ trình bày; C định nghĩa thẻ/UI và gửi ý định; D định nghĩa nghề/kỹ năng/AI và gửi ý định. Core không viết lại logic thẻ/kỹ năng/AI của C/D. Quyền sở hữu gói đã chốt; tên API/lớp/DTO cụ thể vẫn là đề xuất cần thống nhất trước tích hợp.
12. Mục OPEN/PENDING trong DECISIONS không được agent tự chọn hành vi để làm test xanh. Chỉ chặn nhánh còn thiếu; không trì hoãn công việc độc lập và không hỏi lại luật đã được nguồn ưu tiên quy định.

## 5. TEST & DEFINITION OF DONE

- Trước khi code: kiểm tra file hiện có, logic liên quan và quy tắc trong `docs/GAME_RULES.md`; tránh tạo các class duplicate.
- Khi code logic, thêm **test cho trường hợp biên** (nếu test framework sẵn có; không tự cài package nếu chưa đồng ý). Ít nhất kiểm tra các ca liên quan đến thay đổi: qua ô 0, ra đôi liên tiếp, nhà tù; tiền mua/nâng/thuê; 3 nước/4 biển; tự bán/động đất/phá tài sản; thuế cố định; cooldown; Ultimate một lần; AI không phá luật.
- Một test không được thay cho việc xác nhận thực tế trong Unity: kết luận rõ **đã kiểm bằng static inspection / test chạy được / Unity Editor / build**, không gộp làm một.
- Hoàn thành một task: báo cáo file tạo/sửa, quy tắc tương ứng, bước kết nối trong Inspector/Scene, test đã chạy, phần **chưa kiểm chứng**, và rủi ro/hành động thủ công.
- **Không “sửa” lỗi bằng cách bỏ qua cảnh báo, tắt test, sửa số luật cho vừa test, hoặc xóa model/Scene**.

## 6. GIT & AN TOÀN LÀM VIỆC NHÓM

- Repository Git `BTL-Game`; nhánh tích hợp hiện tại **`develop`** (không mặc định tên `dev`), nhánh ổn định `main`; tính năng từ `feature/*` → PR → `develop`; bản ổn định từ `develop` → PR → `main`.
- **Không tự `git add`, `commit`, `push`, `merge`, `rebase`, `reset --hard`, xóa nhánh hoặc force-push** nếu user chỉ yêu cầu viết/sửa code. Cần yêu cầu rõ ràng mới thực hiện; không ghi đè thay đổi người khác.
- `.gitignore` loại trừ `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.sln`, `*.csproj`. **Không commit** nội dung Unity tự sinh.
- `.gitattributes` dùng Git LFS cho `*.fbx`, `*.obj`, `*.blend`, `*.psd`, `*.tga`, `*.wav`, `*.ogg`, `*.glb`. Bắt buộc đi cùng commit asset; **đừng phá LFS tracking**.
- Mỗi thành viên dùng Unity đúng phiên bản đã chốt, Git LFS và commit file `.meta` cùng asset. Hạn chế hai người sửa cùng Scene/Prefab/asset, tránh merge conflict.
- Không gửi API keys/token, file riêng tư, thông tin tài khoản vào repo; không tự mở mạng, tải bổ sung asset hoặc chạy lệnh phá hoại.

## 7. HÀNH VI MỖI LẦN ĐƯỢC GIAO VIỆC

1. Tóm tắt yêu cầu, chỉ ra file và quy tắc liên quan; đọc `docs/GAME_RULES.md` phù hợp.
2. Nếu có lỗ hổng trong đề bài/lệch tài liệu, báo rõ **[Chắc chắn] / [Có khả năng cao] / [Chưa được quy định]**; không đoán ngầm.
3. Thay đổi tối thiểu và có thể kiểm tra, theo kiến trúc hiện tại; không tự tạo tính năng ngoài scope.
4. Kiểm tra compile/test có thể chạy; không được nói "đã chạy Unity" nếu chưa thực sự chạy.
5. Báo cáo ngắn gọn kết quả và bước xác minh thủ công nếu cần; dừng đúng phạm vi được giao.

## Code Review Guidelines

### Review Language

- All code review comments MUST be written in Vietnamese.
- Explain bugs, risks, impacts, and suggested fixes in Vietnamese.
- Keep class names, method names, variable names, file paths, and code snippets in their original language.
- Use clear, concise Vietnamese suitable for a university Unity/C# development team.

### Review Priorities

- Check gameplay logic against docs/GAME_RULES.md and docs/DECISIONS.md.
- Check architecture against docs/ARCHITECTURE.md and docs/DATA_MODEL.md.
- Pay special attention to turn progression, jail rules, money calculations, bankruptcy, character skills, and AI decisions.
- Report only actionable issues supported by code evidence.
- Identify severity: P0, P1, P2, or P3.
- For each issue, explain why it is wrong, a scenario that reproduces it, and how to fix it.
- Recommend regression tests for gameplay bugs.
- Do not claim tests passed unless they were executed.
