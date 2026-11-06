# Kiến trúc đề xuất — Cờ Tỷ Phú RPG

Ngày cập nhật: 2026-11-06. **Luật mới và phân công A/B/C/D đã được người dùng phê duyệt; tên lớp/API và phương án kiến trúc cụ thể vẫn là ĐỀ XUẤT**. Chưa có implementation.

## 1. Nguồn và phạm vi

Đã đọc đầy đủ [AGENTS.md](../AGENTS.md), [GAME_RULES.md](GAME_RULES.md), [README.md](../README.md), các tài liệu kỹ thuật và nội dung [Word gốc](references/GAME_DESIGN_DOCUMENT.docx) gồm643 đoạn/7 bảng. Ưu tiên quyết định người dùng mới → Word cho phần không bị thay đổi → GAME_RULES → tài liệu kỹ thuật. Các ngoại lệ mới ghi trong [DECISIONS](DECISIONS.md); không sửa Word.

Tên lớp/DTO và cách nối kỹ thuật liên quan A01–A06 vẫn là đề xuất. Q04=A, Q05=A, Q06=A, Q09=A, Q11=A, Q13=A, Q20=B và tù3 lượt đã APPROVED; các Q/P còn OPEN/PENDING không có mặc định ngầm. Phân công cũ A07 được thay bằng [TEAM_TASKS](TEAM_TASKS.md), không dùng bảng cũ để giao việc.

Phạm vi: Unity 6.6 (6000.6.4f1), URP, C#, PC/Windows; tối đa 4 người cùng thiết bị hoặc AI. Không online, lưu/khôi phục trận, shop, tài khoản hoặc tính năng ngoài GAME_RULES §1. Tài liệu không tạo script, Scene, prefab hay ScriptableObject.

## 2. Tổng thể: một bộ luật, một trạng thái

Đề xuất một MatchCoordinator bằng plain C# điều phối lệnh và chuyển pha. Nó sở hữu duy nhất một **GameState**; đây chính là khái niệm MatchState trong AGENTS, không tạo thêm một bản MatchState song song.

- **Gameplay domain:** dữ liệu, phép tính, điều kiện hợp lệ, chuyển trạng thái; không phụ thuộc MonoBehaviour, Transform, Animator, Canvas hoặc thời gian frame.
- **Unity presentation:** một bộ điều phối lifecycle trong Gameplay, các view bàn cờ/quân/xúc xắc và UI. Chuyển input thành lệnh, phát animation, hiển thị trạng thái đã được domain xác nhận.
- **Cấu hình:** ScriptableObject chỉ chứa dữ liệu tĩnh; khi tạo trận, chuyển thành catalog plain C# chỉ đọc. Không sửa asset cấu hình để lưu trạng thái trận.
- **AI:** chọn lệnh từ cùng danh sách hành động hợp lệ như người thật. Không trực tiếp trừ tiền, đổi ô hoặc sửa chủ đất.
- **MainMenu:** thu thập MatchSetup và mở Gameplay; không giữ bản sao số dư hoặc quyền sở hữu. Không dùng static singleton chứa một trận thứ hai.

Không cần DI container, event bus toàn cục, ECS, event sourcing, repository/database hoặc một service cho mỗi thẻ. Ban đầu dùng lớp/phương thức thông thường, truyền phụ thuộc qua constructor hoặc Inspector tại điểm nối Unity.

## 3. Trách nhiệm các module

Các tên dưới đây là ranh giới thư mục/namespace, chưa bắt buộc tách assembly.

| Module | Sở hữu trách nhiệm | Không được làm |
|---|---|---|
| Core | Hợp đồng chung; GameState; MatchCoordinator; pha lượt, vòng, clock, nghĩa vụ còn dở; commit thay đổi và kiểm tra thắng | Chứa mọi công thức tài sản/thẻ trong một GameManager; làm animation |
| Board | Catalog 32 ô; hai xúc xắc; tính đường đi, qua ô 0, ô đến và ngữ cảnh di chuyển | Suy ra vị trí từ Transform; tự trừ thuê hoặc kết thúc lượt |
| Properties | Giá niêm yết, mua/nâng/cưỡng chế, sổ vốn, thuê, thuế theo tài sản, bán/thanh lý, truy vấn độc quyền | Duy trì danh sách chủ đất khác GameState; gán material Unity |
| Characters | Catalog sáu nghề, dữ liệu người chơi, điều kiện đang chơi/bị giam/phá sản | Giữ số dư độc lập trong component nhân vật; tự giảm cooldown |
| Skills | Điều kiện và kết quả Passive/Active/Ultimate/Weakness; lịch cooldown, hiệu ứng, khoản vay | Bỏ qua validator tài sản; quyết định lượt dựa trên animation |
| Cards | Catalog đúng 14 loại, trình tự rút có thể kiểm thử, thẻ giữ, chọn mục tiêu, kết quả thẻ | Tự chốt shuffle/rút lại chưa được quy định; gọi trực tiếp UI |
| AI | Heuristic chọn một lệnh hợp lệ và mục tiêu; chỉ đọc thông tin được luật cho phép | Đọc trước thẻ nếu không có Passive; gian lận tiền/xúc xắc |
| UI | Input, popup, HUD và binding tiếng Việt; gửi lựa chọn theo yêu cầu domain | Tự tính lại giá bằng công thức khác; ghi trực tiếp runtime state |

**Ranh giới chính thức:** Hồ Đức Mạnh (A) phụ trách Core + Board logic + Economy/Properties và chỉ Core được ghi state; Nguyễn Xuân Hoàng Lộc (B) phụ trách Board/Characters presentation và Scene Gameplay; Đinh Phương Ly (C) phụ trách UI, Cards và Scene MainMenu; Nguyễn Tiến Lộc (D) phụ trách Characters domain, Skills, AI. Tên “Board” không trao logic xúc xắc/ô/vị trí cho B; tên “Characters” không trao model/animation 3D cho D. C/D trả ý định và kết quả luật chuyên môn qua hợp đồng, A kiểm tra và áp dụng; A không viết lại nội dung14 thẻ hoặc6 bộ kỹ năng.

Board/Characters có thể chứa view Unity riêng trong phần Presentation của module; phần Domain không tham chiếu view. Không cần di chuyển thư mục hiện tại để áp dụng tài liệu này.

## 4. Phụ thuộc không vòng tròn

~~~mermaid
flowchart TD
    Presentation["Unity presentation / UI / AI adapter"] --> Coordinator["Core orchestration: MatchCoordinator"]
    Coordinator --> Board
    Coordinator --> Properties
    Coordinator --> Characters
    Coordinator --> Skills
    Coordinator --> Cards
    Board --> Contracts["Core.Contracts: DTO, ID, enum, catalog chỉ đọc"]
    Properties --> Contracts
    Characters --> Contracts
    Skills --> Contracts
    Cards --> Contracts
    Coordinator --> Contracts
~~~

Core.Contracts không tham chiếu Core orchestration hoặc bất kỳ module nào. Các DTO chung, kể cả trạng thái thẻ/kỹ năng và cấu hình đã chuyển đổi, đặt trong lớp hợp đồng này để không tạo chiều ngược Contracts → Cards/Skills.

Các module luật trả kết quả tính toán/ý định, không gọi vòng nhau. Ví dụ Cards trả ý định Động đất; coordinator chuyển ý định cho Properties kiểm tra mục tiêu và tính thay đổi; Skills cung cấp modifier cần thiết theo yêu cầu coordinator. Properties không gọi ngược Cards hay Skills. Kết quả đọc như giá thuê có thể dùng chung DTO do coordinator tập hợp, không cần thêm framework giao tiếp.

Đây là hướng phụ thuộc ở mức mã nguồn, không phải yêu cầu tách chín assembly. Chỉ tách asmdef nếu nhóm thực sự cần và được giao riêng.

## 5. Một nguồn quản lý trạng thái

Chỉ coordinator được áp dụng thay đổi vào GameState. View và AI nhận snapshot/query chỉ đọc; không trả dictionary/list có thể sửa trực tiếp.

Luồng một lệnh:

1. Kiểm tra trận còn chạy, đúng actor/pha, đúng decisionId và state revision nếu lệnh đến từ popup.
2. Kiểm tra mục tiêu, tiền, cooldown, quyền sở hữu và điều kiện từ GAME_RULES.
3. Tính đầy đủ kết quả bằng dữ liệu hiện tại; nghĩa vụ thuế phải chốt số tiền lúc phát sinh.
4. Nếu cần quyết định/thanh lý, lưu PendingAction/PendingObligation và chờ; không trừ một phần tiền tùy tiện.
5. Khi đủ thông tin, áp dụng kết quả **một lần**, cập nhật revision, rồi kiểm tra thắng sau giao dịch/đổi chủ/phá sản.
6. Trả ActionResult và dữ liệu presentation; lệnh bị từ chối không tiêu hao tiền, thẻ, kỹ năng hoặc Ultimate.

Một thao tác logic có thể gồm nhiều bước theo luật, ví dụ **trả thuê → cho chọn cưỡng chế**. Không gộp hai bước thành giao dịch mua trước rồi bù thuê; thất bại ở bước mua không đảo ngược tiền thuê hợp lệ đã trả.

ID hành động và revision chỉ phục vụ chống callback/click lặp trong bộ nhớ, không phải networking hay nhật ký lưu trận.

## 6. Luồng trận và lượt

### 6.1. Khởi tạo

Nhận MatchSetup → kiểm tra catalog và các lựa chọn đã có quyết định → tạo GameState với tiền 300.000/người, ô 0 → chọn đúng 3 TP lễ hội cố định khác nhau → chọn người bắt đầu theo quyết định Q03 → bắt đầu lượt riêng.

Không cấp 100.000 chỉ vì khởi tạo vị trí ô 0; tiền thưởng gắn với một lần di chuyển hợp lệ qua/dừng ô xuất phát (GAME_RULES §1–2).

### 6.2. Chu trình

~~~mermaid
flowchart TD
    Before["Ngay trước lượt mới: hết RentDouble của chủ theo Q05"] --> Start["Bắt đầu lượt riêng"]
    Start --> Jail{"Đang ở tù?"}
    Jail -->|Có| JailChoice["Chọn tung / trả 200.000 / thẻ Ra tù"]
    JailChoice -->|Tung không đôi| Third{"Lượt tù thứ3?"}
    Third -->|Không| End["Hoàn tất lượt riêng"]
    Third -->|Có| Fee["Nghĩa vụ200.000: thanh lý / phá sản nếu cần"]
    Fee --> Paid{"Sau thanh lý trả được?"}
    Paid -->|Có| Release["Trả thành công: thả cuối lượt, không di chuyển"]
    Paid -->|Không| Bankruptcy["Phá sản, thu hồi tài sản"]
    Bankruptcy --> JailWin{"Thắng tức thời?"}
    JailWin -->|Có| Result["Kết thúc trận"]
    JailWin -->|Không| End
    Release --> End
    JailChoice -->|Tung đôi| Move["Di chuyển bằng kết quả đã chốt"]
    JailChoice -->|Trả tiền hoặc dùng thẻ| Roll["Gieo hai xúc xắc"]
    Jail -->|Không| Window["Cửa sổ hành động hợp lệ theo luật đã xác nhận"]
    Window --> Roll
    Roll --> Reroll["Cơ hội gieo lại nếu kỹ năng hợp lệ; chốt kết quả"]
    Reroll --> Move
    Move --> Landing["Xử lý ô đến / thẻ / nghĩa vụ / lựa chọn"]
    Landing --> Win{"Có thắng tức thời?"}
    Win -->|Có| Result["Kết thúc trận"]
    Win -->|Không| Arrest{"Vừa bị giam? Q11"}
    Arrest -->|Có: hủy quyền gieo thêm| End
    Arrest -->|Không| Expired{"Đã hết giờ?"}
    Expired -->|Có: chuỗi ô đã hoàn tất| End
    Expired -->|Không| Actions["Điểm dừng hành động hợp lệ Q04"]
    Actions --> Extra{"Còn quyền gieo theo luật?"}
    Extra -->|Có| Roll
    Extra -->|Không| End
    End --> Boundary["Q09: nghĩa vụ, thanh lý/phá sản, thắng, mốc lượt, lãi vòng"]
    Boundary --> Check["Kiểm tra thắng / hết giờ"]
    Check -->|Chưa kết thúc| Next["Người chưa phá sản kế tiếp"]
    Next --> Before
    Check -->|Kết thúc| Result
~~~

Các chuyển pha kỹ thuật đề xuất: StartTurn, AwaitJailChoice, AwaitPreRollAction, AwaitRollConfirmation, Moving, ResolvingTile, AwaitDecision, ResolvingObligation, EndTurn, Finished. Pha chờ presentation là một khóa kỹ thuật kèm theo, không tạo lượt riêng mới.

Sơ đồ mô tả nhánh chính, không loại bỏ điểm dùng kỹ năng Q04 hợp lệ trước lựa chọn tung trong tù. P02 chỉ chặn nhánh miễn phí bắt buộc mới; sơ đồ phí ở trên là trường hợp không có miễn nghĩa vụ.

- Ra đôi: xử lý xong ô đến rồi mới xét tung thêm; giữ nguyên personalTurnOrdinal, cooldown và thời hạn hiệu ứng. Không có luật ba lần đôi thì vào tù.
- Q11: vừa vào tù do dừngô8 hoặc thẻ Vào tù khi ra đôi thì hủy quyền gieo thêm và kết thúc lượt. Vẫn xử lý Q09; không bỏ nghĩa vụ đến hạn.
- Q04: chủ động dùng kỹ năng/bán ở điểm dừng trước gieo hoặc sau hoàn tất ô, không giữa animation/giao dịch. Lawyer Active phản ứng trước thuê của chính người dùng; thanh lý bắt buộc được mở cả ngoài lượt.
- Strategist Ultimate tiêu thụ một quyền gieo tiếp theo có thật, kể cả quyền thêm do đôi, tạo MovementContext thay vì DiceResult; không để kết quả đôi cũ tạo thêm quyền gieo sau Ultimate. Nếu không có quyền gieo thì không thực hiện.
- Tù: lúc bị bắt đếm0; mỗi lần bắt đầu lượt khi còn bị giam tăng số lượt tù đã bắt đầu. Lần1/2 tung không đôi thì kết thúc còn tù; lần3 không đôi tạo khoản200.000 bắt buộc. Trả thành công thả cuối lượt, không move, không gieo thêm; thiếu sau thanh lý thì phá sản. Thoát bằng đôi/trả chủ động/thẻ trước nhánh cưỡng bức vẫn đi theo luật thường.
- Cooldown N chỉ sẵn sàng sau khi hoàn thành N lượt riêng **tiếp theo** của người dùng; không tính lần kết thúc lượt kích hoạt.
- Trả nợ Banker 110.000 vào cuối lượt riêng thứ 5 tiếp theo; không đếm theo số lần tung.
- Không dùng tổng số lần tung chia số người để suy ra vòng. Cách biểu diễn tập người chờ Q08 còn PENDING, nhưng thứ tự cuối lượt đã chốt Q09:
  1. Giải quyết nghĩa vụ đến hạn; thanh lý/phá sản nếu cần.
  2. Kiểm tra thắng ngay sau thay đổi có thể tạo thắng; dừng nếu trận kết thúc.
  3. Cập nhật mốc lượt riêng, cooldown và effect đến hạn.
  4. Nếu đủ điều kiện hoàn tất vòng, cộng1% số dư dương còn lại cho Banker theo luật.
  5. Chuyển lượt hoặc xử lý kết quả hết giờ.
- RentDouble của Entrepreneur không dùng quy tắc hết hạn cuối lượt tiếp theo: hiệu lực ngay và hết ngay **trước** lượt riêng tiếp theo của chủ (Q05). Các effect N lượt riêng khác giữ luật của chúng.
- Kiểm tra thắng ở các giao dịch trung gian; bước kiểm tra cuối lượt không thay cho kiểm tra tức thời.

## 7. Animation và thao tác bất đồng bộ

Domain xử lý tuần tự trên luồng điều phối chính; không cần nhiều thread cho luật. Hai xúc xắc được RNG chốt trong domain; hình ảnh xúc xắc trình bày kết quả đó, không lấy kết quả từ va chạm vật lý.

Đề xuất mỗi hành động có một ActionContext đang chờ:

1. Board tính đường đi, số lần qua ô 0 và vị trí cuối.
2. Coordinator commit vị trí logic và tiền qua ô 0 một lần, phát MovementPresentation kèm actionId.
3. Khóa input hành động khác; view chạy đường đi đã chốt.
4. Callback đúng actionId chỉ mở bước xử lý ô đến, không cộng tiền hay di chuyển lần nữa. Callback cũ/lặp bị bỏ qua.

Trong lúc di chuyển, vị trí logic có thể đã ở đích còn hình ảnh đang chạy; HUD dùng dữ liệu/presentation context thống nhất. Không cho AI/UI bắt đầu lượt khác hoặc xử lý ô sớm vì nhìn thấy vị trí cuối trong snapshot.

Chỉ cho phép một continuation hợp lệ tiến trình trận. Không dùng OnTriggerEnter của ô để tự cấp tiền/rút thẻ. Khi scene/view đóng, không nhận callback muộn vào một trận khác. Lỗi animation phải được ghi nhận; không tự tung lại hoặc âm thầm bỏ qua hiệu ứng.

## 8. Đồng hồ, popup và hành động đang dở

Clock thuộc GameState; Unity adapter cung cấp elapsed time. Dùng thời gian thực do adapter đo, không dùng tốc độ animation để đếm lượt. Popup bắt buộc được ghi rõ trong PendingDecision và giữ clock tạm dừng cho đến khi được đóng/giải quyết; tránh nhiều popup đóng một cái đã vô tình tiếp tục clock.

Nếu clock về0 giữa animation/nghĩa vụ: đánh dấu TimeExpired và yêu cầu kết thúc, **không hủy lần dừng**. Q20=B cho hoàn tất toàn bộ lần dừng cùng hiệu ứng/giao dịch trực tiếp: mua/bỏ qua, cưỡng chế sau thuê, dịch chuyển/lễ hội và chuỗi ô liên quan. Không gieo mới, kể cả đôi, hoặc mở kỹ năng chủ động không liên quan; phản ứng thanh toán trực tiếp hợp lệ vẫn qua Q06. Sau chuỗi chạy Q09 rồi xếp hạng tiền mặt → giá niêm yết → đồng thắng. Thắng tức thời ở bất kỳ giao dịch hợp lệ nào kết thúc ngay.

Popup chờ quyết định pause cho cả Human/AI, gồm Mua/Bỏ qua dù khoản mua là tự chọn. Cùng DecisionContext được dùng cho hai controller; animation đơn thuần không được biến thành popup để pause tùy tiện. Đề xuất PendingAction lưu rootLandingId và continuation thuộc chuỗi để không mở lệnh không liên quan sau hết giờ; tên DTO cụ thể cần thống nhất ở giai đoạn0.

Q06: khi có nghĩa vụ, áp hiệu lực có sẵn trước, chốt khoản phải trả; bằng0 không cho tiêu thêm thẻ/kỹ năng. Nếu dương, chỉ một biện pháp chủ động được dùng cho obligationId đó. Bảo vệ công trình là luồng tự động riêng khi có đòn hợp lệ; không tạo một cơ chế trả tiền khác ở UI/Cards/AI.

## 9. Hợp đồng giao tiếp tối thiểu

| Hợp đồng đề xuất | Đầu vào | Đầu ra |
|---|---|---|
| QueryState / QueryLegalActions | Actor, trạng thái/pha hiện tại | Snapshot chỉ đọc; hành động/mục tiêu hợp lệ và lý do khóa |
| SubmitAction | actionId, actorId, loại lệnh, mục tiêu/lựa chọn, expectedRevision | Accepted/Rejected/Pending, lý do, thay đổi và presentation cần phát |
| SubmitDecision | decisionId, actorId, lựa chọn | Tiếp tục đúng nghĩa vụ đang chờ; từ chối lựa chọn lỗi thời |
| CompletePresentation | actionId | Tiếp tục một lần; không áp dụng lại kết quả |
| AdvanceClock | Khoảng thời gian đo được | Cập nhật clock nếu không tạm dừng; đánh dấu hết giờ |

Payload cụ thể xem [DATA_MODEL](DATA_MODEL.md). Đây là hợp đồng để thống nhất cách gọi, không phải yêu cầu tạo interface cho mọi phương thức.

## 10. Phối hợp nhóm và kiểm chứng

Phân công chính thức người dùng đã phê duyệt; xem sản phẩm bàn giao/DoD chi tiết ở [TEAM_TASKS](TEAM_TASKS.md):

| Thành viên / gói | Trách nhiệm chính | Điểm phối hợp |
|---|---|---|
| Hồ Đức Mạnh — A | Core Gameplay & Economy, Board logic, GameState, validator, nghĩa vụ/thắng/clock | Tiếp nhận ý định Cards/Skills; không triển khai trùng chuyên môn C/D |
| Nguyễn Xuân Hoàng Lộc — B | Board3D, view quân/công trình, camera/URP, animation, Gameplay | Nhận snapshot/event; callback presentation không ghi state |
| Đinh Phương Ly — C | UI/UX,14 thẻ, MainMenu, HUD/popup và UI prefab | Gửi quyết định/thẻ qua Core; B gắn prefab vào Gameplay |
| Nguyễn Tiến Lộc — D |6 nghề, Skills, cooldown/effect policy, AI | Định nghĩa ý định/modifier; Core áp dụng và đếm mốc; AI chọn lệnh hợp lệ |

Tên API/DTO cụ thể vẫn là đề xuất. Scene Gameplay do B chịu trách nhiệm chính; MainMenu do C. Các bên có thể làm mock/catalog/view độc lập ngay, không đợi A hoàn thành toàn bộ Core; phải thống nhất shared contract trước tích hợp.

[TEST_CASES](TEST_CASES.md) là đặc tả, chưa chạy. Các Q đã duyệt có oracle cập nhật; những Q/P khác còn PENDING chỉ chặn nhánh liên quan. Kiểm thử plain C# riêng với kiểm tra Unity/visual/build; không lấy tài liệu này làm bằng chứng code đã hoạt động.
