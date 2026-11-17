# Quyết định và câu hỏi chưa xác nhận

Cập nhật: **2026-11-17**. Tách rõ **APPROVED bởi người dùng**, **luật Word còn hiệu lực**, **đề xuất kỹ thuật** và **OPEN/PENDING**. Tài liệu này ghi luật/quyết định, không chứng minh mọi implementation hoặc test đặc tả đã hoàn tất.

## 1. Nguồn thực tế và thứ tự ưu tiên

1. Quyết định mới người dùng phê duyệt: gameplay ngày2026-11-06, đổi người phụ trách ngày2026-11-14 (F02), các câu trả lời và follow-up ngày2026-11-17 dưới đây, gồm mức thưởng Xuất phát (START-200K).
2. [Word gốc](references/GAME_DESIGN_DOCUMENT.docx) đối với luật không bị quyết định mới thay đổi.
3. [GAME_RULES](GAME_RULES.md).
4. [ARCHITECTURE](ARCHITECTURE.md), [DATA_MODEL](DATA_MODEL.md), [TEST_CASES](TEST_CASES.md) và tài liệu kỹ thuật khác.

Đã đối chiếu AGENTS, GAME_RULES, ARCHITECTURE, DATA_MODEL, TEST_CASES, DECISIONS, README và nội dung trích xuất thực tế từ Word. Word hiện có nằm ở docs/references/, không phải docs/reference/; ghi nhận không có Word trong bản biên soạn trước đã lỗi thời. Tên báo cáo cũ trong lịch sử là Bao_cao_Co_Ty_Phu_RPG_Giu_Nguyen_Muc_Luc_Goc(2).docx. Word hiện đã ghi tù tối đa3 lượt; không sửa Word trong nhiệm vụ này.

Nguồn phê duyệt mới: **prompt của người dùng “Cập nhật đồng bộ các tài liệu dự án theo những luật đã được người dùng phê duyệt và tạo tài liệu phân công công việc chi tiết cho 4 thành viên”**, ngày2026-11-06, có mục “Các quyết định gameplay ĐÃ ĐƯỢC PHÊ DUYỆT” và “Phân công chính thức 4 thành viên”. Không dùng lời đề xuất của agent trong các lượt trước làm bằng chứng phê duyệt.

[Chắc chắn] là nội dung có trong nguồn; [Có khả năng cao] cho đề xuất có lý do; OPEN/PENDING không có hành vi mặc định ngầm.

## 2. Nhật ký phê duyệt mới

Người phê duyệt: **người dùng giao việc trong chat dự án**; luật/phân công ban đầu ngày2026-11-06, đổi A/B ngày2026-11-14, trả lời câu hỏi và follow-up gameplay ngày2026-11-17. Bảng giữ lịch sử; quy định mới cùng phạm vi thay quy định cũ. Không gán danh tính người phê duyệt khi prompt không nói.

| ID | Trạng thái | Nội dung có hiệu lực |
|---|---|---|
| Q04 | APPROVED — A | Kỹ năng chủ động trước gieo/sau hoàn tất ô khi hợp lệ; không giữa animation/giao dịch. Lawyer Active trước thuê của chính người dùng. Strategist Ultimate thay quyền gieo tiếp theo kể cả đôi, không sinh đôi. Bán tự nguyện ở điểm hợp lệ; thanh lý bắt buộc ngay cả ngoài lượt. |
| Q05 | APPROVED — A | Ultimate Doanh Nhân có hiệu lực ngay, phủ các đối thủ xen giữa, hết ngay trước lượt riêng tiếp theo của chủ; đôi không kéo dài. |
| Q06 | APPROVED — A | Áp hiệu ứng sẵn có trước; nghĩa vụ0 không tiêu thêm; nghĩa vụ dương chọn trả thường hoặc một biện pháp miễn/giảm chủ động. Giữ tự dùng Bảo vệ và công thức thuê. |
| Q09 | APPROVED — A | Nghĩa vụ đến hạn → thanh lý/phá sản → thắng ngay → mốc lượt/cooldown/effect → lãi1% số dư dương còn lại nếu hết vòng → chuyển lượt/hết giờ. Thắng tức thời kết thúc ngay. |
| Q11 | APPROVED — A | Đôi rồi bị giam do ô8/thẻ kết thúc lượt, mất quyền gieo thêm. Đôi khi thử ra tù vẫn dùng tổng đó để đi và được tung thêm. |
| Q13 | APPROVED — A, 2026-11-06 | Floor thanh toán cuối cho phép tính chưa có quy tắc riêng; thuê/lãi vẫn floor; không float; phân vốn phần lẻ lớn nhất và thứ tự bậc cố định, tổng đúng tiền thực trả. Hướng phá hòa được bổ sung tại P01 ngày2026-11-17. |
| Q20 | APPROVED — B | Hết giờ vẫn hoàn tất lần dừng và chuỗi trực tiếp, gồm popup mua/bỏ qua/cưỡng chế/dịch chuyển/lễ hội; không gieo mới hoặc mở kỹ năng không liên quan; popup pause Human/AI; sau chuỗi chạy Q09, thắng tức thời kết thúc ngay. |
| JAIL-03 | APPROVED — THAY WORD | Tối đa3 lượt riêng ở tù; lượt bị bắt không tính. Lượt3 thử không đôi bắt buộc200.000, được thanh lý, thiếu tiếp phá sản. Trả thành công thả cuối lượt, không đi thêm; lượt sau mới đi. Thoát sớm/đôi giữ luật thường; vẫn thu thuê. |
| F01 | SUPERSEDED — PHÂN CÔNG 2026-11-06 | Phân công cũ: Hồ Đức Mạnh—A Core Gameplay & Economy; Nguyễn Xuân Hoàng Lộc—B Board3D & Visual; Đinh Phương Ly—C UI/UX & Event Cards; Nguyễn Tiến Lộc—D Characters, Skills & AI. Người phụ trách A/B đã được thay bằng F02; không dùng dòng này để giao việc hiện tại. |
| F02 | APPROVED — PHÂN CÔNG 2026-11-14 | Nguyễn Xuân Hoàng Lộc—A Core Gameplay & Economy; Hồ Đức Mạnh—B Board 3D & Visual; Đinh Phương Ly—C UI/UX & Event Cards; Nguyễn Tiến Lộc—D Characters, Skills & AI. Chỉ đổi người nhận A/B; giữ nguyên nhiệm vụ, hợp đồng và ranh giới module. Core duy nhất ghi state; Gameplay do B (Mạnh), MainMenu do C (Ly). |
| Q03 | APPROVED — 2026-11-17 | 2–4 người, được trùng nghề; chọn ngẫu nhiên người đầu, sau đó theo thứ tự ghế cố định, bỏ qua người phá sản. |
| P01 | APPROVED — 2026-11-17 | Phần lẻ bằng nhau ưu tiên Cọc → Nhà1 → Nhà2 → Nhà3 → Hotel. Bảo toàn tổng vốn thực. |
| P02 | APPROVED — 2026-11-17 | Lawyer Ultimate đang có hiệu lực miễn phí tù bắt buộc200.000, vẫn thả cuối lượt; không di chuyển/gieo thêm. Nợ Banker không được miễn. |
| Q10 | APPROVED — 2026-11-17 | Thẻ tới8/24 và dịch vụ24 đều đi tiến, nhận thưởng Xuất phát theo START-200K. Dịch vụ trả100.000, chọn mọi đích trừ24. Nếu thẻ đưa lại24 thì được trả phí dùng tiếp; không giới hạn một lần cho toàn root chain. |
| Q12 | APPROVED — 2026-11-17 | Kỹ Sư Active/Ultimate được lên Hotel ngay cùng lần đến nếu đủ điều kiện/cửa sổ kỹ năng; nâng thường vẫn phải chờ lần dừng sau. |
| Q14 | APPROVED — THAY LUẬT2 LƯỢT, 2026-11-17 | Lễ hội tự tổ chức ô16/thẻ12 dùng chung một TP toàn trận, khác ba lễ hội cố định. Mỗi200.000 cùng chủ/cùng TP thêm50% thuê gốc (50/100/150%...), không hết hạn. Đổi TP hoặc người tổ chức thì hủy mức cũ, tạo mức50% ở TP mới; cộng đại số với buff hợp lệ. |
| Q15 | APPROVED — 2026-11-17 | Lễ hội tổ chức xóa khi TP đổi chủ, bán/phá hoặc về ngân hàng do phá sản. Ba festival cố định không bị xóa. Cúp điện vẫn hủy khi đổi chủ/Hotel. |
| Q16 | APPROVED — 2026-11-17 | Coupon Investor còn chưa dùng thì không kích hoạt lại Active dù cooldown hết; không ghi đè/tích thêm coupon. |
| Q17 | APPROVED — 2026-11-17 | Người trả tự chọn thứ tự tài sản; AI chọn qua cùng API. Nguồn effect phá sản thì giữ qua vòng đang dở; N lượt còn lại chuyển thành N vòng chung mới hoàn chỉnh tiếp theo, không tính vòng dở. Q15/hủy do Hotel vẫn ưu tiên; lễ hội tổ chức không có hạn lượt. |
| Q19 | APPROVED — 2026-11-17 | Thẻ10 tạo nghĩa vụ100.000 đồng thời cho mọi đối thủ còn chơi. Người đủ tiền tự trả ngay; tất cả người thiếu cùng có nghĩa vụ chờ thanh lý/phá sản. Người rút không tiếp tục khi batch chưa giải quyết xong. Không chọn thứ tự ghế để trì hoãn người đủ tiền. |
| JAIL-REROLL | APPROVED — 2026-11-17 | Strategist được gieo lại hai xúc xắc thử ra tù trước chốt; chỉ kết quả cuối quyết định thoát/phí/di chuyển, phải chấp nhận kết quả mới. |
| ENTREPRENEUR-FORCE | APPROVED — 2026-11-17 | Điểm yếu Doanh Nhân+5% chỉ mua từ ngân hàng; cưỡng chế thường vẫn200%, không205%/210%. |
| START-200K | APPROVED — 17/11/2026 | Mỗi lần đi qua hoặc dừng đúng ô Xuất phát trong hành trình hợp lệ thưởng 200.000đ, kể cả khi dịch chuyển. Chỉ thưởng một lần cho mỗi lần vượt mốc ô 0. Phí dịch chuyển ô 24 vẫn là 100.000đ. Các khoản tiền khác giữ nguyên. |

Nguồn F02: yêu cầu của người dùng ngày2026-11-14: “nhiệm vụ A là của hoàng lộc và nhiệm vụ B là của mạnh”. F02 thay phần người phụ trách A/B của F01, không phê duyệt thêm kiến trúc hoặc thay luật gameplay.

Nguồn bổ sung ngày2026-11-17: các câu trả lời trực tiếp của người dùng cho các vấn đề còn thiếu và các follow-up về lễ hội, di chuyển, Setup, kỹ năng và hiệu ứng. Nội dung ở bảng là diễn giải phạm vi câu trả lời; không lấy đề xuất agent làm bằng chứng. Người dùng xác nhận Q17: “Người trả tự chọn thứ tự tài sản; AI tự chọn qua cùng API (đề xuất)” và “Giữ qua vòng đang dở; đếm số vòng mới kế tiếp bằng số lượt còn lại (đề xuất)”. Q02 về deck/shuffle/rút lại **chưa được phê duyệt**.

## 3. Khác biệt với Word và cách áp dụng

| Nội dung Word | Quyết định mới | Hệ quả |
|---|---|---|
| Lịch sử bản Word cũ: §3.2h từng ghi ở tù không giới hạn | JAIL-03 tối đa3 lượt; phí bắt buộc sau lần thử thất bại thứ3 | Word hiện trong repository đã ghi tối đa3 lượt; đây là truy vết thay đổi cũ, không phải mâu thuẫn hiện tại. Giữ counter, nghĩa vụ và test thả cuối lượt. |
| §3.2a cho đôi tung thêm, chưa ghi ngoại lệ mới vào tù | Q11 hủy quyền thêm khi vừa bị giam | Không dùng câu “đôi luôn tung tiếp” để cho gieo trong cùng lượt vừa bị bắt. |
| §4.3b “1 lượt riêng hiệu lực”, chưa chỉ mốc Ultimate Doanh Nhân | Q05=A hết trước lượt riêng kế tiếp | Không dùng phương án B tới hết lượt kế tiếp dù agent từng đề xuất B. |
| Các cửa sổ skill, tổ hợp miễn/giảm, thứ tự cuối lượt, tiền lẻ và ranh giới hết giờ chưa đủ chi tiết | Q04/Q06/Q09/Q13/Q20 bổ sung | Không phải sửa số giá/công thức Word; bổ sung hành vi có nguồn duyệt mới. |
| Word còn ghi mức thưởng Xuất phát trước khi có quyết định START-200K | Áp dụng thưởng 200.000đ từ quyết định ngày 17/11/2026 | Cần đồng bộ báo cáo Word ở lần cập nhật tài liệu Word. Phí dịch chuyển vẫn là 100.000đ. |
| Lễ hội tổ chức+50% trong2 lượt riêng, không có luật cộng dồn toàn cục | Q14/Q15 ngày2026-11-17 thay bằng một TP toàn trận, cùng chủ/cùng TP mỗi200.000 thêm50% thuê gốc, không hết hạn | Luật2 lượt cũ bị thay; ba festival cố định đầu trận không đổi. |
| Thiếu chi tiết Setup/di chuyển/Hotel kỹ năng/nguồn effect phá sản/batch quà | Q03/Q10/Q12/Q17/Q19 và hai chi tiết kỹ năng ngày2026-11-17 | Dùng quyết định mới cho đúng phạm vi, không sửa Word để xóa lịch sử khác biệt. |

Các giá niêm yết,32 ô,20 TP/8 nước,4 biển,14 thẻ,6 nghề/cooldown và phạm vi sản phẩm không đổi; các hành vi/lễ hội được thay hoặc bổ sung theo bảng. Không ghi đè/sửa Word để che khác biệt.

## 4. Luật nền và ngoại lệ đã được duyệt

| ID | Nội dung phải giữ | Nguồn |
|---|---|---|
| R01 | 32 ô, 20 TP/8 nước, 4 biển, 8 chức năng; ID 0–31; TP17 không có; hai TP cuối thuộc Úc | §1, §3 |
| R02 | 2–4 người cùng máy/AI, được trùng nghề, người đầu ngẫu nhiên/thứ tự ghế cố định; 300.000/người, ô0, ba festival cố định, thời gian30/45/60/vô hạn | §1–2; Q03 mới |
| R03 | Hai xúc xắc, đôi thêm không giới hạn trong cùng lượt riêng; qua hoặc dừng đúng ô 0 trong hành trình hợp lệ nhận 200.000 đúng một lần/mốc | §2, START-200K |
| R04 | Cấp, giá, công thức thuê; cộng phần trăm trên thuê gốc rồi nhân đôi, floor cuối; Hotel có miễn nhiễm | §4 |
| R05 | Giá/thuê biển; 4 biển thắng ngay; không nâng/cưỡng chế/phá biển | §5 |
| R06 | Cưỡng chế thường 200% sau trả thuê; Investor Ultimate 150% từ xa không thuê; vốn thực theo bậc | §6 |
| R07 | Thuế 20% chốt tại phát sinh; Lawyer chỉ giảm thuế thẻ 25%; bắt buộc thanh lý trước phá sản | §6–7 |
| R08 | Tù tối đa3 lượt, lượt bị bắt không tính; tung không đôi ở lượt tù thứ3 bắt buộc200.000/thanh lý/phá sản; thả cuối lượt nếu trả thành công; dịch chuyển/lễ hội giữ giá cũ | Quyết định mới JAIL-03; GAME_RULES §7 |
| R09 | Đúng 14 loại thẻ, bốn loại giữ mỗi loại tối đa 1; bảo vệ tự dùng đúng mục tiêu; Cúp điện hủy khi đổi chủ/Hotel | §8 |
| R10 | Sáu nghề, Active cooldown 5/6/7/6/7/5; Ultimate một lần; chỉ tiêu hao khi thành công; khoản vay 100.000→110.000 | §9 |
| R11 | Ba quốc gia / bốn biển / còn một người thắng tức thời; hết giờ so tiền mặt rồi giá niêm yết rồi đồng thắng | §10 |
| R12 | Không online, Save/Load giữa trận, shop hoặc mở rộng kinh tế ngoài đặc tả | §1, §11 |

R03 đọc cùng Q10/Q11/Q20; R10 đọc cùng Q05/Q17/JAIL-REROLL. Vốn đọc cùng Q13/P01. P02 đã duyệt ngoại lệ miễn phí tù; không giữ câu hỏi cũ để chặn lại. Lễ hội tổ chức theo Q14/Q15 mới, không dùng thời hạn2 lượt Word cũ.

## 5. Kiến trúc và hợp đồng kỹ thuật

A01–A06 vẫn **ĐỀ XUẤT**, không tự phê duyệt tên lớp/API/DTO chỉ vì luật và phân công được duyệt. Một nguồn state và quyền Core ghi state là yêu cầu đã có; lựa chọn MatchCoordinator, namespace và cấu trúc DTO cụ thể cần nhóm thống nhất giai đoạn0. Phân công hiện hành F02 cập nhật người phụ trách A/B của F01; bảng trách nhiệm cũ A07 vẫn không áp dụng.

| ID | Đề xuất | Lý do và hệ quả | Trạng thái |
|---|---|---|---|
| A01 | Một MatchCoordinator plain C# sở hữu duy nhất GameState; MatchState là tên cùng khái niệm | Phù hợp AGENTS; tránh state song song, vẫn giữ công thức ở module chuyên môn | ĐỀ XUẤT |
| A02 | Core.Contracts chứa ID/enum/DTO; leaf modules không gọi lẫn nhau; PropertyId=TileId | Giảm vòng phụ thuộc và lỗi ánh xạ; không yêu cầu tách assembly | ĐỀ XUẤT |
| A03 | Lệnh validate → tính → commit một lần → thông báo; actionId/revision/decisionId trong bộ nhớ | Chặn double-click/callback lặp và popup lỗi thời; không xây event sourcing | ĐỀ XUẤT |
| A04 | Catalog tĩnh bằng ít ScriptableObject, chuyển sang plain data; tiền long và tỷ lệ phân số | Tách Unity khỏi luật, không sửa SO runtime; chưa tạo asset/class | ĐỀ XUẤT |
| A05 | Thời hạn theo ordinal lượt riêng; effect có sourcePlayerId; Banker debt có deadline riêng | Không đếm sai vì đôi; tuân thủ Q05/Q09 đã duyệt; biểu diễn vòng Q08 cần rà soát kỹ thuật; vòng đời effect khi source bị loại theo Q17 đã duyệt | ĐỀ XUẤT |
| A06 | Domain chốt đường đi/xúc xắc, presentation chạy sau commit; một continuation hợp lệ | Animation không cấp tiền/rút thẻ hoặc chuyển lượt; cần kiểm tra tích hợp Unity sau này | ĐỀ XUẤT |
| A07 | Phân chia bốn đầu mối cũ, chưa có tên | Đã được thay bằng phân công chính thức trong TEAM_TASKS, hiện áp dụng F02; không dùng lại để giao việc | SUPERSEDED |

## 6. Truy vết đầy đủ Q01–Q20

Cột câu hỏi giữ nguyên văn để đối chiếu lịch sử. Các câu hỏi APPROVED không còn cần xin quyết định lại. Các mục chưa có phê duyệt mới giữ **PENDING**; nếu Word đã trả lời một phần thì không hỏi lại phần ấy. Chỉ chặn nhánh thật sự còn thiếu.

| ID | Trạng thái | Câu hỏi gốc | Phạm vi hiện tại |
|---|---|---|---|
| Q01 | PENDING | Bản Word đã chốt nào cần đối chiếu? Nếu có khác biệt, nhóm xác nhận cách cập nhật nguồn nào? | Word hiện có tại docs/references/GAME_DESIGN_DOCUMENT.docx và đã đọc; không cần người dùng tìm/thêm lại. PENDING chỉ giữ truy vết câu hỏi cũ, không là phê duyệt gameplay. |
| Q02 | PENDING | Có bộ hữu hạn hay rút theo bảng xác suất? Bỏ thẻ/rút trùng đi đâu? Khi hết bài xử lý thế nào? Preview có thay đổi khi thẻ bị giữ/bỏ? | PENDING: phân phối, số bản sao, bỏ/rút lại thẻ; seed là chi tiết kỹ thuật đề xuất, không tự biến thành luật xác suất. |
| Q03 | APPROVED — 2026-11-17 | Số người tối thiểu? Có trùng nghề? Chọn người đầu và thứ tự lượt bằng cách nào? | 2–4 người, được trùng nghề; người đầu ngẫu nhiên rồi thứ tự ghế cố định, bỏ qua người phá sản. |
| Q04 | APPROVED | Active/Ultimate dùng ở pha nào? Lawyer Active có được phản ứng khi ngoài lượt? Khi nào được bán tự nguyện? Strategist Ultimate thay gieo hay thêm di chuyển; sau đó còn gieo/đôi không? | APPROVED=A: điểm dừng trước gieo/sau hoàn tất ô; không giữa animation/giao dịch; Lawyer phản ứng trước thuê của chính mình; bán tự nguyện ở điểm hợp lệ; thanh lý bắt buộc cả ngoài lượt; Strategist Ultimate thay một quyền gieo, không tạo đôi. |
| Q05 | APPROVED | Ultimate nhân đôi bắt đầu/kết thúc đúng mốc nào? Có phủ lượt đối thủ xen giữa không? | APPROVED=A: RentDouble có hiệu lực ngay, phủ đối thủ, hết ngay trước lượt riêng kế tiếp của chủ; không dùng phương án B từng được agent đề xuất. |
| Q06 | APPROVED | Khi đồng thời có Lawyer Active/Ultimate, thẻ giảm thuê/miễn thuế, Cúp điện và các modifier, hỏi/chọn và tiêu hao theo thứ tự nào? Thuê bằng 0 có được dùng thẻ/kỹ năng không? | APPROVED=A: hiệu ứng sẵn có trước; payable0 không tiêu thêm; payable dương chọn trả thường hoặc tối đa một biện pháp chủ động; Bảo vệ tự dùng vẫn theo Word. |
| Q07 | PENDING | Passive/Active/Ultimate Entrepreneur, điểm yếu Engineer, thẻ giảm thuê ảnh hưởng thuê biển thế nào? | PENDING — không có phê duyệt mới; Word xác định biển là BĐS, modifier thuê BĐS và thẻ Giảm thuê không giới hạn TP. Ca cụ thể D07 có oracle từ Word; không tự tạo ngoại lệ biển, không xin duyệt lại phần đã rõ. |
| Q08 | PENDING | Tập người phải hoàn tất vòng được chốt lúc nào? Người bị loại có được tính là đã hoàn tất hay bị gỡ khỏi tập chờ? | PENDING về hợp đồng biểu diễn tập người; Word đã nêu chỉ người chưa phá sản. D08 có oracle cho tình huống đã mô tả; thứ tự cuối lượt Q09 đã duyệt, không hỏi lại luật đó. |
| Q09 | APPROVED | Thứ tự trả nợ, hết hạn effect, lãi vòng, kiểm tra thắng và hết giờ ở cùng mốc? Nhiều người đạt thắng tức thời trong một chuỗi thì xử lý ra sao? | APPROVED=A: nghĩa vụ đến hạn → thanh lý/phá sản → thắng tức thời → mốc lượt/cooldown/effect → lãi Banker nếu hết vòng → chuyển lượt/kết quả hết giờ. Dừng ngay khi đã thắng. |
| Q10 | APPROVED — 2026-11-17 | Thẻ Vào tù/Đến dịch chuyển đi theo đường hay đặt vị trí trực tiếp; có tiền qua ô 0 không? Có cho chọn chính đích 24 nhưng không kích hoạt lại dịch vụ không? Chuỗi thẻ quay về 24 được xem là thao tác mới hay cùng chuỗi? | Thẻ tới8/24 và dịch vụ đi tiến, nhận thưởng Xuất phát theo START-200K; dịch vụ trả100.000, không chọn24. Thẻ đưa lại24 thì được dùng dịch vụ tiếp; không khóa một lần/root chain. |
| Q11 | APPROVED | Nếu gieo đôi rồi dừng ô 8 hoặc rút Vào tù, lượt có kết thúc ngay không? Quyền tung thêm còn giữ không? | APPROVED=A: đôi rồi vào tù qua ô8/thẻ thì kết thúc lượt và mất gieo thêm; đôi để thoát tù đã có từ trước vẫn đi/tung thêm như Word. |
| Q12 | APPROVED — 2026-11-17 | Engineer Active/Ultimate có vượt điều kiện lần dừng sau ngay khi vừa mua Nhà3 không? Có hạn chế mục tiêu khác, kể cả Cọc/Hotel hoặc TP đang Cúp điện? | Active/Ultimate Kỹ Sư được vượt điều kiện chờ lần dừng sau, kể cả vừa mua Nhà3; vẫn cần đúng cửa sổ kỹ năng, mục tiêu/chi phí. Nâng thường không có ngoại lệ này. |
| Q13 | APPROVED | Nếu giá giảm/phụ phí, vốn phân bổ, hoàn 50%, thuế có phần đồng lẻ, làm tròn thế nào và gán phần dư vào bậc nào? | Floor cuối, chia vốn phần lẻ lớn nhất và bảo toàn tổng; P01 ngày2026-11-17 đã chốt phá hòa Cọc→Nhà1→Nhà2→Nhà3→Hotel. |
| Q14 | APPROVED — THAY LUẬT CŨ | Được trả phí để gia hạn/thay thế lễ hội đang có hay phải từ chối? | Một TP lễ hội tổ chức trên toàn trận; mỗi200.000 cùng chủ/cùng TP thêm50% thuê gốc, không hết hạn. Đổi TP/người tổ chức thay mức cũ bằng50%; ô16/thẻ12 cùng cơ chế, không áp biển. |
| Q15 | APPROVED — 2026-11-17 | Lễ hội tạm và hiệu ứng khác trên TP tồn tại, chuyển theo đất hay hủy khi đổi chủ/bán/phá? Festival tạm có mất khi phá sản không? | Xóa lễ hội tổ chức khi đổi chủ/bán/phá/về ngân hàng do phá sản. Không xóa ba festival cố định. Cúp điện giữ quy tắc đổi chủ/Hotel cũ. |
| Q16 | APPROVED — 2026-11-17 | Khi hết cooldown mà coupon cũ chưa dùng, có thể tích/ghi đè/thay thế không? | Coupon chưa dùng thì không kích hoạt lại Active, kể cả cooldown hết; không ghi đè/tích thêm. |
| Q17 | APPROVED — 2026-11-17 | Ai chọn thứ tự tài sản thanh lý? Có thể chấp nhận phá sản trước khi bán hết không? Người tạo hiệu ứng phá sản thì effect có hủy hay có mốc thay thế? | Người trả chọn tài sản, AI qua cùng API; phải thanh lý trước phá sản. Giữ effect qua vòng đang dở, N lượt còn lại chuyển thành N vòng mới hoàn chỉnh tiếp theo; Q15/Hotel vẫn hủy theo luật riêng. |
| Q18 | PENDING — TRUY VẾT | Quà từ đối thủ/Tặng quà có được miễn trong lượt Ultimate không? Nợ Banker luôn không được miễn theo §9 | Quà trong lượt miễn đã có luật Word, không hỏi lại; nợ Banker không miễn. P02 nay APPROVED cho phí tù bắt buộc và thả cuối lượt; không suy rộng sang phí dịch vụ tự chọn. |
| Q19 | APPROVED — 2026-11-17 | Thứ tự xử lý từng đối thủ? Nếu phá sản tạo thắng tức thời giữa chuỗi, có xử lý các người trả còn lại không? | Tất cả đối thủ còn chơi phát sinh nghĩa vụ100.000 đồng thời; đủ tiền tự trả ngay, mọi người thiếu cùng chờ thanh lý/phá sản; người rút không tiếp tục khi batch chưa giải quyết. Không mặc định thứ tự ghế. |
| Q20 | APPROVED | Ranh giới “một hành động” gồm những lựa chọn tự chọn nào khi hết giờ (cưỡng chế sau thuê, dịch chuyển, lễ hội, chuỗi thẻ)? Popup chọn mua/bỏ qua và popup của AI có pause không? | APPROVED=B: hoàn tất lần dừng/chuỗi trực tiếp, kể cả popup tự chọn; không gieo mới hoặc dùng kỹ năng chủ động không liên quan; popup pause Human/AI; sau đó Q09 rồi kết quả, trừ thắng tức thời. |

## 7. Chi tiết bổ sung và phần còn mở

| ID | Trạng thái | Kết luận có hiệu lực | Kiểm chứng cần có |
|---|---|---|---|
| P01 | APPROVED — 2026-11-17 | Phần lẻ bằng nhau ưu tiên Cọc→Nhà1→Nhà2→Nhà3→Hotel |103 theo40:20:20:20 → [41,21,21,20,0], tổng103 |
| P02 | APPROVED — 2026-11-17 | Lawyer Ultimate miễn phí tù bắt buộc200.000 và thả cuối lượt, không di chuyển thêm | Không trừ tiền, không mở gieo mới; nợ Banker vẫn phải trả |
| Q02 | PENDING | Chưa chốt phân phối/số bản sao, shuffle, bỏ/rút lại và preview khi hết/tái tạo deck | Cần quyết định trước production deck; không coi fixture/provider test là luật sản phẩm |

Q01/Q07/Q08/Q18 giữ truy vết lịch sử hoặc phần hợp đồng kỹ thuật, không dùng để hỏi lại luật đã rõ. Q03/Q10/Q12/Q14/Q15/Q16/Q17/Q19 và P01/P02 không còn chặn vì thiếu câu trả lời cũ. A01–A06 vẫn ĐỀ XUẤT, chưa có bằng chứng cả nhóm chấp thuận API; implementation và test code không tự phê duyệt kiến trúc. Không tạo online/Save/Load/shop.
## 8. Cách ghi quyết định tiếp theo

Ghi ID, người xác nhận, ngày, nguyên văn/phạm vi câu trả lời, nguồn phê duyệt và hợp đồng/test chịu ảnh hưởng. Chỉ đổi PENDING khi có bằng chứng; đề xuất kỹ thuật không thay luật. Shared contract phải được A và các gói sử dụng thống nhất trước tích hợp theo [TEAM_TASKS](TEAM_TASKS.md). Mọi test ở đây và TEST_CASES mới là đặc tả, chưa RUN/PASS.
