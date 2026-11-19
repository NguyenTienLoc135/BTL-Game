using System;
using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Core;
using BtlGame.Properties;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class CoreRegressionTests
    {
        [Test]
        public void TimeoutRejectsAnUnrelatedLoanWithoutChangingMoneyOrDebt()
        {
            var a = new PlayerState(1, 0, "Banker", true);
            var state = NewState(a, new PlayerState(2, 1, "B"), MatchDuration.Minutes30);
            var core = new MatchCoordinator(state, new FixedDice());
            core.RollAndMove(1);
            core.AdvanceClock(TimeSpan.FromMinutes(30));
            core.CompleteLanding(state.CurrentActionId);
            long revision = state.Revision;
            Assert.Throws<InvalidOperationException>(() => core.IssueBankerLoan(1, 7));
            Assert.That(a.Cash, Is.EqualTo(300000));
            Assert.That(a.BankerDebtAmount, Is.Zero);
            Assert.That(a.BankerDebtDueAtTurnOrdinal, Is.Null);
            Assert.That(a.ActiveReadyAfterCompletedTurns, Is.Zero);
            Assert.That(state.Revision, Is.EqualTo(revision));
        }

        [TestCase("active")]
        [TestCase("ultimate")]
        [TestCase("double")]
        public void TimeoutBlocksOtherProactiveSkillCommits(string action)
        {
            var core = NewCore();
            core.State.TimeExpired = true;
            core.State.Phase = TurnPhase.EndTurn;
            Action activate = action == "active" ? (Action)(() => core.CommitActiveSkillUse(1, 5)) :
                action == "ultimate" ? () => core.CommitUltimateUse(1) : () => core.ActivateRentDoubleUltimate(1);
            Assert.Throws<InvalidOperationException>(() => activate());
            Assert.That(core.State.GetPlayer(1).UltimateUsed, Is.False);
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.Zero);
            Assert.That(core.State.ActiveEffects.Count, Is.Zero);
        }

        [Test]
        public void DecisionPopupBlocksANewProactiveSkill()
        {
            var core = new MatchCoordinator(NewState(new PlayerState(1, 0, "Banker", true), new PlayerState(2, 1, "B")));
            core.SetDecisionPending(true);
            Assert.Throws<InvalidOperationException>(() => core.IssueBankerLoan(1, 7));
            core.SetDecisionPending(false);
            core.IssueBankerLoan(1, 7);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(400000));
            Assert.That(core.State.GetPlayer(1).BankerDebtDueAtTurnOrdinal, Is.EqualTo(6));
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.EqualTo(8));
        }

        [Test]
        public void TaxLandingCannotCompleteBeforeSettlementOrChargeTwice()
        {
            var core = NewCore();
            ArrangeLanding(core, 1, 30);
            Assert.Throws<InvalidOperationException>(() => core.CompleteLanding(core.State.CurrentActionId));
            MandatoryObligation tax = core.AssessTax(1, ObligationKind.TaxTile);
            Assert.That(tax.AssessedAmount, Is.EqualTo(60000));
            Assert.Throws<InvalidOperationException>(() => core.CompleteLanding(core.State.CurrentActionId));
            core.SettlePendingObligation();
            Assert.That(core.State.TaxSettledAtCurrentLanding, Is.True);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(240000));
            Assert.Throws<InvalidOperationException>(() => core.AssessTax(1, ObligationKind.TaxTile));
            core.CompleteLanding(core.State.CurrentActionId);
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.EndTurn));
        }

        [TestCase(1, 7500)]
        [TestCase(4, 50000)]
        public void OpponentCityAndBeachRequireRentResolution(int tileId, long amount)
        {
            var core = NewCore();
            Own(core, 2, tileId, PropertyLevel.Stake);
            ArrangeLanding(core, 1, tileId);
            Assert.Throws<InvalidOperationException>(() => core.CompleteLanding(core.State.CurrentActionId));
            core.CreateObligation(ObligationKind.Rent, 1, 2, amount, tileId);
            core.SettlePendingObligation();
            Assert.Throws<InvalidOperationException>(() => core.CreateObligation(ObligationKind.Rent, 1, 2, amount, tileId));
            core.CompleteLanding(core.State.CurrentActionId);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000 - amount));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(300000 + amount));
        }

        [Test]
        public void NewMovementResetsTheTaxAndRentResolutionMarkers()
        {
            var core = NewCore();
            core.State.TaxSettledAtCurrentLanding = true;
            core.State.RentSettledAtCurrentLandingTileId = 1;
            core.RollAndMove(1);
            Assert.That(core.State.TaxSettledAtCurrentLanding, Is.False);
            Assert.That(core.State.RentSettledAtCurrentLandingTileId, Is.Null);
        }

        [TestCase(ObligationKind.TaxTile)]
        [TestCase(ObligationKind.TaxCard)]
        [TestCase(ObligationKind.Gift)]
        public void ExistingImmunityPreservesGrossAndCancelsEligiblePayments(ObligationKind kind)
        {
            var core = NewCore();
            core.AddSourceTurnDurationEffect(1, EffectKind.PaymentImmunity, 1, targetPlayerId: 1);
            ArrangeLanding(core, 1, kind == ObligationKind.TaxTile ? 30 : 12);
            MandatoryObligation obligation = kind == ObligationKind.Gift
                ? core.CreateObligation(kind, 1, 2, 60000) : core.AssessTax(1, kind);
            Assert.That(obligation.AssessedAmount, Is.EqualTo(60000));
            Assert.That(obligation.PayableAmount, Is.Zero);
            int consumed = 0;
            Assert.Throws<InvalidOperationException>(() => core.ApplyActiveMitigation(obligation.Id, 0, () => consumed++));
            core.SettlePendingObligation();
            Assert.That(consumed, Is.Zero);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(300000));
            core.CompleteLanding(core.State.CurrentActionId);
            core.CompletePersonalTurn();
            Assert.That(core.State.ActiveEffects.Count, Is.Zero, "Miễn nghĩa vụ hết cuối lượt kích hoạt.");
        }

        [Test]
        public void ZeroRentUnderImmunityDoesNotSpendCooldownOrAPrepaidRentCancel()
        {
            var core = NewCore();
            Own(core, 2, 1, PropertyLevel.House3);
            core.AddSourceTurnDurationEffect(1, EffectKind.CancelRent, 1);
            core.AddSourceTurnDurationEffect(1, EffectKind.PaymentImmunity, 1);
            ArrangeLanding(core, 1, 1);
            var rent = core.CreateObligation(ObligationKind.Rent, 1, 2, 30000, 1);
            Assert.That(rent.PayableAmount, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => core.CommitActiveSkillUse(1, 6));
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.Zero);
            Assert.That(core.State.ActiveEffects.Count, Is.EqualTo(2));
        }

        [Test]
        public void PrepaidRentCancellationIsConsumedOnceButNotForZeroRent()
        {
            var core = NewCore();
            Own(core, 2, 1, PropertyLevel.House3);
            core.AddSourceTurnDurationEffect(1, EffectKind.CancelRent, 1);
            ArrangeLanding(core, 1, 1);
            var zero = core.CreateObligation(ObligationKind.Rent, 1, 2, 0, 1);
            Assert.That(zero.PayableAmount, Is.Zero);
            Assert.That(core.State.ActiveEffects.Count, Is.EqualTo(1));
            core.SettlePendingObligation();
            ArrangeLanding(core, 1, 1);
            var rent = core.CreateObligation(ObligationKind.Rent, 1, 2, 30000, 1);
            Assert.That(rent.PayableAmount, Is.Zero);
            Assert.That(core.State.ActiveEffects.Count, Is.Zero);
            core.SettlePendingObligation();
            ArrangeLanding(core, 1, 1);
            Assert.That(core.CreateObligation(ObligationKind.Rent, 1, 2, 30000, 1).PayableAmount, Is.EqualTo(30000));
        }

        [Test]
        public void ImmunityDoesNotCancelBankerDebtOrApplyOutsideSourcesTurn()
        {
            var core = NewCore();
            core.AddSourceTurnDurationEffect(1, EffectKind.PaymentImmunity, 1);
            Assert.That(core.CreateObligation(ObligationKind.BankerDebt, 1, null, 110000).PayableAmount, Is.EqualTo(110000));
            core.SettlePendingObligation();
            core.State.CurrentPlayerId = 2;
            Assert.That(core.CreateObligation(ObligationKind.Gift, 1, 2, 30000).PayableAmount, Is.EqualTo(30000));
        }

        [Test]
        public void ExistingImmunityKeepsTheNewForcedJailFeePendingUnderP02()
        {
            var core = NewCore();
            core.AddSourceTurnDurationEffect(1, EffectKind.PaymentImmunity, 1);
            var actor = core.State.GetPlayer(1);
            actor.IsJailed = true;
            actor.Position = 8;
            actor.JailTurnsStarted = 3;
            core.State.Phase = TurnPhase.AwaitingJailChoice;
            core.ChooseJailRoll(1, lawyerUltimateCouldMitigateForcedFee: false);
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.AwaitingRuleDecision));
            Assert.That(core.State.PendingRuleDecisionId, Is.EqualTo("P02"));
            Assert.That(core.State.PendingObligation, Is.Null);
            Assert.That(actor.Cash, Is.EqualTo(300000));
        }

        [Test]
        public void BeforeJailChoiceIsAnActionCheckpointButNeverAllowsANormalRoll()
        {
            var actor = new PlayerState(1, 0, "Banker", true) { Position = 8, IsJailed = true, JailTurnsStarted = 1 };
            var core = new MatchCoordinator(NewState(actor, new PlayerState(2, 1, "B")));
            core.State.Phase = TurnPhase.AwaitingJailChoice;
            core.IssueBankerLoan(1, 7);
            Assert.That(actor.Cash, Is.EqualTo(400000));
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.AwaitingJailChoice));
            Assert.Throws<InvalidOperationException>(() => core.RollAndMove(1));
        }

        [TestCase(2, 50400)]
        [TestCase(4, 70000)]
        public void GlobalRentBonusAppliesToCityAndBeach(int tileId, long expected)
        {
            var core = NewCore();
            Own(core, 1, tileId, PropertyLevel.House3);
            core.AddSourceTurnDurationEffect(1, EffectKind.RentBonus, 2, targetPlayerId: 1, modifierBasisPoints: 3000);
            long actual = tileId == 2 ? core.QuoteCityRent(tileId, 1000).FinalRent : core.QuoteBeachRent(tileId, 1000).FinalRent;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void TargetedCityBonusIsCountedOnceAndForeignTargetDoesNotApply()
        {
            var core = NewCore();
            Own(core, 1, 2, PropertyLevel.House3);
            core.AddSourceTurnDurationEffect(1, EffectKind.RentBonus, 2, targetTileId: 2, modifierBasisPoints: 3000);
            Assert.That(core.QuoteCityRent(2, 1000).FinalRent, Is.EqualTo(50400));
            core.AddSourceTurnDurationEffect(1, EffectKind.RentBonus, 2, targetPlayerId: 2, modifierBasisPoints: 3000);
            Assert.That(core.QuoteCityRent(2, 1000).FinalRent, Is.EqualTo(50400));
        }

        [TestCase(true, 7)]
        [TestCase(false, 6)]
        public void RentReactionCooldownDistinguishesOwnAndOutsideTurns(bool ownTurn, long expected)
        {
            var core = NewCore();
            Own(core, 2, 1, PropertyLevel.House3);
            ArrangeLanding(core, 1, 1);
            var rent = core.CreateObligation(ObligationKind.Rent, 1, 2, 30000, 1);
            // Một nghĩa vụ nhắm actor ngoài lượt dùng mốc lượt đã hoàn tất, không cộng lượt đang mở của người khác.
            if (!ownTurn) core.State.CurrentPlayerId = 2;
            core.ApplyActiveMitigation(rent.Id, 0, () => core.CommitActiveSkillUse(1, 6));
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.EqualTo(expected));
        }

        [TestCase(0, 40000, 20000)]
        [TestCase(1000, 44000, 22000)]
        [TestCase(-1500, 34000, 17000)]
        public void OneUpgradeCanReachHouse3AndInvestOnlyInNewTiers(int modifier, long expectedCost, long perNewTier)
        {
            var core = NewCore();
            Own(core, 1, 1, PropertyLevel.House1);
            ArrangeLanding(core, 1, 1);
            long cost = core.UpgradeCity(1, 1, PropertyLevel.House3, core.State.CurrentLandingId, modifier);
            Assert.That(cost, Is.EqualTo(expectedCost));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000 - expectedCost));
            var property = core.State.GetProperty(1);
            Assert.That(property.Level, Is.EqualTo(PropertyLevel.House3));
            CollectionAssert.AreEqual(new long[] { 40000, 20000, perNewTier, perNewTier, 0 }, property.InvestedByLevel);
            Assert.Throws<InvalidOperationException>(() => core.UpgradeCity(1, 1, core.State.CurrentLandingId));
        }

        [Test]
        public void HotelRequiresHouse3AndALaterLandingAndRemovesPowerOutage()
        {
            var core = NewCore();
            Own(core, 1, 1, PropertyLevel.House1);
            ArrangeLanding(core, 1, 1);
            Assert.Throws<InvalidOperationException>(() => core.UpgradeCity(1, 1, PropertyLevel.Hotel, core.State.CurrentLandingId));
            core.UpgradeCity(1, 1, PropertyLevel.House3, core.State.CurrentLandingId);
            core.State.CurrentLandingDecisionUsed = false;
            Assert.Throws<InvalidOperationException>(() => core.UpgradeCity(1, 1, PropertyLevel.Hotel, core.State.CurrentLandingId));
            core.AddSourceTurnDurationEffect(1, EffectKind.PowerOutage, 3, targetTileId: 1);
            ArrangeLanding(core, 1, 1);
            core.UpgradeCity(1, 1, PropertyLevel.Hotel, core.State.CurrentLandingId);
            Assert.That(core.State.GetProperty(1).Level, Is.EqualTo(PropertyLevel.Hotel));
            Assert.That(core.State.GetProperty(1).InvestedByLevel[4], Is.EqualTo(20000));
            Assert.That(core.State.ActiveEffects.Count, Is.Zero);
        }

        [Test]
        public void PendingCapitalTieBreakLeavesAnUpgradeCompletelyUnchanged()
        {
            var core = NewCore();
            Own(core, 1, 2, PropertyLevel.Stake);
            ArrangeLanding(core, 1, 2);
            long revision = core.State.Revision;
            var error = Assert.Throws<InvalidOperationException>(() =>
                core.UpgradeCity(1, 2, PropertyLevel.House3, core.State.CurrentLandingId, 1));
            StringAssert.Contains("P01", error.Message);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.GetProperty(2).Level, Is.EqualTo(PropertyLevel.Stake));
            Assert.That(core.State.CurrentLandingDecisionUsed, Is.False);
            Assert.That(core.State.Revision, Is.EqualTo(revision));
        }

        [Test]
        public void DemolitionAfterAMultilevelUpgradeRemovesOnlyTheLostTier()
        {
            var core = NewCore();
            Own(core, 1, 1, PropertyLevel.House1);
            ArrangeLanding(core, 1, 1);
            core.UpgradeCity(1, 1, PropertyLevel.House3, core.State.CurrentLandingId, -1500);
            ArrangeLanding(core, 2, 12);
            var result = core.ApplyEarthquake(2, 1);
            Assert.That(result.InvestmentRemovedOrRefunded, Is.EqualTo(17000));
            Assert.That(core.State.GetProperty(1).TotalInvestment, Is.EqualTo(77000));
        }

        [Test]
        public void TimeoutKeepsForcedPurchaseContinuationButBlocksNewSalesAndRolls()
        {
            var a = new PlayerState(1, 0, "A");
            var core = new MatchCoordinator(NewState(a, new PlayerState(2, 1, "B"), MatchDuration.Minutes30));
            Own(core, 2, 1, PropertyLevel.House3);
            ArrangeLanding(core, 1, 1);
            core.AdvanceClock(TimeSpan.FromMinutes(30));
            core.CreateObligation(ObligationKind.Rent, 1, 2, 30000, 1);
            core.SettlePendingObligation();
            Assert.That(core.ForceBuyCity(1, 1, 20000), Is.EqualTo(200000));
            core.CompleteLanding(core.State.CurrentActionId);
            Assert.Throws<InvalidOperationException>(() => core.RollAndMove(1));
            Assert.Throws<InvalidOperationException>(() => core.SellToBank(1, 1));
            core.CompletePersonalTurn();
            Assert.That(core.State.Status, Is.EqualTo(MatchStatus.Finished));
            CollectionAssert.AreEqual(new[] { 2 }, core.State.WinnerPlayerIds);
        }

        [Test]
        public void TimeoutStillAllowsRentReactionDirectlyInsideTheLandingChain()
        {
            var core = new MatchCoordinator(NewState(new PlayerState(1, 0, "A"), new PlayerState(2, 1, "B"), MatchDuration.Minutes30));
            Own(core, 2, 1, PropertyLevel.House3);
            ArrangeLanding(core, 1, 1);
            core.AdvanceClock(TimeSpan.FromMinutes(30));
            var rent = core.CreateObligation(ObligationKind.Rent, 1, 2, 30000, 1);
            core.ApplyActiveMitigation(rent.Id, 0, () => core.CommitActiveSkillUse(1, 6));
            Assert.That(rent.PayableAmount, Is.Zero);
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.EqualTo(7));
            core.SettlePendingObligation();
            core.CompleteLanding(core.State.CurrentActionId);
            core.CompletePersonalTurn();
            Assert.That(core.State.Status, Is.EqualTo(MatchStatus.Finished));
        }

        [Test]
        public void BankerPaysDueDebtBeforeRoundInterestAndTimeoutRanking()
        {
            var a = new PlayerState(1, 0, "A") { Cash = 1000, CompletedPersonalTurns = 6 };
            var b = new PlayerState(2, 1, "Banker", true)
            {
                Cash = 111000, CompletedPersonalTurns = 5, BankerDebtAmount = 110000,
                BankerDebtDueAtTurnOrdinal = 6
            };
            var state = new GameState(new[] { a, b }, 2, new[] { 1, 5, 9 }, MatchDuration.Minutes30);
            state.MarkTurnCompletedThisRound(1);
            state.Phase = TurnPhase.EndTurn;
            state.TimeExpired = true;
            var core = new MatchCoordinator(state);
            core.CompletePersonalTurn();
            Assert.That(state.PendingObligation.Kind, Is.EqualTo(ObligationKind.BankerDebt));
            core.SettlePendingObligation();
            Assert.That(b.Cash, Is.EqualTo(1000));
            core.CompletePersonalTurn();
            Assert.That(b.Cash, Is.EqualTo(1010));
            CollectionAssert.AreEqual(new[] { 2 }, state.WinnerPlayerIds);
        }

        [Test]
        public void TaxAmountDoesNotChangeWhenThePayerLiquidatesAProperty()
        {
            var core = NewCore();
            core.State.GetPlayer(1).Cash = 10000;
            Own(core, 1, 7, PropertyLevel.House3);
            ArrangeLanding(core, 1, 30);
            var tax = core.AssessTax(1, ObligationKind.TaxTile);
            Assert.That(tax.AssessedAmount, Is.EqualTo(42000));
            core.LiquidatePropertyForPendingObligation(1, 7);
            Assert.That(tax.Status, Is.EqualTo(ObligationStatus.Settled));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(68000));
            Assert.That(core.State.TaxSettledAtCurrentLanding, Is.True);
        }

        [Test]
        public void ImmediateBeachWinReportsTheBuyerAndSnapshotsAreImmutable()
        {
            var core = NewCore();
            var before = core.QuerySnapshot();
            foreach (int tile in new[] { 4, 14, 18 }) Own(core, 1, tile, PropertyLevel.Stake);
            ArrangeLanding(core, 1, 25);
            core.BuyBeach(1, 25);
            Assert.That(core.State.Result, Is.EqualTo(WinReason.FourBeaches));
            CollectionAssert.AreEqual(new[] { 1 }, core.State.WinnerPlayerIds);
            var after = core.QuerySnapshot();
            Assert.That(before.WinnerPlayerIds.Count, Is.Zero);
            CollectionAssert.AreEqual(new[] { 1 }, after.WinnerPlayerIds);
            Assert.Throws<NotSupportedException>(() => ((IList<int>)after.WinnerPlayerIds).Add(2));
        }

        [Test]
        public void ThreeCountryWinReportsTheBuyerWithoutWaitingForEndTurn()
        {
            var core = NewCore();
            foreach (int tile in new[] { 13, 15, 17, 19, 26 }) Own(core, 1, tile, PropertyLevel.Stake);
            ArrangeLanding(core, 1, 27);
            core.BuyCity(1, 27, PropertyLevel.Stake);
            Assert.That(core.State.Result, Is.EqualTo(WinReason.ThreeCountries));
            CollectionAssert.AreEqual(new[] { 1 }, core.QuerySnapshot().WinnerPlayerIds);
        }

        [Test]
        public void LastSurvivorWinReportsTheCreditorRatherThanTheCurrentDebtor()
        {
            var core = NewCore();
            core.State.GetPlayer(1).Cash = 0;
            core.CreateObligation(ObligationKind.Gift, 1, 2, 100000);
            core.DeclareBankruptcy(1);
            Assert.That(core.State.Result, Is.EqualTo(WinReason.LastSolventPlayer));
            CollectionAssert.AreEqual(new[] { 2 }, core.QuerySnapshot().WinnerPlayerIds);
        }

        [Test]
        public void CashWinnerAndLoanSnapshotAreAvailableWithoutReadingMutableState()
        {
            var a = new PlayerState(1, 0, "A", true);
            var core = new MatchCoordinator(NewState(a, new PlayerState(2, 1, "B"), MatchDuration.Minutes30));
            var before = core.QuerySnapshot();
            core.IssueBankerLoan(1, 7);
            var loan = core.QuerySnapshot().Players[0];
            Assert.That(loan.HasBankerPassive, Is.True);
            Assert.That(loan.BankerDebtAmount, Is.EqualTo(110000));
            Assert.That(loan.BankerDebtDueAtTurnOrdinal, Is.EqualTo(6));
            Assert.That(before.Players[0].BankerDebtAmount, Is.Zero);
            core.AdvanceClock(TimeSpan.FromMinutes(30));
            Assert.That(core.State.Result, Is.EqualTo(WinReason.TimeCash));
            CollectionAssert.AreEqual(new[] { 1 }, core.QuerySnapshot().WinnerPlayerIds);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TimeoutReportsOnlyTheCorrectCashAndPropertyWinners(bool usePropertyTieBreak)
        {
            var a = new PlayerState(1, 0, "A") { Cash = 100000 };
            var b = new PlayerState(2, 1, "B") { Cash = 200000 };
            var c = new PlayerState(3, 2, "C") { Cash = 200000 };
            var core = new MatchCoordinator(new GameState(new[] { a, b, c }, 1, new[] { 1, 5, 9 }, MatchDuration.Minutes30));
            Own(core, 2, 1, PropertyLevel.House3);
            if (!usePropertyTieBreak) Own(core, 3, 4, PropertyLevel.Stake);
            core.AdvanceClock(TimeSpan.FromMinutes(30));
            CollectionAssert.AreEqual(usePropertyTieBreak ? new[] { 2 } : new[] { 2, 3 }, core.QuerySnapshot().WinnerPlayerIds);
            Assert.That(core.State.Result, Is.EqualTo(usePropertyTieBreak ? WinReason.TimePropertyValue : WinReason.TimeShared));
        }

        private static GameState NewState(PlayerState a, PlayerState b, MatchDuration duration = MatchDuration.Unlimited,
            GameplayDecisions decisions = null) =>
            new GameState(new[] { a, b }, a.Id, new[] { 1, 5, 9 }, duration, decisions);

        private static MatchCoordinator NewCore() =>
            // Fixture legacy cố ý bỏ policy để kiểm tra guard của cấu hình chưa hoàn chỉnh.
            new MatchCoordinator(NewState(new PlayerState(1, 0, "A"), new PlayerState(2, 1, "B"),
                decisions: new GameplayDecisions()), new FixedDice());

        private static void ArrangeLanding(MatchCoordinator core, int actorId, int tileId)
        {
            core.State.CurrentPlayerId = actorId;
            core.State.GetPlayer(actorId).Position = tileId;
            core.State.CurrentLandingId++;
            core.State.CurrentActionId++;
            core.State.Phase = TurnPhase.ResolvingLanding;
            core.State.CurrentLandingDecisionUsed = false;
            core.State.RentSettledAtCurrentLandingTileId = null;
            core.State.TaxSettledAtCurrentLanding = false;
        }

        private static void Own(MatchCoordinator core, int ownerId, int tileId, PropertyLevel level)
        {
            PropertyState property = core.State.GetProperty(tileId);
            property.OwnerId = ownerId;
            if (BoardCatalog.Get(tileId).Kind == TileKind.Beach)
                property.BeachInvestment = BoardCatalog.Get(tileId).BeachPrice;
            else
            {
                property.Level = level;
                var investment = EconomyRules.AllocateInvestment(EconomyRules.ListedPrice(tileId, level), level);
                for (int i = 0; i < investment.Count; i++) property.MutableInvestedByLevel[i] = investment[i];
            }
        }

        private sealed class FixedDice : IDiceSource
        {
            public DiceRoll Roll() => new DiceRoll(1, 2);
        }
    }
}
