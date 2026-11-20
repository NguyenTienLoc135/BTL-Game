# Cờ Tỷ Phú RPG — BTL-Game

Đồ án nhóm 11, phát triển bằng Unity **6.6 (6000.6.4f1)**, C# và URP. Game theo lượt trên cùng thiết bị, **2–4 người**, gồm Human và/hoặc AI; được chọn trùng nghề.

## Bắt đầu làm việc

1. Dùng Git LFS để lấy đầy đủ asset của repository; file FBX/OGG phải là tài nguyên thật, không phải nội dung LFS pointer.
2. Trong Unity Hub, thêm project hiện có bằng thư mục gốc `BTL-Game` và mở đúng phiên bản Editor trong `ProjectSettings/ProjectVersion.txt`.
3. Đọc `AGENTS.md`, luật và phân công trước khi sửa module. Không sửa Scene/Prefab của người khác khi chưa thống nhất.
4. Làm trên nhánh cá nhân; tích hợp bằng Pull Request vào `develop`. Phiên bản đã kiểm thử mới được đưa từ `develop` sang `main`; không force push.

## Tài liệu cần giữ

| Tài liệu | Mục đích |
|---|---|
| [AGENTS.md](AGENTS.md) | Hướng dẫn coding agent, ranh giới module, kiểm thử và an toàn Git |
| [GAME_RULES.md](docs/GAME_RULES.md) | Luật gameplay dùng khi lập trình và review |
| [DECISIONS.md](docs/DECISIONS.md) | Quyết định đã duyệt, khác biệt với Word và câu hỏi còn PENDING |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | Kiến trúc đề xuất, quyền ghi trạng thái và luồng điều phối |
| [DATA_MODEL.md](docs/DATA_MODEL.md) | Hợp đồng dữ liệu tĩnh/runtime và đề xuất DTO |
| [TEST_CASES.md](docs/TEST_CASES.md) | Đặc tả kiểm thử theo luật; không phải báo cáo mọi test đã chạy |
| [TEAM_TASKS.md](docs/TEAM_TASKS.md) | Phân công A/B/C/D, bàn giao và điều kiện tích hợp |
| [GAME_DESIGN_DOCUMENT.docx](docs/references/GAME_DESIGN_DOCUMENT.docx) | Báo cáo Word gốc để đối chiếu |

Ưu tiên gameplay: quyết định mới được người dùng phê duyệt → Word cho phần không bị thay đổi → GAME_RULES → tài liệu kỹ thuật. Tên API/DTO đề xuất chưa tự trở thành hợp đồng được nhóm phê duyệt. Không tự chọn hành vi cho mục OPEN/PENDING.

## Bàn giao implementation A/D

Cập nhật tích hợp ngày **2026-11-19**. A/D từ `develop` đã nối với presentation B/C trên `test-B+C` qua `UnityMatchHost`. Kết quả kiểm chứng mới và cách chạy nằm ở phần tích hợp bên dưới; không coi một lượt smoke test là hoàn tất toàn bộ tiêu chí A/B/C/D.

| Phần | Điểm bắt đầu đọc code |
|---|---|
| Tạo trận và Setup | [MatchFactory.cs](Assets/_Game/Scripts/Core/MatchFactory.cs) |
| Lựa chọn/lệnh/kết quả, nhãn tiếng Việt | [GameActions.cs](Assets/_Game/Scripts/Core/GameActions.cs) |
| Gateway và điều phối | [MatchActions.cs](Assets/_Game/Scripts/Core/MatchActions.cs) |
| Snapshot chỉ đọc | [GameSnapshot.cs](Assets/_Game/Scripts/Core/GameSnapshot.cs) |
| Sáu nghề, mô tả UI | [RoleCatalog.cs](Assets/_Game/Scripts/Characters/RoleCatalog.cs) |
| Modifier/chi phí kỹ năng | [SkillRules.cs](Assets/_Game/Scripts/Skills/SkillRules.cs) |
| Driver AI một hành động/lần | [AiController.cs](Assets/_Game/Scripts/AI/AiController.cs) |
| Hợp đồng module thẻ C | [CardIntegration.cs](Assets/_Game/Scripts/Core/CardIntegration.cs) |

Domain/AI là plain C#, **không attach trực tiếp lên GameObject**. B/C tạo adapter MonoBehaviour, view, HUD và binding Inspector; một `MatchCoordinator` dùng chung cho trận, không tạo nguồn ghi tiền/lượt thứ hai.

### Tạo trận và nguồn luật

Dùng `MatchFactory.Create(roster, duration)`: mặc định `SetupDecisions.Approved` là2–4 người, được trùng nghề, người đầu ngẫu nhiên rồi thứ tự ghế cố định; `GameplayDecisions.Approved` chứa các luật bổ sung đã chốt. Factory chọn ba TP lễ hội cố định khác nhau, tạo state và nhận nguồn xúc xắc/provider thẻ khi truyền vào. Cấu hình rỗng/tùy chỉnh phục vụ fixture/kiểm tra guard, không dùng để mở lại câu hỏi đã duyệt hoặc đổi luật trận sản phẩm. Phần A/D không còn chờ câu trả lời cũ; Q02 được người dùng chốt ngày2026-11-19: rút ngẫu nhiên đều14 loại có hoàn lại; provider và kết nối Unity xem phần tích hợp bên dưới.

Ngày2026-11-17 đã chốt P01 bậc thấp trước; P02 miễn phí tù bắt buộc/thả cuối lượt; Q12 ngoại lệ Hotel cùng lần dừng bằng kỹ năng; Q16 không kích hoạt lại khi coupon còn; Q15 xóa lễ hội đổi chủ; Q03 Setup; Q10 di chuyển tiến/thưởng xuất phát; Q17 giữ effect và đổi hạn còn lại sang vòng mới; Q19 batch thẻ10; Strategist gieo lại thử ra tù; Doanh Nhân chỉ phụ phí mua ngân hàng, cưỡng chế giữ200%. Lễ hội tổ chức nay là một TP toàn trận, cùng TP cộng dồn, không hết sau hai lượt. Xem chi tiết/lịch sử trong GAME_RULES và DECISIONS; không dùng tóm tắt này thay nguồn luật.

### Giao tiếp với B/C

1. Đọc `QuerySnapshot(actorId)` và `QueryLegalActions(actorId)`; dùng `LegalAction.Label` và `Cost` hiển thị, không tính lại giá/modifier ở UI/AI.
2. Gửi `LegalAction.ToCommand()` qua `SubmitAction`/`SubmitDecision`, giữ `ExpectedRevision`, `DecisionId`, mục tiêu và `ChoiceId`. Lệnh cũ bị từ chối thì lấy lựa chọn mới; không sửa token để gửi lại lệnh cũ.
3. Gieo thường: `RollDice` → `AwaitingRollConfirmation` → `ConfirmRoll` hoặc `RerollDice` hợp lệ → `Moving`. B trình bày `ActionResult.Movement`/`PendingMovement`, rồi gọi `CompletePresentation(CurrentActionId)` **một lần**. Core chốt đường đi/kết quả, không lấy từ va chạm vật lý.
4. Core tự mở thuế/thuê khi callback hợp lệ, giữ lựa chọn mua/nâng/cưỡng chế và tự đóng ô khi chuỗi đã hoàn tất. Còn lựa chọn thì gửi legal action hoặc `SkipLanding`; không dùng callback để bỏ nghĩa vụ.
5. Adapter gọi `AdvanceClock(elapsed)`. Popup pause cho Human/AI; hết giờ vẫn giải quyết chuỗi rồi Q09/xếp hạng khi an toàn. Không mở gieo/kỹ năng mới không liên quan.

Snapshot có vị trí, chủ/cấp/vốn, cooldown/effect/nợ, tù, hành động và `WinnerPlayerIds` chỉ đọc. Không dùng `State`, `Transform` hoặc UI làm nguồn ghi khác. **Không trộn method mức thấp cũ** như `RollAndMove`, `CompleteLanding` hay method nhận modifier/giá với gateway; chúng không thay handshake quyết định/animation trên.

`LastDiceRoll`/`LastDicePlayerId` giữ hai mặt xúc xắc đã chốt, kể cả thử tù thất bại không di chuyển. `MovementResult.Source` phân biệt Dice/Teleport/Card/StrategistUltimate để B chọn animation đúng; Ultimate Strategist không có xúc xắc nên `LastDiceRoll` là null, không suy ra một kết quả đôi từ đường đi.

### Kỹ năng và AI

`RoleCatalog` chứa sáu nghề và mô tả Passive/Active/Ultimate/Weakness. Core tự áp dụng passive/điểm yếu vào giá/thuê/thuế/lãi; UI gửi kỹ năng qua legal options, không truyền mức giảm/cooldown tùy ý. Strategist được gieo lại thử ra tù trước khi chốt; chỉ kết quả cuối quyết định thoát/phí.

Tạo `AiController(core, playerId)` và gọi `Step()` theo nhịp adapter. Mỗi lần chỉ đọc snapshot theo actor, chọn một legal action rồi gửi cùng gateway Human. `null` nghĩa là chưa có hành động cho AI đó. Driver không tự callback animation, chạy tới hết trận hoặc bỏ qua ô thẻ bị chặn. Quỹ dự phòng/điểm ưu tiên là tuning, không phải luật.

### Module thẻ C và batch nghĩa vụ

C cung cấp `ICardActionProvider` khi tạo coordinator/factory. `PeekNextCard`, `QueryOptions`, `Plan` phải chỉ đọc snapshot và `CardDrawOrdinal`: hỏi lại cùng trạng thái không rút lại/thay thẻ/tiến cursor. Core xác minh nguồn, mục tiêu, số tiền và quyền dùng trước khi áp dụng `CardEffectPlan`; UI/AI chỉ gửi `CardChoice`/`ChoiceId` đã cấp. Xem trước qua API Core chỉ dành cho Strategist.

**Provider sản phẩm:** `RandomEventCardProvider` theo Q02 duyệt ngày2026-11-19; mỗi lần chọn đều14 loại, cho phép lặp ngay, preview ổn định. Thiếu provider vẫn phải báo chặn, không tự bỏ ô thẻ.

Khi callback đến ô thẻ và provider xác định thẻ10, Core tự tạo batch: tất cả đối thủ còn chơi phát sinh nghĩa vụ cùng lúc; người đủ tiền chuyển100.000 ngay, mọi người thiếu cùng có nghĩa vụ chờ thanh lý. HUD đọc `PendingObligations`; popup/AI từng người dùng `QuerySnapshot(payerId)` và `QueryLegalActions(payerId)`, kể cả ngoài lượt. Không lấy riêng `PendingObligation` từ snapshot mặc định để gán sai người; không chuyển lượt/cập nhật cooldown vì người khác đang trả batch. Người rút chờ các nghĩa vụ được giải quyết hoặc trận kết thúc hợp lệ.

Chưa xác nhận kết nối Scene/Prefab/Inspector, UI thật, production cards, trận hoàn chỉnh hoặc player build. A01–A06 vẫn là đề xuất kỹ thuật; API đã có implementation không tự trở thành quyết định được cả nhóm phê duyệt.

## Kiểm thử

Mở Unity Test Runner, chọn **EditMode**, chạy assembly `BtlGame.Core.Tests`. Ngoài test gốc/regression còn có [MatchActionTests](Assets/_Game/Tests/Editor/MatchActionTests.cs), [AiIntegrationTests](Assets/_Game/Tests/Editor/AiIntegrationTests.cs), [GiftBatchTests](Assets/_Game/Tests/Editor/GiftBatchTests.cs), [StrategistJailTests](Assets/_Game/Tests/Editor/StrategistJailTests.cs), [CardGatewayTests](Assets/_Game/Tests/Editor/CardGatewayTests.cs). `AssemblyInfo.cs` chỉ cho test chuẩn bị fixture internal, không cấp quyền ghi state cho UI/AI.

| Mốc kiểm chứng ngày2026-11-17 | Kết quả | Giới hạn |
|---|---|---|
| Unity6000.6.4f1 EditMode sau luật bổ sung và sửa đóng lượt bắt buộc | **215/215 đạt**,0 failed/0 skipped | Không thay kiểm thử trực quan/Scene hoặc player build |
| .NET domain cùng đợt kiểm chứng cuối | **220/220 đạt** | Gồm215 ca dự án và5 ca tái hiện review cũ trong harness Logs bị Git bỏ qua |
| Unity 6000.6.4f1 EditMode sau khi áp dụng START-200K | **216/216 đạt**, 0 failed, 0 skipped | Kiểm tra thưởng 200.000 khi đi qua hoặc dừng ô 0, phí dịch chuyển 100.000 và chống cộng lặp |

Kết quả chạy mới nhất nằm trong `Logs/AD-Push-EditMode-results.xml` và `Logs/AD-Push-EditMode.log` trên máy chạy. Git bỏ qua thư mục `Logs`. Unity batch kết thúc với mã 0. Log không có lỗi hoặc cảnh báo biên dịch C#. TEST_CASES vẫn là đặc tả kiểm thử.

## Tích hợp A/B/C/D ngày2026-11-19

- Nguồn: A/D đã vào `origin/develop` qua PR15; B/C nằm trên `origin/test-B+C` tại `aa714b2`, bao gồm nhánh `feature/event-popup`. PR17 đưa develop vào nhánh thử B/C, không đưa B/C vào develop. Nhánh `dev-locnxh` là implementation A cũ dùng bộ Contracts khác, không chép đè lên A/D hiện tại.
- Mở `Assets/_Game/Scenes/MainMenu.unity`, nhấn Play. Chọn chế độ, số người2–4, thời gian và nghề cho từng người rồi **TẠO TRẬN**. Chế độ AI: ghế đầu là Human, các ghế còn lại là AI; cùng máy: tất cả là Human. Người đi đầu vẫn ngẫu nhiên theo Core.
- **TUNG XÚC XẮC** mở kết quả để chấp nhận/gieo lại hợp lệ; sau xử lý ô, nút này đổi thành **KẾT THÚC LƯỢT** nếu không còn quyền gieo. **KỸ NĂNG** gồm Active và Ultimate hợp lệ; **TÀI SẢN** mở bán tự nguyện. Popup xử lý mua/nâng/thuê/thuế/tù/thẻ/thanh lý. Danh sách mục tiêu dài có thể cuộn bằng chuột.
- `UnityMatchHost` nằm trên một GameObject riêng trong mỗi Scene. Nó tạo đúng một MatchCoordinator khi vào Gameplay, nối UI với legal commands, AI với cùng gateway, BoardView với callback actionId/landingId. Play thẳng Gameplay quay về MainMenu để cấu hình, không tự chọn roster.
- Q02 được người dùng duyệt: mỗi lần rút ngẫu nhiên đều14 loại, có hoàn lại; thẻ giữ trùng bỏ lá mới, preview Strategist không bốc lại. `RandomEventCardProvider` trả ý định, Core là nơi duy nhất ghi state/tiền/thẻ/effect.
- Unity menu **BTL Game → Connect Team Gameplay** cập nhật binding, hướng dẫn Xuất phát200.000 và lễ hội đã duyệt mà không dựng lại visual. **Build Integrated Windows Player** xuất `Builds/TeamIntegration/BTLGame.exe` (Git bỏ qua). Không cần gán Inspector thủ công thêm sau khi nhận đủ Scene/script/meta của bản tích hợp.
- Font UI tạo từ TTF Be Vietnam Pro hiện có tại `Assets/_Game/Art/Fonts/BeVietnamPro-UI SDF.asset`, SDF padding8; giữ nguyên font và model nguồn. Board bind màu/nhãn khi chạy, camera nhìn chéo cố định; prefab quân/công trình được chỉnh kích thước để nhìn thấy trên ô, không đổi mesh nguồn hoặc vị trí gameplay.
- Kiểm chứng tự động cuối ngày2026-11-19: assembly `BtlGame.Core.Tests` **239/239 đạt**, gồm6 trận AI dùng provider sản phẩm và smoke Play Mode MainMenu→Gameplay→animation→callback, font tiếng Việt và màu ô runtime. Log tại `Logs/TeamIntegration-Tests-Final.xml`/`.log`; build Windows thành công ở `Logs/TeamIntegration-Build-Final.log`.
- Đã mở Windows player, kiểm tra bằng chuột: setup có trùng nghề→tạo trận→gieo/chốt xúc xắc→animation→popup mua; Nhà Đầu Tư mua biển100.000 với Passive10%, tiền300.000→210.000. Đã quan sát font, màu32 ô và quân sau sửa. Đây chưa thay kiểm thử thủ công toàn bộ14 thẻ/6 nghề, bố cục nhiều độ phân giải, âm thanh/VFX hoặc mọi tiêu chí đồ án.
- Player còn báo shader hậu kỳ URP (Gaussian/Bokeh Depth of Field, Panini Projection) không có trong build và TLS từ dịch vụ Unity. Luồng chơi đã kiểm tra vẫn chạy; chưa xác nhận hậu kỳ/Console hoàn toàn sạch, không tắt cảnh báo để che lỗi.

## File cần giữ trong Git

Giữ source, test, `.asmdef`, các `.meta` do Unity sinh, `Assets`, `Packages`, `ProjectSettings`, `.gitattributes`, `.gitignore`, `.vsconfig` và giấy phép tài nguyên. Commit `.meta` cùng asset tương ứng; không tạo GUID thủ công.

`Library`, `Temp`, `Logs`, `UserSettings`, build/cache, `.sln` và `.csproj` tự sinh được bỏ qua theo `.gitignore`. Không cần xóa chúng chỉ để chuẩn bị commit; kiểm tra Git status và nội dung staged trước khi đưa lên repository.
