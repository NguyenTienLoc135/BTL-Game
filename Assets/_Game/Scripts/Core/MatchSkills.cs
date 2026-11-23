using System;
using BtlGame.Characters;
using BtlGame.Properties;
using BtlGame.Skills;

namespace BtlGame.Core
{
    public sealed partial class MatchCoordinator
    {
        private bool HasPurchaseCoupon(int playerId)
        {
            foreach (TurnEffectState effect in State.ActiveEffects)
                if (effect.SourcePlayerId == playerId && effect.Kind == EffectKind.PurchaseDiscount) return true;
            return false;
        }

        private int PurchaseDiscount(PlayerState buyer) =>
            SkillRules.BankPurchaseDiscountBasisPoints(buyer.Role, HasPurchaseCoupon(buyer.Id));

        private void ConsumePurchaseCoupon(int playerId)
        {
            for (int i = State.ActiveEffects.Count - 1; i >= 0; i--)
                if (State.ActiveEffects[i].SourcePlayerId == playerId && State.ActiveEffects[i].Kind == EffectKind.PurchaseDiscount)
                    State.RemoveEffect(State.ActiveEffects[i]);
        }

        private bool HasEffect(EffectKind kind, int playerId, int? tileId = null)
        {
            foreach (TurnEffectState effect in State.ActiveEffects)
                if (effect.Kind == kind && effect.SourcePlayerId == playerId &&
                    (!tileId.HasValue || effect.TargetTileId == tileId)) return true;
            return false;
        }

        private long EngineerUpgradeCost(PlayerState player, int tileId, bool ultimate)
        {
            PropertyState property = State.GetProperty(tileId);
            if (player.Role != RoleId.Engineer || BoardCatalog.Get(tileId).Kind != TileKind.City ||
                property.OwnerId != player.Id || !property.Level.HasValue || property.Level == PropertyLevel.Hotel)
                throw new InvalidOperationException("Cần thành phố của Kỹ Sư chưa là Khách sạn.");
            PropertyLevel target = ultimate ? PropertyLevel.Hotel : (PropertyLevel)((int)property.Level.Value + 1);
            if (target == PropertyLevel.Hotel && (property.House3ReachedLandingId == State.CurrentLandingId ||
                property.AcquiredOnLandingId == State.CurrentLandingId))
            {
                if (!State.Decisions.EngineerHotelOnSameLanding.HasValue)
                    throw new InvalidOperationException("Q12: chưa chốt ngoại lệ Khách sạn cùng lần dừng.");
                if (!State.Decisions.EngineerHotelOnSameLanding.Value)
                    throw new InvalidOperationException("Kỹ năng Khách sạn phải chờ lần dừng sau.");
            }
            long cost = ultimate ? SkillRules.EngineerUltimateCost(tileId, property.Level.Value) : 0;
            EconomyRules.AllocateUpgradeInvestment(cost, property.Level.Value, target, State.Decisions.InvestmentTieBreak);
            if (player.Cash < cost) throw new InvalidOperationException("Không đủ tiền nâng Khách sạn bằng Ultimate.");
            return cost;
        }

        private void ApplyEngineerUpgrade(PlayerState player, int tileId, bool ultimate)
        {
            long cost = EngineerUpgradeCost(player, tileId, ultimate);
            PropertyState property = State.GetProperty(tileId);
            PropertyLevel current = property.Level.Value;
            PropertyLevel target = ultimate ? PropertyLevel.Hotel : (PropertyLevel)((int)current + 1);
            var investment = EconomyRules.AllocateUpgradeInvestment(cost, current, target, State.Decisions.InvestmentTieBreak);
            player.Cash -= cost;
            for (int i = (int)current; i < (int)target; i++) property.MutableInvestedByLevel[i] = investment[i];
            property.Level = target;
            if (target == PropertyLevel.House3) property.House3ReachedLandingId = State.CurrentLandingId;
            if (target == PropertyLevel.Hotel) RemovePowerOutageForTile(tileId);
        }

        private void ExecuteActiveSkill(PlayerState player, int? tileId)
        {
            int cooldown = RoleCatalog.Get(player.Role).ActiveCooldownTurns;
            if (player.Role == RoleId.Lawyer)
            {
                MandatoryObligation obligation = State.PendingObligation;
                ApplyActiveMitigation(obligation.Id, 0, () => CommitActiveSkillUse(player.Id, cooldown));
                return;
            }
            EnsureActiveActionWindow(player.Id);
            if (!player.IsActiveReady) throw new InvalidOperationException("Kỹ năng còn cooldown.");
            switch (player.Role)
            {
                case RoleId.Investor:
                    if (HasPurchaseCoupon(player.Id))
                    {
                        if (!State.Decisions.ReplaceUnusedInvestorCoupon.HasValue)
                            throw new InvalidOperationException("Q16: coupon cũ chưa dùng.");
                        if (!State.Decisions.ReplaceUnusedInvestorCoupon.Value)
                            throw new InvalidOperationException("Cần dùng coupon hiện có trước.");
                    }
                    ConsumePurchaseCoupon(player.Id);
                    State.AddEffect(new TurnEffectState(_nextEffectId++, EffectKind.PurchaseDiscount,
                        player.Id, player.Id, null, 1500, null, null));
                    break;
                case RoleId.Entrepreneur:
                    if (HasEffect(EffectKind.RentBonus, player.Id)) throw new InvalidOperationException("Buff thuê vẫn còn.");
                    AddSourceTurnDurationEffect(player.Id, EffectKind.RentBonus, 2, modifierBasisPoints: 3000);
                    break;
                case RoleId.Engineer:
                    ApplyEngineerUpgrade(player, tileId.Value, false);
                    break;
                case RoleId.Banker:
                    IssueBankerLoan(player.Id, cooldown);
                    return;
                default:
                    throw new InvalidOperationException("Active này cần cửa sổ riêng.");
            }
            CommitActiveCooldown(player, cooldown);
            State.Revision++;
        }

        private void ExecuteUltimate(PlayerState player, int? tileId)
        {
            EnsureActiveActionWindow(player.Id);
            if (player.UltimateUsed) throw new InvalidOperationException("Ultimate đã dùng.");
            switch (player.Role)
            {
                case RoleId.Investor:
                    ForceBuyCity(player.Id, tileId.Value, 15000, true);
                    break;
                case RoleId.Entrepreneur:
                    ActivateRentDoubleUltimate(player.Id);
                    return;
                case RoleId.Engineer:
                    ApplyEngineerUpgrade(player, tileId.Value, true);
                    break;
                case RoleId.Lawyer:
                    AddSourceTurnDurationEffect(player.Id, EffectKind.PaymentImmunity, 1);
                    break;
                case RoleId.Banker:
                    player.Cash = checked(player.Cash + 200000);
                    break;
                default:
                    throw new InvalidOperationException("Ultimate này cần quyền gieo và cửa sổ riêng.");
            }
            player.UltimateUsed = true;
            State.Revision++;
        }
    }
}
