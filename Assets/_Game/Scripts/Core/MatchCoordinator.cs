using System;
using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Properties;

namespace BtlGame.Core
{
    public readonly struct DiceRoll
    {
        public DiceRoll(int first, int second)
        {
            if (first < 1 || first > 6 || second < 1 || second > 6)
                throw new ArgumentOutOfRangeException();
            First = first;
            Second = second;
        }

        public int First { get; }
        public int Second { get; }
        public int Total => First + Second;
        public bool IsDouble => First == Second;
    }

    public sealed class MatchCoordinator
    {
        private readonly Func<DiceRoll> _rollDice;

        public MatchCoordinator(GameState state, Func<DiceRoll> rollDice)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            _rollDice = rollDice ?? throw new ArgumentNullException(nameof(rollDice));
        }

        public GameState State { get; }

        public static GameState CreateNew(IReadOnlyList<PlayerState> players, TimeLimit timeLimit)
        {
            if (players == null || players.Count < 2 || players.Count > 4)
                throw new ArgumentException("A match requires two to four players.", nameof(players));

            var ids = new HashSet<string>();
            foreach (PlayerState player in players)
            {
                if (player == null || !ids.Add(player.PlayerId))
                    throw new ArgumentException("Players must be non-null and have unique ids.", nameof(players));
            }

            return new GameState(BoardCatalog.AllTiles, players)
            {
                RemainingSeconds = timeLimit == TimeLimit.Unlimited ? 0 : (long)timeLimit * 60
            };
        }

        public DiceRoll RollAndMove()
        {
            EnsureRunning();
            if (State.TurnPhase != TurnPhase.AwaitingRoll)
                throw new InvalidOperationException("The current turn is not ready to roll.");

            PlayerState player = State.CurrentPlayer;
            DiceRoll roll = _rollDice();
            State.RollOrdinalWithinTurn++;

            MovementResult movement = BoardCatalog.Move(player.PositionTileId, roll.Total);
            player.PositionTileId = movement.DestinationTileId;
            if (movement.StartPassCount > 0)
                player.Cash = checked(player.Cash + movement.StartPassCount * GameConstants.StartPassMoney);

            if (movement.LandedOnJail)
            {
                player.IsJailed = true;
                player.JailTurnsStarted = 0;
                State.TurnPhase = TurnPhase.ResolvingLanding;
            }
            else
            {
                State.TurnPhase = TurnPhase.ResolvingLanding;
            }

            return roll;
        }

        public void CompleteLanding(bool landedInJail)
        {
            EnsureRunning();
            if (State.TurnPhase != TurnPhase.ResolvingLanding)
                throw new InvalidOperationException("A landing is not being resolved.");

            if (landedInJail)
                State.CurrentPlayer.IsJailed = true;

            State.TurnPhase = TurnPhase.AwaitingRoll;
        }

        public void EndTurn()
        {
            EnsureRunning();
            if (State.TurnPhase != TurnPhase.AwaitingRoll && State.TurnPhase != TurnPhase.ResolvingLanding)
                throw new InvalidOperationException("The current turn still has unresolved work.");

            PlayerState player = State.CurrentPlayer;
            player.CompletedPersonalTurns++;
            State.PersonalTurnOrdinal++;
            State.RollOrdinalWithinTurn = 0;
            SelectNextActivePlayer();
            State.TurnPhase = TurnPhase.AwaitingRoll;
            CheckImmediateWin();
        }

        public void Buy(int tileId, PropertyLevel level)
        {
            EnsureRunning();
            PlayerState buyer = State.CurrentPlayer;
            PropertyState property = GetProperty(tileId);
            BoardTile tile = BoardCatalog.GetTile(tileId);
            if (property.IsOwned || tile.Type == TileType.Beach && level != PropertyLevel.Stake)
                throw new InvalidOperationException("The property is not available at the requested level.");
            long price = EconomyCalculator.PurchasePrice(tile, level);
            Spend(buyer, price);
            property.OwnerId = buyer.PlayerId;
            property.Level = level;
            if (tile.Type == TileType.Beach)
                property.BeachInvestment = price;
            else
                Copy(EconomyCalculator.AllocateInvestment(price, level), property.InvestedByTier);
            CheckImmediateWin();
        }

        public void Upgrade(int tileId, PropertyLevel target, string landingId)
        {
            EnsureRunning();
            if (string.IsNullOrWhiteSpace(landingId))
                throw new ArgumentException("Landing id is required.", nameof(landingId));

            PlayerState player = State.CurrentPlayer;
            PropertyState property = GetOwnedProperty(tileId, player.PlayerId);
            BoardTile tile = BoardCatalog.GetTile(tileId);
            if (property.NormalUpgradeUsedOnLandingId == landingId)
                throw new InvalidOperationException("Only one normal upgrade is allowed per landing.");
            if (target == PropertyLevel.Hotel && property.Level != PropertyLevel.House3)
                throw new InvalidOperationException("A hotel requires a prior House 3 landing.");
            long price = EconomyCalculator.UpgradePrice(tile, property.Level, target);
            Spend(player, price);
            property.Level = target;
            property.NormalUpgradeUsedOnLandingId = landingId;
            if (target != PropertyLevel.Hotel)
                property.InvestedByTier[(int)target] = checked(property.InvestedByTier[(int)target] + price);
            else
                property.InvestedByTier[(int)target] = price;
        }

        public long SellToBank(int tileId)
        {
            EnsureRunning();
            PropertyState property = GetOwnedProperty(tileId, State.CurrentPlayer.PlayerId);
            long refund = EconomyCalculator.LiquidationValue(property);
            State.CurrentPlayer.Cash = checked(State.CurrentPlayer.Cash + refund);
            ResetProperty(property);
            return refund;
        }

        private void SelectNextActivePlayer()
        {
            for (int offset = 1; offset <= State.Players.Count; offset++)
            {
                int candidate = (State.CurrentPlayerIndex + offset) % State.Players.Count;
                if (State.Players[candidate].Status == PlayerStatus.Active)
                {
                    State.CurrentPlayerIndex = candidate;
                    return;
                }
            }

            State.MatchStatus = MatchStatus.Finished;
            State.TurnPhase = TurnPhase.Finished;
        }

        private void CheckImmediateWin()
        {
            foreach (PlayerState player in State.Players)
            {
                if (player.Status != PlayerStatus.Active)
                    continue;

                int countries = 0;
                foreach (string country in CountriesOwnedBy(player.PlayerId))
                    if (country != null)
                        countries++;

                int beaches = 0;
                foreach (PropertyState property in State.PropertiesByTileId.Values)
                    if (property.Type == TileType.Beach && property.OwnerId == player.PlayerId)
                        beaches++;

                if (countries >= 3)
                {
                    Finish(player, WinReason.ThreeCountries);
                    return;
                }

                if (beaches == 4)
                {
                    Finish(player, WinReason.FourBeaches);
                    return;
                }
            }
        }

        private IEnumerable<string> CountriesOwnedBy(string playerId)
        {
            var all = new HashSet<string>();
            var completeCountries = new HashSet<string>();
            foreach (BoardTile tile in State.Board)
            {
                if (tile.Type == TileType.City && GetProperty(tile.Id).OwnerId == playerId)
                    all.Add(tile.CountryId);
            }

            foreach (string country in all)
            {
                bool isComplete = true;
                foreach (BoardTile tile in State.Board)
                {
                    if (tile.Type == TileType.City && tile.CountryId == country && GetProperty(tile.Id).OwnerId != playerId)
                    {
                        isComplete = false;
                        break;
                    }
                }

                if (isComplete)
                    completeCountries.Add(country);
            }

            return completeCountries;
        }

        private void Finish(PlayerState player, WinReason reason)
        {
            State.MatchStatus = MatchStatus.Finished;
            State.TurnPhase = TurnPhase.Finished;
            State.WinReason = reason;
            State.Winners = new[] { player.PlayerId };
        }

        private PropertyState GetProperty(int tileId)
        {
            if (!State.PropertiesByTileId.TryGetValue(tileId, out PropertyState property))
                throw new ArgumentException("Tile is not a property.", nameof(tileId));
            return property;
        }

        private PropertyState GetOwnedProperty(int tileId, string ownerId)
        {
            PropertyState property = GetProperty(tileId);
            if (property.OwnerId != ownerId)
                throw new InvalidOperationException("The current player does not own this property.");
            return property;
        }

        private static void Spend(PlayerState player, long amount)
        {
            if (amount < 0 || player.Cash < amount)
                throw new InvalidOperationException("The player cannot afford this optional action.");
            player.Cash -= amount;
        }

        private static void Copy(long[] source, long[] destination)
        {
            Array.Clear(destination, 0, destination.Length);
            Array.Copy(source, destination, source.Length);
        }

        private static void ResetProperty(PropertyState property)
        {
            property.OwnerId = null;
            property.Level = PropertyLevel.Unowned;
            property.BeachInvestment = 0;
            property.NormalUpgradeUsedOnLandingId = null;
            Array.Clear(property.InvestedByTier, 0, property.InvestedByTier.Length);
        }

        private void EnsureRunning()
        {
            if (State.MatchStatus != MatchStatus.Running)
                throw new InvalidOperationException("The match is not running.");
        }
    }
}
