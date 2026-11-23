using System;
using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Core;
using BtlGame.Properties;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class MatchActionTests
    {
        [Test]
        public void StaleCommandCannotSpendUltimateTwice()
        {
            var core = NewCore(RoleId.Banker);
            GameCommand command = Find(core, GameActionKind.UseUltimate).ToCommand();
            Assert.That(core.SubmitAction(command).Status, Is.EqualTo(ActionStatus.Accepted));
            long revision = core.State.Revision;
            Assert.That(core.SubmitAction(command).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(500000));
            Assert.That(core.State.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void ForgedHotelPurchaseIsRejectedWithoutMutatingState()
        {
            var core = NewCore(RoleId.Investor);
            ArrangeLanding(core, 1);
            var command = new GameCommand(GameActionKind.BuyCity, 1, core.State.Revision,
                core.State.DecisionId, 1, PropertyLevel.Hotel);
            long revision = core.State.Revision;
            Assert.That(core.SubmitDecision(command).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.GetProperty(1).OwnerId, Is.Null);
            Assert.That(core.State.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void AnotherPlayerCannotForgeARollForTheCurrentPlayer()
        {
            var core = NewCore(RoleId.Banker);
            Assert.That(core.QueryLegalActions(2).Count, Is.Zero);
            var command = new GameCommand(GameActionKind.RollDice, 2, core.State.Revision, core.State.DecisionId);
            Assert.That(core.SubmitAction(command).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(2).Position, Is.Zero);
        }

        [Test]
        public void RollingRequiresConfirmationAndAnimationBeforeResolvingLanding()
        {
            var core = NewCore(RoleId.Strategist, new SequenceDice(new DiceRoll(1, 2)));
            Submit(core, GameActionKind.RollDice);
            Assert.That(core.State.GetPlayer(1).Position, Is.Zero);
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.AwaitingRollConfirmation));
            ActionResult moving = Submit(core, GameActionKind.ConfirmRoll);
            Assert.That(moving.Status, Is.EqualTo(ActionStatus.Pending));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, moving.Movement.Path);
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.Moving));
            Assert.That(core.QueryLegalActions(1).Count, Is.Zero);
            Assert.That(core.QueryLegalActions(2).Count, Is.Zero);
            core.CompletePresentation(core.State.CurrentActionId);
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.ResolvingLanding));
            Assert.That(Find(core, GameActionKind.BuyCity, 3, PropertyLevel.House3), Is.Not.Null);
        }

        [TestCase(1, 1, 0)]
        [TestCase(1, 2, 1)]
        public void LandingOnOrCrossingStartRewardsOnceEvenWithDuplicateCallback(int first, int second, int destination)
        {
            var core = NewCore(RoleId.Banker, new SequenceDice(new DiceRoll(first, second)));
            core.State.GetPlayer(1).Position = 30;
            Submit(core, GameActionKind.RollDice);
            ActionResult moved = Submit(core, GameActionKind.ConfirmRoll);
            Assert.That(moved.Movement.ToTileId, Is.EqualTo(destination));
            Assert.That(moved.Movement.StartCrossings, Is.EqualTo(1));
            long actionId = core.State.CurrentActionId;
            core.CompletePresentation(actionId);
            long revision = core.State.Revision;
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(500000));
            Assert.That(core.CompletePresentation(actionId).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(500000));
            Assert.That(core.State.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void DoubleLandingInJailDoesNotOfferAnotherRoll()
        {
            var core = NewCore(RoleId.Banker, new SequenceDice(new DiceRoll(4, 4)));
            Submit(core, GameActionKind.RollDice);
            Submit(core, GameActionKind.ConfirmRoll);
            core.CompletePresentation(core.State.CurrentActionId);
            Assert.That(core.State.GetPlayer(1).IsJailed, Is.True);
            Assert.That(core.State.PendingDoubleRoll, Is.False);
            Assert.That(core.QueryLegalActions(1).Count, Is.Zero);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.GetPlayer(1).UltimateUsed, Is.False);
            Assert.That(core.State.CurrentPlayerId, Is.EqualTo(2));
            Assert.That(core.State.GetPlayer(1).JailTurnsStarted, Is.Zero);
        }

        [Test]
        public void DecisionPopupPausesClockForTheSameHumanAndAiActions()
        {
            var core = NewCore(RoleId.Banker, new SequenceDice(new DiceRoll(1, 2)), MatchDuration.Minutes30);
            Submit(core, GameActionKind.RollDice);
            long before = core.State.ClockRemainingTicks;
            core.AdvanceClock(TimeSpan.FromMinutes(3));
            Assert.That(core.State.ClockRemainingTicks, Is.EqualTo(before), "Chờ xác nhận gieo là quyết định chung.");
            Submit(core, GameActionKind.ConfirmRoll);
            core.AdvanceClock(TimeSpan.FromMinutes(1));
            Assert.That(core.State.ClockRemainingTicks, Is.EqualTo(before - TimeSpan.FromMinutes(1).Ticks));
            core.CompletePresentation(core.State.CurrentActionId);
            core.AdvanceClock(TimeSpan.FromMinutes(3));
            Assert.That(core.State.ClockRemainingTicks, Is.EqualTo(before - TimeSpan.FromMinutes(1).Ticks));
        }

        [TestCase(RoleId.Banker)]
        [TestCase(RoleId.Entrepreneur)]
        [TestCase(RoleId.Lawyer)]
        public void TimeExpiredOffersNoNewProactiveSkillOrUltimate(RoleId role)
        {
            var core = NewCore(role);
            core.State.TimeExpired = true;
            core.State.Phase = TurnPhase.EndTurn;
            Assert.That(Contains(core, GameActionKind.ActivateSkill), Is.False);
            Assert.That(Contains(core, GameActionKind.UseUltimate), Is.False);
            Assert.That(Contains(core, GameActionKind.RollDice), Is.False);
            Assert.That(Contains(core, GameActionKind.EndTurn), Is.True);
        }

        [Test]
        public void S01InvestorPassivePurchaseWritesDiscountedCapital()
        {
            var core = NewCore(RoleId.Investor);
            ArrangeLanding(core, 1);
            Assert.That(Find(core, GameActionKind.BuyCity, 1, PropertyLevel.House3).Cost, Is.EqualTo(90000));
            Submit(core, GameActionKind.BuyCity, 1, PropertyLevel.House3);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(210000));
            CollectionAssert.AreEqual(new long[] { 36000, 18000, 18000, 18000, 0 }, core.State.GetProperty(1).InvestedByLevel);
        }

        [Test]
        public void S02InvestorCouponCombinesWithPassiveAndIsConsumedOnlyOnPurchase()
        {
            var core = NewCore(RoleId.Investor);
            Submit(core, GameActionKind.ActivateSkill);
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.EqualTo(6));
            ArrangeLanding(core, 1);
            Assert.That(Find(core, GameActionKind.BuyCity, 1, PropertyLevel.House3).Cost, Is.EqualTo(75000));
            Submit(core, GameActionKind.BuyCity, 1, PropertyLevel.House3);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(225000));
            CollectionAssert.AreEqual(new long[] { 30000, 15000, 15000, 15000, 0 }, core.State.GetProperty(1).InvestedByLevel);
            foreach (TurnEffectState effect in core.State.ActiveEffects)
                Assert.That(effect.Kind, Is.Not.EqualTo(EffectKind.PurchaseDiscount));
        }

        [Test]
        public void S03InvestorNormalUpgradePaysTenPercentExtra()
        {
            var core = NewCore(RoleId.Investor);
            Own(core, 1, 1, PropertyLevel.House1);
            core.State.GetPlayer(1).Cash = 100000;
            ArrangeLanding(core, 1);
            Assert.That(Find(core, GameActionKind.UpgradeCity, 1, PropertyLevel.House3).Cost, Is.EqualTo(44000));
            Submit(core, GameActionKind.UpgradeCity, 1, PropertyLevel.House3);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(56000));
        }

        [Test]
        public void S04InvestorUltimateBuysRemotelyAt150PercentWithoutRent()
        {
            var core = NewCore(RoleId.Investor);
            Own(core, 2, 1, PropertyLevel.House3);
            Submit(core, GameActionKind.UseUltimate, 1);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(150000));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(450000));
            Assert.That(core.State.GetProperty(1).OwnerId, Is.EqualTo(1));
            Assert.That(core.State.GetProperty(1).Level, Is.EqualTo(PropertyLevel.House3));
            CollectionAssert.AreEqual(new long[] { 60000, 30000, 30000, 30000, 0 }, core.State.GetProperty(1).InvestedByLevel);
            Assert.That(core.State.GetPlayer(1).UltimateUsed, Is.True);
            Assert.That(core.State.PendingObligation, Is.Null);
        }

        [TestCase(1, PropertyLevel.Hotel)]
        [TestCase(4, PropertyLevel.Stake)]
        public void S05InvestorUltimateCannotTargetHotelOrBeach(int tile, PropertyLevel level)
        {
            var core = NewCore(RoleId.Investor);
            Own(core, 2, tile, level);
            Assert.That(Contains(core, GameActionKind.UseUltimate, tile), Is.False);
            Assert.That(core.SubmitAction(new GameCommand(GameActionKind.UseUltimate, 1, core.State.Revision,
                core.State.DecisionId, tile)).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(1).UltimateUsed, Is.False);
        }

        [Test]
        public void S06AndS07EntrepreneurPassiveAndActiveAddOnBaseRent()
        {
            var core = NewCore(RoleId.Entrepreneur);
            Own(core, 1, 1, PropertyLevel.House3);
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(33000));
            Submit(core, GameActionKind.ActivateSkill);
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(42000));
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.EqualTo(7));
        }

        [Test]
        public void S09EntrepreneurPurchaseWeaknessRaisesPriceAndCapital()
        {
            var core = NewCore(RoleId.Entrepreneur);
            ArrangeLanding(core, 1);
            Submit(core, GameActionKind.BuyCity, 1, PropertyLevel.House3);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(195000));
            Assert.That(core.State.GetProperty(1).TotalInvestment, Is.EqualTo(105000));
        }

        [Test]
        public void S10EngineerNormalUpgradePays85PercentForNewTiers()
        {
            var core = NewCore(RoleId.Engineer);
            Own(core, 1, 1, PropertyLevel.House1);
            core.State.GetPlayer(1).Cash = 100000;
            ArrangeLanding(core, 1);
            Submit(core, GameActionKind.UpgradeCity, 1, PropertyLevel.House3);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(66000));
            CollectionAssert.AreEqual(new long[] { 40000, 20000, 17000, 17000, 0 }, core.State.GetProperty(1).InvestedByLevel);
        }

        [Test]
        public void S11EngineerActiveAddsFreeHotelFromAnEarlierVisit()
        {
            var core = NewCore(RoleId.Engineer);
            Own(core, 1, 1, PropertyLevel.House3);
            Submit(core, GameActionKind.ActivateSkill, 1);
            Assert.That(core.State.GetProperty(1).Level, Is.EqualTo(PropertyLevel.Hotel));
            Assert.That(core.State.GetProperty(1).TotalInvestment, Is.EqualTo(100000));
            Assert.That(core.State.GetProperty(1).InvestedByLevel[4], Is.Zero);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.EqualTo(8));
        }

        [Test]
        public void S12EngineerUltimatePaysHalfDifferenceWithoutPassive()
        {
            var core = NewCore(RoleId.Engineer);
            Own(core, 1, 1, PropertyLevel.Stake);
            core.State.GetPlayer(1).Cash = 100000;
            Assert.That(Find(core, GameActionKind.UseUltimate, 1).Cost, Is.EqualTo(40000));
            Submit(core, GameActionKind.UseUltimate, 1);
            Assert.That(core.State.GetProperty(1).Level, Is.EqualTo(PropertyLevel.Hotel));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(60000));
            CollectionAssert.AreEqual(new long[] { 40000, 10000, 10000, 10000, 10000 }, core.State.GetProperty(1).InvestedByLevel);
            Assert.That(core.State.GetPlayer(1).UltimateUsed, Is.True);
        }

        [Test]
        public void S14LawyerActiveCancelsOnlyTheCurrentPositiveRentAndStartsCooldown()
        {
            var core = NewCore(RoleId.Lawyer);
            Own(core, 2, 1, PropertyLevel.House3);
            ArrangeLanding(core, 1);
            core.CreateObligation(ObligationKind.Rent, 1, 2, 30000, 1);
            Submit(core, GameActionKind.ActivateSkill);
            Assert.That(core.State.PendingObligation.PayableAmount, Is.Zero);
            Assert.That(core.State.PendingObligation.ActiveMitigationUsed, Is.True);
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.EqualTo(7));
            Submit(core, GameActionKind.SettleObligation);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(300000));
        }

        [Test]
        public void S15LawyerUltimateCancelsTaxWithoutSpendingHeldTaxCard()
        {
            var core = NewCore(RoleId.Lawyer);
            core.State.GetPlayer(1).AddHeldCard(3);
            Submit(core, GameActionKind.UseUltimate);
            ArrangeLanding(core, 30);
            core.AssessTax(1, ObligationKind.TaxTile);
            Assert.That(core.State.PendingObligation.AssessedAmount, Is.EqualTo(60000));
            Assert.That(core.State.PendingObligation.PayableAmount, Is.Zero);
            Assert.That(Contains(core, GameActionKind.UseTaxCard), Is.False);
            Submit(core, GameActionKind.SettleObligation);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            CollectionAssert.Contains(core.State.GetPlayer(1).HeldCards, 3);
        }

        [Test]
        public void S19BankerLoanAndS21UltimateKeepIndependentDebtAndUsage()
        {
            var core = NewCore(RoleId.Banker);
            core.State.GetPlayer(1).Cash = 100000;
            Submit(core, GameActionKind.ActivateSkill);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(200000));
            Assert.That(core.State.GetPlayer(1).BankerDebtAmount, Is.EqualTo(110000));
            Assert.That(core.State.GetPlayer(1).BankerDebtDueAtTurnOrdinal, Is.EqualTo(6));
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.EqualTo(8));
            Submit(core, GameActionKind.UseUltimate);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(400000));
            Assert.That(core.State.GetPlayer(1).BankerDebtAmount, Is.EqualTo(110000));
            Assert.That(Contains(core, GameActionKind.UseUltimate), Is.False);
        }

        [Test]
        public void S23StrategistRerollReplacesBothDiceAndMustAcceptTheNewResult()
        {
            var dice = new SequenceDice(new DiceRoll(3, 3), new DiceRoll(1, 4));
            var core = NewCore(RoleId.Strategist, dice);
            core.State.GetPlayer(1).Position = 1;
            Submit(core, GameActionKind.RollDice);
            Submit(core, GameActionKind.RerollDice);
            Assert.That(dice.Calls, Is.EqualTo(2));
            Assert.That(Contains(core, GameActionKind.RerollDice), Is.False);
            ActionResult moved = Submit(core, GameActionKind.ConfirmRoll);
            Assert.That(moved.Movement.ToTileId, Is.EqualTo(6));
            Assert.That(moved.Movement.IsDouble, Is.False);
            Assert.That(moved.Movement.Source, Is.EqualTo(MovementSource.Dice));
            Assert.That(moved.Snapshot.LastDiceRoll.Value.First, Is.EqualTo(1));
            Assert.That(moved.Snapshot.LastDiceRoll.Value.Second, Is.EqualTo(4));
            Assert.That(core.State.PendingDoubleRoll, Is.False);
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.EqualTo(6));
        }

        [Test]
        public void S24StrategistUltimateCrossesStartAndConsumesExistingDoubleEntitlement()
        {
            var core = NewCore(RoleId.Strategist);
            core.State.GetPlayer(1).Position = 31;
            core.State.GetPlayer(1).Cash = 100000;
            core.State.PendingDoubleRoll = true;
            ActionResult moved = Submit(core, GameActionKind.UseUltimate, 2);
            Assert.That(moved.Movement.Steps, Is.EqualTo(3));
            Assert.That(moved.Movement.Source, Is.EqualTo(MovementSource.StrategistUltimate));
            Assert.That(moved.Snapshot.LastDiceRoll, Is.Null);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.PendingDoubleRoll, Is.False);
            Assert.That(core.State.GetPlayer(1).UltimateUsed, Is.True);
            core.CompletePresentation(core.State.CurrentActionId);
            Submit(core, GameActionKind.SkipLanding);
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.EndTurn));
            Assert.That(Contains(core, GameActionKind.RollDice), Is.False);
        }

        [Test]
        public void S25StrategistUltimateRejectsSevenStepsWithoutSpendingIt()
        {
            var core = NewCore(RoleId.Strategist);
            core.State.GetPlayer(1).Position = 1;
            Assert.That(core.SubmitAction(new GameCommand(GameActionKind.UseUltimate, 1, core.State.Revision,
                core.State.DecisionId, 8)).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(1).UltimateUsed, Is.False);
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(1));
        }

        [Test]
        public void CurrentPlayerBankruptcyWithThreePlayersAdvancesToNextSurvivor()
        {
            var players = new[] { new PlayerState(1, 0, "A", RoleId.Banker),
                new PlayerState(2, 1, "B", RoleId.None), new PlayerState(3, 2, "C", RoleId.None) };
            var core = new MatchCoordinator(new GameState(players, 1, new[] { 27, 29, 31 }));
            players[0].Cash = 10000;
            core.CreateObligation(ObligationKind.Gift, 1, 2, 100000);
            Submit(core, GameActionKind.DeclareBankruptcy);
            Assert.That(players[0].Status, Is.EqualTo(PlayerStatus.Bankrupt));
            Assert.That(players[1].Cash, Is.EqualTo(310000));
            Assert.That(core.State.Status, Is.EqualTo(MatchStatus.Running));
            Assert.That(core.State.CurrentPlayerId, Is.EqualTo(2));
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.AwaitingRoll));
        }

        [Test]
        public void EndTurnDebtSettlementContinuesTheSameClosureWithoutReopeningSkills()
        {
            var core = NewCore(RoleId.Banker);
            var banker = core.State.GetPlayer(1);
            banker.Cash = 111000;
            banker.BankerDebtDueAtTurnOrdinal = 1;
            banker.BankerDebtAmount = 110000;
            core.State.Phase = TurnPhase.EndTurn;
            Submit(core, GameActionKind.EndTurn);
            Assert.That(core.State.PendingObligation.Kind, Is.EqualTo(ObligationKind.BankerDebt));
            Assert.That(banker.CompletedPersonalTurns, Is.Zero);
            Submit(core, GameActionKind.SettleObligation);
            Assert.That(banker.Cash, Is.EqualTo(1000));
            Assert.That(banker.CompletedPersonalTurns, Is.EqualTo(1));
            Assert.That(core.State.CurrentPlayerId, Is.EqualTo(2));
            Assert.That(core.QueryLegalActions(1).Count, Is.Zero);
        }

        private static MatchCoordinator NewCore(RoleId role, IDiceSource dice = null,
            MatchDuration duration = MatchDuration.Unlimited)
        {
            var players = new[] { new PlayerState(1, 0, "A", role), new PlayerState(2, 1, "B", RoleId.None) };
            var core = new MatchCoordinator(new GameState(players, 1, new[] { 27, 29, 31 }, duration), dice);
            core.State.CurrentLandingId = 5;
            return core;
        }

        private static void ArrangeLanding(MatchCoordinator core, int tileId)
        {
            core.State.GetPlayer(1).Position = tileId;
            core.State.Phase = TurnPhase.ResolvingLanding;
            core.State.ClockPaused = false;
            core.State.CurrentLandingId++;
            core.State.CurrentActionId++;
            core.State.LandingResolutionStarted = true;
            core.State.CurrentLandingDecisionUsed = false;
            core.State.RentSettledAtCurrentLandingTileId = null;
            core.State.TaxSettledAtCurrentLanding = false;
            core.State.Revision++;
        }

        private static void Own(MatchCoordinator core, int playerId, int tileId, PropertyLevel level)
        {
            PropertyState property = core.State.GetProperty(tileId);
            property.OwnerId = playerId;
            property.AcquiredOnLandingId = 0;
            if (BoardCatalog.Get(tileId).Kind == TileKind.Beach)
                property.BeachInvestment = BoardCatalog.Get(tileId).BeachPrice;
            else
            {
                property.Level = level;
                var investment = EconomyRules.AllocateInvestment(EconomyRules.ListedPrice(tileId, level), level);
                for (int i = 0; i < investment.Count; i++) property.MutableInvestedByLevel[i] = investment[i];
                if (level >= PropertyLevel.House3) property.House3ReachedLandingId = 0;
            }
        }

        private static LegalAction Find(MatchCoordinator core, GameActionKind kind, int? tile = null, PropertyLevel? level = null)
        {
            foreach (LegalAction option in core.QueryLegalActions(core.State.PendingObligation?.PayerId ?? core.State.CurrentPlayerId))
                if (option.Kind == kind && option.TargetTileId == tile && option.TargetLevel == level) return option;
            Assert.Fail("Không có lựa chọn hợp lệ: " + kind + " / " + tile + " / " + level);
            return null;
        }

        private static bool Contains(MatchCoordinator core, GameActionKind kind, int? tile = null)
        {
            foreach (LegalAction option in core.QueryLegalActions(core.State.PendingObligation?.PayerId ?? core.State.CurrentPlayerId))
                if (option.Kind == kind && (!tile.HasValue || option.TargetTileId == tile)) return true;
            return false;
        }

        private static ActionResult Submit(MatchCoordinator core, GameActionKind kind, int? tile = null, PropertyLevel? level = null)
        {
            ActionResult result = core.SubmitAction(Find(core, kind, tile, level).ToCommand());
            Assert.That(result.Status, Is.Not.EqualTo(ActionStatus.Rejected), result.Message);
            return result;
        }

        private sealed class SequenceDice : IDiceSource
        {
            private readonly Queue<DiceRoll> _rolls;
            public int Calls { get; private set; }
            public SequenceDice(params DiceRoll[] rolls) => _rolls = new Queue<DiceRoll>(rolls);
            public DiceRoll Roll() { Calls++; return _rolls.Dequeue(); }
        }
    }
}
