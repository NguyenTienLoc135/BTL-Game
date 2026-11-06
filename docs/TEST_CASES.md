# Đặc tả test — Cờ Tỷ Phú RPG

Ngày cập nhật: 2026-11-06. **Chưa có test code, chưa chạy test hoặc Unity; không có trạng thái PASS.** Nguồn: quyết định người dùng mới đã phê duyệt → [Word đã đọc](references/GAME_DESIGN_DOCUMENT.docx) → [GAME_RULES](GAME_RULES.md) → các tài liệu kỹ thuật. Bảy Q và tù3 lượt cập nhật theo [DECISIONS](DECISIONS.md).

## 1. Cách đọc và fixture

- ID L/S/C giữ từ bản trước; D04/D05/D06/D09/D11/D13/D20 nay có expected result APPROVED dù vẫn giữ ID D để truy vết. F là ca bổ sung Q; J là ca tù3 lượt. Tất cả chỉ là đặc tả, chưa thực thi. Các số là kết quả áp dụng nguồn có hiệu lực.
- A/B/C/D là PlayerId trong fixture domain, không xác nhận số người tối thiểu hoặc quyền trùng nghề trong màn Setup (Q03).
- Nếu test không nói khác: A là Nhà Chiến Lược, B là Banker, còn hoạt động; không có hiệu ứng/thẻ/độc quyền; TP đang xét không thuộc ba festival cố định; không kết thúc vòng chung trong hành động đó. Mỗi test khởi tạo riêng, không nối trạng thái từ dòng trước.
- Không bỏ Passive/Weakness của nghề. Test nghề khác ghi rõ role được thay; những người có mặt trong fixture dùng role khác nhau. Nhà Chiến Lược/Banker được dùng ở test kinh tế vì không sửa giá/thuê ngay thời điểm đó.
- Vốn [Cọc, Nhà1, Nhà2, Nhà3, Hotel] tính bằng đồng. Giá niêm yết tách vốn thực; người trả và nhận ghi tiền trước/sau khi cần.
- RNG dùng kết quả được cấp trước trong fixture; đây là cách kiểm thử, không chốt thuật toán random/deck của game.
- Chữ “từ chối” nghĩa là không đổi tiền/chủ/cấp/thẻ/skill nếu hành động chưa thực hiện hợp lệ. “Không hiệu lực” của thẻ không tự quyết định thẻ đi đâu trong deck (Q02).
- Chỉ những test trong §10 có trạng thái **PENDING/BỊ CHẶN** mới chờ quyết định; không suy ra trạng thái từ tiền tố ID. Không tự đóng các Q/P chưa duyệt; trước khi hỏi phải tách phần Word đã quy định.

## 2. Bàn cờ, lượt, xúc xắc, xuất phát

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| L01 / §1,3 | MatchSetup bốn người, bốn nghề khác nhau, chọn thời gian 30 phút; catalog đúng | Khởi tạo phần dữ liệu trận | Mỗi người 300.000, ô 0; đúng 3 CityId khác nhau làm festival; chưa tự chọn người đầu trong test |
| L02 / §3 | Catalog 32 ô theo bảng nguồn | Validate ID/loại/quốc gia | Đúng 20 TP, 4 biển tại 4/14/18/25, 8 chức năng, 8 nước; không TP17 giả; Melbourne/Sydney thuộc Úc |
| L03 / §2 | A ô 1; tiền không đổi do các ô đi đến đều bỏ mua; đang lượt riêng k | Tung (1,1)→ô3, (2,2)→ô7, (3,3)→ô13; xử lý từng ô | Cả ba lần đều cho tung tiếp; vẫn lượt k; đi ngang ô8 không bị giam; không áp “ba đôi vào tù” |
| L04 / §2 | Tiếp fixture riêng tương đương A ô13 trong lượt k sau nhiều đôi | Tung (1,2)→ô16, bỏ tổ chức lễ hội | Không có quyền tung thêm từ kết quả này; hoàn tất đúng một lượt riêng, không phải bốn |
| L05 / §2,9 | A đã hoàn tất4 lượt, đang lượt5; Cúp điện do A tạo còn2 lượt tiếp theo, Active còn khóa3 lượt | Giải quyết hai lần đôi và animation, chưa kết thúc lượt5 | completedPersonalTurnCount vẫn4; cooldown vẫn khóa3 lượt, effect còn2; không rút ngắn vì tung thêm |
| L06 / §2 | A ô30, cash 300.000 | Di chuyển (1,1), dừng đúng ô0 | Cash 400.000; thưởng một lần 100.000, không cộng lần thứ hai vì “dừng ô0” |
| L07 / §2 | A ô31, cash 300.000; ô2 chưa chủ | Tung (1,2), đến ô2, bỏ mua | Cash 400.000; vị trí2; chỉ một lần qua0 |
| L08 / §1–2 | A vừa khởi tạo ô0, cash300.000 | Chỉ hiển thị bàn cờ, chưa di chuyển | Cash vẫn300.000; không phát thưởng từ sự kiện view/khởi tạo |
| L09 / §2 | A ô1, cash300.000; ô3 chưa chủ | Di chuyển2, bỏ mua | Vị trí3; cash300.000; không có thưởng xuất phát |

## 3. Giá mua, thuê và độc quyền

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| L10 / §4.1 | A cash300.000; Hà Nội chưa chủ; mỗi lựa chọn là một fixture riêng | Mua Cọc / Nhà1 / Nhà2 / Nhà3 | Lần lượt trả40.000/60.000/80.000/100.000; dư260.000/240.000/220.000/200.000; đúng cấp được chọn |
| L11 / §4.1 | A cash1.000.000; Hà Nội chưa chủ | Chọn mua Hotel lần đầu | Từ chối; Hà Nội vẫn chưa chủ, tiền không đổi |
| L12 / §6.3 | A cash39.999; Hà Nội chưa chủ | Mua Cọc giá40.000 | Từ chối; không tạo quy trình thanh lý bắt buộc cho mua tùy chọn |
| L13 / §4.2 | Hà Nội của B, từng fixture có cấp Cọc/Nhà1/Nhà2/Nhà3/Hotel | Tính thuê gốc | 7.500 / 12.500 / 20.000 / 30.000 / 40.000 |
| L14 / §4.2 | Sydney của B, từng fixture có năm cấp tương ứng | Tính thuê gốc | 60.000 / 100.000 / 160.000 / 240.000 / 320.000 |
| L15 / §3–4 | B sở hữu cả13 và15 (Pháp), Hà Nội không liên quan; Paris Nhà3 | Tính thuê Paris | Thuê gốc111.000; +20% độc quyền →133.200 |
| L16 / §3–4 | B chỉ sở hữu Paris Nhà3, Lyon chưa chủ | Tính thuê Paris | 111.000; không +20% |
| L17 / §4.2 | Hà Nội Nhà3 của B, là festival cố định; không độc quyền | Tính thuê | 45.000 |
| L18 / §4.2 | Hà Nội Nhà3 của B: độc quyền Việt Nam, festival cố định và festival tạm còn hiệu lực | Tính thuê | 30.000 × (1+20%+50%+50%) =66.000; không nhân ba buff nối tiếp |
| L19 / §4.2 | Hà Nội Nhà1 của B; B độc quyền Việt Nam và TP là festival cố định | Tính thuê | 12.500 ×1,7 =21.250; không làm tròn trung gian |
| L20 / §5 | B có lần lượt 1/2/3 biển ở ba fixture, không modifier nghề | A dừng trên một biển của B | Thuê mỗi biển lần lượt50.000/100.000/150.000 |
| L21 / §5 | A cash1.000.000; từng biển chưa chủ ở fixture riêng | Mua biển4/14/18/25 | Trả100.000/130.000/160.000/190.000; vốn thực đúng số trả, không có cấp nhà |
| L22 / §5 | A sở hữu biển4 | Yêu cầu nâng biển | Từ chối; tiền/quyền sở hữu/vốn không đổi |

## 4. Cưỡng chế, nâng cấp, vốn và phá tài sản

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| L23 / §6.1 | A cash300.000 dừng Hà Nội Nhà3 của B; B cash0, vốn cũ80.000 | Trả thuê30.000 rồi xác nhận cưỡng chế200.000 | A còn70.000; B230.000; A giữ Nhà3; vốn mới[80.000,40.000,40.000,40.000,0], không kế thừa80.000 cũ |
| L24 / §6.1 | A cash210.000 dừng Hà Nội Nhà3 của B, Bcash0 | Trả thuê30.000; thử cưỡng chế200.000 | A180.000, B30.000; cưỡng chế bị từ chối, B vẫn chủ; không hoàn lại thuê đã trả |
| L25 / §4–6 | A cash1.000.000; mục tiêu là Hotel Hà Nội hoặc biển4 của B ở hai fixture | Thử cưỡng chế thường | Không cho mua; không đổi chủ/vốn. Nếu là lượt dừng thật, nghĩa vụ thuê riêng vẫn phải xử lý theo luật |
| L26 / §4.1 | Hà Nội Nhà1 của A, vốn[40.000,20.000,0,0,0], A cash100.000; lần dừng hợp lệ | Nâng thường thẳng Nhà3 | Trả40.000; cash60.000; vốn[40.000,20.000,20.000,20.000,0]; một thao tác nâng |
| L27 / §4.1 | A vừa mua Hà Nội Nhà3 trong cùng lần đến | Yêu cầu nâng thường lên Hotel ngay | Từ chối; không trừ thêm tiền |
| L28 / §4.1 | Hà Nội đã Nhà3 từ lần dừng trước, vốn100.000; A cash50.000, nay dừng lại | Nâng Hotel thường | Trả20.000, cash30.000; Hotel, tổng vốn120.000 |
| L29 / §4.1 | A có Nhà2 ở Hà Nội | Yêu cầu hạ cấp tự nguyện | Từ chối; cấp/vốn/tiền không đổi |
| L30 / §6.2,8 | Hà Nội Nhà3 của B vốn[40.000,20.000,20.000,20.000,0], Bcash10.000, không bảo vệ | A dùng Động đất vào Hà Nội | Nhà2; vốn còn80.000; Bcash vẫn10.000; không hoàn20.000 |
| L31 / §6.2 | B có Hà Nội Nhà2, vốn80.000, cash10.000 | Bán ngân hàng | Bcash50.000; TP chưa chủ, cấp null, vốn0 |
| L32 / §6.2,8 | Hà Nội Nhà2 của B, vốn80.000, Bcash10.000; không bảo vệ | A dùng Phá tài sản | Bcash90.000; TP chưa chủ/cấp null/vốn0 |
| L33 / §4–6,8 | Mục tiêu lần lượt: Hà Nội Cọc, Hà Nội Hotel, biển4; không bảo vệ | Dùng Động đất | Cả ba mục tiêu không hợp lệ; không hạ cấp/hoàn tiền |
| L34 / §4–6,8 | Mục tiêu lần lượt: Hà Nội Hotel, biển4 | Dùng Phá tài sản | Không hiệu lực trên mục tiêu; không bồi hoàn |
| L35 / §6.2 | B là Investor đã mua Hà Nội Nhà3 với tổng giảm25%, thực chi75.000, vốn[30.000,15.000,15.000,15.000,0] | Động đất rồi bán ngân hàng | Sau động đất vốn60.000; bán nhận30.000 |
| L36 / §5–6 | A là Investor, sở hữu biển4 mua từ ngân hàng với Passive, vốn thực90.000, cash10.000 | Bán ngân hàng tại điểm dừng hợp lệ Q04 | Nhận45.000, cash55.000; biển về chưa chủ, vốn0 |

## 5. Thuế, nghĩa vụ, thanh lý và phá sản

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| L37 / §7.1 | A cash100.000, sở hữu Hà Nội Nhà3 giá100.000 và biển4 giá100.000 | Phát sinh thuế ô30 | Nghĩa vụ cố định60.000; sau trả còn40.000 |
| L38 / §7.1 | A là Lawyer; cùng tài sản/cash như L37, không thẻ miễn | Rút Nộp phạt thuế | Thuế gốc60.000 giảm25% →45.000; cash55.000 |
| L39 / §7.1 | A là Lawyer; cùng tài sản/cash như L37 | Chịu thuế ô30 | Trả60.000, cash40.000; Passive không giảm |
| L40 / §6–7 | A cash10.000, Tokyo Nhà3 giá/vốn200.000; không miễn/giảm | Chốt thuế42.000; bán Tokyo nhận100.000; trả thuế | Thuế vẫn42.000; A còn68.000, Tokyo về ngân hàng; không tính lại thuế sau bán |
| L41 / §6.3 | A cash10.000, không tài sản; nghĩa vụ Tặng quà200.000 cho B; Bcash300.000 | Giải quyết thiếu tiền và phá sản | Chuyển10.000 thực còn lại; B310.000; A0/bị loại; thiếu190.000 không truy thu |
| L42 / §6.3 | Đang áp dụng kết quả phá sản hợp lệ cho A; snapshot trước bước thu hồi còn Hotel Hà Nội và biển4 owner=A | Hoàn tất thu hồi tài sản phá sản | Cả Hotel và biển về ngân hàng; không chuyển cho chủ nợ; A không nhận lượt mới |
| L43 / §6.3 | A cash99.999, có tài sản; đang tại ô24 | Chọn dịch chuyển100.000 | Không trừ tiền/di chuyển; không buộc thanh lý để trả phí tự chọn |
| L44 / §6.3 | A cash199.999, có TP của mình | Chọn lễ hội200.000 | Không trừ tiền/tạo effect, không ép thanh lý |
| L45 / §6.3 | A đủ tiền nghĩa vụ thuê30.000, actionId đã thanh toán | Gửi lại cùng quyết định thanh toán | Yêu cầu kỹ thuật đề xuất A03: không thanh toán lần hai; test tích hợp sau khi A03 được chấp thuận |

## 6. Nhà tù, dịch chuyển và lễ hội

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| L46 / §7.2, Q11 | A ô5, chưa tù | Tung(1,2) đếnô8 | A ô8, isJailed=true, jailTurnsStarted=0; kết thúc lượt, không tính lượt bị bắt là lượt tù1 |
| L47 / §7.2 | A ô7, chưa tù; ô10 chưa chủ | Tung (1,2) đếnô10, bỏ mua | Không bị giam do đi ngangô8 |
| L48 / §7.2, JAIL-03 | A bắt đầu lượt tù thứ1, không tiền/thẻ | Chọn tung(1,2) | Vẫn tù ô8, jailTurnsStarted=1; kết thúc lượt; chưa phát sinh phí bắt buộc200.000 |
| L49 / §7.2 | A bị giam; ô10 chưa chủ | Chọn tung (1,1), đếnô10, bỏ mua | Ra tù bắt buộc, dùng chính tổng2 để di chuyển; được tung thêm trong cùng lượt |
| L50 / §7.2 | A bị giam, cash300.000 | Trả200.000 rồi tung (1,2) | Cash100.000; ra tù, đếnô11; xử lýô11 bình thường |
| L51 / §7.2,8 | A bị giam, có thẻ Ra tù, cash0 | Dùng thẻ rồi tung (1,2) | Bỏ đúng thẻ Ra tù, không trừ tiền; ra tù, đếnô11 |
| L52 / §7.2 | B bị giam, sở hữu Hà Nội Nhà3; A cash100.000, Bcash0 | A trả thuê khi dừng Hà Nội | A70.000, B30.000; bị giam không làm mất quyền thu thuê |
| L53 / §7.3 | A ô24, cash200.000; chọn đường đi hợp lệ qua25…31→0→1; Hà Nội chưa chủ | Trả100.000 dịch chuyển đếnô1, bỏ mua | Trừ100.000 rồi thưởng100.000 qua0, cash200.000; vị trí1 và kích hoạt lựa chọn mua |
| L54 / §7.3 | A ô24, cash200.000; biển25 chưa chủ | Trả100.000 dịch chuyển đếnô25, bỏ mua | Cash100.000; không qua0; xử lý ô biển25 |
| L55 / §7.3,8 | A đã đếnô24 từ thẻ; cash200.000 | Chọn dùng chức năng dịch chuyển đếnô25, bỏ mua | Dịch vụ vẫn thu100.000; không miễn phí vì đến từ thẻ |
| L56 / §7.4 | A cash300.000 tạiô16, sở hữu Hà Nội; chưa có festival tạm | Chọn Hà Nội và xác nhận lễ hội | Cash100.000; Hà Nội có +50% trong2 lượt riêng tiếp theo của A; không áp biển |
| L57 / §4.2,9 | A tạo festival tạm ở lượt k; đang ở phần cuối lượt k, sau đó có nhiều lần đôi; tiếp tục tới cuối k+1 và k+2 | Theo dõi mốc hoàn tất lượt | Cuối k không tính một lượt tiếp theo; đôi không rút thời hạn; hiệu lực còn qua khoảng đối thủ xen giữa và hết sau khi hoàn tất k+2 |
| L58 / §7.4 | A cash300.000, chỉ sở hữu biển4; không TP | Chọn biển4 cho lễ hội | Mục tiêu không hợp lệ; không trừ200.000 |

## 7. Đủ 14 thẻ sự kiện

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| C01 / §8, thẻ1 | A cash100.000 | Giải quyết Tiền thưởng | Cash400.000, không lấy từ đối thủ |
| C02 / §8, thẻ2 | Fixture L30 | Giải quyết Động đất | Kết quả L30; chỉ một bậc bị mất |
| C03 / §8, thẻ3 | A có Miễn thuế, cash100.000, không tài sản; nghĩa vụ thuế20.000; không miễn/giảm khác | Chủ động dùng thẻ | Không trả thuế; cash100.000; thẻ bị dùng một lần |
| C04 / §8, thẻ4 | A có Giảm thuê; thuê Hà Nội Nhà3 của B là30.000; A100.000, B0 | Chủ động dùng thẻ và thanh toán | A85.000, B15.000; thẻ hết; không giảm tiền cưỡng chế |
| C05 / §8, thẻ5 | B có Bảo vệ; Hà Nội Nhà2 vốn80.000 | A Phá tài sản vào Hà Nội | Tự dùng một Bảo vệ; TP/cấp/vốn giữ nguyên; không hoàn80.000 |
| C06 / §8, thẻ6 | Fixture L38 | Rút Nộp phạt thuế | Thuế45.000 cho Lawyer; không coi là tiền thuê |
| C07 / §8, thẻ7 | A không bị giam | Rút Vào nhà tù | Vị trí8 và isJailed=true; tiền qua0 chưa có kỳ vọng, xem D10 |
| C08 / §8, thẻ8 | A chưa có Ra tù, không bị giam | Rút Ra tù | Thêm một thẻ vào set giữ; chưa tự sử dụng |
| C09 / §8, thẻ9 | Fixture L32 | Giải quyết Phá tài sản | Hoàn100% vốn80.000 từ ngân hàng, TP về chưa chủ |
| C10 / §8, thẻ10 | A100.000; B200.000; C300.000; Dđã phá sản; không miễn trừ, đủ tiền | A rút Quà từ đối thủ | B100.000, C200.000, A300.000; D không phải trả |
| C11 / §8, thẻ11 | A tại một ô thẻ; không hiệu ứng liên quan | Rút Đến ô dịch chuyển | Vị trí24; có lựa chọn trả100.000 để dùng dịch vụ; tiền qua0 trên chặng thẻ chờ D10 |
| C12 / §8, thẻ12 | A300.000, sở hữu Hà Nội, chưa festival tạm | Rút Tổ chức lễ hội; chọn trả phí và Hà Nội | A100.000; effect+50%/2 lượt riêng; cùng luật ô16 |
| C13 / §8, thẻ13 | A dùng thẻ ở lượt k; B có Hà Nội Nhà3 | Chọn Hà Nội; đi qua nhiều lần tung đôi, hoàn tất k+1,k+2,k+3 | Không thu thuê khi effect còn hiệu lực; đôi không đếm; hết sau cuối k+3 của A, không theo lượt B |
| C14 / §8, thẻ14 | A300.000, B100.000, không miễn trừ | A chọn tặng B200.000 | A100.000, B300.000 |
| C15 / §8 | A giữ đủ {Miễn thuế, Giảm thuê, Bảo vệ, Ra tù} | Rút thêm Miễn thuế | Vẫn đúng4 thẻ, mỗi loại1; bỏ lá mới; chưa quyết định deck destination |
| C16 / §8 | B giữ Bảo vệ, tài sản là Hotel Hà Nội hoặc biển4 | A thử Động đất/Phá tài sản/Cúp điện vào tài sản miễn nhiễm | Không có tác động; B vẫn giữ Bảo vệ |
| C17 / §8 | B có Hà Nội Nhà1 và Bảo vệ | A dùng Động đất hợp lệ | Thẻ Bảo vệ tự bị dùng; Nhà1/vốn giữ nguyên |
| C18 / §8 | B có Hà Nội Nhà3 và Bảo vệ | A dùng Cúp điện hợp lệ | Bảo vệ bị dùng; không tạo effect mất thuê |
| C19 / §8 | Cúp điện đang gắn Hà Nội Nhà3 của B | A cưỡng chế hợp lệ đổi chủ, hoặc B nâng Hotel ở lần dừng hợp lệ, hai fixture riêng | Cúp điện hủy ngay, không chờ hết3 lượt |
| C20 / §8 | Tất cả đối thủ chỉ có Hotel/biển, không TP hợp lệ | A rút Động đất hoặc Phá tài sản | Không hiệu lực; không tự đổi mục tiêu sang Hotel/biển |

## 8. Sáu nghề nghiệp và cooldown

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| S01 / §9 Investor | A Investor300.000; Hà Nội chưa chủ; không Active | Mua Nhà3 | Trả90.000, cash210.000; vốn[36.000,18.000,18.000,18.000,0] |
| S02 / §9 Investor | A Investor300.000, chưa coupon/khóa; Hà Nội chưa chủ | Dùng Active hợp lệ, mua Nhà3 | Tổng giảm25%, trả75.000; cash225.000; vốn[30.000,15.000,15.000,15.000,0]; coupon dùng cho lần mua này |
| S03 / §9 Investor | A Investor có Hà Nội Nhà1; cash100.000 | Nâng thường Nhà3 | Chênh lệch40.000 +10%=44.000; cash56.000 |
| S04 / §9 Investor | A Investor300.000, Ultimate chưa dùng; Hà Nội Nhà3 của B, vốn cũ100.000 | Ultimate cưỡng chế từ xa | Trả150.000 cho B, không thuê; A150.000; cấp Nhà3, vốn mới[60.000,30.000,30.000,30.000,0]; Ultimate đã dùng |
| S05 / §9 Investor | A Investor có đủ tiền; mục tiêu lần lượt Hotel/biển/chưa chủ | Thử Ultimate cưỡng chế | Không hợp lệ, không dùng Ultimate, không đổi tiền/chủ |
| S06 / §9 Entrepreneur | B Entrepreneur, Hà Nội Nhà3, không modifier khác; A100.000, B0 | A trả thuê | Thuê33.000; A67.000/B33.000 |
| S07 / §9 Entrepreneur | B Entrepreneur đã có Active hợp lệ còn hiệu lực; Hà Nội Nhà3 không festival/độc quyền | Tính thuê | 30.000×(1+10%+30%)=42.000 |
| S08 / §9 Entrepreneur | B Entrepreneur; Hà Nội Nhà3 có độc quyền, hai loại festival; Active và Ultimate đang được xác nhận là có hiệu lực trong fixture | Tính thuê ở đúng thời điểm đó | 30.000×(1+20%+50%+50%+10%+30%)×2=156.000; mốc bật/tắt Ultimate theo D05 |
| S09 / §9 Entrepreneur | A Entrepreneur300.000, Hà Nội chưa chủ | Mua Nhà3 | Giá+5%=105.000; cash195.000; vốn thực105.000 |
| S10 / §9 Engineer | A Engineer100.000, Hà Nội Nhà1 | Nâng thường Nhà3 | Trả40.000×85%=34.000; cash66.000; vốn hai bậc mới17.000 mỗi bậc |
| S11 / §9 Engineer | A Engineer có Hà Nội Nhà3 từ lần đến trước, vốn100.000; Active sẵn sàng | Active nâng từ xa một cấp | Hotel, vốn Hotel0; tổng vốn vẫn100.000; không trừ tiền; Active vào cooldown |
| S12 / §9 Engineer | A Engineer100.000, Hà Nội Cọc vốn40.000 từ lần đến trước; Ultimate sẵn sàng | Ultimate nâng thẳng Hotel từ xa | Chênh lệch120.000−40.000=80.000, trả50%=40.000, không nhân85%; cash60.000, tổng vốn80.000 |
| S13 / §9 Engineer | B Engineer sở hữu Hà Nội Nhà3, không modifier khác | Tính thuê | 30.000×90%=27.000 |
| S14 / §9 Lawyer | A Lawyer100.000, thuê30.000, Active hợp lệ trước thanh toán, không thẻ giảm/miễn khác | Kích hoạt hủy một lần thuê | Cash A/B không đổi vì thuê đó bị hủy; Active cooldown6 |
| S15 / §9 Lawyer | A Lawyer, Ultimate đã kích hoạt hợp lệ trong lượt hiện tại; thuế ô30 chốt20.000; không effect khác | Xử lý nghĩa vụ thuế | Khoản thuế được hủy trong lượt đó; Ultimate đã dùng |
| S16 / §9 Lawyer | Fixture đơn vị của bộ miễn nghĩa vụ: payer A Lawyer200.000, Ultimate còn hiệu lực; đưa vào nghĩa vụ loại BankerDebt110.000 | Đánh giá miễn trừ và thanh toán nghĩa vụ được cấp | Không miễn BankerDebt; nếu thanh toán còn90.000. Đây là kiểm tra ngoại lệ ghi trong luật, không khẳng định Lawyer có thể tự vay bằng Active của Banker |
| S17 / §9 Banker | B Banker cash100.099, vòng chung hoàn tất bình thường, không phá sản/nghĩa vụ ở cùng mốc | Áp Passive cuối vòng | Lãi floor(1.000,99)=1.000; cash101.099 |
| S18 / §9 Banker | B Banker cash0, vòng chung hoàn tất bình thường | Áp Passive | Lãi0; không tạo tiền từ số dư không dương |
| S19 / §9 Banker | B Banker100.000, Active sẵn sàng, dùng ở lượt riêng k | Vay thành công | Cash200.000; nợ110.000 hạn cuối k+5; cooldown7 |
| S20 / §9 Banker | Nợ từ lượt k, Bcash200.000; không mốc vòng chung/miễn trừ | Hoàn tất k+4 rồi k+5 | Cuối k+4 chưa trả; cuối k+5 trả110.000, còn90.000; đôi không làm đến hạn sớm |
| S21 / §9 Banker | B Banker100.000, Ultimate chưa dùng | Dùng Ultimate thành công | Cash300.000, Ultimate đã dùng; lần dùng thứ hai bị từ chối |
| S22 / §9 Strategist | Chính sách rút fixture cấp thứ tự [Tiền thưởng, Động đất,…]; A Strategist | Preview nhiều lần rồi rút | Preview luôn Tiền thưởng; không tiến cursor; lần rút thật đúng Tiền thưởng; không suy ra thuật toán deck sản phẩm |
| S23 / §9 Strategist | A Strategist ô1; đã gieo(3,3), chưa di chuyển; Active sẵn sàng; kết quả gieo lại(1,4) | Dùng Active gieo lại | Dùng tổng5 đếnô6, không tổng6 cũ; phải chấp nhận kết quả mới, không chọn lại kết quả cũ |
| S24 / §9, Q04 | A Strategist ô31, có quyền gieo tiếp theo, Ultimate sẵn sàng, cash100.000; ô2 chưa chủ | Ultimate chọnô2 phía trước3 bước, bỏ mua | Đếnô2, nhận100.000 qua0; Ultimate đã dùng, quyền gieo bị thay đã tiêu; không tự có gieo thêm vì Ultimate |
| S25 / §9 Strategist | A Strategist ô1, Ultimate sẵn sàng | Chọn ô8 cách7 bước | Từ chối; Ultimate chưa tiêu hao |
| S26 / §9 chung | Mỗi role ở fixture riêng dùng Active thành công tại lượt1, N=5/6/7/6/7/5 | Hoàn tất lượt1 và N−1 lượt tiếp theo, rồi lượt thứ N tiếp theo | Cuối lượt1 không tính; trước hoàn tất lượt1+N vẫn khóa thời gian; sau đó hết khóa thời gian, cửa sổ dùng theo Q04 |
| S27 / §9 chung | Engineer không có TP hợp lệ; Active sẵn sàng | Thử nâng miễn phí | Từ chối; cooldown không khởi động; tiền/cấp không đổi |
| S28 / §9 chung | Một Ultimate đã dùng thành công | Gửi yêu cầu dùng lại Ultimate | Từ chối; không tạo hiệu quả lần hai |
| S29 / §9 Investor | A Investor300.000, biển4 chưa chủ; hai fixture không coupon / có coupon Active15% đã kích hoạt hợp lệ | Mua biển4 từ ngân hàng | Không coupon trả90.000, còn210.000; có coupon trả75.000, còn225.000 và dùng coupon. Passive/Active áp giá mua BĐS ngân hàng, khác câu hỏi thuê biển Q07 |

## 9. Thắng, hết giờ và presentation

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| L59 / §3,10 | A có toàn Pháp(13,15), Ý(17,19), Anh chỉ26; đủ tiền mua27, chưa thắng | Mua27 hợp lệ | A thắng tức thời với3 nước, chỉ6 TP; không đòi9 TP |
| L60 / §5,10 | A có biển4/14/18, chưa25, cash500.000 | Mua biển25 giá190.000 | A còn310.000 và thắng ngay; không cần tính thuê cho trạng thái4 biển |
| L61 / §6,10 | A/B còn chơi, C/D đã phá sản; A không có tài sản, cash0 và nghĩa vụ không trả được | A phá sản sau xử lý nghĩa vụ | B thắng vì là người chưa phá sản duy nhất |
| L62 / §2,10 | Hết giờ; mọi hành động đang dở đã hoàn tất; A cash200.000/B100.000; không ai đạt thắng tức thời | Xếp hạng | A thắng dù B có giá niêm yết BĐS cao hơn |
| L63 / §2,10 | Hết giờ an toàn; A/B cash200.000; A có Hà Nội Nhà3 giá100.000, B có Sydney Cọc giá320.000; không thắng tức thời | Xếp hạng | B thắng bằng giá niêm yết, không so vốn thực hoặc tổng tiền+đất ở bước đầu |
| L64 / §2,10 | Hết giờ an toàn; A/B cash200.000, cùng tổng giá niêm yết100.000; không thắng tức thời | Xếp hạng | A và B đồng thắng |
| L65 / §2,10 | Clock còn1 giây, A cash0 ô30 đã chốt(1,1), animation chưa xong; Bcash200.000; ô0 không nghĩa vụ | Clock hết rồi hoàn tất hành động | A đến0 và nhận100.000; không bắt đầu tung thêm dù ra đôi; sau xử lý an toàn B thắng |
| L66 / §2, Q20 | Hai fixture Human/AI riêng, clock60 giây, popup chờ quyết định đang mở | Chờ10 giây rồi đóng popup, đo thêm5 giây không popup | Cả hai: lúc chờ vẫn60; sau5 giây tiếp tục còn55 |
| L67 / §10 | Chế độ vô hạn, chưa có3 nước/4 biển/chỉ còn1 người | Thời gian thực trôi qua60 phút | Không kết thúc vì hết giờ |
| L68 / §6,10 | A mua tài sản cuối để đủ3 nước, hành động commit xong; chưa kết thúc lượt | Kiểm tra kết quả sau giao dịch | Kết thúc ngay, không đợi người chơi bấm kết thúc lượt |
| L69 / AGENTS §4, đề xuất A03/A06 | Một movementId đã cấp100.000 qua0, view còn animation | Callback hoàn tất đến hai lần | Yêu cầu kỹ thuật đề xuất: chỉ tiếp tục xử lý ô một lần, không cộng thêm100.000 hoặc sinh lượt mới |
| L70 / AGENTS §4, đề xuất A03 | Popup mua giá40.000 tại revision r; trước submit trạng thái đổi và revision r+1 | Submit quyết định cũ | Yêu cầu kỹ thuật đề xuất: từ chối/làm mới quyết định; không trừ tiền theo snapshot cũ |
| L71 / §11 | Người thật và AI có cùng trạng thái/role/tiền/pha | Gửi cùng yêu cầu cưỡng chế Hotel | Cả hai bị từ chối giống nhau; AI không bypass validator |

L45, L69 và L70 là yêu cầu kiểm chứng kỹ thuật **đề xuất**, không tuyên bố A03/A06 đã được nhóm duyệt. Các test luật còn lại là oracle từ nguồn, không chứng minh đã có implementation.

## 9a. Expected result của bảy quyết định đã được phê duyệt

Giữ ID gốc để truy vết, **không còn bị chặn bởi bảy câu hỏi này**. Những tương tác khác vẫn theo PENDING riêng; đây chưa phải test đã RUN/PASS.

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| D04 / Q04 APPROVED-A | A Strategist ô1 đã xử lý xong ô sau đôi, còn một quyền gieo thêm; Ultimate sẵn sàng; ô3 chưa chủ | Ultimate chọnô3 cách2 bước, bỏ mua | Đến3, Ultimate dùng một lần, quyền gieo thêm bị thay; không còn quyền gieo vì không có kết quả đôi mới |
| D05 / Q05 APPROVED-A | A Entrepreneur kích hoạt Ultimate ở lượt k, có Hà Nội Nhà3 không modifier khác; A0/B100.000 trước thuê | B dừng Hà Nội trong lượt xen giữa; sau đó A bắt đầu lượt k+1 | B trả66.000, còn34.000; A66.000. Ngay trước k+1 effect mất, query thuê còn33.000; đôi không đổi mốc |
| D06 / Q06 APPROVED-A | A Lawyer100.000, B0; thuê30.000; Ultimate A đã có hiệu lực, thẻ Giảm thuê đang giữ, Active sẵn sàng | Giải quyết nghĩa vụ và thử dùng thêm thẻ/Active | Payable0; không chuyển tiền, không tiêu thẻ, không khởi động cooldown Active |
| D09 / Q09 APPROVED-A | B Banker111.000 nợ110.000 đến hạn; B kết thúc lượt làm hoàn tất vòng; A1.000/C500 còn chơi; hết giờ, không BĐS/điều kiện thắng khác | Đóng lượt theo Q09 | Trả110.000 còn1.000; cập nhật mốc; lãi10 thành1.010; sau đó B thắng hết giờ. Không cộng lãi trên111.000 trước nợ |
| D11 / Q11 APPROVED-A | A ô6, chưa bị giam, đang lượt k | Tung(1,1) đếnô8 | Vào tù, jailTurnsStarted=0, hủy gieo thêm và kết thúc lượt k; Q09 vẫn chạy nếu trận chưa kết thúc |
| D13 / Q13 APPROVED-A | A cash100.001, Hà Nội Nhà3 giá100.000; không miễn/giảm | Thuế ô30, giá trị chính xác40.000,2 | Chốt phải trả40.000; A còn60.001; không tính lại sau thanh lý |
| D20 / Q20 APPROVED-B | A270.000/B30.000 sau trả thuê Hà Nội Nhà3 của B; còn lựa chọn cưỡng chế200.000, clock vừa hết, không kết vòng | A chọn cưỡng chế trong lần dừng đang dở | Vẫn cho mua: A70.000/B230.000, chủ A, vốn[80.000,40.000,40.000,40.000,0]; không gieo mới; chạy Q09 rồi B thắng hết giờ |

## 9b. Bổ sung kiểm chứng Q04/Q05/Q06/Q09/Q13/Q20

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| F01 / Q04 | A Investor đang điểm dừng sau khi xử lý hoàn tất ô; Active sẵn sàng, chưa có coupon | Dùng Active hợp lệ | Có coupon15% cho mua ngân hàng tiếp theo và cooldown5; không cần đợi lượt mới để mở điểm hành động này |
| F02 / Q04 | A Banker Ultimate chưa dùng; animation di chuyển đang chạy | Bấm Ultimate nhận200.000 | Từ chối do sai cửa sổ; tiền và ultimateUsed không đổi |
| F03 / Q04 | A có Hà Nội Nhà1 vốn60.000, cash10.000; đầu lượt trước gieo, không đứng trên Hà Nội | Bán tự nguyện qua bảng tài sản | Nhận30.000, cash40.000, TP về ngân hàng; không yêu cầu dẫm đúng ô mới bán |
| F04 / Q04, §8 | B đang có lượt và rút Quà từ đối thủ; A là người trả, cash10.000, Tokyo Nhà3 vốn200.000 | A thanh lý Tokyo rồi trả100.000 cho B | A bán nhận100.000, trả100.000 còn10.000; B nhận100.000; không cần chuyển lượt cho A, không tăng lượt riêng của A |
| F05 / Q04 | A Strategist không còn quyền gieo, Ultimate chưa dùng | Thử Ultimate di chuyển thêm | Từ chối; không tiêu Ultimate, không biến thành hành động di chuyển bổ sung |
| F06 / Q05 | A Entrepreneur có RentDouble tại lượt k; Hà Nội Nhà3; các lần đôi liên tiếp trong k | Query thuê trước và ngay trước bắt đầu k+1 | Trong khoảng còn hiệu lực thuê66.000; effect bị xóa trước k+1 nên còn33.000; không đợi hết k+1 |
| F07 / Q06 | Hà Nội Nhà3 đang Cúp điện, A là Lawyer có thẻ Giảm thuê và Active sẵn sàng | Phát sinh lượt dừng A và thử tiêu biện pháp miễn/giảm | Thuê0; không tiêu thẻ/Active, không chuyển tiền |
| F08 / Q06 | Hai fixture A Lawyer100.000/B0, thuê30.000, có Giảm thuê/Active sẵn sàng, không Ultimate | Fixture1 chọn thẻ; fixture2 chọn Active; sau đó thử chọn thêm biện pháp cho cùng obligationId | Fixture1 A85.000/B15.000, mất thẻ, Active giữ; fixture2 không chuyển tiền, Active cooldown6, thẻ giữ; không áp biện pháp thứ2 |
| F09 / Q09 | B Banker109.000, nợ110.000 đến hạn, không tài sản; cuối lượt có thể kết vòng; A/C vẫn chơi | Xử lý cuối lượt | B thiếu sau thanh lý nên phá sản, không được cộng1.090 trước nợ để tự cứu; A/C tiếp tục nếu chưa thắng khác |
| F10 / Q13 | Fixture đơn vị thuật toán vốn: khoản101 đồng, tỷ trọng40:20:20:20 cho Cọc..Nhà3 | Chia theo phần lẻ lớn nhất | Vốn[41,20,20,20,0], tổng101; không khẳng định101 là giá mua thực tế một TP trong catalog |
| F11 / Q13, Lawyer | A Lawyer cash100.008, Hà Nội Nhà3 giá100.000; không thẻ miễn | Rút Nộp phạt thuế | Gross40.001,6; giảm25% còn30.001,2; trả30.001, cash70.007. Không floor gross trước giảm để ra30.000 |
| F12 / Q20 | AI hoặc Human đang popup cưỡng chế, clock còn5 giây | Chờ7 giây thực trước chọn Mua/Bỏ qua | Clock vẫn5 ở cả hai controller; không tự đóng popup để kết thúc trận |
| F13 / Q20 | Clock hết, A Engineer đang giải quyết lần dừngô1; ô2 của A không liên quan chuỗi; Active sẵn sàng | Thử dùng Active mới nângô2 | Từ chối hành động chủ động không liên quan; cooldown/ô2 không đổi; vẫn tiếp tục popup/hiệu ứng củaô1 |
| F14 / Q20 | Chuỗi lần dừng đã đưa A tới24 bằng thẻ, cash300.000; clock hết; biển25 chưa chủ, không qua0 khi đi24→25 | Chọn dịch chuyển100.000 tới25 và mua biển190.000 | Vẫn xử lý cả hai lựa chọn, A còn10.000, sở hữu25 với vốn190.000; không gieo mới, rồi xử lý Q09 và kết quả |

## 9c. Nhà tù tối đa ba lượt riêng — luật mới JAIL-03

Những nhánh miễn phí bắt buộc bằng skill còn P02; các ca dưới không có miễn nghĩa vụ. Phí trả chủ động trước lần thử và phí bắt buộc sau lần thử không đôi ở lượt tù thứ3 là hai continuation khác nhau.

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| J01 / JAIL-03, Q11 | A chưa tù, đang lượt k, ô6 | Tung đôi(1,1) và dừng8 | Bị giam, counter0; lượt k không là lượt tù1; mất gieo thêm và chạy cuối lượt |
| J02 / JAIL-03, Q11 | A chưa tù, ô18 | Tung(1,1) tới20, rút Vào nhà tù | Ô8, bị giam, counter0; hủy gieo thêm. Không assert tiền qua0 của chặng thẻ còn Q10 |
| J03 / JAIL-03 | A bắt đầu lượt riêng kế tiếp sau khi bị bắt, cash300.000 | Chọn thử(1,2) | Lượt tù1, còn tù ô8, không trừ200.000; kết thúc lượt |
| J04 / JAIL-03 | A đã qua lượt tù1, bắt đầu lần thứ2, cash300.000 | Chọn thử(2,3) | Lượt tù2, còn tù, cash300.000; chưa thu phí bắt buộc |
| J05 / JAIL-03 | A đã qua2 lượt tù, cash300.000 | Lượt tù3 thử(1,2), thanh toán bắt buộc | Trả200.000 còn100.000; thả cuối lượt3, vị trí8, không dùng tổng3 để đi và không gieo thêm; lượt riêng sau mới đi |
| J06 / JAIL-03 | A cash50.000, Tokyo Nhà3 vốn200.000 là toàn bộ tài sản, bắt đầu lượt tù3 | Thử không đôi, thanh lý Tokyo lấy100.000 | Tổng150.000 vẫn thiếu200.000: phá sản, chuyển tiền còn lại cho ngân hàng, không thả để đi; không truy thu50.000 |
| J07 / JAIL-03 | A cash150.000, Hà Nội Nhà3 vốn100.000, lượt tù3 | Thử không đôi; bán Hà Nội nhận50.000 rồi trả200.000 | Cash0 nhưng không phá sản; TP về ngân hàng, thả cuối lượt, không di chuyển |
| J08 / JAIL-03, Word | A ở lượt tù3, cash300.000; ô10 chưa chủ | Thử(1,1) rồi bỏ mua ở10 | Ra tù bằng đôi, đến10, cash300.000, được gieo thêm; không phát sinh phí bắt buộc |
| J09 / JAIL-03, Word | A bắt đầu lượt tù3, cash300.000; ô11 chưa chủ | Chọn trả200.000 trước thử, rồi gieo(1,2), bỏ mua | Cash100.000; ra tù và đến11 trong lượt này; không dùng nhánh thả cuối lượt không đi |
| J10 / JAIL-03, Word | A lượt tù3, cash0, có Ra tù; ô11 chưa chủ | Dùng thẻ rồi gieo(1,2), bỏ mua | Thẻ mất, không trừ tiền; ra tù và đi11 ngay; cash0 không tự gây phá sản |
| J11 / JAIL-03, Q11 | A đang lượt tù2 | Thử(6,6) ra tù, đến20 rồi rút Vào tù | Đợt giam cũ kết thúc; bị bắt lại counter0; lượt này kết thúc, không giữ quyền gieo đôi cho đợt mới; chặng thẻ không assert tiền qua0 |

## 9d. Ca có đủ căn cứ Word, không là phê duyệt Q mới

Các ca dưới có phạm vi fixture cụ thể đủ luật Word và không bị quyết định mới thay đổi. Giữ Q07/Q08/Q18 ở PENDING trong bảng truy vết theo yêu cầu; không tự ghi APPROVED và không hỏi lại phần đã có luật. Chỉ nhánh thực sự chưa rõ mới bị chặn.

| ID / nguồn | Trạng thái đầu vào | Hành động | Kết quả mong đợi |
|---|---|---|---|
| D07 / Word §2.2, §3.2i, §4.1/4.3b | B Entrepreneur có1 biển, Active còn hiệu lực; A có thẻ Giảm thuê; không effect khác | A chọn dùng thẻ và trả thuê biển | 50.000×(1+10%+30%−50%)=45.000; thẻ bị dùng, không nhân liên tiếp các phần trăm |
| D08 / Word §3.2a, §4.1/4.3e; Q09 | A đã xong lượt, B Engineer đã phá sản trước lượt, C Banker100.000 là người còn lại của vòng; không nợ/effect đến hạn | C kết thúc lượt | A/C đều hoàn tất lượt nên hết vòng; C nhận1.000 lãi, cash101.000; không đợi B đã bị loại |
| D18 / Word §3.2g, §4.3d; Q06 | A Lawyer Ultimate đang hiệu lực trong lượt mình, cash300.000, rút Tặng quà200.000 cho B | Xử lý nghĩa vụ quà với miễn sẵn có | Nghĩa vụ bắt buộc được hủy, A/B không chuyển tiền; không tiêu thêm biện pháp chủ động. Không áp oracle này cho phí tù mới P02 |

## 10. Test PENDING còn bị chặn

Các mục dưới chưa có phê duyệt bổ sung hoặc chi tiết hợp đồng; không tự đặt kết quả. Việc giữ PENDING không có nghĩa Word hoàn toàn không có nội dung liên quan; phải đọc phần đã rõ trước khi hỏi nhóm. Bảy D đã APPROVED được chuyển sang §9a, không để lẫn trong bảng này.

| ID / quyết định | Trạng thái đầu vào | Hành động cần test | Kết quả còn phải xác nhận |
|---|---|---|---|
| D02 / Q02 | Trình tự thẻ đã hết, một số loại đang được giữ | Rút tiếp và xem trước thẻ | Cách tái tạo deck, xác suất, vị trí lá bỏ; chưa có oracle |
| D03 / Q03 | Setup1 người hoặc hai người chọn cùng nghề | Xác nhận tạo trận | Số tối thiểu, trùng nghề, người đầu tiên; không mặc định cho phép/từ chối |
| D10 / Q10 | A ô28, cash100.000 | Rút Vào tù hoặc Đến dịch chuyển | Chặng thẻ có qua0/nhận100.000 không; chưa gán oracle cho cash |
| D12 / Q12 | A Engineer vừa mua Hà Nội Nhà3 ở cùng lần đến | Dùng Active nâng Hotel | Ngoại lệ “khi hợp lệ” có vượt điều kiện lần dừng sau hay không |
| D14 / Q14 | Hà Nội đã có festival tạm còn1 lượt, A đủ200.000 | Mua festival tiếp trên cùng TP | Từ chối hay gia hạn/thay thế; tuyệt đối không tự cho hai effect chồng |
| D15 / Q15 | Hà Nội có festival tạm do A tạo; B cưỡng chế thành công | Tiếp tục thời hạn/thuê | Festival tạm còn hay mất, nguồn đếm lượt theo ai |
| D16 / Q16 | Investor còn coupon15% chưa dùng, cooldown đã hết | Dùng Active lần nữa | Cấm, thay thế hay tích; mức giảm lần mua kế tiếp chưa chốt |
| D17 / Q17 | A đã tạo Cúp điện lên TP B rồi A phá sản trước khi hết3 lượt | Đến lượt các người còn lại | Effect bị hủy hay cách xác định hết hạn khi source không còn lượt |
| D19 / Q19 | A rút Quà từ đối thủ; B/C thiếu tiền, có thể phải thanh lý hoặc phá sản; chưa ai thắng lúc bắt đầu | Giải quyết từng người trả | Thứ tự xử lý từng đối thủ chưa chốt nên chưa có oracle đầy đủ; khi đã có thắng tức thời phải dừng ngay theo Q09/Q20 |
| D21 / P01 PENDING | Fixture phân vốn103 đồng theo40:20:20:20, có ba phần lẻ0,6 bằng nhau | Phân hai đồng dư sau phần nguyên | Tổng103 phải bảo toàn; vector theo đúng thứ tự phá hòa chờ nhóm chỉ hướng, không tự chọn hai bậc nhận dư |
| D22 / P02 PENDING | A Lawyer Ultimate còn hiệu lực, lượt tù3 thử không đôi, cash300.000 | Xử lý ForcedJailReleaseFee200.000 và quyết định thả | Chưa chốt có được miễn phí bắt buộc mới và miễn có thả cuối lượt không; không tự đặt số trả hoặc trạng thái thả |

Q01 không tạo test gameplay; Word hiện có và đã đọc.

## 11. Cách đưa vào kiểm thử sau này

1. Tách test công thức/validator/chuyển trạng thái plain C# và test presentation Unity; chưa cài package trong nhiệm vụ này.
2. Dùng RNG/thẻ cấp trước; assert trạng thái trước/sau, tiền cả hai phía, vốn từng bậc và tài nguyên bị dùng.
3. Bảy Q đã APPROVED dùng oracle cập nhật §9a; Q/P còn PENDING chờ đúng nhánh thiếu. Không bỏ qua test bằng kết quả tự đoán, không đổi yêu cầu cũ cho test xanh.
4. Báo cáo riêng: đặc tả đã rà soát, test tự động đã chạy, Unity đã chạy, build đã chạy. Hiện tài liệu này chỉ đạt mức **đặc tả đã soạn**, không có kết quả test thực thi.
