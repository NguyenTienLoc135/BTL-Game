# Quyết định và câu hỏi chưa xác nhận

Cập nhật: 2026-11-06. Tách rõ **APPROVED bởi người dùng**, **luật Word còn hiệu lực**, **đề xuất kỹ thuật** và **OPEN/PENDING**. Không có implementation hoặc test thực thi được xác nhận bởi tài liệu này.

## 1. Nguồn thực tế và thứ tự ưu tiên

1. Quyết định mới được người dùng phê duyệt rõ ràng trong prompt cập nhật tài liệu/phân công ngày2026-11-06.
2. [Word gốc](references/GAME_DESIGN_DOCUMENT.docx) đối với luật không bị quyết định mới thay đổi.
3. [GAME_RULES](GAME_RULES.md).
4. [ARCHITECTURE](ARCHITECTURE.md), [DATA_MODEL](DATA_MODEL.md), [TEST_CASES](TEST_CASES.md) và tài liệu kỹ thuật khác.

Đã đọc đầy đủ AGENTS, GAME_RULES, ARCHITECTURE, DATA_MODEL, TEST_CASES, DECISIONS, README và trích xuất nội dung Word643 đoạn/7 bảng. Word thực tế nằm ở docs/references/, không phải docs/reference/; ghi nhận không có Word trong bản biên soạn trước đã lỗi thời. Tên báo cáo cũ trong lịch sử là Bao_cao_Co_Ty_Phu_RPG_Giu_Nguyen_Muc_Luc_Goc(2).docx. Không sửa Word đã nộp.

Nguồn phê duyệt mới: **prompt của người dùng “Cập nhật đồng bộ các tài liệu dự án theo những luật đã được người dùng phê duyệt và tạo tài liệu phân công công việc chi tiết cho 4 thành viên”**, ngày2026-11-06, có mục “Các quyết định gameplay ĐÃ ĐƯỢC PHÊ DUYỆT” và “Phân công chính thức 4 thành viên”. Không dùng lời đề xuất của agent trong các lượt trước làm bằng chứng phê duyệt.

[Chắc chắn] là nội dung có trong nguồn; [Có khả năng cao] cho đề xuất có lý do; OPEN/PENDING không có hành vi mặc định ngầm.

## 2. Nhật ký phê duyệt mới

Người phê duyệt: **người dùng giao việc trong chat dự án**, ngày2026-11-06. Không gán vai trò người phê duyệt cho một thành viên cụ thể khi prompt không nói.

| ID | Trạng thái | Nội dung có hiệu lực |
|---|---|---|
| Q04 | APPROVED — A | Kỹ năng chủ động trước gieo/sau hoàn tất ô khi hợp lệ; không giữa animation/giao dịch. Lawyer Active trước thuê của chính người dùng. Strategist Ultimate thay quyền gieo tiếp theo kể cả đôi, không sinh đôi. Bán tự nguyện ở điểm hợp lệ; thanh lý bắt buộc ngay cả ngoài lượt. |
| Q05 | APPROVED — A | Ultimate Doanh Nhân có hiệu lực ngay, phủ các đối thủ xen giữa, hết ngay trước lượt riêng tiếp theo của chủ; đôi không kéo dài. |
| Q06 | APPROVED — A | Áp hiệu ứng sẵn có trước; nghĩa vụ0 không tiêu thêm; nghĩa vụ dương chọn trả thường hoặc một biện pháp miễn/giảm chủ động. Giữ tự dùng Bảo vệ và công thức thuê. |
| Q09 | APPROVED — A | Nghĩa vụ đến hạn → thanh lý/phá sản → thắng ngay → mốc lượt/cooldown/effect → lãi1% số dư dương còn lại nếu hết vòng → chuyển lượt/hết giờ. Thắng tức thời kết thúc ngay. |
| Q11 | APPROVED — A | Đôi rồi bị giam do ô8/thẻ kết thúc lượt, mất quyền gieo thêm. Đôi khi thử ra tù vẫn dùng tổng đó để đi và được tung thêm. |
| Q13 | APPROVED — A | Floor thanh toán cuối cho phép tính chưa có quy tắc riêng; thuê/lãi vẫn floor; không float; phân vốn phần lẻ lớn nhất và thứ tự bậc cố định, tổng đúng tiền thực trả. P01 chỉ là hướng phá hòa chưa nêu. |
| Q20 | APPROVED — B | Hết giờ vẫn hoàn tất lần dừng và chuỗi trực tiếp, gồm popup mua/bỏ qua/cưỡng chế/dịch chuyển/lễ hội; không gieo mới hoặc mở kỹ năng không liên quan; popup pause Human/AI; sau chuỗi chạy Q09, thắng tức thời kết thúc ngay. |
| JAIL-03 | APPROVED — THAY WORD | Tối đa3 lượt riêng ở tù; lượt bị bắt không tính. Lượt3 thử không đôi bắt buộc200.000, được thanh lý, thiếu tiếp phá sản. Trả thành công thả cuối lượt, không đi thêm; lượt sau mới đi. Thoát sớm/đôi giữ luật thường; vẫn thu thuê. |
| F01 | APPROVED — PHÂN CÔNG | Hồ Đức Mạnh—A Core Gameplay & Economy; Nguyễn Xuân Hoàng Lộc—B Board3D & Visual; Đinh Phương Ly—C UI/UX & Event Cards; Nguyễn Tiến Lộc—D Characters, Skills & AI. Core duy nhất ghi state; Gameplay do B, MainMenu do C. |

## 3. Khác biệt với Word và cách áp dụng

| Nội dung Word | Quyết định mới | Hệ quả |
|---|---|---|
| §3.2h ở tù không giới hạn | JAIL-03 tối đa3 lượt; phí bắt buộc sau lần thử thất bại thứ3 | Luật Word cũ không áp dụng cho giới hạn tù; cần counter, nghĩa vụ mới và test thả cuối lượt. |
| §3.2a cho đôi tung thêm, chưa ghi ngoại lệ mới vào tù | Q11 hủy quyền thêm khi vừa bị giam | Không dùng câu “đôi luôn tung tiếp” để cho gieo trong cùng lượt vừa bị bắt. |
| §4.3b “1 lượt riêng hiệu lực”, chưa chỉ mốc Ultimate Doanh Nhân | Q05=A hết trước lượt riêng kế tiếp | Không dùng phương án B tới hết lượt kế tiếp dù agent từng đề xuất B. |
| Các cửa sổ skill, tổ hợp miễn/giảm, thứ tự cuối lượt, tiền lẻ và ranh giới hết giờ chưa đủ chi tiết | Q04/Q06/Q09/Q13/Q20 bổ sung | Không phải sửa số giá/công thức Word; bổ sung hành vi có nguồn duyệt mới. |

Các giá,32 ô,20 TP/8 nước,4 biển,14 thẻ,6 nghề/cooldown và phạm vi sản phẩm không bị sửa. Không ghi đè Word bằng bản mới hoặc sửa Word để che khác biệt.

## 4. Luật nền và ngoại lệ đã được duyệt

| ID | Nội dung phải giữ | Nguồn |
|---|---|---|
| R01 | 32 ô, 20 TP/8 nước, 4 biển, 8 chức năng; ID 0–31; TP17 không có; hai TP cuối thuộc Úc | §1, §3 |
| R02 | Tối đa 4 người cùng máy/AI, 300.000/người, ô 0, ba TP lễ hội cố định, thời gian 30/45/60/vô hạn | §1–2 |
| R03 | Hai xúc xắc, đôi thêm không giới hạn trong cùng lượt riêng; qua ô 0 nhận 100.000 đúng một lần/mốc | §2 |
| R04 | Cấp, giá, công thức thuê; cộng phần trăm trên thuê gốc rồi nhân đôi, floor cuối; Hotel có miễn nhiễm | §4 |
| R05 | Giá/thuê biển; 4 biển thắng ngay; không nâng/cưỡng chế/phá biển | §5 |
| R06 | Cưỡng chế thường 200% sau trả thuê; Investor Ultimate 150% từ xa không thuê; vốn thực theo bậc | §6 |
| R07 | Thuế 20% chốt tại phát sinh; Lawyer chỉ giảm thuế thẻ 25%; bắt buộc thanh lý trước phá sản | §6–7 |
| R08 | Tù tối đa3 lượt, lượt bị bắt không tính; tung không đôi ở lượt tù thứ3 bắt buộc200.000/thanh lý/phá sản; thả cuối lượt nếu trả thành công; dịch chuyển/lễ hội giữ giá cũ | Quyết định mới JAIL-03; GAME_RULES §7 |
| R09 | Đúng 14 loại thẻ, bốn loại giữ mỗi loại tối đa 1; bảo vệ tự dùng đúng mục tiêu; Cúp điện hủy khi đổi chủ/Hotel | §8 |
| R10 | Sáu nghề, Active cooldown 5/6/7/6/7/5; Ultimate một lần; chỉ tiêu hao khi thành công; khoản vay 100.000→110.000 | §9 |
| R11 | Ba quốc gia / bốn biển / còn một người thắng tức thời; hết giờ so tiền mặt rồi giá niêm yết rồi đồng thắng | §10 |
| R12 | Không online, Save/Load giữa trận, shop hoặc mở rộng kinh tế ngoài đặc tả | §1, §11 |

R03 luôn phải đọc cùng ngoại lệ Q11/Q20. R10 về thời hạn đọc cùng mốc đặc biệt Q05. Những nguyên tắc vốn/thuế đọc cùng Q13; tương tác miễn nghĩa vụ mới ở P02 chưa được tự quyết.

## 5. Kiến trúc và hợp đồng kỹ thuật

A01–A06 vẫn **ĐỀ XUẤT**, không tự phê duyệt tên lớp/API/DTO chỉ vì luật và phân công được duyệt. Một nguồn state và quyền Core ghi state là yêu cầu đã có; lựa chọn MatchCoordinator, namespace và cấu trúc DTO cụ thể cần nhóm thống nhất giai đoạn0. Phân công F01 chính thức thay bảng trách nhiệm cũ A07.

| ID | Đề xuất | Lý do và hệ quả | Trạng thái |
|---|---|---|---|
| A01 | Một MatchCoordinator plain C# sở hữu duy nhất GameState; MatchState là tên cùng khái niệm | Phù hợp AGENTS; tránh state song song, vẫn giữ công thức ở module chuyên môn | ĐỀ XUẤT |
| A02 | Core.Contracts chứa ID/enum/DTO; leaf modules không gọi lẫn nhau; PropertyId=TileId | Giảm vòng phụ thuộc và lỗi ánh xạ; không yêu cầu tách assembly | ĐỀ XUẤT |
| A03 | Lệnh validate → tính → commit một lần → thông báo; actionId/revision/decisionId trong bộ nhớ | Chặn double-click/callback lặp và popup lỗi thời; không xây event sourcing | ĐỀ XUẤT |
| A04 | Catalog tĩnh bằng ít ScriptableObject, chuyển sang plain data; tiền long và tỷ lệ phân số | Tách Unity khỏi luật, không sửa SO runtime; chưa tạo asset/class | ĐỀ XUẤT |
| A05 | Thời hạn theo ordinal lượt riêng; effect có sourcePlayerId; Banker debt có deadline riêng | Không đếm sai vì đôi; tuân thủ Q05/Q09 đã duyệt; cách biểu diễn vòng và effect khi source bị loại còn Q08/Q17 | ĐỀ XUẤT |
| A06 | Domain chốt đường đi/xúc xắc, presentation chạy sau commit; một continuation hợp lệ | Animation không cấp tiền/rút thẻ hoặc chuyển lượt; cần kiểm tra tích hợp Unity sau này | ĐỀ XUẤT |
| A07 | Phân chia bốn đầu mối cũ, chưa có tên | Đã được thay bằng phân công chính thức F01 trong TEAM_TASKS; không dùng lại để giao việc | SUPERSEDED |

## 6. Truy vết đầy đủ Q01–Q20

Cột câu hỏi giữ nguyên văn để đối chiếu lịch sử. Các câu hỏi APPROVED không còn cần xin quyết định lại. Các mục chưa có phê duyệt mới giữ **PENDING**; nếu Word đã trả lời một phần thì không hỏi lại phần ấy. Chỉ chặn nhánh thật sự còn thiếu.

| ID | Trạng thái | Câu hỏi gốc | Phạm vi hiện tại |
|---|---|---|---|
| Q01 | PENDING | Bản Word đã chốt nào cần đối chiếu? Nếu có khác biệt, nhóm xác nhận cách cập nhật nguồn nào? | Word hiện có tại docs/references/GAME_DESIGN_DOCUMENT.docx và đã đọc; không cần người dùng tìm/thêm lại. PENDING chỉ giữ truy vết câu hỏi cũ, không là phê duyệt gameplay. |
| Q02 | PENDING | Có bộ hữu hạn hay rút theo bảng xác suất? Bỏ thẻ/rút trùng đi đâu? Khi hết bài xử lý thế nào? Preview có thay đổi khi thẻ bị giữ/bỏ? | PENDING: phân phối, số bản sao, bỏ/rút lại thẻ; seed là chi tiết kỹ thuật đề xuất, không tự biến thành luật xác suất. |
| Q03 | PENDING | Số người tối thiểu? Có trùng nghề? Chọn người đầu và thứ tự lượt bằng cách nào? | PENDING: số tối thiểu, trùng nghề và người đầu. Kế hoạch test 2–4 người không tự chốt số tối thiểu của Setup. |
| Q04 | APPROVED | Active/Ultimate dùng ở pha nào? Lawyer Active có được phản ứng khi ngoài lượt? Khi nào được bán tự nguyện? Strategist Ultimate thay gieo hay thêm di chuyển; sau đó còn gieo/đôi không? | APPROVED=A: điểm dừng trước gieo/sau hoàn tất ô; không giữa animation/giao dịch; Lawyer phản ứng trước thuê của chính mình; bán tự nguyện ở điểm hợp lệ; thanh lý bắt buộc cả ngoài lượt; Strategist Ultimate thay một quyền gieo, không tạo đôi. |
| Q05 | APPROVED | Ultimate nhân đôi bắt đầu/kết thúc đúng mốc nào? Có phủ lượt đối thủ xen giữa không? | APPROVED=A: RentDouble có hiệu lực ngay, phủ đối thủ, hết ngay trước lượt riêng kế tiếp của chủ; không dùng phương án B từng được agent đề xuất. |
| Q06 | APPROVED | Khi đồng thời có Lawyer Active/Ultimate, thẻ giảm thuê/miễn thuế, Cúp điện và các modifier, hỏi/chọn và tiêu hao theo thứ tự nào? Thuê bằng 0 có được dùng thẻ/kỹ năng không? | APPROVED=A: hiệu ứng sẵn có trước; payable0 không tiêu thêm; payable dương chọn trả thường hoặc tối đa một biện pháp chủ động; Bảo vệ tự dùng vẫn theo Word. |
| Q07 | PENDING | Passive/Active/Ultimate Entrepreneur, điểm yếu Engineer, thẻ giảm thuê ảnh hưởng thuê biển thế nào? | PENDING — không có phê duyệt mới; Word xác định biển là BĐS, modifier thuê BĐS và thẻ Giảm thuê không giới hạn TP. Ca cụ thể D07 có oracle từ Word; không tự tạo ngoại lệ biển, không xin duyệt lại phần đã rõ. |
| Q08 | PENDING | Tập người phải hoàn tất vòng được chốt lúc nào? Người bị loại có được tính là đã hoàn tất hay bị gỡ khỏi tập chờ? | PENDING về hợp đồng biểu diễn tập người; Word đã nêu chỉ người chưa phá sản. D08 có oracle cho tình huống đã mô tả; thứ tự cuối lượt Q09 đã duyệt, không hỏi lại luật đó. |
| Q09 | APPROVED | Thứ tự trả nợ, hết hạn effect, lãi vòng, kiểm tra thắng và hết giờ ở cùng mốc? Nhiều người đạt thắng tức thời trong một chuỗi thì xử lý ra sao? | APPROVED=A: nghĩa vụ đến hạn → thanh lý/phá sản → thắng tức thời → mốc lượt/cooldown/effect → lãi Banker nếu hết vòng → chuyển lượt/kết quả hết giờ. Dừng ngay khi đã thắng. |
| Q10 | PENDING | Thẻ Vào tù/Đến dịch chuyển đi theo đường hay đặt vị trí trực tiếp; có tiền qua ô 0 không? Có cho chọn chính đích 24 nhưng không kích hoạt lại dịch vụ không? Chuỗi thẻ quay về 24 được xem là thao tác mới hay cùng chuỗi? | PENDING: đường đi/thưởng ô0 của thẻ và nhận diện chuỗi dịch chuyển; giữ cấm tự lặp cùng thao tác ở ô24. |
| Q11 | APPROVED | Nếu gieo đôi rồi dừng ô 8 hoặc rút Vào tù, lượt có kết thúc ngay không? Quyền tung thêm còn giữ không? | APPROVED=A: đôi rồi vào tù qua ô8/thẻ thì kết thúc lượt và mất gieo thêm; đôi để thoát tù đã có từ trước vẫn đi/tung thêm như Word. |
| Q12 | PENDING | Engineer Active/Ultimate có vượt điều kiện lần dừng sau ngay khi vừa mua Nhà3 không? Có hạn chế mục tiêu khác, kể cả Cọc/Hotel hoặc TP đang Cúp điện? | PENDING cho các tiểu mục trước đây chưa được phê duyệt riêng; cửa sổ Q04 đã rõ và Word có ngoại lệ Engineer từ xa. Không hỏi lại ngoại lệ Word đã quy định. |
| Q13 | APPROVED | Nếu giá giảm/phụ phí, vốn phân bổ, hoàn 50%, thuế có phần đồng lẻ, làm tròn thế nào và gán phần dư vào bậc nào? | APPROVED=A: floor thanh toán cuối, không float; chia vốn theo phần lẻ lớn nhất, bảo toàn tổng; hướng thứ tự phá hòa cụ thể còn P01. |
| Q14 | PENDING | Được trả phí để gia hạn/thay thế lễ hội đang có hay phải từ chối? | PENDING: gia hạn/thay thế festival tạm; giới hạn một effect/TP đã rõ. |
| Q15 | PENDING | Lễ hội tạm và hiệu ứng khác trên TP tồn tại, chuyển theo đất hay hủy khi đổi chủ/bán/phá? Festival tạm có mất khi phá sản không? | PENDING: vòng đời festival tạm khi mất/đổi quyền sở hữu. Cúp điện hủy khi đổi chủ/Hotel đã có luật. |
| Q16 | PENDING | Khi hết cooldown mà coupon cũ chưa dùng, có thể tích/ghi đè/thay thế không? | PENDING: Active Investor mới khi coupon cũ chưa dùng; không tự tăng tổng giảm ngoài luật. |
| Q17 | PENDING | Ai chọn thứ tự tài sản thanh lý? Có thể chấp nhận phá sản trước khi bán hết không? Người tạo hiệu ứng phá sản thì effect có hủy hay có mốc thay thế? | PENDING phần chọn tài sản/thời hạn effect khi source bị loại; thanh lý trước phá sản đã có luật, không cho tự bỏ nghĩa vụ còn thanh toán được. |
| Q18 | PENDING | Quà từ đối thủ/Tặng quà có được miễn trong lượt Ultimate không? Nợ Banker luôn không được miễn theo §9 | PENDING — chưa có phê duyệt mới riêng; Word §3.2g/§4.3d đủ giải quyết D18 về quà bắt buộc trong lượt miễn. Không hỏi lại ngoại lệ nợ Banker; không áp sang phí tù mới P02. |
| Q19 | PENDING | Thứ tự xử lý từng đối thủ? Nếu phá sản tạo thắng tức thời giữa chuỗi, có xử lý các người trả còn lại không? | PENDING về thứ tự xử lý từng đối thủ; không tự coi thứ tự ghế là luật ưu tiên thắng. Khi đã phát sinh thắng tức thời thì dừng ngay theo Q09/Q20, không hỏi lại phần này. |
| Q20 | APPROVED | Ranh giới “một hành động” gồm những lựa chọn tự chọn nào khi hết giờ (cưỡng chế sau thuê, dịch chuyển, lễ hội, chuỗi thẻ)? Popup chọn mua/bỏ qua và popup của AI có pause không? | APPROVED=B: hoàn tất lần dừng/chuỗi trực tiếp, kể cả popup tự chọn; không gieo mới hoặc dùng kỹ năng chủ động không liên quan; popup pause Human/AI; sau đó Q09 rồi kết quả, trừ thắng tức thời. |

## 7. Chi tiết mới chưa được phê duyệt

| ID | Trạng thái | Vấn đề và câu hỏi | Chỉ chặn |
|---|---|---|---|
| P01 | PENDING | Q13 yêu cầu thứ tự bậc cố định để phá hòa nhưng chưa nêu thứ tự cụ thể: ưu tiên từ Cọc lên Hotel hay ngược lại? Có thể đề xuất thứ tự kỹ thuật, không tự đánh dấu đã duyệt. | Vector chính xác khi các phần lẻ bằng nhau; vẫn kiểm tra tổng vốn và trường hợp không hòa. |
| P02 | PENDING | Phí200.000 bắt buộc sau lần thử thất bại ở lượt tù3 là nghĩa vụ mới. Lawyer Ultimate có miễn khoản này không, và nếu miễn thì có được thả cuối lượt không? Không tự suy ra một ngoại lệ hoặc điều kiện thả mới. | Tương tác miễn nghĩa vụ với nhánh tù3; các ca trả bình thường/thanh lý/phá sản vẫn làm được. |

Giai đoạn3 có kế hoạch test2–4 người; đó không tự phê duyệt số người tối thiểu của màn Setup (Q03). Chưa chọn trọng số/rút lại bộ bài hay chính sách tích coupon. Không tạo online/Save/Load/shop.

## 8. Cách ghi quyết định tiếp theo

Ghi ID, người xác nhận, ngày, nguyên văn/phạm vi câu trả lời, nguồn phê duyệt và hợp đồng/test chịu ảnh hưởng. Chỉ đổi PENDING khi có bằng chứng; đề xuất kỹ thuật không thay luật. Shared contract phải được A và các gói sử dụng thống nhất trước tích hợp theo [TEAM_TASKS](TEAM_TASKS.md). Mọi test ở đây và TEST_CASES mới là đặc tả, chưa RUN/PASS.
