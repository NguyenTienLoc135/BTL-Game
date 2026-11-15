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
