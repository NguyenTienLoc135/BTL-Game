using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Core;
using BtlGame.Properties;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class CoreRulesTests
    {
        [Test]
        public void BoardCatalogContainsTheRequiredPropertyGroups()
        {
            Assert.AreEqual(32, BoardCatalog.AllTiles.Count);
            Assert.AreEqual(20, CountTiles(TileType.City));
            Assert.AreEqual(4, CountTiles(TileType.Beach));
            Assert.AreEqual(8, CountFunctionalTiles());
            Assert.AreEqual("Australia", BoardCatalog.GetTile(29).CountryId);
            Assert.AreEqual("TP20", BoardCatalog.GetTile(29).CityCode);
            Assert.AreEqual("TP21", BoardCatalog.GetTile(31).CityCode);
        }

        [Test]
        public void RentModifiersAreAddedBeforeTheFinalMultiplier()
        {
            BoardTile tile = BoardCatalog.GetTile(1);

            RentQuote quote = EconomyCalculator.CalculateRent(
                tile,
                PropertyLevel.House3,
                0,
                countryMonopoly: true,
                fixedFestival: true,
                temporaryFestival: true,
                additivePercent: 0,
                doubled: true);

            Assert.AreEqual(30000, quote.BaseRent);
            Assert.AreEqual(132000, quote.FinalRent);
        }

        [Test]
        public void LawyerCardTaxKeepsFractionsUntilTheFinalFloor()
        {
            Assert.AreEqual(
                30001,
                EconomyCalculator.TaxAtAssessmentAfterLawyerCardReduction(
                    100008,
                    new[] { 100000L }));
        }

        [Test]
        public void UpgradeInvestmentIsAllocatedAcrossEveryNewTier()
        {
            PlayerState player = new PlayerState("A", 0, "A", ControllerKind.Human);
            var state = MatchCoordinator.CreateNew(
                new List<PlayerState>
                {
                    player,
                    new PlayerState("B", 1, "B", ControllerKind.Ai)
                },
                TimeLimit.Unlimited,
                new[] { 1, 5, 9 });
            var coordinator = new MatchCoordinator(state, () => new DiceRoll(1, 1));

            coordinator.RollAndMove();
            coordinator.Buy(2, PropertyLevel.House1);
            coordinator.Upgrade(2, PropertyLevel.House3, "landing-1");

            CollectionAssert.AreEqual(new long[] { 56000, 28000, 28000, 28000, 0 },
                state.PropertiesByTileId[2].InvestedByTier);
        }

        [Test]
        public void BuyRejectsRemotePurchaseBeforeChargingThePlayer()
        {
            PlayerState player = new PlayerState("A", 0, "A", ControllerKind.Human);
            var state = MatchCoordinator.CreateNew(
                new List<PlayerState>
                {
                    player,
                    new PlayerState("B", 1, "B", ControllerKind.Ai)
                },
                TimeLimit.Unlimited,
                new[] { 1, 5, 9 });
            var coordinator = new MatchCoordinator(state, () => new DiceRoll(1, 1));

            Assert.Throws<System.InvalidOperationException>(
                () => coordinator.Buy(1, PropertyLevel.Stake));
            Assert.AreEqual(GameConstants.StartingCash, player.Cash);
            Assert.IsFalse(state.PropertiesByTileId[1].IsOwned);
        }

        [Test]
        public void NewMatchSelectsExactlyThreeDistinctFestivalCities()
        {
            var state = MatchCoordinator.CreateNew(
                new List<PlayerState>
                {
                    new PlayerState("A", 0, "A", ControllerKind.Human),
                    new PlayerState("B", 1, "B", ControllerKind.Ai)
                },
                TimeLimit.Minutes30);

            Assert.AreEqual(3, state.FixedFestivalCityIds.Count);
            CollectionAssert.AllItemsAreUnique(state.FixedFestivalCityIds);
            foreach (int tileId in state.FixedFestivalCityIds)
                Assert.AreEqual(TileType.City, BoardCatalog.GetTile(tileId).Type);
        }

        [Test]
        public void TemporaryFestivalDoesNotConsumeTheActivatingTurn()
        {
            PlayerState player = new PlayerState("A", 0, "A", ControllerKind.Human);
            var state = MatchCoordinator.CreateNew(
                new List<PlayerState>
                {
                    player,
                    new PlayerState("B", 1, "B", ControllerKind.Ai)
                },
                TimeLimit.Unlimited,
                new[] { 1, 5, 9 });
            var coordinator = new MatchCoordinator(state, () => new DiceRoll(1, 1));

            coordinator.RollAndMove();
            coordinator.Buy(2, PropertyLevel.Stake);
            coordinator.CreateTemporaryFestival(2);
            coordinator.EndTurn();

            Assert.AreEqual(2, state.PropertiesByTileId[2].TemporaryFestivalTurnsRemaining);
        }

        [Test]
        public void BankruptcyRejectsPayerWhoCanStillSettle()
        {
            PlayerState player = new PlayerState("A", 0, "A", ControllerKind.Human);
            var state = MatchCoordinator.CreateNew(
                new List<PlayerState>
                {
                    player,
                    new PlayerState("B", 1, "B", ControllerKind.Ai)
                },
                TimeLimit.Unlimited,
                new[] { 1, 5, 9 });
            var coordinator = new MatchCoordinator(state, () => new DiceRoll(1, 1));

            coordinator.RollAndMove();
            coordinator.Buy(2, PropertyLevel.House3);
            coordinator.AssessTax(false);

            Assert.Throws<System.InvalidOperationException>(
                () => coordinator.MarkCurrentPlayerBankrupt());
            Assert.AreEqual(PlayerStatus.Active, player.Status);
        }

        [Test]
        public void BankerBankruptcyContinuesTheExistingTurnBoundaryOnce()
        {
            PlayerState banker = new PlayerState("A", 0, "A", ControllerKind.Human, "Banker");
            PlayerState opponent = new PlayerState("B", 1, "B", ControllerKind.Ai);
            PlayerState thirdPlayer = new PlayerState("C", 2, "C", ControllerKind.Ai);
            var state = MatchCoordinator.CreateNew(
                new List<PlayerState> { banker, opponent, thirdPlayer },
                TimeLimit.Unlimited,
                new[] { 1, 5, 9 });
            var coordinator = new MatchCoordinator(
                state,
                new Queue<DiceRoll>(new[] { new DiceRoll(1, 2), new DiceRoll(2, 3) }).Dequeue);

            coordinator.RollAndMove();
            coordinator.Buy(3, PropertyLevel.House1);
            coordinator.TakeBankerLoan();
            coordinator.EndTurn();

            coordinator.RollAndMove();
            coordinator.Buy(5, PropertyLevel.House1);
            coordinator.EndTurn();
            coordinator.EndTurn();

            for (int completedTurn = 2; completedTurn <= 5; completedTurn++)
            {
                if (completedTurn == 4)
                    coordinator.ApplyPowerCut(5);

                coordinator.AssessTax(false);
                coordinator.ResolvePendingObligation();
                coordinator.EndTurn();
                if (completedTurn < 5)
                {
                    coordinator.EndTurn();
                    coordinator.EndTurn();
                }
            }

            Assert.IsTrue(state.TurnBoundaryPending);
            Assert.AreEqual(5, banker.CompletedPersonalTurns);
            Assert.AreEqual(7, state.PersonalTurnOrdinal);
            Assert.AreEqual(1, state.CompletedPlayersThisRound);
            Assert.AreEqual(110000, state.PendingObligation.Value.Amount);
            Assert.AreEqual(2, state.PropertiesByTileId[5].PowerCutTurnsRemaining);

            coordinator.SellToBank(3);
            coordinator.MarkCurrentPlayerBankrupt();

            Assert.AreEqual(PlayerStatus.Bankrupt, banker.Status);
            Assert.AreEqual(5, banker.CompletedPersonalTurns);
            Assert.AreEqual(7, state.PersonalTurnOrdinal);
            Assert.AreEqual(1, state.CompletedPlayersThisRound);
            Assert.AreEqual(2, state.PropertiesByTileId[5].PowerCutTurnsRemaining);
            Assert.IsFalse(state.TurnBoundaryPending);
            Assert.IsFalse(state.PendingObligation.HasValue);
            Assert.AreSame(opponent, state.CurrentPlayer);
            Assert.AreEqual(TurnPhase.AwaitingRoll, state.TurnPhase);
            Assert.AreEqual(PlayerStatus.Active, thirdPlayer.Status);
        }

        [Test]
        public void BankerLoanSettlementAtTimeLimitRanksTheMatchImmediately()
        {
            PlayerState firstPlayer = new PlayerState("A", 0, "A", ControllerKind.Human);
            PlayerState secondPlayer = new PlayerState("C", 1, "C", ControllerKind.Ai);
            PlayerState banker = new PlayerState("B", 2, "B", ControllerKind.Ai, "Banker");
            var state = MatchCoordinator.CreateNew(
                new List<PlayerState> { firstPlayer, secondPlayer, banker },
                TimeLimit.Minutes30,
                new[] { 1, 5, 9 });
            var coordinator = new MatchCoordinator(state, () => new DiceRoll(1, 1));

            coordinator.AssessTax(false);
            coordinator.ResolvePendingObligation();
            coordinator.EndTurn();
            coordinator.AssessTax(false);
            coordinator.ResolvePendingObligation();
            coordinator.EndTurn();

            coordinator.TakeBankerLoan();
            coordinator.EndTurn();
            for (int bankerTurn = 2; bankerTurn <= 5; bankerTurn++)
            {
                coordinator.EndTurn();
                coordinator.EndTurn();
                if (bankerTurn == 5)
                    coordinator.AdvanceClock(30 * 60);
                coordinator.EndTurn();
            }

            Assert.IsTrue(state.TimeExpired);
            Assert.AreEqual(TurnPhase.AwaitingObligation, state.TurnPhase);
            Assert.AreEqual(110000, state.PendingObligation.Value.Amount);

            coordinator.ResolvePendingObligation();

            Assert.AreEqual(MatchStatus.Finished, state.MatchStatus);
            Assert.AreEqual(TurnPhase.Finished, state.TurnPhase);
            Assert.AreEqual(WinReason.TimeLimit, state.WinReason);
            CollectionAssert.AreEqual(new[] { "B" }, state.Winners);
            Assert.AreEqual(292000, banker.Cash);
            Assert.AreEqual(240000, firstPlayer.Cash);
            Assert.AreEqual(240000, secondPlayer.Cash);
        }

        private static int CountTiles(TileType type)
        {
            int count = 0;
            foreach (BoardTile tile in BoardCatalog.AllTiles)
            {
                if (tile.Type == type)
                    count++;
            }

            return count;
        }

        private static int CountFunctionalTiles()
        {
            int count = 0;
            foreach (BoardTile tile in BoardCatalog.AllTiles)
            {
                if (!tile.IsProperty)
                    count++;
            }

            return count;
        }
    }
}
