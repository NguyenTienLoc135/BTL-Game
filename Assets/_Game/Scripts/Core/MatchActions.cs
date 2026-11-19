using System;
using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Properties;
using BtlGame.Skills;

namespace BtlGame.Core
{
    public sealed partial class MatchCoordinator
    {
        public IReadOnlyList<LegalAction> QueryLegalActions(int playerId)
        {
            var options = new List<LegalAction>();
            if (State.Status != MatchStatus.Running || State.Phase == TurnPhase.Moving ||
                State.Phase == TurnPhase.AwaitingRuleDecision) return options.AsReadOnly();
            PlayerState player;
            try { player = State.GetPlayer(playerId); }
            catch (ArgumentException) { return options.AsReadOnly(); }
            if (player.Status != PlayerStatus.Active) return options.AsReadOnly();

            void Add(GameActionKind kind, int? tile = null, PropertyLevel? level = null, long cost = 0, string label = null)
            {
                options.Add(new LegalAction(kind, playerId, tile, level, cost, State.Revision, State.DecisionId,
                    label ?? ActionLabels.Describe(kind, tile, level)));
            }

            MandatoryObligation obligation = State.GiftBatchActive ? State.FindPendingForPayer(playerId) : State.PendingObligation;
            if (obligation != null)
            {
                if (obligation.PayerId != playerId) return options.AsReadOnly();
                if (player.Cash >= obligation.PayableAmount) Add(GameActionKind.SettleObligation, cost: obligation.PayableAmount);
                else
                {
                    foreach (PropertyState property in State.Properties)
                        if (property.OwnerId == playerId && CanChangeOwnership(property.TileId)) Add(GameActionKind.LiquidateProperty, property.TileId);
                    if (!HasProperty(playerId) && CanResolveSourceBankruptcy(playerId)) Add(GameActionKind.DeclareBankruptcy);
                }
                if (obligation.PayableAmount > 0 && !obligation.ActiveMitigationUsed)
                {
                    if (obligation.Kind == ObligationKind.Rent)
                    {
                        if (player.Role == RoleId.Lawyer && player.IsActiveReady) Add(GameActionKind.ActivateSkill);
                        if (player.HasHeldCard(4)) Add(GameActionKind.UseRentCard);
                    }
                    if ((obligation.Kind == ObligationKind.TaxTile || obligation.Kind == ObligationKind.TaxCard) && player.HasHeldCard(3))
                        Add(GameActionKind.UseTaxCard);
                }
                return options.AsReadOnly();
            }
            if (State.GiftBatchActive || State.CurrentPlayerId != playerId) return options.AsReadOnly();
            if (State.Phase == TurnPhase.AwaitingRollConfirmation)
            {
                Add(GameActionKind.ConfirmRoll);
                if (!State.TimeExpired && player.Role == RoleId.Strategist && player.IsActiveReady)
                    Add(GameActionKind.RerollDice);
                return options.AsReadOnly();
            }
            if (State.Phase == TurnPhase.ResolvingLanding)
            {
                TileDefinition tile = BoardCatalog.Get(player.Position);
                if (!State.LandingResolutionStarted) return options.AsReadOnly();
                if (tile.Kind == TileKind.EventCard && !State.CardLandingResolved) return QueryCardActions(player);
                if (tile.Kind == TileKind.City || tile.Kind == TileKind.Beach)
                {
                    PropertyState property = State.GetProperty(tile.Id);
                    if (!State.CurrentLandingDecisionUsed)
                    {
                        if (!property.OwnerId.HasValue)
                        {
                            if (tile.Kind == TileKind.Beach)
                            {
                                long cost = EconomyRules.RentToPay(tile.BeachPrice, PurchaseDiscount(player));
                                if (player.Cash >= cost) Add(GameActionKind.BuyBeach, tile.Id, cost: cost);
                            }
                            else for (int i = 1; i <= 4; i++)
                            {
                                var level = (PropertyLevel)i;
                                long cost = EconomyRules.RentToPay(EconomyRules.ListedPrice(tile.Id, level), PurchaseDiscount(player));
                                if (player.Cash >= cost && CanAllocate(cost, level)) Add(GameActionKind.BuyCity, tile.Id, level, cost);
                            }
                        }
                        else if (property.OwnerId == playerId && tile.Kind == TileKind.City)
                        {
                            for (int i = (int)property.Level.Value + 1; i <= 5; i++)
                            {
                                var level = (PropertyLevel)i;
                                if (level == PropertyLevel.Hotel && (property.Level != PropertyLevel.House3 ||
                                    property.House3ReachedLandingId == State.CurrentLandingId)) continue;
                                long difference = EconomyRules.ListedPrice(tile.Id, level) - EconomyRules.ListedPrice(tile.Id, property.Level.Value);
                                long cost = EconomyRules.RentToPay(difference, SkillRules.NormalUpgradeDiscountBasisPoints(player.Role));
                                if (player.Cash >= cost && CanAllocateUpgrade(cost, property.Level.Value, level))
                                    Add(GameActionKind.UpgradeCity, tile.Id, level, cost);
                            }
                        }
                        else if (tile.Kind == TileKind.City && property.Level != PropertyLevel.Hotel &&
                            (player.Role != RoleId.Entrepreneur || State.Decisions.EntrepreneurForcedPurchaseBasisPoints.HasValue) &&
                            State.RentSettledAtCurrentLandingTileId == tile.Id && CanChangeOwnership(tile.Id))
                        {
                            long cost = checked(EconomyRules.ListedPrice(property) * NormalForcedPurchaseBasisPoints(player)) / 10000;
                            if (player.Cash >= cost && CanAllocate(cost, property.Level.Value))
                                Add(GameActionKind.ForceBuyCity, tile.Id, cost: cost);
                        }
                    }
                }
                if (tile.Kind == TileKind.Teleport && (!State.TeleportUsedInCurrentChain || State.Decisions.TeleportOncePerChain == false) && player.Cash >= TeleportFee)
                    foreach (TileDefinition target in BoardCatalog.Tiles)
                        if (target.Id != 24) Add(GameActionKind.Teleport, target.Id, cost: TeleportFee);
                if (tile.Kind == TileKind.Festival && player.Cash >= 200000)
                    foreach (PropertyState target in State.Properties)
                        if (BoardCatalog.Get(target.TileId).Kind == TileKind.City && target.OwnerId == playerId)
                            Add(GameActionKind.OrganizeFestival, target.TileId, cost: 200000);
                Add(GameActionKind.SkipLanding, label: "Hoàn tất ô / Bỏ qua");
                return options.AsReadOnly();
            }
            bool checkpoint = State.Phase == TurnPhase.AwaitingRoll || State.Phase == TurnPhase.EndTurn ||
                State.Phase == TurnPhase.AwaitingJailChoice;
            if (!checkpoint || (State.ClockPaused && !State.DecisionAllowsSkills)) return options.AsReadOnly();
            if (State.Phase == TurnPhase.EndTurn) Add(GameActionKind.EndTurn);
            if (State.PersonalTurnMustClose) return options.AsReadOnly();
            if (State.TimeExpired) return options.AsReadOnly();
            if (State.Phase == TurnPhase.AwaitingRoll && !player.IsJailed) Add(GameActionKind.RollDice);
            if (State.Phase == TurnPhase.AwaitingJailChoice)
            {
                Add(GameActionKind.JailRoll);
                if (player.Cash >= 200000) Add(GameActionKind.JailPay, cost: 200000);
                if (player.HasHeldCard(8)) Add(GameActionKind.JailCard);
            }
            foreach (PropertyState property in State.Properties)
                if (property.OwnerId == playerId && CanChangeOwnership(property.TileId)) Add(GameActionKind.SellProperty, property.TileId);
            AddSkillOptions(player, options);
            return options.AsReadOnly();
        }

        private void AddSkillOptions(PlayerState player, List<LegalAction> options)
        {
            void Add(GameActionKind kind, int? tile = null, long cost = 0) => options.Add(new LegalAction(kind,
                player.Id, tile, null, cost, State.Revision, State.DecisionId, ActionLabels.Describe(kind, tile, null)));
            if (player.Role == RoleId.None) return;
            if (player.IsActiveReady)
            {
                if (player.Role == RoleId.Investor && (!HasPurchaseCoupon(player.Id) || State.Decisions.ReplaceUnusedInvestorCoupon == true))
                    Add(GameActionKind.ActivateSkill);
                if (player.Role == RoleId.Entrepreneur && !HasEffect(EffectKind.RentBonus, player.Id)) Add(GameActionKind.ActivateSkill);
                if (player.Role == RoleId.Banker && !player.BankerDebtDueAtTurnOrdinal.HasValue) Add(GameActionKind.ActivateSkill);
                if (player.Role == RoleId.Engineer)
                    foreach (PropertyState property in State.Properties)
                        if (TryEngineerCost(player, property.TileId, false, out long cost)) Add(GameActionKind.ActivateSkill, property.TileId, cost);
            }
            if (player.UltimateUsed) return;
            switch (player.Role)
            {
                case RoleId.Entrepreneur:
                case RoleId.Lawyer:
                case RoleId.Banker:
                    Add(GameActionKind.UseUltimate);
                    break;
                case RoleId.Investor:
                    foreach (PropertyState property in State.Properties)
                    {
                        if (BoardCatalog.Get(property.TileId).Kind != TileKind.City || !property.OwnerId.HasValue ||
                            property.OwnerId == player.Id || property.Level == PropertyLevel.Hotel || !CanChangeOwnership(property.TileId)) continue;
                        long cost = EconomyRules.ListedPrice(property) * 3 / 2;
                        if (player.Cash >= cost && CanAllocate(cost, property.Level.Value)) Add(GameActionKind.UseUltimate, property.TileId, cost);
                    }
                    break;
                case RoleId.Engineer:
                    foreach (PropertyState property in State.Properties)
                        if (TryEngineerCost(player, property.TileId, true, out long cost)) Add(GameActionKind.UseUltimate, property.TileId, cost);
                    break;
                case RoleId.Strategist:
                    if (State.Phase == TurnPhase.AwaitingRoll && !player.IsJailed)
                        for (int steps = 1; steps <= 6; steps++) Add(GameActionKind.UseUltimate, (player.Position + steps) % 32);
                    break;
            }
        }

        private bool CanAllocate(long cost, PropertyLevel level)
        {
            try { EconomyRules.AllocateInvestment(cost, level, State.Decisions.InvestmentTieBreak); return true; }
            catch (InvalidOperationException) { return false; }
        }

        private bool CanAllocateUpgrade(long cost, PropertyLevel from, PropertyLevel to)
        {
            try { EconomyRules.AllocateUpgradeInvestment(cost, from, to, State.Decisions.InvestmentTieBreak); return true; }
            catch (InvalidOperationException) { return false; }
        }

        private bool TryEngineerCost(PlayerState player, int tileId, bool ultimate, out long cost)
        {
            try { cost = EngineerUpgradeCost(player, tileId, ultimate); return true; }
            catch (InvalidOperationException) { cost = 0; return false; }
        }

        private bool CanChangeOwnership(int tileId)
        {
            foreach (TurnEffectState effect in State.ActiveEffects)
                if (effect.Kind == EffectKind.TemporaryFestival && effect.TargetTileId == tileId &&
                    !State.Decisions.RemoveFestivalOnOwnershipChange.HasValue) return false;
            return true;
        }

        public ActionResult SubmitDecision(GameCommand command) => SubmitAction(command);

        public ActionResult SubmitAction(GameCommand command)
        {
            if (command == null) return Rejected("Thiếu lệnh.");
            if (command.ExpectedRevision != State.Revision || command.DecisionId != State.DecisionId)
                return Rejected("Lựa chọn đã cũ; hãy lấy lại trạng thái và lựa chọn hợp lệ.");
            LegalAction chosen = null;
            foreach (LegalAction option in QueryLegalActions(command.ActorId))
                if (option.Kind == command.Kind && option.TargetTileId == command.TargetTileId &&
                    option.TargetLevel == command.TargetLevel && option.ChoiceId == command.ChoiceId && option.TargetPlayerId == command.TargetPlayerId)
                { chosen = option; break; }
            if (chosen == null) return Rejected("Hành động/đích/cấp không hợp lệ tại trạng thái hiện tại.");

            PlayerState player = State.GetPlayer(command.ActorId);
            if (State.GiftBatchActive) State.PendingObligation = State.FindPendingForPayer(player.Id);
            CardEffectPlan cardPlan = null;
            if (command.Kind == GameActionKind.CardChoice)
            {
                try
                {
                    cardPlan = _cardProvider.Plan(QuerySnapshot(), player.Id, command.ChoiceId.Value);
                    ValidateCardPlan(player, cardPlan);
                    if (cardPlan.TargetTileId != command.TargetTileId || cardPlan.TargetPlayerId != command.TargetPlayerId)
                        return Rejected("Ý định thẻ đã thay đổi mục tiêu; cần lấy lại lựa chọn.");
                }
                catch (ArgumentException error) { return Rejected(error.Message); }
                catch (InvalidOperationException error) { return Rejected(error.Message); }
            }
            MovementResult movement = null;
            switch (command.Kind)
            {
                case GameActionKind.RollDice:
                    DiceRoll roll = _diceSource.Roll();
                    State.PendingDiceRoll = new DiceRoll(roll.First, roll.Second);
                    State.Phase = TurnPhase.AwaitingRollConfirmation;
                    break;
                case GameActionKind.RerollDice:
                    DiceRoll reroll = _diceSource.Roll();
                    State.PendingDiceRoll = new DiceRoll(reroll.First, reroll.Second);
                    CommitActiveCooldown(player, BtlGame.Characters.RoleCatalog.Get(player.Role).ActiveCooldownTurns);
                    break;
                case GameActionKind.ConfirmRoll:
                    if (State.PendingJailTrial)
                    {
                        movement = ConfirmJailTrial(player.Id);
                        break;
                    }
                    DiceRoll confirmed = State.PendingDiceRoll.Value;
                    State.Phase = TurnPhase.AwaitingRoll;
                    State.PendingDiceRoll = null;
                    movement = MoveByDiceAllowCommittedRoll(player.Id, confirmed);
                    QueuePresentation(movement);
                    break;
                case GameActionKind.SkipLanding:
                    State.ClockPaused = false;
                    CompleteLanding(State.CurrentActionId);
                    break;
                case GameActionKind.EndTurn:
                    State.ClockPaused = false;
                    CompletePersonalTurn();
                    break;
                case GameActionKind.BuyCity: BuyCity(player.Id, command.TargetTileId.Value, command.TargetLevel.Value); break;
                case GameActionKind.BuyBeach: BuyBeach(player.Id, command.TargetTileId.Value); break;
                case GameActionKind.UpgradeCity: UpgradeCity(player.Id, command.TargetTileId.Value, command.TargetLevel.Value, State.CurrentLandingId); break;
                case GameActionKind.ForceBuyCity: ForceBuyCity(player.Id, command.TargetTileId.Value, NormalForcedPurchaseBasisPoints(player)); break;
                case GameActionKind.SellProperty: SellToBank(player.Id, command.TargetTileId.Value); break;
                case GameActionKind.SettleObligation: SettlePendingObligation(); break;
                case GameActionKind.LiquidateProperty: LiquidatePropertyForPendingObligation(player.Id, command.TargetTileId.Value); break;
                case GameActionKind.DeclareBankruptcy: DeclareBankruptcy(player.Id); break;
                case GameActionKind.JailPay: PayToLeaveJail(player.Id); break;
                case GameActionKind.JailCard: ReleaseFromJailWithCard(player.Id); break;
                case GameActionKind.JailRoll:
                    if (player.Role == RoleId.Strategist)
                    {
                        BeginJailTrial(player.Id);
                        break;
                    }
                    int oldPosition = player.Position;
                    DiceRoll jailRoll = ChooseJailRoll(player.Id, HasExistingPaymentImmunity(player.Id));
                    if (jailRoll.IsDouble)
                    {
                        movement = BoardRules.CalculateMovement(oldPosition, jailRoll);
                        QueuePresentation(movement);
                    }
                    break;
                case GameActionKind.ActivateSkill: ExecuteActiveSkill(player, command.TargetTileId); break;
                case GameActionKind.UseUltimate:
                    if (player.Role == RoleId.Strategist)
                    {
                        int steps = BoardRules.CountForwardPath(player.Position, command.TargetTileId.Value);
                        movement = MoveWithoutDice(player.Id, steps, false);
                        State.TeleportUsedInCurrentChain = false;
                        player.UltimateUsed = true;
                        QueuePresentation(movement);
                    }
                    else ExecuteUltimate(player, command.TargetTileId);
                    break;
                case GameActionKind.Teleport:
                    movement = TeleportFromCurrentLanding(player.Id, command.TargetTileId.Value);
                    QueuePresentation(movement);
                    break;
                case GameActionKind.OrganizeFestival: OrganizeFestival(player.Id, command.TargetTileId.Value); break;
                case GameActionKind.UseTaxCard:
                    ApplyActiveMitigation(State.PendingObligation.Id, 0, () => player.ConsumeHeldCard(3));
                    break;
                case GameActionKind.UseRentCard:
                    long reduced = QuoteRent(State.PendingObligation.PropertyTileId.Value, -5000).FinalRent;
                    ApplyActiveMitigation(State.PendingObligation.Id, reduced, () => player.ConsumeHeldCard(4));
                    break;
                case GameActionKind.CardChoice: movement = ExecuteCardChoice(player, cardPlan); break;
                default: return Rejected("Chưa hỗ trợ lệnh này.");
            }
            AutoProgressCompletedDecision();
            State.DecisionId++;
            State.Revision++;
            RefreshDecisionPause();
            return new ActionResult(State.Phase == TurnPhase.Moving || State.PendingObligation != null ||
                State.PendingRuleDecisionId != null ? ActionStatus.Pending : ActionStatus.Accepted,
                "Đã xử lý hành động.", QuerySnapshot(), movement);
        }

        private ActionResult Rejected(string reason) => new ActionResult(ActionStatus.Rejected, reason, QuerySnapshot());

        private void AutoProgressCompletedDecision()
        {
            if (State.Status != MatchStatus.Running) return;
            if (State.Phase == TurnPhase.ResolvingLanding && State.PendingObligation == null)
            {
                var choices = QueryLegalActions(State.CurrentPlayerId);
                if (State.CurrentLandingDecisionUsed || (choices.Count == 1 && choices[0].Kind == GameActionKind.SkipLanding))
                {
                    State.ClockPaused = false;
                    CompleteLanding(State.CurrentActionId);
                }
            }
            if (State.Status == MatchStatus.Running && State.Phase == TurnPhase.EndTurn &&
                (State.TimeExpired || State.PersonalTurnMustClose || State.GetPlayer(State.CurrentPlayerId).Status == PlayerStatus.Bankrupt))
            {
                State.ClockPaused = false;
                CompletePersonalTurn();
            }
        }

        private void RefreshDecisionPause()
        {
            State.DecisionAllowsSkills = State.Phase == TurnPhase.AwaitingJailChoice;
            State.ClockPaused = State.Status == MatchStatus.Running && (State.PendingObligation != null ||
                State.Phase == TurnPhase.ResolvingLanding || State.Phase == TurnPhase.AwaitingJailChoice ||
                State.Phase == TurnPhase.AwaitingRollConfirmation || State.Phase == TurnPhase.AwaitingRuleDecision);
        }

        private RentQuote QuoteRent(int tileId, int additional = 0) => BoardCatalog.Get(tileId).Kind == TileKind.City
            ? QuoteCityRent(tileId, additional) : QuoteBeachRent(tileId, additional);
    }
}
