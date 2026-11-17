namespace BtlGame.Core
{
    public enum InvestmentTieBreak { LowestTierFirst, HighestTierFirst }

    /// <summary>Null nghĩa là chưa có quyết định; không dùng một mặc định để tự chốt luật.</summary>
    public sealed class GameplayDecisions
    {
        // Chỉ chứa các câu trả lời đã được người dùng phê duyệt ngày 2026-11-17.
        // Null còn lại được giữ để chặn nhánh chưa có quyết định.
        public static GameplayDecisions Approved => new GameplayDecisions(
            investmentTieBreak: BtlGame.Core.InvestmentTieBreak.LowestTierFirst,
            lawyerWaivesForcedJailFee: true, engineerHotelOnSameLanding: true, replaceUnusedInvestorCoupon: false,
            removeFestivalOnOwnershipChange: true, removeEffectsWhenSourceBankrupt: false,
            cardMovementAwardsStartBonus: true, teleportOncePerChain: false,
            entrepreneurForcedPurchaseBasisPoints: 20000, keepOrphanEffectsThroughCurrentRound: true);
        public InvestmentTieBreak? InvestmentTieBreak { get; }
        public bool? LawyerWaivesForcedJailFee { get; }
        public bool? EngineerHotelOnSameLanding { get; }
        public bool? ReplaceUnusedInvestorCoupon { get; }
        public bool? RemoveFestivalOnOwnershipChange { get; }
        public bool? RemoveEffectsWhenSourceBankrupt { get; }
        public bool? CardMovementAwardsStartBonus { get; }
        public bool? TeleportOncePerChain { get; }
        public int? EntrepreneurForcedPurchaseBasisPoints { get; }
        public bool? KeepOrphanEffectsThroughCurrentRound { get; }

        public GameplayDecisions(InvestmentTieBreak? investmentTieBreak = null,
            bool? lawyerWaivesForcedJailFee = null, bool? engineerHotelOnSameLanding = null,
            bool? replaceUnusedInvestorCoupon = null, bool? removeFestivalOnOwnershipChange = null,
            bool? removeEffectsWhenSourceBankrupt = null, bool? cardMovementAwardsStartBonus = null,
            bool? teleportOncePerChain = null,
            int? entrepreneurForcedPurchaseBasisPoints = null, bool? keepOrphanEffectsThroughCurrentRound = null)
        {
            InvestmentTieBreak = investmentTieBreak; LawyerWaivesForcedJailFee = lawyerWaivesForcedJailFee;
            EngineerHotelOnSameLanding = engineerHotelOnSameLanding;
            ReplaceUnusedInvestorCoupon = replaceUnusedInvestorCoupon;
            RemoveFestivalOnOwnershipChange = removeFestivalOnOwnershipChange;
            RemoveEffectsWhenSourceBankrupt = removeEffectsWhenSourceBankrupt;
            CardMovementAwardsStartBonus = cardMovementAwardsStartBonus;
            TeleportOncePerChain = teleportOncePerChain;
            if (entrepreneurForcedPurchaseBasisPoints.HasValue && entrepreneurForcedPurchaseBasisPoints != 20000 &&
                entrepreneurForcedPurchaseBasisPoints != 20500 && entrepreneurForcedPurchaseBasisPoints != 21000)
                throw new System.ArgumentOutOfRangeException(nameof(entrepreneurForcedPurchaseBasisPoints));
            EntrepreneurForcedPurchaseBasisPoints = entrepreneurForcedPurchaseBasisPoints;
            KeepOrphanEffectsThroughCurrentRound = keepOrphanEffectsThroughCurrentRound;
        }
    }
}
