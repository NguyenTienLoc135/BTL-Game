# GAME_RULES.md — Đặc tả đã chốt cho Cờ Tỷ Phú RPG

> **Nguồn ưu tiên:** quyết định mới người dùng phê duyệt → [Word gốc](references/GAME_DESIGN_DOCUMENT.docx) đối với phần không bị thay đổi → tài liệu này → tài liệu kỹ thuật. Word trước đây được nhắc bằng tên `Bao_cao_Co_Ty_Phu_RPG_Giu_Nguyen_Muc_Luc_Goc(2).docx`. Ngày 2026-11-06 người dùng phê duyệt Q04=A, Q05=A, Q06=A, Q09=A, Q11=A, Q13=A, Q20=B và giới hạn tù 3 lượt. Ngoại lệ mới được ghi trong [DECISIONS](DECISIONS.md); **không sửa Word gốc**.
>
> **Trạng thái:** Đặc tả nội dung đã chốt, **không phải** bằng chứng đã lập trình hoặc test xong. Đơn vị tiền tệ là **đồng (VND) mô phỏng trong game**.

## 1. Giới hạn sản phẩm và luồng chơi

- Unity **6.6 (6000.6.4f1)** + C#, **URP**, low-poly 3D, góc camera từ trên xuống nhìn chéo cố định, UI 2D. Điều khiển bằng chuột/nút.
- Board game chiến thuật theo lượt, tài chính/bất động sản kèm kỹ năng RPG nhẹ. Bàn cờ **32 ô**, **20 thành phố thuộc 8 quốc gia**, **4 bãi biển**, **8 ô chức năng**.
- Tối đa **4 người** (AI và/hoặc người cùng dùng một máy); có chế độ đấu AI và chế độ nhiều người lần lượt trên **cùng thiết bị**. Không có online/multiplayer network/account.
- Chọn đồng hồ **30 / 45 / 60 phút / vô hạn** trước khi bắt đầu; cả hai chế độ đều dùng các tùy chọn này.
- Mỗi người chọn 1 trong 6 nghề nghiệp. Khi bắt đầu, mỗi người có **300.000**, ở ô **0**; xác định ngẫu nhiên **3 thành phố lễ hội cố định** xuyên suốt trận.
- Game flow: MainMenu → chọn chế độ và thời gian, số người → chọn nghề nghiệp → tạo trận → tới lượt chọn kỹ năng hợp lệ, gieo hai xúc xắc, di chuyển, xử lý ô/tài sản/thẻ → kiểm tra thắng → người/AI kế tiếp → kết thúc.
- Dữ liệu trạng thái trận chỉ nằm trong bộ nhớ khi chạy. **Không Save/Load tiếp tục giữa trận**.
- Không nhiệm vụ tuần, skin, shop, tiền xu, nạp tiền, tài khoản, hệ thống cấp nhân vật/cây kỹ năng, trao đổi bất động sản tự do.

## 2. Lượt, vòng, xúc xắc và thời gian

- **Lượt riêng**: một người được quyền thao tác; một **vòng chung** hoàn thành khi tất cả người chơi **chưa phá sản** đã kết thúc lượt riêng.
- Sử dụng **hai xúc xắc 6 mặt**. Tổng điểm di chuyển về phía tăng chỉ số ô, quấn vòng ở 31 → 0.
- **Tung đôi:** được tung tiếp **không giới hạn** sau khi giải quyết xong ô đến. Tất cả lần tung thêm nằm trong **cùng lượt riêng**, không làm chạy cooldown, đếm lại thời hạn hiệu ứng hay sinh vòng chung mới.
- **Q11 đã duyệt:** nếu tung đôi rồi dừng ô8 hoặc rút Vào tù thì vào trạng thái giam, **hủy quyền gieo thêm và kết thúc lượt ngay**; vẫn phải xử lý các mốc cuối lượt cần thiết. Không đổi luật tung đôi để ra tù ở §7.2.
- Đi ngang hoặc dừng đúng ô xuất phát theo đường di chuyển hợp lệ nhận **100.000 mỗi lần qua ô 0**; không nhận hai lần cho cùng một lần vượt mốc.
- **Q20 đã duyệt:** đồng hồ hữu hạn tạm dừng khi chờ quyết định trong popup cho cả **Human và AI**. Hết giờ ghi nhận yêu cầu kết thúc; hoàn tất **toàn bộ lần dừng đang xử lý và hiệu ứng/giao dịch phát sinh trực tiếp**, gồm lựa chọn mua/bỏ qua, cưỡng chế, dịch chuyển, lễ hội và chuỗi liên quan. Không mở lần gieo mới, kể cả đôi; không bắt đầu kỹ năng chủ động mới không liên quan đến chuỗi đang xử lý. Không cắt bỏ phản ứng hợp lệ trực tiếp thuộc nghĩa vụ đang giải quyết.
- Sau khi hoàn tất chuỗi, xử lý mốc cuối lượt theo thứ tự dưới đây rồi mới xác định kết quả hết giờ; thắng tức thời trong chuỗi kết thúc trận ngay.
- Cuối thời gian nếu chưa có thắng tức thời: so **tiền mặt** của người còn chơi → hòa thì so **tổng giá niêm yết hiện tại của bất động sản** → vẫn hòa thì **đồng thắng**.

### Thứ tự cuối lượt — Q09 đã duyệt

1. Xử lý nghĩa vụ đến hạn, đặc biệt nợ Banker.
2. Cho thanh lý nếu thiếu tiền; xử lý phá sản nếu vẫn thiếu.
3. Kiểm tra thắng tức thời ngay sau thay đổi có thể tạo thắng; nếu có thì dừng trận, không chạy bước không cần thiết.
4. Cập nhật mốc lượt riêng, cooldown và thời hạn hiệu ứng.
5. Nếu hoàn tất vòng chung, cộng lãi Banker **1% tiền mặt dương còn lại**, floor theo luật.
6. Chuyển lượt hoặc xác định kết quả hết giờ.

Mốc Ultimate Doanh Nhân là ngoại lệ đã chốt ở §9: xóa hiệu lực **ngay trước** khi người dùng bắt đầu lượt riêng tiếp theo, không xóa sớm ở cuối lượt kích hoạt và không kéo tới cuối lượt kế tiếp.

## 3. Bảng đầy đủ 32 ô (ID bất biến)

**Mã TP là mã hiển thị ở báo cáo; không có mã TP17.** ID nội bộ có thể khác nhưng mọi logic phải dùng **ô 0–31** đúng bảng này.

| Ô | Loại | Tên / quốc gia / mã hoặc chức năng | Giá Nhà 3 tham chiếu (đ) |
|---:|---|---|---:|
| 0 | Xuất phát | Qua/dừng ô 0: +100.000 theo luật | — |
| 1 | Thành phố | Hà Nội — Việt Nam — TP1 | 100.000 |
| 2 | Thành phố | Đà Nẵng — Việt Nam — TP2 | 120.000 |
| 3 | Thành phố | TP. Hồ Chí Minh — Việt Nam — TP3 | 140.000 |
| 4 | Bãi biển | Bãi biển 1 — **giá mua 100.000** | — |
| 5 | Thành phố | Kyoto — Nhật Bản — TP4 | 160.000 |
| 6 | Thành phố | Osaka — Nhật Bản — TP5 | 180.000 |
| 7 | Thành phố | Tokyo — Nhật Bản — TP6 | 200.000 |
| 8 | Nhà tù | Dừng vào thì bị giam; đi ngang không bị | — |
| 9 | Thành phố | Incheon — Hàn Quốc — TP7 | 230.000 |
| 10 | Thành phố | Busan — Hàn Quốc — TP8 | 260.000 |
| 11 | Thành phố | Seoul — Hàn Quốc — TP9 | 290.000 |
| 12 | Thẻ sự kiện | Rút một thẻ | — |
| 13 | Thành phố | Lyon — Pháp — TP10 | 330.000 |
| 14 | Bãi biển | Bãi biển 2 — **giá mua 130.000** | — |
| 15 | Thành phố | Paris — Pháp — TP11 | 370.000 |
| 16 | Tổ chức sự kiện | Có thể trả **200.000** để tổ chức lễ hội | — |
| 17 | Thành phố | Milan — Ý — TP12 | 410.000 |
| 18 | Bãi biển | Bãi biển 3 — **giá mua 160.000** | — |
| 19 | Thành phố | Rome — Ý — TP13 | 450.000 |
| 20 | Thẻ sự kiện | Rút một thẻ | — |
| 21 | Thành phố | Chicago — Mỹ — TP14 | 500.000 |
| 22 | Thành phố | Los Angeles — Mỹ — TP15 | 550.000 |
| 23 | Thành phố | New York — Mỹ — TP16 | 600.000 |
| 24 | Dịch chuyển | Có thể trả **100.000** để chọn ô đến | — |
| 25 | Bãi biển | Bãi biển 4 — **giá mua 190.000** | — |
| 26 | Thành phố | Manchester — Anh — TP18 | 650.000 |
| 27 | Thành phố | London — Anh — TP19 | 700.000 |
| 28 | Thẻ sự kiện | Rút một thẻ | — |
| 29 | Thành phố | Melbourne — **Úc** — TP20 | 750.000 |
| 30 | Thuế | Thuế **20% tổng tài sản** | — |
| 31 | Thành phố | Sydney — **Úc** — TP21 | 800.000 |

> **Chú ý:** Báo cáo hiện tại dùng **Úc (Melbourne, Sydney)** ở TP20/21, **không phải Đức**. Dùng tài liệu Word này làm chuẩn; không áp dụng các phiên bản thiết kế trước đó nếu trái bảng.
>
> Trong UI, 4 ô biển **chỉ hiển thị “Bãi biển”**, không thêm số thứ tự vào tên hiển thị; số 1–4 ở bảng dùng để phân biệt dữ liệu.

### Nhóm quốc gia

- Việt Nam: Hà Nội, Đà Nẵng, TP. Hồ Chí Minh.
- Nhật Bản: Kyoto, Osaka, Tokyo.
- Hàn Quốc: Incheon, Busan, Seoul.
- Pháp: Lyon, Paris.
- Ý: Milan, Rome.
- Mỹ: Chicago, Los Angeles, New York.
- Anh: Manchester, London.
- **Úc**: Melbourne, Sydney.

Độc quyền một quốc gia khi sở hữu **tất cả** các TP thuộc quốc gia đó (2 hoặc 3). Sở hữu trọn **ba quốc gia bất kỳ** → thắng ngay; không cần đủ 9 thành phố.

## 4. Thành phố, mua, nâng cấp và giá thuê

### 4.1. Trạng thái sở hữu và bảng hệ số

`Chưa có chủ` là trạng thái riêng, **không phải cấp công trình**. Thành phố có chủ có đúng 5 mức:

| Cấp | Giá niêm yết so với Nhà 3 | Hệ số thuê áp lên 5% giá Nhà 3 |
|---|---:|---:|
| Cọc | 40% | ×1,5 |
| Nhà 1 | 60% | ×2,5 |
| Nhà 2 | 80% | ×4 |
| Nhà 3 | 100% | ×6 |
| Khách sạn | 120% | ×8 |

- Giá niêm yết = **giá tham chiếu Nhà 3 × tỷ lệ cấp**. Ví dụ TP1: Cọc 40k → Nhà1 60k → Nhà2 80k → Nhà3 100k → Khách sạn 120k.
- Khi dẫm TP **chưa có chủ**, có thể mua ngay **Cọc / Nhà1 / Nhà2 / Nhà3** (hoặc bỏ qua), không thể mua trực tiếp Khách sạn.
- Khi dẫm TP **của mình**, được chọn **một thao tác nâng cấp thường** lên cấp cao hơn, tối đa Nhà 3, với **chênh lệch giá các cấp**, có xét chi phí kỹ năng/điểm yếu nếu áp dụng. Không nâng khi không đủ tiền; không hạ cấp tự nguyện.
- Một lần dừng chỉ có **một quyết định nâng cấp thường**; tách thành nhiều lệnh/nút bấm không cho phép Nhà1→Nhà2 rồi Nhà2→Nhà3 trong cùng lần dừng. Kỹ năng vẫn có điều kiện riêng.
- **Nhà3 → Khách sạn** chỉ được nâng trong **một lần dừng sau đó** trên TP mình sở hữu khi đã Nhà3. **Không** được hoàn thành Khách sạn trong cùng lần đến mua đầu tiên. Kỹ năng Kỹ Sư có ngoại lệ nâng từ xa theo đúng mô tả ở mục 9.
- Khách sạn miễn nhiễm với **mua cưỡng chế, Động đất, Phá tài sản, Cúp điện**, nhưng **có thể bán cho ngân hàng** hoặc bị thu hồi khi chủ phá sản.

### 4.2. Tiền thuê TP, lễ hội và độc quyền

1. Tiền thuê gốc TP = `giá Nhà 3 × 5% × hệ số của cấp`; **không làm tròn ở các bước trung gian**.
2. Thưởng **độc quyền đất nước +20% thuê** trên các TP thuộc nước độc quyền đó.
3. **Đầu trận chọn ngẫu nhiên 3 TP khác nhau** làm lễ hội cố định, **+50% thuê** cho cả trận.
4. Ô 16 hoặc thẻ lễ hội: người chơi có thể trả **200.000** chọn **một TP mình sở hữu** để **+50% thuê trong hai lượt riêng tiếp theo**. Không chọn biển; **không có hai hiệu ứng lễ hội tạm cùng lúc trên một TP**. Phân biệt đúng vòng đời hiệu ứng tính theo **lượt riêng của người dùng**.
5. Các phần trăm tăng/giảm được **cộng đại số trên tiền thuê gốc**, không nhân tuần tự mỗi buff. Sau đó mới áp dụng hiệu ứng **nhân đôi** (Ultimate Doanh Nhân), làm tròn xuống **đơn vị đồng ở kết quả cuối**.
6. Khi tòa nhà chịu **Cúp điện** hợp lệ, chủ **không được thu tiền thuê** ở TP đó đến khi hết hạn hoặc hủy hiệu ứng theo luật.
7. Các Passive/Active có thể ảnh hưởng tiền thuê cần xem mục 9, tránh cộng trùng.

## 5. Bãi biển

- Ô 4, 14, 18, 25 — giá lần lượt **100.000, 130.000, 160.000, 190.000**.
- Bãi biển **không nâng cấp**. Tiền thuê trên **mỗi ô biển** thuộc cùng chủ phụ thuộc **số bãi biển chủ đó sở hữu**: 1 biển → **50.000**, 2 → **100.000**, 3 → **150.000**.
- Sở hữu **cả 4 biển → thắng ngay**; không cần tính thuê trạng thái 4.
- Bãi biển có chủ **không thể cưỡng chế mua**; không chịu Động đất, Phá tài sản, Cúp điện.
- Trở về ngân hàng nếu chủ tự bán, thanh lý hoặc phá sản.
- UI hiển thị tên cả bốn ô chỉ là `Bãi biển`, phân biệt dữ liệu bằng ô và giá.

## 6. Cưỡng chế, sổ vốn, bán tài sản, phá sản

### 6.1. Mua cưỡng chế

- Dẫm vào **TP đối thủ Cọc / Nhà1 / Nhà2 / Nhà3**: phải **trả tiền thuê trước**, sau đó **có thể** chọn cưỡng chế nếu đủ tiền.
- Giá mua cưỡng chế thông thường = **200% giá niêm yết của cấp hiện tại**; trả cho chủ cũ, chủ cũ không được từ chối.
- Ultimate Nhà Đầu Tư = **mua từ xa** TP đối thủ chưa là Khách sạn với giá **150% giá niêm yết hiện tại**, **không phát sinh tiền thuê**.
- Không có cưỡng chế mua **Khách sạn hoặc biển**. Không có cơ chế mua bán tự do giữa người chơi.
- Khi đổi chủ TP giữ nguyên **cấp hiện tại**; **sổ vốn của chủ mới** bắt đầu từ **giá cưỡng chế thực trả** (phân bổ theo bậc), **không kế thừa vốn của chủ cũ**. Tiền thuê không cộng vào vốn.

### 6.2. Sổ vốn thực tế

- Theo dõi vốn **thực trả theo từng bậc** (Cọc, Nhà1, Nhà2, Nhà3, Khách sạn), **không đồng nhất giá niêm yết với vốn**. Mua trực tiếp cấp cao: phân bổ số tiền thực chi theo **tỷ trọng phần giá của các bậc đã có**; mua cưỡng chế cũng phân bổ theo tỷ trọng cấp hiện tại.
- Kỹ năng nâng cấp miễn phí: vốn của phần nâng miễn phí = **0**; giảm giá hoặc phụ phí nâng cấp phải được phản ánh theo số tiền **thực chi**.
- **Động đất:** chọn TP đối thủ đang Nhà1/2/3, hạ **đúng một cấp**, trừ **vốn gắn với bậc bị mất**, **không hoàn tiền**; Cọc/Khách sạn và biển không hợp lệ.
- **Bán tự nguyện/thanh lý cho ngân hàng:** nhận lại **50% tổng vốn thực còn ghi nhận**; trả lại cả TP và công trình về trạng thái chưa chủ. Biển cũng được bán/thu hồi về ngân hàng theo nguyên tắc vốn thực.
- **Q04 đã duyệt:** giữ quyền bán tự nguyện tại các điểm dừng hành động hợp lệ trước gieo hoặc sau khi ô đã xử lý hoàn tất. Không bán tự nguyện giữa animation/giao dịch đang dở; thanh lý cho nghĩa vụ bắt buộc là luồng riêng, được thực hiện ngay kể cả ngoài lượt riêng.
- **Thẻ Phá tài sản:** chọn TP đối thủ **Cọc đến Nhà3**, phá toàn bộ quyền sở hữu/công trình, **ngân hàng hoàn 100% vốn thực còn lại cho chủ cũ**; không phá biển/khách sạn.
- Ví dụ từ báo cáo: TP1 Nhà3 100k phân bổ vốn 40k(Cọc)+20k(Nhà1)+20k(Nhà2)+20k(Nhà3). Động đất Nhà3→Nhà2 làm vốn còn 80k; bán ngân hàng 40k, thẻ Phá tài sản bồi hoàn 80k.

### 6.2a. Tiền và đồng dư — Q13 đã duyệt

- Không dùng float biểu diễn tiền; tính tỷ lệ chính xác bằng số nguyên/phân số.
- Những phép tính tiền chưa có quy tắc riêng được **làm tròn xuống đồng nguyên ở kết quả thanh toán cuối cùng**. Thuê và lãi Banker giữ floor theo Word; không làm tròn các bước trung gian.
- Phân bổ khoản vốn thực trả theo tỷ trọng các bậc: lấy phần nguyên của mỗi phần, phân phối đồng dư theo **phần lẻ lớn nhất**, dùng **thứ tự bậc cố định để phá hòa**. Tổng vốn phải bằng đúng số thực trả, không mất đồng vì làm tròn.
- Prompt chưa chỉ rõ hướng của thứ tự phá hòa; chi tiết đó còn PENDING P01 trong DECISIONS. Không tự coi một hướng sắp xếp là đã được duyệt.
- Nâng miễn phí không chia lại vốn cũ; Động đất chỉ xóa vốn bậc mất.

### 6.3. Thanh toán và phá sản

- Nghĩa vụ bắt buộc: **thuê, thuế, khoản tiền bắt buộc từ thẻ Quà/Tặng quà, trả nợ Banker**.
- Nếu tiền mặt không đủ nghĩa vụ, phải cho người chơi **bán tài sản cho ngân hàng** để thanh toán. Nếu đã thanh lý mà vẫn thiếu: **chuyển toàn bộ tiền mặt còn lại cho chủ nợ/người nhận (nếu có), phá sản, không truy thu thiếu**.
- Khi phá sản: **mọi tài sản còn lại**, gồm khách sạn và biển, **trở về ngân hàng chưa có chủ**; người chơi bị loại khỏi trận.
- Phí/tùy chọn **mua đất, nâng cấp, dịch chuyển, lễ hội** chỉ thực hiện khi đủ tiền; **không bắt buộc thanh lý để mua dịch vụ**.
- **Q06 đã duyệt:** áp dụng hiệu ứng đã có sẵn trước. Nếu nghĩa vụ bằng0 thì không cho tiêu thêm thẻ/kỹ năng. Nếu còn dương, người chơi chọn trả thường hoặc tối đa **một biện pháp miễn/giảm chủ động** phù hợp cho nghĩa vụ đó. Giữ công thức thuê và quy tắc tự động Bảo vệ; không tự kích hoạt một kỹ năng mới ngoài cửa sổ hợp lệ.
- Khoản **200.000 bắt buộc ra tù sau lần tung thất bại ở lượt tù thứ3** là nghĩa vụ bắt buộc mới, phải cho thanh lý trước phá sản. Tương tác miễn khoản này và điều kiện thả sau miễn còn PENDING P02, không tự thêm ngoại lệ.
- Sau giao dịch/đổi chủ/phá sản cần kiểm tra thắng ngay theo các điều kiện.

## 7. Thuế, tù, dịch chuyển và các ô đặc biệt

### 7.1. Thuế

- Ô **30** và thẻ **Nộp phạt thuế**: `20% × (tiền mặt + tổng giá niêm yết của tất cả TP và bãi biển đang sở hữu)` ở **thời điểm phát sinh**.
- Số tiền phải nộp **được cố định khi bắt đầu nghĩa vụ**, không tính lại sau thanh lý.
- Luật Sư Passive **giảm 25% chỉ với thuế từ thẻ**, **không** giảm thuế ô 30.

### 7.2. Nhà tù (ô 8)

- Dừng tại ô 8 bằng di chuyển thường → bị giam; **đi ngang không bị giam**. Thẻ Vào nhà tù cũng dẫn đến trạng thái giam.
- **Luật mới người dùng phê duyệt, thay thế Word §3.2h:** tối đa **3 lượt riêng ở tù**; **lượt bị bắt không tính**. Trong thời gian bị giam vẫn thu tiền thuê BĐS.
- Khi đến lượt trong tù, bắt buộc chọn 1 trong 3: **(1) tung hai xúc xắc**, **(2) trả 200.000**, **(3) dùng thẻ Ra tù đang giữ**.
- Chọn tung ở lượt tù1 hoặc2: **ra đôi → bắt buộc ra tù, di chuyển theo chính kết quả đó và được tung thêm theo luật đôi**; không đôi → còn tù, kết thúc lượt.
- Ở lượt tù thứ3, vẫn chọn tung/trả200.000/thẻ Ra tù. Nếu chọn tung và **không đôi**, phát sinh nghĩa vụ bắt buộc **200.000**; thiếu thì thanh lý, vẫn không đủ thì phá sản. Trả thành công được thả **cuối lượt thứ3, không di chuyển thêm**; đến lượt riêng kế tiếp mới đi bình thường.
- Nếu lượt tù thứ3 tung được đôi hoặc chủ động trả tiền/dùng thẻ trước tung thì ra tù theo cách thông thường, không áp nhánh thả cuối lượt vì thất bại. Đếm lượt tù tách khỏi số lần tung và cooldown; một lần bị bắt lại bắt đầu đợt giam mới.
- Chọn trả tiền/dùng thẻ → ra tù, **sau đó tung xúc xắc bình thường**.

### 7.3. Dịch chuyển (ô 24)

- Tại ô 24 có thể **trả 100.000** để chọn ô đích; ô đích vẫn **kích hoạt đầy đủ hiệu ứng**. Nếu hành trình chọn **vượt qua ô xuất phát** thì nhận **100.000** đúng luật.
- Không cho một thao tác dịch chuyển kích hoạt lại **chính ô 24** tạo vòng vô hạn.
- Thẻ **Đến ô dịch chuyển** đưa nhân vật đến ô 24; việc sử dụng chức năng dịch chuyển **vẫn phải trả phí 100.000**.

### 7.4. Tổ chức sự kiện (ô 16)

- Có thể trả **200.000** để chọn một **TP thuộc chính mình**, +50% tiền thuê trong **2 lượt riêng tiếp theo**, không áp dụng biển.

## 8. Đúng 14 thẻ sự kiện

| STT | Thẻ | Hành vi bắt buộc |
|---:|---|---|
| 1 | Tiền thưởng | Người rút nhận **300.000** từ ngân hàng. |
| 2 | Động đất | Chọn **TP đối thủ Nhà1–Nhà3**, hạ **1 cấp**, mất vốn bậc đó; không Cọc/khách sạn/biển. |
| 3 | Miễn thuế | **Giữ lại**, chủ động dùng miễn **một lần chịu thuế**; mỗi người giữ tối đa 1. |
| 4 | Giảm tiền thuê | **Giữ lại**, khi đến nghĩa vụ thuê có thể giảm **50% một lần**; mỗi người giữ tối đa 1. |
| 5 | Bảo vệ công trình | **Giữ lại**, tự kích hoạt chặn **một** tác động Động đất/Phá tài sản/Cúp điện hợp lệ; mỗi người giữ tối đa 1. |
| 6 | Nộp phạt thuế | Bắt buộc nộp **20% tổng tài sản**; Passive Luật Sư giảm 25% **số thuế thẻ**; thiếu tiền phải thanh lý/phá sản. |
| 7 | Vào nhà tù | Chuyển người rút tới nhà tù và vào trạng thái bị giam. |
| 8 | Ra tù | **Giữ lại**, dùng ra khỏi tù; mỗi người giữ tối đa 1. |
| 9 | Phá tài sản | Phá một TP đối thủ **Cọc–Nhà3**: về ngân hàng, chủ cũ được ngân hàng hoàn **100% vốn thực còn lại**; không khách sạn/biển. |
| 10 | Quà từ đối thủ | **Mỗi đối thủ còn chơi** bắt buộc chuyển **100.000** cho người rút; thiếu thì thanh lý/phá sản. |
| 11 | Đến ô dịch chuyển | Di chuyển đến ô **24**; nếu chọn dùng dịch chuyển thì **vẫn mất 100.000**, sau đó kích hoạt ô đến. |
| 12 | Tổ chức lễ hội | Có thể trả **200.000** cho 1 TP của mình, **+50% tiền thuê trong 2 lượt riêng**; không biển. |
| 13 | Cúp điện | Chọn TP đối thủ **chưa là Khách sạn**, chủ TP không thu thuê cho tới hết **3 lượt riêng tiếp theo của người sử dụng thẻ**; đổi chủ hoặc lên Khách sạn thì hủy ngay. |
| 14 | Tặng quà 200.000 | Người rút bắt buộc chọn một đối thủ và chuyển **200.000**; thiếu thì thanh lý/phá sản. |

- **4 loại thẻ có thể giữ**: Miễn thuế, Giảm tiền thuê, Bảo vệ công trình, Ra tù. Tối đa **1 thẻ mỗi loại**, tức tối đa 4 tổng cộng mỗi người; rút trùng loại đang giữ → **bỏ thẻ mới**.
- Bảo vệ công trình **tự kích hoạt** khi một tấn công hợp lệ chạm tài sản; nếu mục tiêu vốn miễn nhiễm thì **không mất thẻ**. Miễn thuế và Giảm tiền thuê cho người chơi **chủ động quyết định** dùng khi nghĩa vụ phát sinh.
- Không có mục tiêu hợp lệ → thẻ **không có hiệu lực**; không tự chuyển mục tiêu sang khách sạn/biển. Nếu Bảo vệ chặn Phá tài sản → **không phá, không bồi hoàn**.
- Cúp điện **hủy ngay** khi TP đổi chủ hoặc nâng thành Khách sạn; **tung đôi thêm không tạo lượt mới** để đếm hiệu lực.
- Thẻ chuyển tiền áp dụng nghĩa vụ bắt buộc; người nhận chỉ lấy **số dư thực còn lại** khi bên trả đã thanh lý hết mà vẫn phá sản.
- Nhà Chiến Lược Passive xem trước **thẻ tiếp theo** trong trình tự rút; logic rút thẻ cần cung cấp thông tin nhất quán nhưng **báo cáo không quy định thuật toán shuffle/rút lại**, không tự tuyên bố một kiểu deck là luật bắt buộc.

## 9. Sáu nghề nghiệp — Passive, Active, Ultimate, Weakness

### Quy tắc kỹ năng chung

- **Passive** áp dụng tự động; **Active** có cooldown theo **lượt riêng của chính nhân vật**; **Ultimate chỉ dùng 1 lần/trận**.
- `Cooldown N` nghĩa là phải **hoàn thành N lượt riêng tiếp theo** sau lần Active **đã dùng thành công** mới được dùng lại.
- Hiệu lực `N lượt riêng tiếp theo` gắn với **lượt riêng của người dùng**, trong khoảng đó bao gồm lượt của các đối thủ xen giữa. Tung đôi/di chuyển thêm **không tạo lượt riêng**.
- **Q04 đã duyệt:** kỹ năng chủ động dùng ở điểm dừng hợp lệ trước gieo hoặc sau khi ô đến đã xử lý hoàn tất, nếu đáp ứng điều kiện; không dùng giữa animation hoặc giao dịch chưa xong. Lawyer Active có cửa sổ phản ứng trước chính nghĩa vụ thuê của người dùng. Không tự thêm cửa sổ kích hoạt khác.
- **Q05 đã duyệt:** Ultimate Doanh Nhân nhân đôi có hiệu lực ngay, phủ lượt đối thủ xen giữa, hết **ngay trước khi người kích hoạt bắt đầu lượt riêng tiếp theo**. Không dùng mốc hết cuối lượt tiếp theo và không kéo dài vì tung đôi.
- **Q04 đã duyệt:** Strategist Ultimate **thay một lần gieo tiếp theo đang được phép**, kể cả lần thêm do đôi; không có kết quả xúc xắc nên không tự sinh quyền gieo thêm. Nếu không còn quyền gieo thì không tự biến thành di chuyển bổ sung.
- **Chỉ tiêu hao kỹ năng/Ultimate khi hành động đã thành công hợp lệ** (có mục tiêu, đủ tiền nếu cần). Không tiêu hao nếu nhấn nút nhưng không thể thực thi.
- Thứ tự thuê: **thuê gốc → cộng đại số các % thay đổi → nhân đôi Ultimate nếu có → làm tròn xuống đồng ở kết quả cuối**.

| Nghề | Passive | Active | Cooldown | Ultimate (1 lần/trận) | Điểm yếu |
|---|---|---|---:|---|---|
| **Nhà Đầu Tư** | Giảm **10%** giá mua BĐS **chưa chủ từ ngân hàng**; không cưỡng chế | Thêm **15% giảm cho lần mua tiếp theo từ ngân hàng**, có thể chờ đến khi dùng; Passive + Active tổng giảm **25%** | **5 lượt riêng** | Mua cưỡng chế **từ xa** TP đối thủ Cọc–Nhà3 với **150%** giá niêm yết; không trả thuê, không biển/khách sạn | Giá nâng cấp thường **+10%** |
| **Doanh Nhân** | Thuê BĐS nhận từ đối thủ **+10%** | Thuê **+30% trong 2 lượt riêng** | **6 lượt riêng** | **Nhân đôi khoản thuê** từ lúc kích hoạt tới **ngay trước lượt riêng kế tiếp** của mình (Q05) | Giá mua BĐS **+5%** |
| **Kỹ Sư** | Nâng cấp thường **−15% chi phí** | Nâng **miễn phí đúng 1 cấp** TP của mình từ xa, kể cả Nhà3→Khách sạn khi hợp lệ | **7 lượt riêng** | Nâng **từ xa thẳng Khách sạn**, trả **50% tổng chênh lệch niêm yết** của các cấp chưa có; **không** chồng Passive −15% | Tiền thuê cơ bản **−10%** |
| **Luật Sư** | Giảm **25%** thuế/phạt **chỉ từ thẻ Nộp phạt thuế**, không giảm thuế ô30 | **Hủy một lần trả thuê**, kích hoạt trước lúc thanh toán | **6 lượt riêng** | Hủy các nghĩa vụ thanh toán bắt buộc phát sinh **trong lượt riêng kích hoạt** (thuê, thuế, phạt); không miễn mua/nâng/dịch vụ tự chọn hoặc trả nợ Banker | Không tạo tiền trực tiếp |
| **Banker** | Cuối **mỗi vòng chung**, nhận **1% tiền mặt dương**, làm tròn xuống | Vay ngay **100.000**, bắt buộc trả **110.000 vào cuối lượt riêng thứ 5 tiếp theo** | **7 lượt riêng** | Nhận **200.000** từ ngân hàng | Không ưu đãi giá mua BĐS |
| **Nhà Chiến Lược** | Xem trước thẻ sự kiện kế tiếp | Gieo lại **hai xúc xắc trước khi di chuyển**, phải chấp nhận kết quả mới | **5 lượt riêng** | Thay lần gieo đang được phép bằng chọn ô **phía trước 1–6 ô**, đến và kích hoạt ô; không tự sinh gieo thêm | Không ưu đãi kinh tế cố định |

**Giữ nguyên điểm yếu** kể cả khi nghề có lợi thế mạnh; không cắt bỏ Weakness. Ultimate Kỹ Sư có chi phí 50% chênh lệch, không phải nâng miễn phí.

## 10. Điều kiện thắng/thua

1. Người chơi sở hữu trọn thành phố của **3 quốc gia bất kỳ** → **thắng tức thời**.
2. Người chơi sở hữu đủ **4 bãi biển** → **thắng tức thời**.
3. Chỉ còn **1 người chưa phá sản** → **người đó thắng tức thời**.
4. Nếu **hết thời gian 30/45/60 phút** mà chưa có thắng tức thời, hoàn tất lần dừng và chuỗi liên quan theo Q20, xử lý mốc cuối lượt Q09, so **tiền mặt** người còn chơi; hòa thì so **tổng giá niêm yết BĐS hiện có**; vẫn hòa → **đồng thắng**.
5. Chế độ **vô hạn** không có thắng do thời gian; chỉ kết thúc khi đạt một trong ba điều kiện thắng tức thời.

Sau giao dịch hoặc thay đổi quyền sở hữu, kiểm tra điều kiện thắng để không bỏ lỡ thắng ngay.

## 11. Yêu cầu UI / AI / trải nghiệm

- Main menu: **Bắt đầu**, **Hướng dẫn/Luật chơi**, **Cài đặt âm thanh**, **Thoát**.
- Setup: **chế độ AI hoặc cùng máy**, số người ≤4, thời gian 30/45/60/vô hạn, chọn một trong **6 nghề nghiệp**; hiển thị Passive/Active/Ultimate/Weakness.
- Gameplay: bàn cờ 3D camera cố định; quân cờ, 2 xúc xắc, người tới lượt, tiền mặt, vị trí ô, BĐS, cooldown/thẻ, nút hành động/kết thúc lượt.
- Popup mua TP: **Cọc/Nhà1/Nhà2/Nhà3/Bỏ qua**, tiền trước/sau giao dịch; popup mua cưỡng chế cho biết **tiền thuê bắt buộc trước rồi giá mua 200%**.
- Bảng tài sản: quốc gia, TP, ô, cấp, giá Nhà3, giá cấp hiện tại, thuê gốc/thuê sau hiệu ứng, vốn thực, chủ sở hữu, lễ hội/Cúp điện; biển có giá mua và thuê theo số lượng biển.
- Popup thuê/thuế, nâng cấp, thanh lý, dịch chuyển, lễ hội, ra tù, thẻ sự kiện, kỹ năng, phá sản, kết quả phải hiện **đúng nghĩa vụ và trạng thái**, không cho thao tác bất hợp lệ.
- Trong tù hiển thị đúng ba lựa chọn tung/trả 200k/dùng thẻ Ra tù, bật/tắt theo điều kiện thực tế.
- Đồng hồ **tạm dừng ở popup chờ quyết định cho cả Human và AI**, gồm các lựa chọn của lần dừng còn xử lý sau hết giờ theo Q20.
- AI chơi bằng **cùng luật** người thật; có thể dùng heuristic dựa trên tiền, thuê, độc quyền, khả năng thanh toán; **không yêu cầu học máy**.
- UI tiếng Việt, font Be Vietnam Pro hỗ trợ dấu; không có Save/Load/shop/skin/microtransaction.

## 12. Asset và nguồn giấy phép đã chốt

**Kenney CC0 (14 bộ):**

| Nhóm | Bộ asset | Trang nguồn |
|---|---|---|
| 3D | City Kit Commercial | https://kenney.nl/assets/city-kit-commercial |
| 3D | City Kit Suburban | https://kenney.nl/assets/city-kit-suburban |
| 3D | City Kit Roads | https://kenney.nl/assets/city-kit-roads |
| 3D | Modular Buildings | https://kenney.nl/assets/modular-buildings |
| 3D | Mini Characters | https://kenney.nl/assets/mini-characters |
| 3D | Nature Kit | https://kenney.nl/assets/nature-kit |
| 2D | Flag Pack | https://kenney.nl/assets/flag-pack |
| 2D | UI Pack | https://kenney.nl/assets/ui-pack |
| 2D | Board Game Icons | https://kenney.nl/assets/board-game-icons |
| 2D | Board Game Info | https://kenney.nl/assets/board-game-info |
| VFX | Particle Pack | https://kenney.nl/assets/particle-pack |
| Audio | Casino Audio | https://kenney.nl/assets/casino-audio |
| Audio | Interface Sounds | https://kenney.nl/assets/interface-sounds |
| Audio | Music Jingles | https://kenney.nl/assets/music-jingles |

**Font:** Be Vietnam Pro — https://fonts.google.com/specimen/Be+Vietnam+Pro — **OFL**, không phải CC0.

- Các bộ được import trong `Assets/ThirdParty/Kenney/`; font trong `Assets/ThirdParty/Fonts/BeVietnamPro/`. Bản model chính là **FBX** cùng **texture/colormap riêng từng bộ**; audio OGG, UI PNG, font TTF.
- Không tự động import lại tất cả định dạng model (OBJ/GLB/DAE/STL) hoặc package Particle cũ có thể không tương thích.
- Khung hình 3D dùng **low poly city miniature**, camera nhìn chéo cố định, UI 2D. Asset chỉ là model/âm thanh/icon; **không có hệ thống gameplay dựng sẵn**. Nếu thiếu mẫu, nhóm dựng prefab riêng theo style Kenney.
- 3 FBX NatureKit từng có 5 cảnh báo trùng tên Mesh (`tree_palmDetailedTall`, `tree_palmDetailedShort`, `crops_cornStageD`); tên trùng được sửa trong FBX; không thay lại bản gốc nếu không được yêu cầu.

## 13. Các trường hợp biên cần có bài test

- Lượt: hai xúc xắc ra đôi **liên tục**; nhận Xuất phát đúng một lần mỗi lần đi qua, bao gồm dịch chuyển hợp lệ; hết giờ trong chuỗi tung đôi.
- Vòng chung và Banker lãi 1% khi có người phá sản; deadline khoản vay cuối lượt riêng thứ 5 sau khi vay, cooldown 7.
- Mua thẳng Nhà3; cấm mua Khách sạn lần đầu; chỉ nâng khách sạn khi đủ điều kiện hoặc Kỹ Sư hợp lệ.
- Bậc giá thuê TP1 và Sydney, cộng **cố định +50%**, **độc quyền +20%**, lễ hội tạm **+50%**, Passive/Active nghề, nhân đôi **đúng thứ tự**.
- Mua cưỡng chế **trả thuê trước**, không vượt quy định Khách sạn/biển, sổ vốn cưỡng chế không kế thừa.
- Mua với giá giảm, nâng miễn phí, Động đất hạ bậc, bán ngân hàng 50% vốn thực, Phá tài sản hoàn 100% vốn còn lại.
- Thuế cố định theo tổng tài sản lúc kích hoạt; Luật Sư chỉ giảm **thẻ thuế**, không giảm ô30; phá sản sau khi thanh lý.
- Tù: vào tù khi đôi hủy gieo thêm; lượt bị bắt không tính; tối đa3 lượt; tung không đôi ở lượt tù thứ3 bắt buộc200.000/thanh lý/phá sản; trả thành công thả cuối lượt không di chuyển; ra tù sớm/đôi vẫn theo luật thường; vẫn thu thuê.
- Có bốn thẻ giữ các loại khác nhau, rút trùng không nhận; khi bị đánh vào Khách sạn/biển không tiêu thụ Bảo vệ vô ích.
- Cúp điện 3 **lượt riêng của người dùng thẻ**, hủy khi đổi chủ/lên khách sạn; hiệu ứng không giảm thời hạn vì tung đôi.
- 3 quốc gia gồm cả nước chỉ có 2 TP; 4 biển; phá sản; tie cash → BĐS → đồng thắng.
- AI dùng action validator chung; HUD hiển thị tiền thuê, cooldown và nghĩa vụ chính xác.

## 14. Những điều **chưa được báo cáo quy định chi tiết** — không tự coi là luật mới

Phần đã được người dùng phê duyệt mới không còn bị chặn bởi sự thiếu chi tiết của Word: Q04/Q05/Q06/Q09/Q11/Q13/Q20 và giới hạn tù ở trên. Những mục chưa được phê duyệt trong DECISIONS vẫn OPEN/PENDING; không tự đóng chúng vì một đề xuất kỹ thuật.

- Thuật toán shuffle/bốc lại 14 thẻ, trọng số xác suất mỗi thẻ, cách tạo seed RNG.
- Có cho **trùng nghề nghiệp** giữa những người chơi hay không; số lượng người tối thiểu mỗi trận.
- Những tương tác chưa nằm trong phạm vi một biện pháp chủ động của Q06, đặc biệt phí ra tù bắt buộc mới với miễn nghĩa vụ (P02).
- Q07 giữ truy vết, không hỏi lại phần Word đã rõ: biển là BĐS; modifier thuê BĐS và thẻ Giảm thuê không giới hạn riêng TP. Ca D07 có kết quả cụ thể trong TEST_CASES, không còn bị chặn vì câu hỏi chung này.
- Cách biểu diễn tập người chờ trong vòng khi phá sản còn cần thống nhất hợp đồng; điều kiện vòng theo Word vẫn dựa trên người chưa phá sản. **Thứ tự cuối lượt đã chốt Q09**, không hỏi lại trình tự đó.
- Các nhánh thực sự còn thiếu: đường đi/thưởng ô0 của thẻ và nhận diện chuỗi dịch chuyển (Q10); ngoại lệ Hotel cùng lần dừng của Engineer (Q12); gia hạn/đổi chủ lễ hội tạm (Q14/Q15); coupon Investor chưa dùng khi hết cooldown (Q16); lựa chọn tài sản thanh lý và effect khi source bị loại (Q17); thứ tự người trả của thẻ nhiều nghĩa vụ (Q19). Khi đã có thắng tức thời thì dừng theo Q09/Q20, không hỏi lại có tiếp tục chuỗi sau thắng.
- P01: hướng phá hòa giữa các bậc có phần lẻ bằng nhau khi chia đồng dư; P02: phí tù bắt buộc mới và miễn nghĩa vụ/thả tù; xem DECISIONS.

Agent được phép đề xuất phương án kỹ thuật có lý do cho các vấn đề này, nhưng **không tự khẳng định là quyết định gameplay đã chốt**. Nếu chức năng có thể làm mà không đụng chỗ thiếu, tiếp tục làm phần độc lập.
