using System;
using System.Collections.Generic;

namespace BtlGame.Core
{
    public sealed partial class MatchCoordinator
    {
        private void EnsureOwnershipChangeAllowed(int tileId)
        {
            if (!CanChangeOwnership(tileId))
                throw new InvalidOperationException("Q15: cần quyết định lễ hội tạm khi đổi chủ.");
        }

        private void OnPropertyOwnershipChanging(int tileId)
        {
            if (State.Decisions.RemoveFestivalOnOwnershipChange != true) return;
            for (int i = State.ActiveEffects.Count - 1; i >= 0; i--)
                if (State.ActiveEffects[i].Kind == EffectKind.TemporaryFestival && State.ActiveEffects[i].TargetTileId == tileId)
                    State.RemoveEffect(State.ActiveEffects[i]);
        }

        private bool CanResolveSourceBankruptcy(int playerId)
        {
            if (State.Decisions.RemoveEffectsWhenSourceBankrupt == true) return true;
            if (State.Decisions.RemoveEffectsWhenSourceBankrupt == false &&
                State.Decisions.KeepOrphanEffectsThroughCurrentRound.HasValue) return true;
            foreach (TurnEffectState effect in State.ActiveEffects)
                if (effect.SourcePlayerId == playerId) return false;
            return true;
        }

        private void EnsureSourceBankruptcyEffectsDecided(int playerId)
        {
            if (!CanResolveSourceBankruptcy(playerId))
                throw new InvalidOperationException("Q17: chưa có mốc hiệu ứng khi nguồn bị loại.");
        }

        private void RemoveBankruptSourceEffects(int playerId)
        {
            if (State.Decisions.RemoveEffectsWhenSourceBankrupt == false &&
                State.Decisions.KeepOrphanEffectsThroughCurrentRound.HasValue)
            {
                RebaseEffectsAfterSourceBankruptcy(playerId);
                return;
            }
            if (State.Decisions.RemoveEffectsWhenSourceBankrupt != true) return;
            for (int i = State.ActiveEffects.Count - 1; i >= 0; i--)
                if (State.ActiveEffects[i].SourcePlayerId == playerId) State.RemoveEffect(State.ActiveEffects[i]);
        }

        public IReadOnlyList<string> QueryIntegrationBlocks()
        {
            var blocks = new List<string>();
            if (State.PendingRuleDecisionId != null) blocks.Add(State.PendingRuleDecisionId);
            if (!State.Decisions.InvestmentTieBreak.HasValue) blocks.Add("P01: thứ tự phá hòa vốn chưa được xác nhận.");
            if (!State.Decisions.EngineerHotelOnSameLanding.HasValue) blocks.Add("Q12: ngoại lệ Khách sạn cùng lần dừng.");
            if (!State.Decisions.ReplaceUnusedInvestorCoupon.HasValue) blocks.Add("Q16: kích hoạt khi còn coupon.");
            if (!State.Decisions.RemoveFestivalOnOwnershipChange.HasValue) blocks.Add("Q15: lễ hội khi đổi chủ.");
            if (!State.Decisions.RemoveEffectsWhenSourceBankrupt.HasValue) blocks.Add("Q17: hiệu ứng khi nguồn phá sản.");
            if (!State.Decisions.LawyerWaivesForcedJailFee.HasValue) blocks.Add("P02: miễn phí tù bắt buộc.");
            if (!State.Decisions.CardMovementAwardsStartBonus.HasValue) blocks.Add("Q10: đường đi của thẻ.");
            if (!State.Decisions.TeleportOncePerChain.HasValue) blocks.Add("Q10: lặp dịch vụ trong chuỗi.");
            if (State.Decisions.RemoveEffectsWhenSourceBankrupt == false && !State.Decisions.KeepOrphanEffectsThroughCurrentRound.HasValue)
                blocks.Add("Q17: mốc hết hạn thay thế khi nguồn bị loại.");
            if (!State.Decisions.EntrepreneurForcedPurchaseBasisPoints.HasValue) blocks.Add("Điểm yếu Doanh Nhân khi mua lại.");
            if (!State.GiftBatchActive && State.PendingCardId.HasValue && QueryCardActions(State.GetPlayer(State.CurrentPlayerId)).Count == 0)
                blocks.Add("C_CARD_NO_VALID_OPTION: kiểm tra policy hoặc ý định do module C cấp.");
            return blocks.AsReadOnly();
        }
    }
}
