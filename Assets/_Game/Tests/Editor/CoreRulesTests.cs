using System;
using BtlGame.Board;
using BtlGame.Core;
using BtlGame.Properties;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class CoreRulesTests
    {
        [Test]
        public void CatalogContains32Tiles20CitiesAndFourBeaches()
        {
            int cities = 0;
            int beaches = 0;
            foreach (TileDefinition tile in BoardCatalog.Tiles)
            {
                if (tile.Kind == TileKind.City) cities++;
                if (tile.Kind == TileKind.Beach) beaches++;
            }

            Assert.That(BoardCatalog.Tiles.Count, Is.EqualTo(32));
            Assert.That(cities, Is.EqualTo(20));
            Assert.That(beaches, Is.EqualTo(4));
            Assert.That(BoardCatalog.Get(29).Country, Is.EqualTo(CountryId.Australia));
            Assert.That(BoardCatalog.Get(31).Country, Is.EqualTo(CountryId.Australia));
        }

        [Test]
        public void MovementWrapsAndCountsStartOnce()
        {
            MovementResult movement = BoardRules.CalculateMovement(31, new DiceRoll(2, 3));

            Assert.That(movement.ToTileId, Is.EqualTo(4));
            Assert.That(movement.StartCrossings, Is.EqualTo(1));
            Assert.That(movement.Path[0], Is.EqualTo(0));
        }

        [Test]
        public void CityRentAddsModifiersBeforeDoubling()
        {
            RentQuote quote = EconomyRules.CityRent(1, PropertyLevel.House3, 1_000,
                true, true, true, true, true, false);

            Assert.That(quote.BaseRent, Is.EqualTo(30_000));
            Assert.That(quote.ModifierBasisPoints, Is.EqualTo(16_000));
            Assert.That(quote.FinalRent, Is.EqualTo(156_000));
        }

        [Test]
        public void DirectHouse3PurchaseTracksActualInvestmentAndTaxSnapshot()
        {
            (MatchCoordinator coordinator, PlayerState a, _) = CreateMatch(
                new DiceRoll(1, 1), new DiceRoll(4, 4), new DiceRoll(6, 6), new DiceRoll(4, 4));
            LandOnTile(coordinator, 2);
            Assert.That(coordinator.BuyCity(a.Id, 2, PropertyLevel.House3), Is.EqualTo(120_000));
            Assert.That(a.Cash, Is.EqualTo(180_000));
            CollectionAssert.AreEqual(new long[] { 48_000, 24_000, 24_000, 24_000, 0 },
                coordinator.State.GetProperty(2).InvestedByLevel);

            coordinator.CompleteLanding(coordinator.State.CurrentActionId);
            coordinator.RollAndMove(a.Id);
            coordinator.CompleteLanding(coordinator.State.CurrentActionId);
            coordinator.RollAndMove(a.Id);
            coordinator.CompleteLanding(coordinator.State.CurrentActionId);
            coordinator.RollAndMove(a.Id);
            Assert.That(a.Position, Is.EqualTo(30));

            MandatoryObligation tax = coordinator.AssessTax(a.Id, ObligationKind.TaxTile);
            Assert.That(tax.AssessedAmount, Is.EqualTo(60_000));
            coordinator.SettlePendingObligation();
            Assert.That(a.Cash, Is.EqualTo(120_000));
        }

        [Test]
        public void ObligationLiquidationTransfersCashAndReturnsPropertiesBeforeBankruptcy()
        {
            (MatchCoordinator coordinator, PlayerState a, PlayerState b) = CreateMatch(new DiceRoll(1, 1));
            LandOnTile(coordinator, 2);
            coordinator.BuyCity(a.Id, 2, PropertyLevel.House3);
            coordinator.CreateObligation(ObligationKind.Gift, a.Id, b.Id, 300_000);

            Assert.That(coordinator.LiquidatePropertyForPendingObligation(a.Id, 2), Is.EqualTo(60_000));
            Assert.That(a.Status, Is.EqualTo(PlayerStatus.Bankrupt));
            Assert.That(a.Cash, Is.Zero);
            Assert.That(b.Cash, Is.EqualTo(540_000));
            Assert.That(coordinator.State.GetProperty(2).OwnerId, Is.Null);
            Assert.That(coordinator.State.Status, Is.EqualTo(MatchStatus.Finished));
            Assert.That(coordinator.State.Result, Is.EqualTo(WinReason.LastSolventPlayer));
        }

        [Test]
        public void CapitalAllocationStopsWhenPendingTieBreakWouldChangeTheResult()
        {
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(
                () => EconomyRules.AllocateInvestment(3, PropertyLevel.House3));

            StringAssert.Contains("P01", error.Message);
        }

        [Test]
        public void DuplicateLandingCallbackIsRejected()
        {
            (MatchCoordinator coordinator, _, _) = CreateMatch(new DiceRoll(1, 1));
            MovementResult movement = coordinator.RollAndMove(1);
            long actionId = coordinator.State.CurrentActionId;
            coordinator.CompleteLanding(actionId);

            Assert.Throws<InvalidOperationException>(() => coordinator.CompleteLanding(actionId));
            Assert.That(movement.ToTileId, Is.EqualTo(2));
        }

        [Test]
        public void BankerInterestIsPaidOnlyWhenTheWholeActiveRoundCompletes()
        {
            var a = new PlayerState(1, 0, "A");
            var banker = new PlayerState(2, 1, "B", hasBankerPassive: true);
            var coordinator = new MatchCoordinator(new GameState(new[] { a, banker }, 1, new[] { 1, 5, 9 }),
                new SequenceDiceSource(new DiceRoll(1, 2), new DiceRoll(1, 2)));

            PlayOrdinaryTurn(coordinator);
            Assert.That(banker.Cash, Is.EqualTo(300_000));
            PlayOrdinaryTurn(coordinator);

            Assert.That(banker.Cash, Is.EqualTo(303_000));
            Assert.That(coordinator.State.RoundCompletedAtLastTurnBoundary, Is.True);
        }

        [Test]
        public void ThirdFailedJailRollCreatesMandatoryFeeAndReleasesWithoutMovement()
        {
            var a = new PlayerState(1, 0, "A");
            var b = new PlayerState(2, 1, "B");
            var coordinator = new MatchCoordinator(new GameState(new[] { a, b }, 1, new[] { 1, 5, 9 }),
                new SequenceDiceSource(new DiceRoll(1, 1), new DiceRoll(2, 2), new DiceRoll(1, 1),
                    new DiceRoll(1, 2), new DiceRoll(1, 2), new DiceRoll(1, 2),
                    new DiceRoll(1, 2), new DiceRoll(1, 2), new DiceRoll(1, 2)));
            coordinator.RollAndMove(a.Id);
            coordinator.CompleteLanding(coordinator.State.CurrentActionId);
            coordinator.RollAndMove(a.Id);
            coordinator.CompleteLanding(coordinator.State.CurrentActionId);
            coordinator.RollAndMove(a.Id);
            Assert.That(a.IsJailed, Is.True);
            Assert.That(coordinator.State.PendingDoubleRoll, Is.False);
            coordinator.CompletePersonalTurn();

            for (int attempt = 0; attempt < 3; attempt++)
            {
                PlayOrdinaryTurn(coordinator);
                Assert.That(coordinator.State.CurrentPlayerId, Is.EqualTo(a.Id));
                coordinator.ChooseJailRoll(a.Id, lawyerUltimateCouldMitigateForcedFee: false);
                if (attempt < 2) coordinator.CompletePersonalTurn();
            }

            Assert.That(coordinator.State.PendingObligation.AssessedAmount, Is.EqualTo(200_000));
            coordinator.SettlePendingObligation();
            Assert.That(a.Cash, Is.EqualTo(100_000));
            Assert.That(a.IsJailed, Is.False);
            Assert.That(a.Position, Is.EqualTo(8));
            Assert.That(coordinator.State.Phase, Is.EqualTo(TurnPhase.EndTurn));
        }

        [Test]
        public void ExpiredClockRanksAfterCurrentTurnBoundary()
        {
            var a = new PlayerState(1, 0, "A");
            var b = new PlayerState(2, 1, "B");
            var state = new GameState(new[] { a, b }, 1, new[] { 1, 5, 9 }, MatchDuration.Minutes30);
            var coordinator = new MatchCoordinator(state);

            coordinator.AdvanceClock(TimeSpan.FromMinutes(30));

            Assert.That(state.TimeExpired, Is.True);
            Assert.That(state.Status, Is.EqualTo(MatchStatus.Finished));
            Assert.That(state.Result, Is.EqualTo(WinReason.TimeShared));
        }

        [Test]
        public void ObligationAllowsOnlyOneSuccessfulActiveMitigation()
        {
            (MatchCoordinator coordinator, PlayerState a, PlayerState b) = CreateMatch();
            MandatoryObligation obligation = coordinator.CreateObligation(ObligationKind.Gift, a.Id, b.Id, 30_000);
            int consumed = 0;

            coordinator.ApplyActiveMitigation(obligation.Id, 15_000, () => consumed++);
            Assert.Throws<InvalidOperationException>(() =>
                coordinator.ApplyActiveMitigation(obligation.Id, 0, () => consumed++));
            coordinator.SettlePendingObligation();

            Assert.That(consumed, Is.EqualTo(1));
            Assert.That(a.Cash, Is.EqualTo(285_000));
            Assert.That(b.Cash, Is.EqualTo(315_000));
            Assert.That(obligation.AssessedAmount, Is.EqualTo(30_000));
            Assert.That(obligation.PayableAmount, Is.EqualTo(15_000));
        }

        [Test]
        public void P02PreservesThirdJailRollAndBlocksTheUnresolvedBranch()
        {
            var a = new PlayerState(1, 0, "A");
            var b = new PlayerState(2, 1, "B");
            var coordinator = new MatchCoordinator(new GameState(new[] { a, b }, 1, new[] { 1, 5, 9 }),
                new SequenceDiceSource(new DiceRoll(1, 1), new DiceRoll(2, 2), new DiceRoll(1, 1),
                    new DiceRoll(1, 2), new DiceRoll(1, 2), new DiceRoll(1, 2),
                    new DiceRoll(1, 2), new DiceRoll(1, 2), new DiceRoll(1, 2)));
            coordinator.RollAndMove(a.Id);
            coordinator.CompleteLanding(coordinator.State.CurrentActionId);
            coordinator.RollAndMove(a.Id);
            coordinator.CompleteLanding(coordinator.State.CurrentActionId);
            coordinator.RollAndMove(a.Id);
            coordinator.CompletePersonalTurn();

            for (int attempt = 0; attempt < 2; attempt++)
            {
                PlayOrdinaryTurn(coordinator);
                coordinator.ChooseJailRoll(a.Id, lawyerUltimateCouldMitigateForcedFee: false);
                coordinator.CompletePersonalTurn();
            }
            PlayOrdinaryTurn(coordinator);
            DiceRoll thirdAttempt = coordinator.ChooseJailRoll(a.Id, lawyerUltimateCouldMitigateForcedFee: true);

            Assert.That(thirdAttempt.IsDouble, Is.False);
            Assert.That(coordinator.State.PendingRuleDecisionId, Is.EqualTo("P02"));
            Assert.That(coordinator.State.Phase, Is.EqualTo(TurnPhase.AwaitingRuleDecision));
            Assert.That(coordinator.State.PendingObligation, Is.Null);
            Assert.Throws<InvalidOperationException>(() => coordinator.RollAndMove(a.Id));
        }

        [Test]
        public void RentDoubleExpiresImmediatelyBeforeTheOwnersNextTurn()
        {
            var a = new PlayerState(1, 0, "A");
            var b = new PlayerState(2, 1, "B");
            var coordinator = new MatchCoordinator(new GameState(new[] { a, b }, 1, new[] { 1, 5, 9 }),
                new SequenceDiceSource(new DiceRoll(1, 2), new DiceRoll(1, 2)));

            coordinator.ActivateRentDoubleUltimate(a.Id);
            Assert.That(coordinator.IsRentDoubleActive(a.Id), Is.True);
            PlayOrdinaryTurn(coordinator);
            Assert.That(coordinator.IsRentDoubleActive(a.Id), Is.True);
            PlayOrdinaryTurn(coordinator);

            Assert.That(coordinator.State.CurrentPlayerId, Is.EqualTo(a.Id));
            Assert.That(coordinator.IsRentDoubleActive(a.Id), Is.False);
            Assert.That(a.UltimateUsed, Is.True);
        }

        private static (MatchCoordinator, PlayerState, PlayerState) CreateMatch(params DiceRoll[] rolls)
        {
            var a = new PlayerState(1, 0, "A");
            var b = new PlayerState(2, 1, "B");
            var state = new GameState(new[] { a, b }, 1, new[] { 1, 5, 9 });
            return (new MatchCoordinator(state, new SequenceDiceSource(rolls)), a, b);
        }

        private static void LandOnTile(MatchCoordinator coordinator, int tileId)
        {
            MovementResult movement = coordinator.RollAndMove(coordinator.State.CurrentPlayerId);
            Assert.That(movement.ToTileId, Is.EqualTo(tileId));
        }

        private static void PlayOrdinaryTurn(MatchCoordinator coordinator)
        {
            int playerId = coordinator.State.CurrentPlayerId;
            coordinator.RollAndMove(playerId);
            coordinator.CompleteLanding(coordinator.State.CurrentActionId);
            coordinator.CompletePersonalTurn();
        }

        private sealed class SequenceDiceSource : IDiceSource
        {
            private readonly System.Collections.Generic.Queue<DiceRoll> _rolls;
            public SequenceDiceSource(params DiceRoll[] rolls) => _rolls = new System.Collections.Generic.Queue<DiceRoll>(rolls);
            public DiceRoll Roll()
            {
                if (_rolls.Count == 0) throw new InvalidOperationException("Test dice sequence exhausted.");
                return _rolls.Dequeue();
            }
        }
    }
}
