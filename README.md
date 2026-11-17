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

Cập nhật ngày **2026-11-17**. A/D hiện có domain, gateway hành động, catalog nghề, kỹ năng và AI. Đây chưa phải xác nhận toàn bộ nhiệm vụ A/D hoặc trận chơi trong Scene đã hoàn tất; B/C vẫn cần rà soát và thống nhất API trước tích hợp.

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

Dùng `MatchFactory.Create(roster, duration)`: mặc định `SetupDecisions.Approved` là2–4 người, được trùng nghề, người đầu ngẫu nhiên rồi thứ tự ghế cố định; `GameplayDecisions.Approved` chứa các luật bổ sung đã chốt. Factory chọn ba TP lễ hội cố định khác nhau, tạo state và nhận nguồn xúc xắc/provider thẻ khi truyền vào. Cấu hình rỗng/tùy chỉnh phục vụ fixture/kiểm tra guard, không dùng để mở lại câu hỏi đã duyệt hoặc đổi luật trận sản phẩm. Phần A/D không còn chờ câu trả lời cũ; Q02 production deck vẫn thuộc C và chưa chốt.

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

**Chưa có production deck/provider C trong A/D.** Thiếu provider thì đến ô thẻ phải giữ trạng thái bị chặn và báo nguyên nhân, không bỏ ô hoặc lấy deck test làm luật. Q02 về phân phối/rút lại vẫn chưa được phê duyệt.

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

## File Unity và Git

Giữ source, test, `.asmdef`, các `.meta` do Unity sinh, `Assets`, `Packages`, `ProjectSettings`, `.gitattributes`, `.gitignore`, `.vsconfig` và giấy phép tài nguyên. Commit `.meta` cùng asset tương ứng; không tạo GUID thủ công.

`Library`, `Temp`, `Logs`, `UserSettings`, build/cache, `.sln` và `.csproj` tự sinh được bỏ qua theo `.gitignore`. Không cần xóa chúng chỉ để chuẩn bị commit; kiểm tra Git status và nội dung staged trước khi đưa lên repository.
