using System;
using BtlGame.Core;
using BtlGame.Properties;

namespace BtlGame.Skills
{
    public static class SkillRules
    {
        // Giá trị dương là giảm giá, âm là phụ phí. Chỉ áp dụng cho mua chưa chủ từ ngân hàng.
        public static int BankPurchaseDiscountBasisPoints(RoleId role, bool investorCoupon)
        {
            ValidateRole(role);
            if (role == RoleId.Investor) return investorCoupon ? 2500 : 1000;
            return role == RoleId.Entrepreneur ? -500 : 0;
        }

        public static int NormalUpgradeDiscountBasisPoints(RoleId role)
        {
            ValidateRole(role);
            if (role == RoleId.Investor) return -1000;
            return role == RoleId.Engineer ? 1500 : 0;
        }

        public static int RentModifierBasisPoints(RoleId role)
        {
            ValidateRole(role);
            if (role == RoleId.Entrepreneur) return 1000;
            return role == RoleId.Engineer ? -1000 : 0;
        }

        public static int TaxCardReductionBasisPoints(RoleId role)
        {
            ValidateRole(role);
            return role == RoleId.Lawyer ? 2500 : 0;
        }

        public static long EngineerUltimateCost(int tileId, PropertyLevel currentLevel)
        {
            if (currentLevel < PropertyLevel.Stake || currentLevel >= PropertyLevel.Hotel)
                throw new ArgumentOutOfRangeException(nameof(currentLevel));
            long difference = EconomyRules.ListedPrice(tileId, PropertyLevel.Hotel) -
                EconomyRules.ListedPrice(tileId, currentLevel);
            // Ultimate trả 50% phần còn thiếu; không chồng Passive giảm 15% của nâng thường.
            return difference / 2;
        }

        private static void ValidateRole(RoleId role)
        {
            // None chỉ phục vụ fixture cũ chưa gắn nghề, không xuất hiện trong RoleCatalog.All.
            if (!Enum.IsDefined(typeof(RoleId), role)) throw new ArgumentOutOfRangeException(nameof(role));
        }
    }
}
