using System;
using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Properties;

namespace BtlGame.Core
{
    public sealed class MatchCoordinator
    {
        public GameState State { get; private set; }

        public MatchCoordinator(GameState state)
        {
            State = state ?? throw new ArgumentNullException("state");
        }

        public MovementResult MoveCurrentPlayer(DiceResult dice)
        {
            EnsureRunning();
            PlayerState player = State.GetPlayer(State.CurrentPlayerId);
            MovementResult movement = BoardRules.CalculateMovement(player.PositionTileId, dice);
            player.PositionTileId = movement.DestinationTileId;
            player.Cash += movement.PassedStartCount * 100000L;
            if (BoardCatalog.GetTile(movement.DestinationTileId).Type == TileType.Jail)
            {
                player.IsJailed = true;
                player.JailTurnsStarted = 0;
            }

            State.NextLandingId++;
            State.Commit();
            return movement;
        }

        public void BuyCity(int playerId, int tileId, CityLevel level, int landingId, long actualPrice)
        {
            EnsureRunning();
            PlayerState buyer = State.GetPlayer(playerId);
            BoardTile tile = BoardCatalog.GetTile(tileId);
            PropertyState property = State.GetProperty(tileId);
            if (tile.Type != TileType.City || property.OwnerId.HasValue || level == CityLevel.Hotel)
            {
                throw new InvalidOperationException("City purchase is not valid.");
            }

            long listedPrice = EconomyRules.GetPurchasePrice(tile, level);
            ValidatePayment(buyer, actualPrice);
            if (actualPrice != listedPrice)
            {
                throw new InvalidOperationException("Purchase price must match the selected listed price.");
            }

            buyer.Cash -= actualPrice;
            property.OwnerId = playerId;
            property.CityLevel = level;
            property.InvestedByTier = EconomyRules.AllocateInvestment(actualPrice, tile, level);
            property.AcquiredOnLandingId = landingId;
            if (level == CityLevel.House3)
            {
                property.House3ReachedOnLandingId = landingId;
            }

            State.Commit();
            CheckImmediateWin();
        }

        public void BuyBeach(int playerId, int tileId, long actualPrice)
        {
            EnsureRunning();
            PlayerState buyer = State.GetPlayer(playerId);
            BoardTile tile = BoardCatalog.GetTile(tileId);
            PropertyState property = State.GetProperty(tileId);
            if (tile.Type != TileType.Beach || property.OwnerId.HasValue || actualPrice != tile.BeachPurchasePrice)
            {
                throw new InvalidOperationException("Beach purchase is not valid.");
            }

            ValidatePayment(buyer, actualPrice);
            buyer.Cash -= actualPrice;
            property.OwnerId = playerId;
            property.BeachActualInvestment = actualPrice;
            State.Commit();
            CheckImmediateWin();
        }

        public RentQuote QuoteRent(int payerId, int tileId, bool doubled)
        {
            PlayerState payer = State.GetPlayer(payerId);
            PropertyState property = State.GetProperty(tileId);
            if (!property.OwnerId.HasValue || property.OwnerId.Value == payerId)
            {
                throw new InvalidOperationException("Rent requires an opponent-owned property.");
            }

            BoardTile tile = BoardCatalog.GetTile(tileId);
            if (tile.Type == TileType.Beach)
            {
                return new RentQuote(
                    EconomyRules.GetBeachRent(CountOwnedBeaches(property.OwnerId.Value)),
                    0,
                    doubled,
                    EconomyRules.GetBeachRent(CountOwnedBeaches(property.OwnerId.Value)) * (doubled ? 2 : 1));
            }

            int modifier = 0;
            if (OwnsAllCitiesInCountry(property.OwnerId.Value, tile.CountryId))
            {
                modifier += 20;
            }

            if (State.FixedFestivalCityIds.Contains(tileId))
            {
                modifier += 50;
            }

            return EconomyRules.QuoteCityRent(tile, property.CityLevel.Value, modifier, doubled);
        }

        public void PayRent(int payerId, int tileId, long amount)
        {
            EnsureRunning();
            PlayerState payer = State.GetPlayer(payerId);
            PropertyState property = State.GetProperty(tileId);
            if (!property.OwnerId.HasValue || property.OwnerId.Value == payerId)
            {
                throw new InvalidOperationException("Rent requires an opponent-owned property.");
            }

            ValidatePayment(payer, amount);
            payer.Cash -= amount;
            State.GetPlayer(property.OwnerId.Value).Cash += amount;
            State.Commit();
        }

        public void ForcePurchase(int buyerId, int tileId, int percentOfListedPrice, long actualPrice)
        {
            EnsureRunning();
            PlayerState buyer = State.GetPlayer(buyerId);
            PropertyState property = State.GetProperty(tileId);
            BoardTile tile = BoardCatalog.GetTile(tileId);
            if (!property.OwnerId.HasValue || property.OwnerId.Value == buyerId ||
                tile.Type != TileType.City || property.CityLevel == CityLevel.Hotel)
            {
                throw new InvalidOperationException("Forced purchase is not valid.");
            }

            long expectedPrice = EconomyRules.GetForcedPurchasePrice(tile, property.CityLevel.Value, percentOfListedPrice);
            if (actualPrice != expectedPrice)
            {
                throw new InvalidOperationException("Forced purchase price is incorrect.");
            }

            ValidatePayment(buyer, actualPrice);
            int previousOwnerId = property.OwnerId.Value;
            buyer.Cash -= actualPrice;
            State.GetPlayer(previousOwnerId).Cash += actualPrice;
            property.OwnerId = buyerId;
            property.InvestedByTier = EconomyRules.AllocateInvestment(actualPrice, tile, property.CityLevel.Value);
            property.AcquiredOnLandingId = State.NextLandingId;
            State.Commit();
            CheckImmediateWin();
        }

        public void UpgradeCity(int playerId, int tileId, CityLevel targetLevel, int landingId, long actualPrice)
        {
            EnsureRunning();
            PlayerState player = State.GetPlayer(playerId);
            BoardTile tile = BoardCatalog.GetTile(tileId);
            PropertyState property = State.GetProperty(tileId);
            if (!property.OwnerId.HasValue || property.OwnerId.Value != playerId || tile.Type != TileType.City)
            {
                throw new InvalidOperationException("City does not belong to the player.");
            }

            if (!property.CityLevel.HasValue || targetLevel <= property.CityLevel.Value ||
                targetLevel == CityLevel.Hotel && property.CityLevel.Value != CityLevel.House3 ||
                property.NormalUpgradeUsedOnLandingId == landingId)
            {
                throw new InvalidOperationException("Upgrade is not valid.");
            }

            if (targetLevel == CityLevel.Hotel && property.House3ReachedOnLandingId == landingId)
            {
                throw new InvalidOperationException("Hotel cannot be completed on the initial House 3 landing.");
            }

            long priceDifference = EconomyRules.GetListedPrice(tile, targetLevel) -
                EconomyRules.GetListedPrice(tile, property.CityLevel.Value);
            if (actualPrice != priceDifference)
            {
                throw new InvalidOperationException("Upgrade price is not the listed difference.");
            }

            ValidatePayment(player, actualPrice);
            player.Cash -= actualPrice;
            property.CityLevel = targetLevel;
            property.InvestedByTier[(int)targetLevel] = actualPrice;
            property.NormalUpgradeUsedOnLandingId = landingId;
            State.Commit();
        }

        public long SellToBank(int playerId, int tileId)
        {
            EnsureRunning();
            PlayerState player = State.GetPlayer(playerId);
            PropertyState property = State.GetProperty(tileId);
            if (!property.OwnerId.HasValue || property.OwnerId.Value != playerId)
            {
                throw new InvalidOperationException("Property does not belong to the player.");
            }

            long payout = EconomyRules.GetSaleValue(property.TotalInvestment);
            property.Reset();
            player.Cash += payout;
            State.Commit();
            return payout;
        }

        public long PayTax(int playerId)
        {
            EnsureRunning();
            PlayerState player = State.GetPlayer(playerId);
            long listedValue = 0;
            foreach (PropertyState property in State.GetProperties())
            {
                if (property.OwnerId.HasValue && property.OwnerId.Value == playerId)
                {
                    BoardTile tile = BoardCatalog.GetTile(property.TileId);
                    if (tile.Type == TileType.Beach)
                    {
                        listedValue += tile.BeachPurchasePrice;
                    }
                    else
                    {
                        listedValue += EconomyRules.GetListedPrice(tile, property.CityLevel.Value);
                    }
                }
            }

            long tax = EconomyRules.GetTax(player.Cash, listedValue);
            ValidatePayment(player, tax);
            player.Cash -= tax;
            State.Commit();
            return tax;
        }

        public WinResult CheckImmediateWin()
        {
            List<int> winners = new List<int>();
            foreach (PlayerState player in State.GetPlayers())
            {
                if (player.Status != PlayerStatus.Active)
                {
                    continue;
                }

                if (OwnsAllBeaches(player.PlayerId))
                {
                    winners.Add(player.PlayerId);
                    return Finish(winners, WinReason.FourBeaches);
                }

                if (OwnsThreeCountries(player.PlayerId))
                {
                    winners.Add(player.PlayerId);
                    return Finish(winners, WinReason.ThreeCountries);
                }
            }

            int activeCount = 0;
            int lastActiveId = -1;
            foreach (PlayerState player in State.GetPlayers())
            {
                if (player.Status == PlayerStatus.Active)
                {
                    activeCount++;
                    lastActiveId = player.PlayerId;
                }
            }

            if (activeCount == 1)
            {
                winners.Add(lastActiveId);
                return Finish(winners, WinReason.LastActivePlayer);
            }

            return new WinResult(false, winners, WinReason.None);
        }

        private bool OwnsAllBeaches(int playerId)
        {
            int[] beaches = { 4, 14, 18, 25 };
            for (int i = 0; i < beaches.Length; i++)
            {
                if (State.GetProperty(beaches[i]).OwnerId != playerId)
                {
                    return false;
                }
            }

            return true;
        }

        private int CountOwnedBeaches(int playerId)
        {
            int count = 0;
            int[] beaches = { 4, 14, 18, 25 };
            for (int i = 0; i < beaches.Length; i++)
            {
                if (State.GetProperty(beaches[i]).OwnerId == playerId)
                {
                    count++;
                }
            }

            return count;
        }

        private bool OwnsAllCitiesInCountry(int playerId, string countryId)
        {
            if (string.IsNullOrEmpty(countryId))
            {
                return false;
            }

            int owned = 0;
            foreach (PropertyState property in State.GetProperties())
            {
                BoardTile tile = BoardCatalog.GetTile(property.TileId);
                if (tile.Type == TileType.City && tile.CountryId == countryId && property.OwnerId == playerId)
                {
                    owned++;
                }
            }

            return owned == BoardCatalog.CountCitiesInCountry(countryId);
        }

        private bool OwnsThreeCountries(int playerId)
        {
            string[] countries = { "Vietnam", "Japan", "Korea", "France", "Italy", "USA", "UK", "Australia" };
            int complete = 0;
            for (int i = 0; i < countries.Length; i++)
            {
                int owned = 0;
                int total = BoardCatalog.CountCitiesInCountry(countries[i]);
                foreach (PropertyState property in State.GetProperties())
                {
                    if (property.OwnerId == playerId &&
                        BoardCatalog.GetTile(property.TileId).CountryId == countries[i])
                    {
                        owned++;
                    }
                }

                if (owned == total)
                {
                    complete++;
                }
            }

            return complete >= 3;
        }

        private WinResult Finish(IList<int> winners, WinReason reason)
        {
            State.Status = MatchStatus.Finished;
            State.Result = new WinResult(true, winners, reason);
            State.Commit();
            return State.Result;
        }

        private static void ValidatePayment(PlayerState player, long amount)
        {
            if (amount < 0 || player.Cash < amount)
            {
                throw new InvalidOperationException("Insufficient cash for an optional transaction.");
            }
        }

        private void EnsureRunning()
        {
            if (State.Status != MatchStatus.Running)
            {
                throw new InvalidOperationException("The match is not running.");
            }
        }
    }
}
