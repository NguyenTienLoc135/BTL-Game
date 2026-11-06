# Phân công chính thức và kế hoạch tích hợp

Cập nhật: 2026-11-06. **Tên thành viên và phạm vi gói A/B/C/D đã được người dùng phê duyệt** trong prompt cập nhật tài liệu/phân công. Kế hoạch triển khai, tên lớp/API và chi tiết hợp đồng bên dưới là đề xuất để nhóm thống nhất, không phải bằng chứng đã lập trình hoặc chạy test.

Nguồn: [AGENTS](../AGENTS.md), [GAME_RULES](GAME_RULES.md), [DECISIONS](DECISIONS.md), [ARCHITECTURE](ARCHITECTURE.md), [DATA_MODEL](DATA_MODEL.md), [TEST_CASES](TEST_CASES.md) và [Word gốc](references/GAME_DESIGN_DOCUMENT.docx). Gameplay ưu tiên quyết định mới đã phê duyệt → Word cho phần không đổi → GAME_RULES → tài liệu kỹ thuật.

Phạm vi: Unity6.6/6000.6.4f1, URP, C#, bàn3D low-poly/camera cố định, UI2D, tối đa4 người cùng máy hoặc AI. Không online, Save/Load trận, shop hoặc hệ thống ngoài đặc tả.

## 1. Quyền sở hữu và nguyên tắc chung

| Thành viên | Gói chính thức | Quyền sở hữu chính |
|---|---|---|
| **Hồ Đức Mạnh** | **A — Core Gameplay & Economy** | GameState, điều phối/validator, Board logic, Properties/Economy, clock/thắng/nghĩa vụ |
| **Nguyễn Xuân Hoàng Lộc** | **B — Board 3D & Visual** | Board/nhân vật/công trình presentation, camera/URP/animation, **Scene Gameplay** |
| **Đinh Phương Ly** | **C — UI/UX & Event Cards** | UI/HUD/popup,14 thẻ, UI prefab, **Scene MainMenu** |
| **Nguyễn Tiến Lộc** | **D — Characters, Skills & AI** | Catalog6 nghề, kỹ năng/điểm yếu/cooldown policy, AI và test chuyên môn |

**Core là nơi duy nhất trực tiếp ghi trạng thái gameplay có thẩm quyền**, kể cả tiền, chủ đất, vị trí, thẻ giữ, cooldown, effect, khoản vay và tù. C/D định nghĩa hành vi chuyên môn và gửi intent/modifier qua hợp đồng; A thực thi/kiểm tra/commit, không viết lại luật14 thẻ hoặc6 nghề thay C/D.

Không cần đợi thành viên A hoàn thành toàn bộ Core rồi B/C/D mới làm. B/C/D có thể làm catalog, mock, prefab, UI và test độc lập ngay; chỉ cần thống nhất hợp đồng trước các phần tích hợp phụ thuộc. Dữ liệu giả lập không trở thành nguồn trạng thái trận thứ hai.

## 2. Hồ Đức Mạnh — Gói A: Core Gameplay & Economy

### Nhiệm vụ chi tiết

- Thiết kế GameState duy nhất, điều phối trận và kiểm tra lệnh hợp lệ; thống nhất DTO/state dùng chung với B/C/D.
- Lượt riêng, vòng chung, quyền gieo thường/thêm, kết quả hai xúc xắc, đôi và ngoại lệ vào tù.
- Board logic32 ô: vị trí, đường đi, qua ô0, xử lý điều phối ô; không phụ thuộc animation để tính tiền/chuyển lượt.
- Chủ sở hữu, mua đất/biển, cấp công trình, một quyết định nâng thường/lần dừng, Hotel, mua cưỡng chế và chuyển chủ.
- Vốn thực theo bậc; phân bổ đồng dư theo Q13, bán ngân hàng, mất vốn do hạ cấp và xử lý hoàn vốn.
- Thuê, thuế, nghĩa vụ, Q06 một biện pháp chủ động, thanh lý/phá sản; không tạo hệ thanh toán riêng cho từng thẻ.
- Đồng hồ, popup pause Human/AI, chuỗi sau hết giờ Q20; mốc cuối lượt Q09 và điều kiện thắng.
- Tù3 lượt: đếm riêng, lượt bắt không tính, phí bắt buộc sau lần thử thất bại thứ3, thả cuối lượt không đi thêm.
- Unit test kinh tế, nghĩa vụ và vòng đời trận.

### Bàn giao và điều kiện chạy

| Sản phẩm phải bàn giao | Chức năng phải kiểm chứng |
|---|---|
| GameState và catalog Board/Properties đã thống nhất | Đủ32 ô/24 tài sản, một nguồn state; đúng ID/giá |
| Bộ điều phối, validator và hợp đồng action/result/event | Cùng lệnh Human/AI cho cùng kết quả; lệnh lỗi không trừ tiền/tiêu tài nguyên |
| Bộ tính toán kinh tế và sổ vốn | Thuê/thuế/mua/nâng/cưỡng chế/bán/hoàn vốn đúng công thức và không mất đồng |
| Lifecycle lượt/vòng/clock/tù/thắng | Q04/05/06/09/11/13/20, JAIL-03; callback không áp dụng hai lần |
| Test domain và hướng dẫn tích hợp | Có lệnh chạy/test result thực tế khi triển khai; hiện mới là đặc tả |

Test chính: L01–L44, L46–L68 và các ca D04/D09/D11/D13/D20, F/J tương ứng trong TEST_CASES; C/D cung cấp fixture tác động thẻ/kỹ năng. Những ca PENDING chỉ kiểm tra phần đã rõ, không tự điền oracle.

**Cần nhận:** C cung cấp định nghĩa/intent14 thẻ, D cung cấp6 bộ kỹ năng/modifier/AI command, B cung cấp mapping tileId→view và callback. Có thể nhận fake intent tại test trước khi module thật xong.

**Không triển khai trùng:** không dựng lại bộ thẻ,6 nghề hoặc thuật toán lựa chọn AI; không phụ trách mỹ thuật/animation thay B hoặc dựng HUD thay C.

**Mở PR tích hợp khi:** shared contract được bên dùng xác nhận; test phần thay đổi đã chạy hoặc ghi rõ phần chưa chạy; không ghi state từ view; không chạm Scene B/C ngoài phạm vi được giao; các nhánh còn thiếu luật được chỉ rõ, không âm thầm mặc định.

## 3. Nguyễn Xuân Hoàng Lộc — Gói B: Board 3D & Visual

### Nhiệm vụ chi tiết

- Dựng đủ32 ô đúng thứ tự Word/GAME_RULES:20 TP,4 biển,8 chức năng; bố trí đúng quốc gia và ID.
- Dùng model Kenney hiện có, tạo prefab bàn cờ/ô/công trình, thể hiện Cọc/Nhà1/Nhà2/Nhà3/Hotel.
- Quân đại diện tối đa4 người; camera nhìn chéo cố định, ánh sáng/material URP.
- Animation di chuyển/hiệu ứng hình ảnh, highlight ô và hiển thị chủ sở hữu.
- Phụ trách chính Scene Gameplay và visual prefab; nhận UI prefab C để gắn vào Scene theo hợp đồng.
- Test trực quan đường đi, qua góc bàn, thay đổi công trình, chủ đất và kết thúc animation.

### Bàn giao và điều kiện chạy

| Sản phẩm phải bàn giao | Chức năng phải kiểm chứng |
|---|---|
| BoardView và mapping32 tileId | Không thiếu/trùng ô, đi31→0 đúng vị trí |
| Prefab ô/công trình/quân | Đủ5 cấp TP; biển không có cấp nhà; tối đa4 quân phân biệt |
| Camera/ánh sáng/material/visual effect | Kiểm tra thực tế trong Unity URP, không chỉ khẳng định từ định dạng FBX |
| Animation adapter và Gameplay | Chạy đường đi được Core cấp; callback đúng actionId/landingId, không gọi trả tiền |
| Biên bản kiểm tra visual/Inspector | Ghi những reference đã gán và những visual còn thiếu |

**Cần nhận:** A cấp vị trí/path/chủ/cấp, snapshot/event và actionId; C cấp HUD/popup prefab và binding; D cấp roleId/mô tả để ánh xạ visual, không cấp luật di chuyển.

**Làm ngay bằng mock:** snapshot vị trí/chủ/cấp, đường đi mẫu và mô phỏng callback. Không cần Core hoàn chỉnh.

**Không triển khai trùng:** không gieo RNG xác định kết quả thật, không tính tiền thuê/giá, không đổi tiền/chủ/position logic. Transform và OnTriggerEnter không là nguồn luật. Không sửa model nguồn ThirdParty nếu chưa có nhiệm vụ riêng.

**Mở PR tích hợp khi:**32 mapping đúng, prefab/reference không mất, visual đã chạy trong Unity và callback chỉ phản ánh lệnh Core; commit .meta cùng asset trong công việc triển khai sau này; không tự sửa Scene MainMenu.

## 4. Đinh Phương Ly — Gói C: UI/UX & Event Cards

### Nhiệm vụ chi tiết

- Main Menu; chọn AI/cùng máy, số người, nghề, thời gian; hướng dẫn luật và cài đặt âm thanh.
- HUD tiền, lượt, hai xúc xắc, tài sản, clock, cooldown/effect/thẻ.
- Popup mua/bỏ qua/nâng/bán, thuê/thuế, thanh lý/phá sản, tù, thẻ/chọn mục tiêu, kết quả.
- UI tù thể hiện lượt tù1/2/3, phí bắt buộc lượt3 và nhánh thả cuối lượt không di chuyển.
- Định nghĩa dữ liệu và hành vi đủ14 thẻ; thẻ giữ, rút trùng, bảo vệ tự dùng và ý định tác động.
- UI prefab và Scene MainMenu; phối hợp B để đưa UI vào Gameplay.
- Kiểm thử UI và tình huống thẻ, gồm Q06 và popup Q20 cho Human/AI.

### Bàn giao và điều kiện chạy

| Sản phẩm phải bàn giao | Chức năng phải kiểm chứng |
|---|---|
| MainMenu/setup/hướng dẫn/audio UI | Không có nút online/Save/Load/shop; lựa chọn chưa có luật không tự bật theo mặc định |
| HUD/popup prefab và binding | Đọc snapshot/quote từ Core; hiển thị đúng tiền trước/sau, chỉ lựa chọn hợp lệ |
| CardCatalog và xử lý hành vi thẻ | Đủ14 loại,4 loại giữ tối đa1/loại; tác động qua intent/Core |
| Popup quyết định và thanh lý | decisionId/revision đúng, không submit hai lần; pause clock chung theo Q20 |
| Test thẻ/UI và bàn giao prefab | Test C01–C20; xác minh visual bằng Unity sau khi có implementation |

**Cần nhận:** A cung cấp snapshot, quote, legal options, SubmitDecision/Action và result/event; D cung cấp mô tả nghề/skill, cooldown và thông tin được phép xem; B tích hợp prefab vào Gameplay.

**Làm ngay bằng mock:** wireframe, popup prefabs, MainMenu, mô tả thẻ và fixture. Cơ chế shuffle/rút lại còn Q02 PENDING; không lấy deck giả lập làm luật sản phẩm.

**Không triển khai trùng:** UI/Cards không trực tiếp sửa tiền, chủ hoặc tự tạo hệ thanh toán/thanh lý; không tự tính giá khác Core. Không sửa Gameplay khi B đang sở hữu; gửi prefab/yêu cầu binding hoặc nhận bàn giao rõ ràng trước.

**Mở PR tích hợp khi:** mapping14 CardId đúng, intent qua hợp đồng, UI kiểm tra được bằng mock rồi Core thật, thao tác không hợp lệ bị khóa/giải thích, font tiếng Việt đúng; không ghi mutable state trong SO hoặc HUD.

## 5. Nguyễn Tiến Lộc — Gói D: Characters, Skills & AI

### Nhiệm vụ chi tiết

- Cấu hình đủ Nhà Đầu Tư, Doanh Nhân, Kỹ Sư, Luật Sư, Banker, Nhà Chiến Lược; Passive/Active/Ultimate/Weakness.
- Quy tắc cooldown, thời hạn theo lượt riêng và điều kiện mục tiêu; Q05 hết RentDouble trước lượt kế tiếp.
- Định nghĩa modifier/intent giảm giá, thuê, nâng cấp, miễn nghĩa vụ, khoản vay, cưỡng chế từ xa, gieo lại/chọn ô.
- Kiểm tra chỉ tiêu hao khi thành công; phối hợp A về Q06 một biện pháp/obligation, Q09 deadline/lãi và Q04 quyền gieo bị thay.
- AI chọn mua/nâng/bán/cưỡng chế, dùng skill và mục tiêu; xử lý thẻ, thuế, nợ và tù3 lượt qua legal actions.
- Test tự động kỹ năng/AI; giữ nguyên yếu điểm và dùng validator chung.

### Bàn giao và điều kiện chạy

| Sản phẩm phải bàn giao | Chức năng phải kiểm chứng |
|---|---|
| Role/Skill catalog | Đủ6 nghề và các thông số Word/quyết định mới; không tự thêm role hoặc cấp nhân vật |
| Bộ quy tắc skill/modifier/intent | Đúng target/cost/duration; tiêu hao chỉ khi Core xác nhận thành công |
| Heuristic AI và controller adapter | Dùng QueryLegalActions/SubmitAction như Human, không sửa state hoặc gọi thẳng công thức lách luật |
| Bộ fixture/test Skills/AI | S01–S29, D04/D05/D06, Q09/clock/tù liên quan; AI không đọc thẻ ngoài quyền Strategist |
| Hướng dẫn nối C/A | Mô tả/quote/decision cho UI; intent và deadline policy cho Core |

**Cần nhận:** A cung cấp immutable snapshot, phase/quyền gieo, legal actions, obligation và action result; C cấp CardDefinition/ý định thẻ, cơ chế preview đã thống nhất; B cấp binding visual nếu có hiệu ứng.

**Làm ngay bằng interface/mock đề xuất:** catalog nghề, phép tính modifier/target và heuristic trên legal actions giả lập; chưa gán đó là API chính thức nếu chưa thống nhất.

**Không triển khai trùng:** không có GameState/kinh tế/luật lượt thứ hai; không làm bộ thẻ thay C. A thực thi cập nhật cooldown/effect/nợ vào state tại các mốc, D cung cấp quy tắc chứ không tự đếm lại trên animation.

**Mở PR tích hợp khi:** tất cả thông số và test phần triển khai được đối chiếu; AI và Human cùng validator; Q05 đúng mốc, không cộng dồn/tự đóng mục PENDING; bàn giao hợp đồng cùng A/C trước khi dùng trong Scene.

## 6. A. Công việc có thể làm song song ngay

| Người / gói | Có thể làm ngay | Không cần đợi |
|---|---|---|
| Mạnh — A | Thiết kế Core/GameState, validator, công thức kinh tế và test domain | Toàn bộ visual/UI/AI thật |
| Hoàng Lộc — B | Board32 ô, camera, dùng asset, prefab/animation mẫu với mock | Core hoàn thành toàn bộ |
| Ly — C | MainMenu, UI/popup, dữ liệu14 thẻ, fixture | Scene Gameplay/Core đầy đủ |
| Tiến Lộc — D | Catalog6 nghề, Skills/AI design, fixture và kịch bản test | Board3D hoặc mọi tính năng kinh tế xong |

“Làm ngay” không cho phép tự chốt luật OPEN/PENDING hoặc tạo shared contract khác nhau ở bốn nhánh.

## 7. B. Công việc cần hợp đồng trước khi tích hợp

Các tên DTO dưới đây là đề xuất, không bắt buộc tạo thêm service/interface nếu không cần.

| Luồng bàn giao | Dữ liệu/hợp đồng tối thiểu cần thống nhất | Bên xác nhận |
|---|---|---|
| A → B | tileId/position/path, ownerId, cityLevel, movementId/landingId và gameplay events | A+B |
| B → A | CompletePresentation(actionId), callback một lần; không kèm kết quả kinh tế tự tính | A+B |
| A → C | Snapshot/HUD, Price/RentQuote, PendingDecision/Obligation, lựa chọn hợp lệ, clock/result | A+C |
| C → A | SubmitAction/Decision, CardIntent và mục tiêu/nguồn; expectedRevision nếu hợp đồng chọn | A+C |
| D → A | SkillIntent/modifier, cooldown/expiry/debt policy và target validation | A+D |
| A → D | QueryLegalActions, snapshot chỉ đọc, phase/roll entitlement, kết quả thành công/từ chối | A+D |
| C ↔ D | Mô tả nghề/thẻ, skill/card UI request, preview đúng quyền; AI chọn quyết định thẻ qua Core | A+C+D |
| C → B | HUD/popup prefab và reference/binding cần gắn vào Gameplay | B+C |

Core duy nhất commit; không để Cards/Skills viết trực tiếp các dictionary của GameState. Một tác động thẻ/kỹ năng có thể tạo nghĩa vụ/pending continuation, không nhân đôi settlement ở UI.

## 8. C. Thứ tự triển khai đề xuất

### Giai đoạn0 — Cả nhóm thống nhất trước tích hợp

- GameState và DTO/state dùng chung: tiền/vốn, lượt/vòng, jailTurnsStarted, effect expiry, obligation và rootLandingId.
- Command/action/result/gameplay event; trách nhiệm tính và quyền ghi; rejected action không tiêu hao.
- Chủ sở hữu từng thư mục/script/Scene và cách bàn giao thay đổi.
- Luật APPROVED, các Q/P còn mở; đặc biệt P01 đồng dư và P02 miễn phí tù mới.
- Hợp đồng mock dùng chung và đường chạy test khi triển khai; không cài package tự động.

### Giai đoạn1 — Làm song song

- A xây nền Core/economy với test domain.
- B dựng Board3D, visual, prefab/camera/animation độc lập.
- C dựng UI/MainMenu, dữ liệu/thao tác thẻ trên mock.
- D xây nghề/skill/AI trên hợp đồng hoặc mock đã thống nhất.

### Giai đoạn2 — Tích hợp từng phần

1. Core ↔ BoardView: một đường đi và callback không đổi tiền hai lần.
2. Core ↔ UI: một vòng mua/thuê/nâng/thanh lý hiển thị đúng.
3. Cards ↔ Core:14 loại và nghĩa vụ nhiều người, thẻ giữ/bảo vệ.
4. Skills/AI ↔ Core:6 nghề, cooldown/Ultimate, quyền thông tin và legal actions.
5. Chạy một vòng chơi hoàn chỉnh; thử đôi→tù, tù3, hết giờ trong chuỗi và nợ/lãi cùng mốc.

### Giai đoạn3 — Hoàn thiện

- Chạy kịch bản2–4 người, AI/cùng máy; đây là kế hoạch test, không chốt số người tối thiểu chưa duyệt của Q03.
- Test mọi điều kiện thắng, phá sản, thẻ, kỹ năng và giao nhau giữa module.
- Sửa lỗi tích hợp, visual/UI/hiệu năng, Inspector và build.
- Chỉ báo đã chạy Unity/build/test khi thực sự có kết quả; không coi mock/demo đơn lẻ là game hoàn chỉnh.

## 9. D. Quy tắc chống xung đột Git/Unity

- Nhánh feature/* riêng tạo từ develop; mở PR về develop; không push thẳng main. Tài liệu này không thực hiện lệnh Git.
- B chịu trách nhiệm chính Gameplay; C chịu trách nhiệm chính MainMenu. Tránh hai người cùng sửa một Scene/prefab; trao đổi bàn giao trước.
- Thư mục đề xuất để phân ranh trong code tương lai: Core/Properties và Board/Domain do A; Board/Presentation và prefab3D do B; UI/Cards do C; Characters/Domain, Skills, AI do D. Không tạo/move thư mục trong nhiệm vụ tài liệu này.
- Core.Contracts/shared DTO do A quản lý file nhưng A không tự thay hợp đồng mà chưa trao đổi bên dùng; B/C/D không tự tạo bản DTO thay thế. Catalog Board/Properties do A, Cards do C, Role/Skill do D; mapping visual do B, cần duyệt chung ở giai đoạn0.
- Commit .meta cùng asset trong công việc triển khai được giao; không tự tạo/đổi GUID hoặc chép .meta giữa asset.
- Không commit Library, Temp, Logs, UserSettings hoặc file sinh tự động; giữ .gitignore/.gitattributes và Git LFS hiện có.
- Không để AI coding agent sửa file module người khác nếu chưa được phân công; sửa shared contract phải có phạm vi và các bên liên quan thống nhất.
- Không tự quyết luật PENDING để tránh conflict code; ghi vấn đề trong DECISIONS và chỉ chặn nhánh phụ thuộc.

## 10. E. Cổng hoàn thành và PR tích hợp

Các mục DoD từng gói nằm ở §2–5. Cổng chung:

1. Bàn giao đúng sản phẩm đã nhận; chỉ một nguồn trạng thái và một bộ thanh toán.
2. Ghi luật/test ID áp dụng, kết quả test đã chạy và giới hạn chưa kiểm chứng; không đổi toàn bộ đặc tả thành PASS.
3. Reference/Inspector và người chịu trách nhiệm Scene được ghi rõ khi có thay đổi Unity.
4. Contract thay đổi được bên dùng xác nhận; mock được thay bằng adapter thật khi tuyên bố tích hợp.
5. Không triển khai trùng module, không mở rộng online/Save/Load/shop, không xóa asset để che lỗi.
6. PR mô tả hành vi, cách kiểm chứng và nhánh PENDING còn chặn; đề xuất người review theo hợp đồng liên quan, không tự giả định ai đã duyệt.

File này là phân công/kế hoạch; không tạo script, Scene, prefab hoặc asset và không bắt đầu lập trình trong lượt cập nhật tài liệu.
