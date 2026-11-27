#if UNITY_EDITOR
namespace BTLGame.EditorTools
{
    /// <summary>
    /// Nội dung mục "Hướng dẫn" ở MainMenu, tóm tắt đúng theo docs/GAME_RULES.md (cùng các quyết định trong docs/DECISIONS.md).
    /// Khi luật đổi, sửa ở đây rồi chạy Tools > CoTyPhu > Build All Screens.
    /// </summary>
    internal static class GuideText
    {
        private static string H(string title) => "\n<color=#E0A100><size=36>● </size></color><color=#2B3A9E><size=36><b>" + title + "</b></size></color>\n";

        internal static string Body =>

            H("Cách thắng") +
            "• Sở hữu trọn thành phố của <b>3 quốc gia bất kỳ</b>: thắng ngay.\n" +
            "• Sở hữu <b>cả 4 bãi biển</b>: thắng ngay.\n" +
            "• Chỉ còn <b>một người chưa phá sản</b>: người đó thắng.\n" +
            "• Hết giờ (30 / 45 / 60 phút): so <b>tiền mặt</b>, hòa thì so <b>tổng giá bất động sản</b>, vẫn hòa thì đồng thắng. Chế độ vô hạn không tính giờ.\n" +

            H("Chuẩn bị") +
            "• 2–4 người, chơi với Bot hoặc nhiều người trên cùng một máy; được trùng nghề.\n" +
            "• Mỗi người có <b>300.000đ</b>, bắt đầu ở ô Xuất phát. Người đi đầu chọn ngẫu nhiên.\n" +
            "• Đầu trận chọn ngẫu nhiên <b>3 thành phố lễ hội</b> (+50% tiền thuê suốt trận), có viền đèn và bóng bay.\n" +

            H("Một lượt chơi") +
            "• Tung hai xúc xắc rồi đi theo tổng điểm.\n" +
            "• <b>Ra đôi</b> được tung tiếp, không giới hạn, vẫn tính là cùng một lượt.\n" +
            "• Đi qua hoặc dừng ở ô Xuất phát nhận <b>200.000đ</b>.\n" +
            "• Xử lý ô vừa đến, dùng kỹ năng nếu muốn, rồi bấm Kết thúc lượt.\n" +

            H("Thành phố") +
            "• 20 thành phố thuộc 8 quốc gia. Cấp: Cọc → Nhà 1 → Nhà 2 → Nhà 3 → Khách sạn.\n" +
            "• Giá mỗi cấp so với giá Nhà 3: <b>40% / 60% / 80% / 100% / 120%</b>.\n" +
            "• Tiền thuê = 5% giá Nhà 3 × <b>1,5 / 2,5 / 4 / 6 / 8</b> theo cấp.\n" +
            "• Đất chưa chủ: mua Cọc đến Nhà 3, hoặc bỏ qua. Không mua thẳng Khách sạn.\n" +
            "• Dừng lại đất mình: nâng cấp một lần, trả phần chênh lệch. Khách sạn chỉ nâng ở lần dừng sau khi đã có Nhà 3.\n" +
            "• <b>Độc quyền một nước</b> (đủ mọi thành phố của nước đó): +20% tiền thuê.\n" +

            H("Bãi biển") +
            "• 4 bãi ở ô 4, 14, 18, 25; giá 100.000 / 130.000 / 160.000 / 190.000đ. Không nâng cấp.\n" +
            "• Tiền thuê mỗi bãi theo số bãi cùng chủ: 1 bãi <b>50.000</b>, 2 bãi <b>100.000</b>, 3 bãi <b>150.000đ</b>.\n" +

            H("Mua lại đất đối thủ") +
            "• Dừng ở thành phố đối thủ (Cọc đến Nhà 3): trả tiền thuê trước, sau đó được chọn <b>mua lại</b> với <b>200%</b> giá niêm yết. Chủ cũ không được từ chối.\n" +
            "• Không mua lại được Khách sạn và bãi biển.\n" +

            H("Bán tài sản và phá sản") +
            "• Có thể bán cho ngân hàng trước khi tung hoặc sau khi xử lý xong ô; nhận <b>50% vốn đã bỏ ra</b>.\n" +
            "• Phải trả tiền (thuê, thuế, phạt, nợ) mà không đủ thì thanh lý tài sản. Vẫn thiếu thì <b>phá sản</b>; tài sản về ngân hàng.\n" +

            H("Ô đặc biệt") +
            "• <b>Thẻ sự kiện</b>: rút ngẫu nhiên một trong 14 thẻ.\n" +
            "• <b>Tổ chức sự kiện (ô 16)</b>: trả 200.000đ để tổ chức lễ hội tại một thành phố của mình. Làm tiếp ở đúng thành phố đó thì cộng thêm 50% mỗi lần; đổi thành phố hoặc người khác tổ chức thì lễ hội cũ mất.\n" +
            "• <b>Dịch chuyển (ô 24)</b>: trả 100.000đ rồi bấm vào ô muốn bay tới.\n" +
            "• <b>Thuế (ô 30)</b>: nộp 20% tổng tài sản (tiền mặt + giá bất động sản).\n" +

            H("Nhà tù (ô 8)") +
            "• Dừng vào thì bị giam, đi ngang thì không. Vẫn thu tiền thuê khi đang ở tù.\n" +
            "• Mỗi lượt chọn: tung đôi để ra, trả 200.000đ, hoặc dùng thẻ Ra tù.\n" +
            "• Tối đa 3 lượt. Lượt thứ 3 tung không đôi thì bắt buộc trả 200.000đ và được thả cuối lượt.\n" +

            H("Mười bốn thẻ sự kiện") +
            "• Tiền thưởng: nhận 300.000đ.\n" +
            "• Động đất: hạ một cấp Nhà 1–3 của đối thủ.\n" +
            "• Miễn thuế (giữ lại).\n" +
            "• Giảm 50% tiền thuê (giữ lại).\n" +
            "• Bảo vệ công trình (giữ lại, tự kích hoạt).\n" +
            "• Nộp phạt thuế: 20% tổng tài sản.\n" +
            "• Vào nhà tù.\n" +
            "• Ra tù (giữ lại).\n" +
            "• Phá tài sản: phá một thành phố đối thủ (Cọc–Nhà 3), chủ được hoàn vốn.\n" +
            "• Quà từ đối thủ: mỗi đối thủ đưa bạn 100.000đ.\n" +
            "• Đến ô dịch chuyển.\n" +
            "• Tổ chức lễ hội: tùy chọn trả 200.000đ.\n" +
            "• Cúp điện: một thành phố đối thủ không thu được tiền thuê trong 3 lượt của bạn.\n" +
            "• Tặng quà: đưa một đối thủ 200.000đ.\n" +
            "Mỗi loại thẻ giữ chỉ được một lá. Khách sạn và bãi biển không bị phá hoại.\n" +

            H("Sáu nghề nghiệp") +
            "Mỗi nghề có nội tại, kỹ năng chủ động (hồi theo lượt của mình), chiêu cuối dùng một lần mỗi trận và điểm yếu:\n" +
            "• <b>Nhà Đầu Tư</b>: mua đất từ ngân hàng rẻ hơn; chiêu cuối mua lại từ xa với 150%; nâng cấp đắt hơn 10%.\n" +
            "• <b>Doanh Nhân</b>: thu thuê nhiều hơn; chiêu cuối nhân đôi tiền thuê; mua đất đắt hơn 5%.\n" +
            "• <b>Kỹ Sư</b>: nâng cấp rẻ hơn, nâng từ xa; chiêu cuối lên thẳng Khách sạn; tiền thuê cơ bản giảm 10%.\n" +
            "• <b>Luật Sư</b>: giảm thuế từ thẻ, hủy một lần trả thuê; chiêu cuối miễn các khoản bắt buộc trong lượt.\n" +
            "• <b>Banker</b>: lãi 1% tiền mặt mỗi vòng, vay 100.000 trả 110.000; chiêu cuối nhận 200.000đ.\n" +
            "• <b>Nhà Chiến Lược</b>: xem trước thẻ kế tiếp, tung lại xúc xắc; chiêu cuối chọn ô trong 1–6 bước.\n";
    }
}
#endif
