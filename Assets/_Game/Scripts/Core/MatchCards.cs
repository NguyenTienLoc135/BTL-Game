using System;
using System.Collections.Generic;
using BtlGame.Board;

namespace BtlGame.Core
{
    public sealed partial class MatchCoordinator
    {
        private int NormalForcedPurchaseBasisPoints(PlayerState buyer)
        {
            if (buyer.Role != RoleId.Entrepreneur) return 20000;
            return State.Decisions.EntrepreneurForcedPurchaseBasisPoints ??
                throw new InvalidOperationException("Cần xác nhận điểm yếu Doanh Nhân khi cưỡng chế.");
        }

        private IReadOnlyList<LegalAction> QueryCardActions(PlayerState player)
        {
            var actions = new List<LegalAction>();
            if (_cardProvider == null || !State.PendingCardId.HasValue) return actions.AsReadOnly();
            var offered = _cardProvider.QueryOptions(QuerySnapshot(), player.Id);
            if (offered == null) return actions.AsReadOnly();
            var choiceIds = new HashSet<int>();
            foreach (CardActionOption option in offered)
            {
                if (option == null || !choiceIds.Add(option.ChoiceId)) continue;
                try
                {
                    CardEffectPlan plan = _cardProvider.Plan(QuerySnapshot(), player.Id, option.ChoiceId);
                    ValidateCardPlan(player, plan);
                    if (option.TargetTileId != plan.TargetTileId || option.TargetPlayerId != plan.TargetPlayerId)
                        throw new InvalidOperationException("Mục tiêu hiển thị khác ý định thực thi.");
                    long cost = plan.Kind == CardEffectKind.Festival ? 200000 : 0;
                    actions.Add(new LegalAction(GameActionKind.CardChoice, player.Id, option.TargetTileId, null,
                        cost, State.Revision, State.DecisionId, option.Label, option.ChoiceId, option.TargetPlayerId));
                }
                catch (InvalidOperationException) { /* Nhánh thiếu luật hoặc mục tiêu không còn hợp lệ. */ }
                catch (ArgumentException) { /* Provider đã bỏ lựa chọn cũ; không tạo lệnh thực thi. */ }
            }
            return actions.AsReadOnly();
        }

        private void ValidateCardPlan(PlayerState actor, CardEffectPlan plan)
        {
            if (plan == null || plan.SourceCardId != State.PendingCardId)
                throw new InvalidOperationException("Ý định không khớp thẻ đang xử lý.");
            if (plan.SourceCardId == 10)
                throw new InvalidOperationException("Quà từ đối thủ được Core xử lý đồng thời ngay khi rút.");
            CardEffectKind expected;
            switch (plan.SourceCardId)
            {
                case 1: expected = CardEffectKind.BankCredit; break;
                case 2: expected = CardEffectKind.Earthquake; break;
                case 3: case 4: case 5: case 8: expected = CardEffectKind.StoreCard; break;
                case 6: expected = CardEffectKind.Tax; break;
                case 7: expected = CardEffectKind.SendJail; break;
                case 9: expected = CardEffectKind.Destroy; break;
                case 10: case 14: expected = CardEffectKind.Gift; break;
                case 11: expected = CardEffectKind.GoTeleport; break;
                case 12: expected = CardEffectKind.Festival; break;
                case 13: expected = CardEffectKind.PowerOutage; break;
                default: throw new InvalidOperationException("CardId không hợp lệ.");
            }
            if (plan.Kind == CardEffectKind.None)
            {
                bool canSkip = plan.SourceCardId == 12;
                if (plan.SourceCardId == 2 || plan.SourceCardId == 9 || plan.SourceCardId == 13)
                {
                    canSkip = true;
                    foreach (PropertyState property in State.Properties)
                        if (IsCardAttackTarget(actor.Id, property.TileId, expected)) { canSkip = false; break; }
                }
                if (!canSkip || plan.Amount != 0) throw new InvalidOperationException("Không được bỏ nghĩa vụ/thẻ bắt buộc.");
                return;
            }
            if (plan.Kind != expected) throw new InvalidOperationException("Tác động không đúng loại thẻ.");
            long requiredAmount = plan.SourceCardId == 1 ? 300000 : plan.SourceCardId == 10 ? 100000 :
                plan.SourceCardId == 14 ? 200000 : plan.SourceCardId == 12 ? 200000 : 0;
            if (plan.Amount != requiredAmount) throw new InvalidOperationException("Số tiền thẻ không đúng luật.");
            if (plan.Kind == CardEffectKind.BankCredit) checked { long cash = actor.Cash + 300000; }
            if (plan.Kind == CardEffectKind.Earthquake || plan.Kind == CardEffectKind.Destroy || plan.Kind == CardEffectKind.PowerOutage)
            {
                if (!plan.TargetTileId.HasValue || !IsCardAttackTarget(actor.Id, plan.TargetTileId.Value, plan.Kind))
                    throw new InvalidOperationException("Mục tiêu tấn công không hợp lệ.");
                if (plan.Kind == CardEffectKind.Destroy &&
                    !State.GetPlayer(State.GetProperty(plan.TargetTileId.Value).OwnerId.Value).HasHeldCard(5))
                    EnsureOwnershipChangeAllowed(plan.TargetTileId.Value);
            }
            if ((plan.Kind == CardEffectKind.SendJail || plan.Kind == CardEffectKind.GoTeleport) &&
                !State.Decisions.CardMovementAwardsStartBonus.HasValue)
                throw new InvalidOperationException("Q10: cần quyết định đường đi của thẻ.");
            if (plan.Kind == CardEffectKind.Festival)
            {
                if (!plan.TargetTileId.HasValue || actor.Cash < 200000 ||
                    BoardCatalog.Get(plan.TargetTileId.Value).Kind != TileKind.City ||
                    State.GetProperty(plan.TargetTileId.Value).OwnerId != actor.Id)
                    throw new InvalidOperationException("Không đủ điều kiện lễ hội.");
            }
            if (plan.Kind == CardEffectKind.Gift)
            {
                if (!plan.TargetPlayerId.HasValue || plan.TargetPlayerId.Value == actor.Id)
                    throw new InvalidOperationException("Cần đối thủ làm mục tiêu.");
                GetActivePlayer(plan.TargetPlayerId.Value);
            }
        }

        private bool IsCardAttackTarget(int actorId, int tileId, CardEffectKind kind)
        {
            if (BoardCatalog.Get(tileId).Kind != TileKind.City) return false;
            PropertyState property = State.GetProperty(tileId);
            return property.OwnerId.HasValue && property.OwnerId != actorId && property.Level != PropertyLevel.Hotel &&
                (kind != CardEffectKind.Earthquake || property.Level != PropertyLevel.Stake);
        }

        private MovementResult ExecuteCardChoice(PlayerState actor, CardEffectPlan plan)
        {
            ValidateCardPlan(actor, plan);
            MovementResult movement = null;
            switch (plan.Kind)
            {
                case CardEffectKind.BankCredit: actor.Cash = checked(actor.Cash + 300000); break;
                case CardEffectKind.Tax: AssessTax(actor.Id, ObligationKind.TaxCard); break;
                case CardEffectKind.Earthquake: ApplyEarthquake(actor.Id, plan.TargetTileId.Value); break;
                case CardEffectKind.Destroy: DestroyCityForCard(actor.Id, plan.TargetTileId.Value); break;
                case CardEffectKind.StoreCard: StoreHeldCard(actor.Id, plan.SourceCardId); break;
                case CardEffectKind.Festival: OrganizeFestival(actor.Id, plan.TargetTileId.Value, true); break;
                case CardEffectKind.PowerOutage:
                    PlayerState owner = State.GetPlayer(State.GetProperty(plan.TargetTileId.Value).OwnerId.Value);
                    if (owner.HasHeldCard(5)) owner.ConsumeHeldCard(5);
                    else AddSourceTurnDurationEffect(actor.Id, EffectKind.PowerOutage, 3, targetTileId: plan.TargetTileId);
                    break;
                case CardEffectKind.Gift:
                    CreateObligation(ObligationKind.Gift, actor.Id, plan.TargetPlayerId, 200000);
                    break;
            }
            State.PendingCardId = null;
            State.CardLandingResolved = true;
            State.PendingRuleDecisionId = null;
            if (plan.Kind == CardEffectKind.SendJail || plan.Kind == CardEffectKind.GoTeleport)
            {
                int destination = plan.Kind == CardEffectKind.SendJail ? 8 : 24;
                bool keepDouble = plan.Kind != CardEffectKind.SendJail;
                int steps = BoardRules.CountForwardPath(actor.Position, destination);
                movement = State.Decisions.CardMovementAwardsStartBonus == true && steps > 0
                    ? BoardRules.CalculateMovement(actor.Position, steps, source: MovementSource.Card)
                    : new MovementResult(actor.Position, destination, 0, 0, false, new List<int> { destination }, MovementSource.Card);
                ApplyMovement(actor, movement, keepDouble);
                QueuePresentation(movement);
            }
            State.Revision++;
            return movement;
        }
    }
}
