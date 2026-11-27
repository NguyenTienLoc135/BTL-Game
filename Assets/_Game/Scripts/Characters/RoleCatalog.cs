using System;
using System.Collections.Generic;
using BtlGame.Core;

namespace BtlGame.Characters
{
    public sealed class RoleDefinition
    {
        public RoleId Id { get; }
        public string DisplayName { get; }
        public string PassiveDescription { get; }
        public string ActiveDescription { get; }
        public int ActiveCooldownTurns { get; }
        public string UltimateDescription { get; }
        public string WeaknessDescription { get; }

        internal RoleDefinition(RoleId id, string displayName, string passiveDescription,
            string activeDescription, int activeCooldownTurns, string ultimateDescription, string weaknessDescription)
        {
            Id = id;
            DisplayName = displayName;
            PassiveDescription = passiveDescription;
            ActiveDescription = activeDescription;
            ActiveCooldownTurns = activeCooldownTurns;
            UltimateDescription = ultimateDescription;
            WeaknessDescription = weaknessDescription;
        }
    }

    public static class RoleCatalog
    {
        private static readonly IReadOnlyList<RoleDefinition> Definitions = Array.AsReadOnly(new[]
        {
            new RoleDefinition(RoleId.Investor, "Nhà Đầu Tư",
                "Giảm 10% giá mua bất động sản chưa có chủ từ ngân hàng; không giảm giá mua lại.",
                "Thêm 15% giảm cho lần mua tiếp theo từ ngân hàng, chờ đến khi dùng; cùng Passive giảm tổng 25%.", 5,
                "Một lần/trận: mua từ xa thành phố đối thủ Cọc–Nhà 3 với 150% giá niêm yết, không trả thuê; không mua biển/Khách sạn.",
                "Chi phí nâng cấp thường tăng 10%."),
            new RoleDefinition(RoleId.Entrepreneur, "Doanh Nhân",
                "Tiền thuê bất động sản nhận từ đối thủ tăng 10%.",
                "Tiền thuê tăng 30% trong 2 lượt riêng tiếp theo của người dùng.", 6,
                "Một lần/trận: nhân đôi khoản thuê ngay khi kích hoạt, hết ngay trước lượt riêng tiếp theo của mình.",
                "Giá mua bất động sản tăng 5%."),
            new RoleDefinition(RoleId.Engineer, "Kỹ Sư",
                "Chi phí nâng cấp thường giảm 15%.",
                "Nâng miễn phí đúng một cấp thành phố mình sở hữu từ xa, kể cả Nhà 3 lên Khách sạn ngay cùng lần đến, tại điểm hành động hợp lệ.", 7,
                "Một lần/trận: nâng thành phố mình từ xa thẳng Khách sạn, được ngay cùng lần đến; trả 50% chênh lệch giá niêm yết còn thiếu, không cộng Passive.",
                "Tiền thuê cơ bản giảm 10%."),
            new RoleDefinition(RoleId.Lawyer, "Luật Sư",
                "Giảm 25% khoản thuế từ thẻ Nộp phạt thuế; không giảm thuế ô 30.",
                "Hủy một lần trả thuê, dùng trước khi thanh toán nghĩa vụ thuê của mình.", 6,
                "Một lần/trận: miễn nghĩa vụ bắt buộc trong lượt kích hoạt, kể cả phí tù bắt buộc và được thả cuối lượt; không miễn phí tự chọn hoặc nợ Banker.",
                "Không có kỹ năng tạo tiền trực tiếp."),
            new RoleDefinition(RoleId.Banker, "Banker",
                "Cuối mỗi vòng chung, nhận 1% tiền mặt dương còn lại, làm tròn xuống đồng.",
                "Nhận khoản vay 100.000; trả 110.000 cuối lượt riêng thứ 5 tiếp theo, phải thanh lý nếu thiếu.", 7,
                "Một lần/trận: nhận ngay 200.000 từ ngân hàng.",
                "Không có ưu đãi giảm giá khi mua bất động sản."),
            new RoleDefinition(RoleId.Strategist, "Nhà Chiến Lược",
                "Xem trước thẻ sự kiện kế tiếp từ cùng trình tự rút thẻ của game.",
                "Gieo lại cả hai xúc xắc trước khi chốt, gồm lần thử ra tù; phải nhận kết quả mới để quyết định ra tù, phí và di chuyển.", 5,
                "Một lần/trận: thay một quyền gieo hợp lệ bằng chọn ô phía trước 1–6 bước; kích hoạt ô đến và không tạo quyền gieo thêm.",
                "Không có ưu đãi kinh tế cố định.")
        });

        public static IReadOnlyList<RoleDefinition> All => Definitions;

        public static RoleDefinition Get(RoleId role)
        {
            int index = (int)role - 1;
            if (index < 0 || index >= Definitions.Count)
                throw new ArgumentOutOfRangeException(nameof(role), "Cần một trong sáu nghề nghiệp hợp lệ.");
            return Definitions[index];
        }
    }
}
