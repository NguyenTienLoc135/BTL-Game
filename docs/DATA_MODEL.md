# Hợp đồng dữ liệu đề xuất — Cờ Tỷ Phú RPG

Ngày cập nhật: **2026-11-17**. **Luật bổ sung đã được người dùng duyệt; biểu diễn trường/DTO dưới đây vẫn cần nhóm thống nhất hợp đồng**. Đối chiếu API A/D hiện có qua README/source; triển khai một DTO không tự phê duyệt toàn bộ A01–A06.

Đã đọc đầy đủ các Markdown nguồn và [Word](references/GAME_DESIGN_DOCUMENT.docx). Ưu tiên quyết định người dùng mới → Word cho phần không đổi → [GAME_RULES](GAME_RULES.md) → hợp đồng này. Nguồn duyệt ngày2026-11-06 và ngoại lệ so với Word nằm trong [DECISIONS](DECISIONS.md); không tự bổ sung luật OPEN/PENDING.

## 1. Quy ước hợp đồng

- GameState là một nguồn trạng thái duy nhất, tương đương tên MatchState trong AGENTS.
- ID và enum kỹ thuật bên dưới là đề xuất A02; số ô, giá và luật lấy nguyên từ GAME_RULES.
- PlayerId là ID ổn định trong trận, khác seatIndex; không dùng tên hiển thị làm khóa.
- TileId là số nguyên 0–31. Với tài sản, **PropertyId dùng chính TileId**, tránh một bảng đánh số quyền sở hữu thứ hai.
- CityId đề xuất dùng TileId của thành phố; BeachId dùng TileId của biển. Mã TP1…TP21 chỉ là displayCode, **không tạo TP17 giả**.
- Các object, dictionary/list trong snapshot chỉ đọc; code view/AI không được giữ tham chiếu ghi vào aggregate.
- Tiền dùng số nguyên 64 bit, đơn vị đồng; tỷ lệ tính bằng số nguyên/phân số. Không dùng float cho giá, tiền thuê, vốn hoặc thuế.
- Giá trị chưa xác nhận không được biến thành giá trị mặc định âm thầm; tính năng phụ thuộc phải chặn hoặc trả lý do thiếu quyết định Qxx.

## 2. Dữ liệu tĩnh và runtime

| Dữ liệu tĩnh, dùng chung chỉ đọc | Trạng thái runtime, riêng từng trận |
|---|---|
| BoardTile, quốc gia, giá Nhà3, giá biển, tỷ lệ giá và thuê | Vị trí, tiền, chủ đất, cấp công trình, vốn thực |
| Mô tả và thông số sáu nghề | Cooldown, Ultimate đã dùng, coupon mua tiếp theo, khoản vay |
| Định nghĩa 14 thẻ và loại có thể giữ | Thẻ từng người giữ, trình tự rút hiện tại |
| Luật thời gian 30/45/60 phút/vô hạn | Thời gian còn lại, pause, hành động đang dở |
| Tên/ảnh/icon/âm thanh phục vụ presentation | Ba TP lễ hội cố định được chọn cho trận; hiệu ứng tạm |

Không ghi owner, cash, cooldown hoặc festival runtime vào ScriptableObject. Không lưu bản runtime trong Scene/Prefab. Không thiết kế Save/Load trận.

## 3. BoardTile và định danh đầy đủ

BoardTile là định nghĩa tĩnh: tileId, tileType, displayName, countryId tùy chọn, cityDisplayCode tùy chọn, referenceHouse3Price tùy chọn, beachPurchasePrice tùy chọn. Thông tin hình ảnh/Transform ánh xạ bằng tileId tại presentation.

Bảng dưới sao từ GAME_RULES §3; ID ô bất biến. Dấu — nghĩa là không áp dụng, không phải giá bằng 0.

| TileId | TileType đề xuất | Tên | CountryId / mã hiển thị | Giá Nhà3 hoặc giá mua biển |
|---:|---|---|---|---:|
| 0 | Start | Xuất phát | Thưởng 200.000 khi đi qua hoặc dừng đúng ô trong hành trình hợp lệ | — |
| 1 | City | Hà Nội | Vietnam / TP1 | 100.000 |
| 2 | City | Đà Nẵng | Vietnam / TP2 | 120.000 |
| 3 | City | TP. Hồ Chí Minh | Vietnam / TP3 | 140.000 |
| 4 | Beach | Bãi biển | — | 100.000 |
| 5 | City | Kyoto | Japan / TP4 | 160.000 |
| 6 | City | Osaka | Japan / TP5 | 180.000 |
| 7 | City | Tokyo | Japan / TP6 | 200.000 |
| 8 | Jail | Nhà tù | — | — |
| 9 | City | Incheon | Korea / TP7 | 230.000 |
| 10 | City | Busan | Korea / TP8 | 260.000 |
| 11 | City | Seoul | Korea / TP9 | 290.000 |
| 12 | EventCard | Thẻ sự kiện | — | — |
| 13 | City | Lyon | France / TP10 | 330.000 |
| 14 | Beach | Bãi biển | — | 130.000 |
| 15 | City | Paris | France / TP11 | 370.000 |
| 16 | Festival | Tổ chức sự kiện | — | — |
| 17 | City | Milan | Italy / TP12 | 410.000 |
| 18 | Beach | Bãi biển | — | 160.000 |
| 19 | City | Rome | Italy / TP13 | 450.000 |
| 20 | EventCard | Thẻ sự kiện | — | — |
| 21 | City | Chicago | USA / TP14 | 500.000 |
| 22 | City | Los Angeles | USA / TP15 | 550.000 |
| 23 | City | New York | USA / TP16 | 600.000 |
| 24 | Teleport | Dịch chuyển | — | — |
| 25 | Beach | Bãi biển | — | 190.000 |
| 26 | City | Manchester | UK / TP18 | 650.000 |
| 27 | City | London | UK / TP19 | 700.000 |
| 28 | EventCard | Thẻ sự kiện | — | — |
| 29 | City | Melbourne | Australia / TP20 | 750.000 |
| 30 | Tax | Thuế | — | — |
| 31 | City | Sydney | Australia / TP21 | 800.000 |

20 CityId: 1, 2, 3, 5, 6, 7, 9, 10, 11, 13, 15, 17, 19, 21, 22, 23, 26, 27, 29, 31.

4 BeachId: 4, 14, 18, 25. Tên hiển thị tất cả đều là “Bãi biển”.

8 ô chức năng: 0, 8, 12, 16, 20, 24, 28, 30.

CountryCatalog lấy tập TileId từ bảng, không suy đoán quốc gia theo khoảng chỉ số. Quốc gia 2 TP vẫn tính đủ một độc quyền. Melbourne/Sydney thuộc Úc, không phải Đức.

## 4. GameState — aggregate runtime

| Nhóm trường đề xuất | Ý nghĩa / bất biến |
|---|---|
| matchId, revision, matchStatus | Nhận diện trận trong bộ nhớ; revision tăng tại commit; Setup/Running/Finished |
| setup | Human/AI cùng máy, seat/role/timeLimit;2–4 người, trùng nghề hợp lệ, ngẫu nhiên người đầu rồi giữ thứ tự ghế |
| playersById, turnOrder | PlayerState duy nhất và thứ tự ghế ổn định; người phá sản bị bỏ qua khi chọn lượt |
| propertiesByTileId | Đúng 24 PropertyState; không tạo tài sản cho ô chức năng |
| turn | actorId, phase, activePersonalTurnOrdinal, lastDice, rollOrdinalWithinTurn, pendingDoubleContinuation |
| rollEntitlement | Quyền gieo kế tiếp và nguồn Normal/Double; Strategist Ultimate tiêu quyền đó, không tạo DiceResult hay quyền đôi mới |
| round | roundOrdinal, tập người đã hoàn tất lượt trong vòng, thông tin chờ quyết định Q08; không tính bằng tổng số lần tung |
| clock | Hữu hạn/vô hạn, remainingTime, timeExpired/endRequested, lý do pause quyết định Human/AI; hết giờ vẫn hoàn tất rootLandingId đang dở |
| fixedFestivalCityIds | Đúng 3 CityId khác nhau, cố định xuyên suốt trận |
| organizedFestival | Tối đa một CityId toàn trận, chủ tổ chức và tổng bonus50/100/150%...; không có hạn lượt; tách khỏi fixedFestivalCityIds |
| cardDrawState | Trình tự/cursor/next card do chính sách Q02 quyết định; dùng cùng nguồn cho rút và xem trước |
| activeEffects | Nơi lưu duy nhất EffectState runtime; có hạn theo lượt nguồn hoặc vòng mới sau khi nguồn phá sản; player/property chỉ query |
| pendingAction, pendingDecision, pendingObligations | Continuation, câu hỏi và danh sách nghĩa vụ; thẻ10 có batch nhiều payer. Snapshot theo payer chọn đúng PendingObligation, không tạo bản nợ thứ hai |
| result | WinnerPlayerIds, WinReason; để trống khi chưa kết thúc; nhiều winner khi đồng thắng |

Không lưu song song tổng tài sản, danh sách TP sở hữu, số biển và các tập độc quyền như các nguồn có thể ghi độc lập. Tính từ propertiesByTileId + catalog. Nếu cache để vẽ HUD, cache chỉ đọc, gắn revision và có thể bỏ đi.

## 5. PlayerState

| Trường đề xuất | Nội dung |
|---|---|
| playerId, seatIndex, displayName, controllerKind, roleId | ID không đổi; Human/AI không tạo khác biệt luật |
| cash | long; số dư sau giao dịch hoàn tất không âm; khoản thiếu nằm trong nghĩa vụ |
| positionTileId | 0–31; domain quản lý, không lấy ngược từ Transform |
| playerStatus | Active hoặc Bankrupt |
| isJailed | Trạng thái giam; đứng tại ô 8 và bị giam là dữ liệu khác với chỉ đi ngang ô 8 |
| jailTurnsStarted |0 lúc bị bắt;1/2/3 khi bắt đầu các lượt riêng còn bị giam; không tăng vì đôi hoặc lượt vừa bị bắt |
| jailReleasePending | Không/ForcedFeeDue/ReleaseAtTurnEnd; giữ nhánh thả sau trả bắt buộc, không nhầm với ra tù chủ động rồi gieo bình thường |
| completedPersonalTurnCount | Chỉ tăng một lần sau một lượt riêng; không tăng vì ra đôi |
| heldCards | Set bốn loại được giữ, mỗi loại tối đa 1 |
| skillState | Thời điểm được dùng lại Active; ultimateUsed; tham chiếu effect theo truy vấn, không sao chép effect |
| outstandingLoan | Nếu có: số phải trả 110.000, dueAfterCompletedOwnTurnOrdinal, nguồn Active Banker |

Đề xuất một khoản vay đang chờ là đủ với cooldown 7 và hạn trả 5; không mở rộng thành hệ thống tín dụng. Nếu sau này chốt tình huống luật cho phép nhiều khoản, hợp đồng phải được duyệt lại.

Giới hạn3 lượt là luật mới đã duyệt, thay Word cũ; có thể là hằng trong catalog, không là tùy chọn cho người chơi. Bị bắt giữa lượt đặt jailTurnsStarted=0; đầu lượt riêng tiếp theo tăng1. Lượt3 tung không đôi tạo ForcedJailReleaseFee200.000, cho thanh lý; trả thành công đặt ReleaseAtTurnEnd, không tạo movement/roll mới. Thoát sớm/đôi theo luật thường và kết thúc đợt giam; bị bắt lại tạo đợt mới. Phá sản trả mọi PropertyState về ngân hàng, không chuyển cho chủ nợ.

## 6. PropertyState, tiền và vốn

| Trường đề xuất | Nội dung |
|---|---|
| propertyId / tileId | Cùng ID ô; loại City/Beach tra catalog |
| ownerId tùy chọn | null = ngân hàng/chưa chủ; không tạo người chơi ngân hàng giả |
| cityLevel tùy chọn | Chỉ City có chủ: Stake, House1, House2, House3, Hotel |
| investedByTier | Sổ vốn thực Cọc/Nhà1/Nhà2/Nhà3/Khách sạn của chủ hiện tại; chỉ dùng cho City có chủ |
| beachActualInvestment | Vốn thực của Beach có chủ; không giả lập cấp nhà cho biển |
| lastOwnershipChangeActionId; acquiredOnLandingId, house3ReachedOnLandingId tùy chọn | Lưu nguồn đổi chủ và lần dừng đã mua/đạt Nhà3; mọi nút bấm trong cùng lần dừng dùng cùng landingId, không chỉ so actionId |
| normalUpgradeUsedOnLandingId tùy chọn | Chặn quyết định nâng thường thứ2 trong cùng lần dừng, kể cả actionId khác; không dùng marker này để tự cấm ngoại lệ kỹ năng |

**Unowned không nằm trong enum cấp nhà.** City chưa chủ có ownerId=null, cityLevel=null, vốn bằng 0; Beach luôn cityLevel=null.

Board logic A tạo landingId cho mỗi lần dừng; view B chỉ nhận ID. Mọi lựa chọn cùng lần dừng dùng cùng landingId. So mốc đạt Nhà3 cho Hotel thường; normalUpgradeUsedOnLandingId chặn nâng thường lần2. Q12 đã duyệt ngoại lệ Active/Ultimate Kỹ Sư lên Hotel cùng lần đến; không dùng marker nâng thường để cấm ngoại lệ kỹ năng.

Giá niêm yết hiện tại được tính từ catalog và cấp, không lấy từ vốn:

| Cấp | Tỷ lệ giá Nhà3 | Phần chênh lệch của bậc | Hệ số thuê trên 5% Nhà3 |
|---|---:|---:|---:|
| Stake (Cọc) | 40% | 40% | 1,5 |
| House1 | 60% | 20% | 2,5 |
| House2 | 80% | 20% | 4 |
| House3 | 100% | 20% | 6 |
| Hotel | 120% | 20% | 8 |

- Mua trực tiếp cấp cao: phân bổ số thực trả theo tỷ trọng chênh lệch các bậc đã có. TP1 Nhà3 100.000 → [40.000, 20.000, 20.000, 20.000, 0].
- Mua TP1 Nhà3 với tổng giảm 25% → thực chi 75.000, vốn [30.000, 15.000, 15.000, 15.000, 0].
- Cưỡng chế TP1 Nhà3 giá 200.000 → vốn chủ mới [80.000, 40.000, 40.000, 40.000, 0]; không kế thừa vốn cũ, không cộng tiền thuê.
- Nâng miễn phí: chỉ thêm cấp và vốn bậc mới = 0, không chia lại vốn cũ.
- Động đất: xóa vốn đúng bậc bị hạ, không hoàn tiền và không phân bổ lại phần còn lại.
- Bán ngân hàng: 50% tổng vốn thực còn lại. Phá tài sản hợp lệ: hoàn 100% tổng vốn còn lại.
- Q13 đã duyệt: nghiệp vụ chưa có quy tắc riêng floor số thanh toán cuối; không floor các bước trung gian.
- Khi chia vốn cho nhiều bậc, dùng tỷ trọng chính xác, lấy phần nguyên rồi phân đồng dư theo phần lẻ giảm dần. **P01 đã duyệt:** phần lẻ bằng nhau ưu tiên Cọc→Nhà1→Nhà2→Nhà3→Hotel; tổng đúng tiền thực trả.103 theo40:20:20:20 → [41,21,21,20,0].

RentQuote đề xuất chứa baseRent, các modifier có nguồn, multiplier, finalRent; chỉ là kết quả query, không phải một số thuê lưu lâu dài. Với City: cộng đại số tỷ lệ trên thuê gốc → nhân đôi nếu hợp lệ → floor kết quả cuối. Thuê Beach gốc: 1/2/3 biển tương ứng 50.000/100.000/150.000. Word xác định biển là BĐS; các modifier thuê BĐS và thẻ Giảm thuê không giới hạn riêng TP, xem D07. Q07 giữ PENDING để truy vết, không chặn lại tình huống đã đủ luật Word.

## 7. Thẻ, kỹ năng, hiệu ứng và nghĩa vụ

### 7.1. CardDefinition và HeldCards

CardId đề xuất ánh xạ STT GAME_RULES §8, không phải số lượng bản sao trong deck:

| CardId | Thẻ | Giữ được |
|---:|---|---|
| 1 | Tiền thưởng | Không |
| 2 | Động đất | Không |
| 3 | Miễn thuế | Có |
| 4 | Giảm tiền thuê | Có |
| 5 | Bảo vệ công trình | Có |
| 6 | Nộp phạt thuế | Không |
| 7 | Vào nhà tù | Không |
| 8 | Ra tù | Có |
| 9 | Phá tài sản | Không |
| 10 | Quà từ đối thủ | Không |
| 11 | Đến ô dịch chuyển | Không |
| 12 | Tổ chức lễ hội | Không |
| 13 | Cúp điện | Không |
| 14 | Tặng quà 200.000 | Không |

CardDefinition chứa mô tả, loại giữ, điều kiện mục tiêu và thông số theo luật. Không tự coi 14 loại là bộ bài chỉ có 14 lá, xác suất bằng nhau hoặc bỏ vào đáy sau rút (Q02).

HeldCards là set CardId thuộc {3,4,5,8}. Rút trùng loại đang giữ → bỏ thẻ mới. Bảo vệ tự dùng chỉ khi tác động hợp lệ; Miễn thuế/Giảm thuê do người chơi quyết định. CardDrawState phải cho Strategist nhìn đúng lá kế tiếp, không tạo RNG lần hai khi preview.

### 7.2. SkillState và lịch theo lượt riêng

RoleId: Investor, Entrepreneur, Engineer, Lawyer, Banker, Strategist. Passive/Weakness tra RoleDefinition, không bật/tắt tùy tiện trong runtime. UltimateUsed chỉ chuyển sang true khi hành động thành công.

| RoleId | ActiveCooldown, số lượt riêng tiếp theo |
|---|---:|
| Investor | 5 |
| Entrepreneur | 6 |
| Engineer | 7 |
| Lawyer | 6 |
| Banker | 7 |
| Strategist | 5 |

Đề xuất skillState lưu activeReadyAfterCompletedOwnTurnOrdinal thay cho giảm một biến mỗi lần animation kết thúc. Nếu dùng Active ở lượt riêng k với cooldown N, điều kiện thời gian là đã hoàn tất lượt k+N; cuối lượt k không tính là một trong N lượt tiếp theo. Ví dụ Investor dùng lượt 1 → sau khi hoàn tất lượt 6 mới hết khóa thời gian; chỉ kích hoạt lại tại cửa sổ Q04 đã duyệt và khi đáp ứng các điều kiện khác của kỹ năng.

Khoản vay Banker phát sinh lượt k: nhận 100.000, nghĩa vụ 110.000 đến hạn cuối lượt k+5. Cooldown là 7, độc lập hạn nợ 5.

### 7.3. EffectState

Một danh sách activeEffects trong GameState; mỗi record đề xuất có effectId, effectKind, sourcePlayerId, sourceActionId, targetPlayerId/targetTileId, activatedOwnTurnOrdinal, expiryMode, expiresAfterCompletedSourceTurnOrdinal hoặc expiresBeforeSourceTurnOrdinal tùy chọn theo mode, remainingUses tùy chọn.

Không cần một hệ buff tổng quát có ngôn ngữ biểu thức. Chỉ có các loại cụ thể theo luật:

- OrganizedFestival (tên biểu diễn đề xuất): một City toàn trận, tách ba festival cố định. Cùng chủ/cùng City mỗi200.000 thêm50% thuê gốc; không hết hạn. Đổi City/người tổ chức thay bằng50%; đổi chủ/bán/phá/về ngân hàng thì xóa. Không giữ TTL2 lượt của TemporaryFestival cũ.
- PowerOutage: một City đối thủ chưa Hotel, 3 lượt riêng tiếp theo của người dùng; đổi chủ hoặc thành Hotel thì xóa ngay.
- NextBankPurchaseDiscount: Investor Active thêm15% cho lần mua ngân hàng tiếp theo; coupon chưa dùng thì không kích hoạt lại, dù cooldown hết; không tích/ghi đè Q16.
- RentBoost: Entrepreneur Active +30%,2 lượt riêng theo luật; RentDouble Ultimate dùng expiryMode=BeforeNextSourceTurn và expiresBeforeSourceTurnOrdinal=k+1 khi kích hoạt lượt k. Có hiệu lực ngay, bao gồm đối thủ xen giữa; không dùng EndOfSourceTurn(k+1), không tăng mốc vì đôi.
- CancelNextRent: Lawyer Active là lựa chọn phản ứng trước chính nghĩa vụ thuê; chỉ áp dụng khi obligation còn dương và chưa dùng biện pháp chủ động khác (Q04/Q06).
- MandatoryPaymentImmunity: Lawyer Ultimate trong lượt kích hoạt, trừ nợ Banker/phí tự chọn. P02 đã duyệt miễn ForcedJailReleaseFee và thả cuối lượt không di chuyển; Q18 quà đã có oracle Word.

Thẻ giữ không biến thành buff theo lượt. Ba festival cố định và lễ hội tổ chức toàn cục đều không có TTL lượt nhưng là hai dữ liệu riêng. Effect có hạn theo sourcePlayerId; nếu nguồn phá sản, ghi lại N lượt còn lại rồi giữ qua vòng dở, hết sau N vòng mới hoàn chỉnh tiếp theo. Cần mode/deadline theo vòng riêng; không chờ counter của người đã bị loại tăng lại. Hủy do đổi chủ/Hotel vẫn ưu tiên Q15; không lấy lượt của một người khác thay thế nguồn.

### 7.4. PendingObligation và PendingDecision

PendingObligation đề xuất: obligationId, sourceKind, payerId, recipientId tùy chọn (null=ngân hàng), assessmentSnapshot, grossAmountNumerator/Denominator, fixedPayableAmount (long sau áp dụng quy tắc), appliedExistingEffects, activeMitigationUsed tùy chọn, stage. Thuế chụp cash/giá niêm yết lúc phát sinh, giữ giá trị chính xác tới số thanh toán cuối theo Q13; không floor thuế gốc rồi floor tiếp phần giảm, không tính lại sau thanh lý.

Q06: activeMitigationUsed tối đa một lựa chọn thành công/obligation. Áp hiệu lực sẵn có trước; payable0 khóa tiêu thêm. Cards/Skills trả ý định; Core ghi tiền và tiêu hao cùng một kết quả hợp lệ. ForcedJailReleaseFee200.000 có continuation thả cuối lượt; P02 cho Lawyer Ultimate miễn và vẫn thả, không di chuyển.

Các stage kỹ thuật: AwaitMitigationDecision, AwaitLiquidation, ReadyToSettle, Settled/Cancelled. Không trừ tiền hai lần khi popup/callback lặp. Tiền thiếu không biến cash thành số âm. Nếu phải phá sản, chuyển số tiền thực còn lại cho người nhận nếu có; phần thiếu không truy thu.

PendingDecision đề xuất: decisionId, actorId, actionId, rootLandingId, expectedRevision, decisionKind, validTargets/options, requiresClockPause. Popup chờ quyết định pause cho Human/AI như nhau theo Q20. actorId của quyết định có thể khác người đang có lượt khi phải thanh lý cho thẻ chuyển tiền.

PendingAction đề xuất có actionId, rootLandingId, sourceKind, continuationKind, currentStep, remainingParticipants, settledParticipantIds và relatedDecisionIds. Q19 thẻ10 tạo đồng thời nghĩa vụ cho mọi đối thủ; đủ tiền tự trả ngay, người thiếu cùng chờ. Lưu ai đã giải quyết để không thu lại; QuerySnapshot(payerId)/QueryLegalActions(payerId) dùng nghĩa vụ của đúng người, không tăng lượt riêng khi thanh toán ngoài lượt. Q20 giữ chuỗi sau hết giờ, chặn lệnh mới không liên quan.

MovementContext đề xuất: movementId, landingId, rootLandingId, sourceKind, from/destinationTileId, path, số lần qua0, diceResult tùy chọn, consumedRollEntitlementId, teleportOperationId và presentationPending. Q10: thẻ tới8/24 và dịch vụ đi tiến, có thưởng qua0; dịch vụ không chọn24, thẻ đưa lại24 được dùng tiếp. Không đặt cờ khóa toàn root. Strategist Ultimate không tạo đôi; thử tù có pending roll/jail-trial flag để gieo lại trước kết quả cuối. Q11 hủy gieo thêm khi bị bắt; thả cuối lượt3/P02 không tạo movement.

Số lần qua ô 0 tính cả khi ô 0 là điểm dừng của hành trình hợp lệ. Core cộng 200.000 cho mỗi lần được tính. Phí dịch chuyển 100.000 là khoản riêng.

EndTurnContext đề xuất lưu closingPersonalTurnOrdinal, pendingDueObligationIds, deadlineSettlementCompleted, countersUpdated, roundInterestApplied để Q09 chỉ chạy mỗi bước một lần sau popup/callback. Khi đã có thắng tức thời thì không chạy các bước không cần thiết. Mốc BeforeNextSourceTurn của RentDouble được kiểm tra ngay trước bắt đầu lượt chủ, độc lập các effect hết cuối lượt.

## 8. Enum đề xuất

| Enum | Giá trị/phạm vi |
|---|---|
| MatchStatus | Setup, Running, Finished |
| ControllerKind | Human, AI |
| MatchTimeLimit | Minutes30, Minutes45, Minutes60, Unlimited |
| TurnPhase | Các pha kỹ thuật trong ARCHITECTURE §6 |
| TileType | Start, City, Beach, Jail, EventCard, Festival, Teleport, Tax |
| CountryId | Vietnam, Japan, Korea, France, Italy, USA, UK, Australia |
| CityLevel | Stake, House1, House2, House3, Hotel; không chứa Unowned |
| RoleId | Investor, Entrepreneur, Engineer, Lawyer, Banker, Strategist |
| SkillKind | Passive, Active, Ultimate, Weakness |
| CardId | 1–14 như bảng; không dùng tên hiển thị làm ID |
| EffectKind | Các hiệu ứng cụ thể tại §7.3 |
| ObligationSource | Rent, TaxTile, TaxCard, GiftFromOpponents, GiftToOpponent, BankerDebt, ForcedJailReleaseFee |
| MovementSource | Dice, Teleport24, GoToJailCard, GoToTeleportCard, StrategistUltimate |
| ActionStatus | Accepted, Rejected, Pending |
| WinReason | ThreeCountries, FourBeaches, LastSolventPlayer, TimeCash, TimePropertyValue, TimeShared |

Giá trị số của enum mới chưa được nhóm chốt, ngoại trừ TileId và ánh xạ CardId đề xuất nêu trên. Chưa có serialization Save/Load; tránh tự phát sinh mã công khai không thể đổi.

## 9. ScriptableObject phù hợp và kiểm tra catalog

Đề xuất giữ số asset ít: một BoardCatalog tại Data/Board, một PropertyRulesCatalog tại Data/Properties, một RoleCatalog tại Data/Characters, một SkillCatalog tại Data/Skills và một CardCatalog tại Data/Cards. Có thể gộp Role/Skill khi nhóm muốn giảm số asset; chọn một nơi duy nhất cho mỗi thông số, không lặp cooldown ở hai catalog.

- BoardCatalog: vị trí/loại/tên/quốc gia/mã hiển thị và các giá tham chiếu của bảng.
- PropertyRulesCatalog: tỷ lệ cấp, thuê, phí, tỷ lệ hoàn vốn; không lặp giá từng TP.
- RoleCatalog/SkillCatalog: ID, mô tả, cooldown và thông số sáu nghề; một bộ dữ liệu chuẩn.
- CardCatalog: 14 định nghĩa thẻ; không chứa pile runtime hoặc chính sách rút chưa chốt.
- Tham chiếu Sprite/AudioClip nếu cần chỉ nằm ở SO/presentation; plain C# runtime dùng ID/catalog đã chuyển đổi, không phụ thuộc UnityEngine.Object.

Kiểm tra dữ liệu trước khi tạo trận: 32 ô khác ID; đúng 20 City + 4 Beach + 8 chức năng; đủ 8 nhóm quốc gia; giá theo bảng; 6 role; 14 card; 4 loại giữ; không TP17 giả; đủ quan hệ ID. Kiểm tra runtime: chủ hợp lệ, cấp/vốn đúng loại, effect có source/target hợp lệ, ba festival khác nhau, không state mutable trong SO.

Các kiểm tra hợp đồng kỹ thuật khác test luật; xem [TEST_CASES](TEST_CASES.md). Không tạo các SO hoặc class trong nhiệm vụ tài liệu này.

## 10. Quyền ghi và điểm bàn giao

Theo [TEAM_TASKS](TEAM_TASKS.md), A/Core là bên duy nhất ghi GameState, kể cả heldCards, cooldown, effect và nợ. C sở hữu định nghĩa/hành vi14 thẻ; D sở hữu định nghĩa6 nghề, kỹ năng và quyết định AI; cả hai cung cấp intent/modifier qua hợp đồng, không sở hữu một state kinh tế khác. B nhận vị trí/chủ/cấp để trình bày, không tính kết quả xúc xắc.

Giai đoạn0 phải chốt DTO snapshot/query, command/decision, result, gameplay event và callback actionId/landingId trước tích hợp. Các tên trường ở tài liệu này chưa là API được nhóm phê duyệt; dữ liệu giả lập chỉ phục vụ phát triển view/test, không thay thế nguồn trạng thái có thẩm quyền.
