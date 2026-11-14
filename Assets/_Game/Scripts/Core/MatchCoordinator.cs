using System;
using System.Collections.Generic;
using System.Linq;
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
                RemainingSeconds = timeLimit == TimeLimit.Unlimited ? 0 : (long)timeLimit * 60,
                RoundOrdinal = 1,
                FixedFestivalCityIds = new[] { 1, 5, 9 }
            };
        }

        public static GameState CreateNew(
            IReadOnlyList<PlayerState> players,
            TimeLimit timeLimit,
            IReadOnlyList<int> fixedFestivalCityIds)
        {
            if (fixedFestivalCityIds == null || fixedFestivalCityIds.Count != 3)
                throw new ArgumentException("Exactly three festival cities are required.", nameof(fixedFestivalCityIds));

            var state = CreateNew(players, timeLimit);
            var selected = new HashSet<int>();
            foreach (int tileId in fixedFestivalCityIds)
            {
                BoardTile tile = BoardCatalog.GetTile(tileId);
                if (tile.Type != TileType.City || !selected.Add(tileId))
                    throw new ArgumentException("Festival targets must be three distinct city tiles.", nameof(fixedFestivalCityIds));
            }

            state.FixedFestivalCityIds = new List<int>(fixedFestivalCityIds);
            return state;
        }

        public DiceRoll RollAndMove()
        {
            EnsureRunning();
            EnsureNoObligation();
            if (State.TimeExpired)
                throw new InvalidOperationException("The time limit has expired; finish the current landing chain.");
            if (State.CurrentPlayer.IsJailed)
                throw new InvalidOperationException("A jailed player must resolve jail before rolling.");
            EnsurePhase(TurnPhase.AwaitingRoll);

            DiceRoll roll = _rollDice();
            State.RollOrdinalWithinTurn++;
            MovementResult movement = MoveCurrentPlayer(roll);
            if (movement.LandedOnJail)
            {
                State.CurrentPlayer.IsJailed = true;
                State.CurrentPlayer.JailTurnsStarted = 0;
            }
            State.ExtraRollAvailable = roll.IsDouble;
            State.TurnWasForcedToEnd = State.CurrentPlayer.IsJailed;
            State.TurnPhase = TurnPhase.ResolvingLanding;
            return roll;
        }

        public DiceRoll RollInJail()
        {
            EnsureRunning();
            EnsureNoObligation();
            PlayerState player = State.CurrentPlayer;
            if (!player.IsJailed)
                throw new InvalidOperationException("The current player is not jailed.");
            EnsurePhase(TurnPhase.AwaitingRoll);

            if (player.JailTurnsStarted == 0)
                player.JailTurnsStarted = 1;

            DiceRoll roll = _rollDice();
            State.RollOrdinalWithinTurn++;
            if (!roll.IsDouble && player.JailTurnsStarted < GameConstants.MaximumJailTurns)
            {
                State.ExtraRollAvailable = false;
                State.TurnWasForcedToEnd = true;
                State.TurnPhase = TurnPhase.ResolvingLanding;
                return roll;
            }

            if (!roll.IsDouble && player.JailTurnsStarted >= GameConstants.MaximumJailTurns)
            {
                SetPendingObligation(new PendingObligation(
                    player.PlayerId, null, GameConstants.JailFee, ObligationKind.JailFee));
                State.ExtraRollAvailable = false;
                State.TurnWasForcedToEnd = true;
                return roll;
            }

            player.IsJailed = false;
            player.JailTurnsStarted = 0;
            MoveCurrentPlayer(roll);
            State.ExtraRollAvailable = roll.IsDouble;
            State.TurnWasForcedToEnd = false;
            State.TurnPhase = TurnPhase.ResolvingLanding;
            return roll;
        }

        public void PayJailFee()
        {
            EnsureRunning();
            PlayerState player = State.CurrentPlayer;
            if (!player.IsJailed || State.TurnPhase != TurnPhase.AwaitingRoll)
                throw new InvalidOperationException("The current player cannot pay a jail fee now.");

            SetPendingObligation(new PendingObligation(player.PlayerId, null, GameConstants.JailFee, ObligationKind.JailFee));
            ResolvePendingObligation();
        }

        public void UseJailReleaseCard()
        {
            EnsureRunning();
            PlayerState player = State.CurrentPlayer;
            if (!player.IsJailed || State.TurnPhase != TurnPhase.AwaitingRoll)
                throw new InvalidOperationException("The current player cannot use a jail card now.");

            player.IsJailed = false;
            player.JailTurnsStarted = 0;
            State.TurnWasForcedToEnd = true;
            EndTurnInternal();
        }

        public void CompleteLanding(bool landedInJail)
        {
            EnsureRunning();
            EnsurePhase(TurnPhase.ResolvingLanding);
            if (landedInJail)
            {
                State.CurrentPlayer.IsJailed = true;
                State.CurrentPlayer.JailTurnsStarted = 0;
                State.TurnWasForcedToEnd = true;
                State.ExtraRollAvailable = false;
            }

            if (State.TurnWasForcedToEnd || !State.ExtraRollAvailable)
                EndTurnInternal();
            else
                State.TurnPhase = TurnPhase.AwaitingRoll;
        }

        public void EndTurn()
        {
            EnsureRunning();
            EnsureNoObligation();
            if (State.TurnPhase == TurnPhase.ResolvingLanding)
                CompleteLanding(false);
            else
            {
                EnsurePhase(TurnPhase.AwaitingRoll);
                EndTurnInternal();
            }
        }

        public void AdvanceClock(long elapsedSeconds)
        {
            EnsureRunning();
            if (elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            if (State.ClockPaused || State.RemainingSeconds == 0)
                return;

            State.RemainingSeconds = Math.Max(0, State.RemainingSeconds - elapsedSeconds);
            if (State.RemainingSeconds == 0)
                State.TimeExpired = true;
        }

        public void SetClockPaused(bool paused, string reason = null)
        {
            EnsureRunning();
            if (paused && string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A pause reason is required.", nameof(reason));

            State.ClockPaused = paused;
            State.ClockPauseReason = paused ? reason : null;
        }

        public void FinalizeExpiredMatch()
        {
            EnsureRunning();
            if (!State.TimeExpired || State.TurnPhase != TurnPhase.AwaitingRoll)
                throw new InvalidOperationException("The current landing and obligations must finish before ranking.");

            RankAtTimeLimit();
        }

        private void RankAtTimeLimit()
        {
            PlayerState best = null;
            foreach (PlayerState candidate in State.Players)
            {
                if (candidate.Status != PlayerStatus.Active)
                    continue;
                if (best == null || candidate.Cash > best.Cash ||
                    candidate.Cash == best.Cash && ListedAssets(candidate.PlayerId) > ListedAssets(best.PlayerId))
                    best = candidate;
            }

            if (best == null)
            {
                Finish(new List<string>(), WinReason.TimeLimit);
                return;
            }

            var winners = new List<string>();
            foreach (PlayerState candidate in State.Players)
            {
                if (candidate.Status == PlayerStatus.Active &&
                    candidate.Cash == best.Cash &&
                    ListedAssets(candidate.PlayerId) == ListedAssets(best.PlayerId))
                    winners.Add(candidate.PlayerId);
            }

            Finish(winners, WinReason.TimeLimit);
        }

        public void Buy(int tileId, PropertyLevel level)
        {
            Buy(tileId, level, null);
        }

        public void Buy(int tileId, PropertyLevel level, string landingId)
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
            property.NormalUpgradeUsedOnLandingId = null;
            property.House3ReachedOnLandingId = level == PropertyLevel.House3 ? landingId : null;
            if (tile.Type == TileType.Beach)
                property.BeachInvestment = price;
            else
                Copy(EconomyCalculator.AllocateInvestment(price, level), property.InvestedByTier);
            CheckImmediateWin();
        }

        public void CreateTemporaryFestival(int tileId)
        {
            EnsureRunning();
            BoardTile tile = BoardCatalog.GetTile(tileId);
            PropertyState property = GetOwnedProperty(tileId, State.CurrentPlayer.PlayerId);
            if (tile.Type != TileType.City || property.TemporaryFestivalTurnsRemaining > 0)
                throw new InvalidOperationException("Festival requires an owned city without an active temporary festival.");

            Spend(State.CurrentPlayer, 200000);
            property.TemporaryFestivalTurnsRemaining = 2;
        }

        public void TakeBankerLoan()
        {
            EnsureRunning();
            PlayerState player = State.CurrentPlayer;
            if (player.RoleId != "Banker" || player.OutstandingLoanAmount != 0)
                throw new InvalidOperationException("Only a Banker without an existing loan can borrow.");

            player.Cash = checked(player.Cash + 100000);
            player.OutstandingLoanAmount = 110000;
            player.LoanDueAfterCompletedTurn = player.CompletedPersonalTurns + 5;
        }

        public MovementResult UseTeleport(int destinationTileId, string actionId)
        {
            EnsureRunning();
            if (State.CurrentPlayer.PositionTileId != 24 || string.IsNullOrWhiteSpace(actionId))
                throw new InvalidOperationException("Teleport requires the current player to be on tile 24.");
            if (destinationTileId == 24)
                throw new InvalidOperationException("Teleport cannot target its own source tile.");
            if (State.TeleportSourceActionId == actionId)
                throw new InvalidOperationException("The same teleport action cannot be repeated.");

            Spend(State.CurrentPlayer, 100000);
            MovementResult movement = BoardCatalog.Move(24, (destinationTileId - 24 + GameConstants.BoardSize) %
                GameConstants.BoardSize);
            State.CurrentPlayer.PositionTileId = destinationTileId;
            if (movement.StartPassCount > 0)
                State.CurrentPlayer.Cash = checked(
                    State.CurrentPlayer.Cash + movement.StartPassCount * GameConstants.StartPassMoney);
            State.TeleportSourceActionId = actionId;
            if (destinationTileId == 8)
            {
                State.CurrentPlayer.IsJailed = true;
                State.CurrentPlayer.JailTurnsStarted = 0;
            }
            return movement;
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
            if (target == PropertyLevel.Hotel &&
                (property.Level != PropertyLevel.House3 || property.House3ReachedOnLandingId == landingId))
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

        public void ForcePurchase(int tileId, int percentOfListedPrice)
        {
            EnsureRunning();
            PropertyState property = GetProperty(tileId);
            if (!property.IsOwned || property.Type != TileType.City || property.Level == PropertyLevel.Hotel)
                throw new InvalidOperationException("Only an opponent city below Hotel can be force-purchased.");
            if (property.OwnerId == State.CurrentPlayer.PlayerId)
                throw new InvalidOperationException("A player cannot force-purchase their own property.");

            long price = EconomyCalculator.ListedPrice(BoardCatalog.GetTile(tileId), property.Level) *
                percentOfListedPrice / 100;
            Spend(State.CurrentPlayer, price);
            PlayerState previousOwner = FindPlayer(property.OwnerId);
            previousOwner.Cash = checked(previousOwner.Cash + price);
            property.OwnerId = State.CurrentPlayer.PlayerId;
            Array.Clear(property.InvestedByTier, 0, property.InvestedByTier.Length);
            Copy(EconomyCalculator.AllocateInvestment(price, property.Level), property.InvestedByTier);
            CheckImmediateWin();
        }

        public void CreateRentObligation(int tileId, bool doubled, int additivePercent = 0)
        {
            EnsureRunning();
            PropertyState property = GetProperty(tileId);
            if (!property.IsOwned || property.OwnerId == State.CurrentPlayer.PlayerId)
                throw new InvalidOperationException("Rent requires an opponent-owned property.");

            BoardTile tile = BoardCatalog.GetTile(tileId);
            bool monopoly = tile.Type == TileType.City && OwnsAllCitiesInCountry(property.OwnerId, tile.CountryId);
            bool fixedFestival = tile.Type == TileType.City &&
                State.FixedFestivalCityIds != null && State.FixedFestivalCityIds.Contains(tileId);
            if (property.PowerCutTurnsRemaining > 0)
            {
                SetPendingObligation(new PendingObligation(
                    State.CurrentPlayer.PlayerId, property.OwnerId, 0, ObligationKind.Rent));
                return;
            }
            RentQuote quote = EconomyCalculator.CalculateRent(
                tile,
                property.Level,
                CountOwnedBeaches(property.OwnerId),
                monopoly,
                fixedFestival,
                property.TemporaryFestivalTurnsRemaining > 0,
                additivePercent,
                doubled);
            SetPendingObligation(new PendingObligation(
                State.CurrentPlayer.PlayerId,
                property.OwnerId,
                quote.FinalRent,
                ObligationKind.Rent));
        }

        public long AssessTax(bool fromCard)
        {
            EnsureRunning();
            var listedPrices = new List<long>();
            foreach (PropertyState property in State.PropertiesByTileId.Values)
            {
                if (property.OwnerId == State.CurrentPlayer.PlayerId)
                    listedPrices.Add(EconomyCalculator.ListedPrice(BoardCatalog.GetTile(property.TileId), property.Level));
            }

            long tax = EconomyCalculator.TaxAtAssessment(State.CurrentPlayer.Cash, listedPrices);
            if (fromCard && State.CurrentPlayer.RoleId == "Lawyer")
                tax = EconomyCalculator.ApplyLawyerCardTaxReduction(tax);
            SetPendingObligation(new PendingObligation(
                State.CurrentPlayer.PlayerId, null, tax, ObligationKind.Tax));
            return tax;
        }

        public void ReducePendingObligation(int reductionPercent)
        {
            EnsureRunning();
            if (!State.PendingObligation.HasValue || State.ObligationAdjustmentUsed ||
                reductionPercent <= 0 || reductionPercent >= 100)
                throw new InvalidOperationException("The obligation cannot be adjusted.");

            PendingObligation current = State.PendingObligation.Value;
            long adjusted = current.Amount * (100 - reductionPercent) / 100;
            State.PendingObligation = new PendingObligation(
                current.PayerId, current.RecipientId, adjusted, current.Kind);
            State.ObligationAdjustmentUsed = true;
        }

        public void WaivePendingObligation()
        {
            EnsureRunning();
            if (!State.PendingObligation.HasValue || State.ObligationAdjustmentUsed)
                throw new InvalidOperationException("The obligation cannot be waived.");

            PendingObligation current = State.PendingObligation.Value;
            State.PendingObligation = new PendingObligation(
                current.PayerId, current.RecipientId, 0, current.Kind);
            State.ObligationAdjustmentUsed = true;
        }

        public void ResolvePendingObligation()
        {
            EnsureRunning();
            if (!State.PendingObligation.HasValue)
                throw new InvalidOperationException("There is no pending obligation.");

            PendingObligation obligation = State.PendingObligation.Value;
            PlayerState payer = FindPlayer(obligation.PayerId);
            if (payer.Cash < obligation.Amount)
                throw new InvalidOperationException("The payer must liquidate assets before settling this obligation.");

            payer.Cash -= obligation.Amount;
            if (obligation.RecipientId != null)
                FindPlayer(obligation.RecipientId).Cash = checked(
                    FindPlayer(obligation.RecipientId).Cash + obligation.Amount);
            State.PendingObligation = null;
            State.ObligationAdjustmentUsed = false;
            if (obligation.Kind == ObligationKind.BankerLoan)
            {
                payer.OutstandingLoanAmount = 0;
                payer.LoanDueAfterCompletedTurn = 0;
                State.TurnBoundaryPending = false;
                ApplyBankerInterestIfRoundComplete();
                SelectNextActivePlayer();
                if (State.MatchStatus == MatchStatus.Running)
                    State.TurnPhase = TurnPhase.AwaitingRoll;
                CheckImmediateWin();
                return;
            }
            if (obligation.Kind == ObligationKind.JailFee)
            {
                payer.IsJailed = false;
                payer.JailTurnsStarted = 0;
                State.TurnWasForcedToEnd = true;
                EndTurnInternal();
                return;
            }
            if (State.TurnPhase == TurnPhase.AwaitingObligation)
                State.TurnPhase = TurnPhase.AwaitingRoll;
        }

        public void MarkCurrentPlayerBankrupt()
        {
            EnsureRunning();
            if (!State.PendingObligation.HasValue)
                throw new InvalidOperationException("Bankruptcy requires a pending obligation.");

            PlayerState player = FindPlayer(State.PendingObligation.Value.PayerId);
            string recipientId = State.PendingObligation.Value.RecipientId;
            if (recipientId != null && player.Cash > 0)
                FindPlayer(recipientId).Cash = checked(FindPlayer(recipientId).Cash + player.Cash);
            foreach (PropertyState property in State.PropertiesByTileId.Values)
            {
                if (property.OwnerId == player.PlayerId)
                    ResetProperty(property);
            }

            player.Cash = 0;
            player.Status = PlayerStatus.Bankrupt;
            player.OutstandingLoanAmount = 0;
            player.LoanDueAfterCompletedTurn = 0;
            State.PendingObligation = null;
            State.ObligationAdjustmentUsed = false;
            CheckImmediateWin();
            if (State.MatchStatus == MatchStatus.Running)
                EndTurnInternal();
        }

        public long DamageProperty(int tileId)
        {
            EnsureRunning();
            PropertyState property = GetProperty(tileId);
            if (!property.IsOwned || property.Type != TileType.City ||
                property.Level == PropertyLevel.Stake || property.Level == PropertyLevel.Hotel)
                throw new InvalidOperationException("Damage requires an opponent city at House 1-3.");

            long lostCapital = property.InvestedByTier[(int)property.Level];
            property.InvestedByTier[(int)property.Level] = 0;
            property.Level--;
            return lostCapital;
        }

        public long DestroyProperty(int tileId)
        {
            EnsureRunning();
            PropertyState property = GetProperty(tileId);
            if (!property.IsOwned || property.Type != TileType.City ||
                property.Level == PropertyLevel.Hotel)
                throw new InvalidOperationException("Property destruction requires an opponent city below Hotel.");

            long refund = EconomyCalculator.DamageRefund(property);
            PlayerState owner = FindPlayer(property.OwnerId);
            owner.Cash = checked(owner.Cash + refund);
            ResetProperty(property);
            CheckImmediateWin();
            return refund;
        }

        public void ApplyPowerCut(int tileId)
        {
            EnsureRunning();
            PropertyState property = GetProperty(tileId);
            if (!property.IsOwned || property.Type != TileType.City ||
                property.Level == PropertyLevel.Hotel)
                throw new InvalidOperationException("Power cut requires an opponent city below Hotel.");

            property.PowerCutTurnsRemaining = 3;
        }

        private MovementResult MoveCurrentPlayer(DiceRoll roll)
        {
            MovementResult movement = BoardCatalog.Move(State.CurrentPlayer.PositionTileId, roll.Total);
            State.CurrentPlayer.PositionTileId = movement.DestinationTileId;
            if (movement.StartPassCount > 0)
                State.CurrentPlayer.Cash = checked(
                    State.CurrentPlayer.Cash + movement.StartPassCount * GameConstants.StartPassMoney);
            return movement;
        }

        private void EndTurnInternal()
        {
            PlayerState player = State.CurrentPlayer;
            player.CompletedPersonalTurns++;
            State.PersonalTurnOrdinal++;
            State.RollOrdinalWithinTurn = 0;
            State.ExtraRollAvailable = false;
            State.TurnWasForcedToEnd = false;
            State.CompletedPlayersThisRound++;
            UpdatePlayerEffects(player);
            if (player.OutstandingLoanAmount > 0 &&
                player.CompletedPersonalTurns >= player.LoanDueAfterCompletedTurn)
            {
                State.TurnBoundaryPending = true;
                SetPendingObligation(new PendingObligation(
                    player.PlayerId, null, player.OutstandingLoanAmount, ObligationKind.BankerLoan));
                return;
            }
            ApplyBankerInterestIfRoundComplete();
            SelectNextActivePlayer();
            if (State.MatchStatus == MatchStatus.Running)
                State.TurnPhase = TurnPhase.AwaitingRoll;
            CheckImmediateWin();
            if (State.MatchStatus == MatchStatus.Running && State.TimeExpired)
                RankAtTimeLimit();
        }

        private void UpdatePlayerEffects(PlayerState player)
        {
            foreach (PropertyState property in State.PropertiesByTileId.Values)
            {
                if (property.OwnerId == player.PlayerId && property.TemporaryFestivalTurnsRemaining > 0)
                    property.TemporaryFestivalTurnsRemaining--;
                if (property.OwnerId == player.PlayerId && property.PowerCutTurnsRemaining > 0)
                    property.PowerCutTurnsRemaining--;
            }
        }

        private void ApplyBankerInterestIfRoundComplete()
        {
            int activeCount = 0;
            foreach (PlayerState player in State.Players)
            {
                if (player.Status == PlayerStatus.Active)
                    activeCount++;
            }

            if (activeCount == 0 || State.CompletedPlayersThisRound < activeCount)
                return;

            foreach (PlayerState player in State.Players)
            {
                if (player.Status == PlayerStatus.Active && player.RoleId == "Banker" && player.Cash > 0)
                    player.Cash = checked(player.Cash + player.Cash * GameConstants.BankerInterestPercent / 100);
            }

            State.CompletedPlayersThisRound = 0;
            State.RoundOrdinal++;
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

            Finish(new List<string>(), WinReason.LastActivePlayer);
        }

        private void CheckImmediateWin()
        {
            foreach (PlayerState player in State.Players)
            {
                if (player.Status != PlayerStatus.Active)
                    continue;
                if (CountriesOwnedBy(player.PlayerId).Count >= 3)
                {
                    Finish(new List<string> { player.PlayerId }, WinReason.ThreeCountries);
                    return;
                }
                if (CountOwnedBeaches(player.PlayerId) == 4)
                {
                    Finish(new List<string> { player.PlayerId }, WinReason.FourBeaches);
                    return;
                }
            }

            int activeCount = 0;
            string lastActiveId = null;
            foreach (PlayerState player in State.Players)
            {
                if (player.Status == PlayerStatus.Active)
                {
                    activeCount++;
                    lastActiveId = player.PlayerId;
                }
            }

            if (activeCount == 1)
                Finish(new List<string> { lastActiveId }, WinReason.LastActivePlayer);
        }

        private HashSet<string> CountriesOwnedBy(string playerId)
        {
            var complete = new HashSet<string>();
            var countries = new HashSet<string>();
            foreach (BoardTile tile in State.Board)
            {
                if (tile.Type == TileType.City && GetProperty(tile.Id).OwnerId == playerId)
                    countries.Add(tile.CountryId);
            }

            foreach (string country in countries)
            {
                bool ownsAll = true;
                foreach (BoardTile tile in State.Board)
                {
                    if (tile.Type == TileType.City && tile.CountryId == country &&
                        GetProperty(tile.Id).OwnerId != playerId)
                    {
                        ownsAll = false;
                        break;
                    }
                }
                if (ownsAll)
                    complete.Add(country);
            }
            return complete;
        }

        private long ListedAssets(string playerId)
        {
            long total = 0;
            foreach (PropertyState property in State.PropertiesByTileId.Values)
            {
                if (property.OwnerId == playerId)
                    total = checked(total + EconomyCalculator.ListedPrice(
                        BoardCatalog.GetTile(property.TileId), property.Level));
            }
            return total;
        }

        private int CountOwnedBeaches(string playerId)
        {
            int count = 0;
            foreach (PropertyState property in State.PropertiesByTileId.Values)
            {
                if (property.Type == TileType.Beach && property.OwnerId == playerId)
                    count++;
            }
            return count;
        }

        private bool OwnsAllCitiesInCountry(string playerId, string countryId)
        {
            int total = 0;
            int owned = 0;
            foreach (BoardTile tile in State.Board)
            {
                if (tile.Type == TileType.City && tile.CountryId == countryId)
                {
                    total++;
                    if (GetProperty(tile.Id).OwnerId == playerId)
                        owned++;
                }
            }
            return total > 0 && total == owned;
        }

        private void SetPendingObligation(PendingObligation obligation)
        {
            if (obligation.Amount < 0 || State.PendingObligation.HasValue)
                throw new InvalidOperationException("An obligation is already pending or invalid.");
            State.PendingObligation = obligation;
            State.TurnPhase = TurnPhase.AwaitingObligation;
        }

        private void Finish(IReadOnlyList<string> winners, WinReason reason)
        {
            State.MatchStatus = MatchStatus.Finished;
            State.TurnPhase = TurnPhase.Finished;
            State.WinReason = reason;
            State.Winners = winners;
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

        private PlayerState FindPlayer(string playerId)
        {
            foreach (PlayerState player in State.Players)
            {
                if (player.PlayerId == playerId)
                    return player;
            }
            throw new InvalidOperationException("Unknown player.");
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
            property.House3ReachedOnLandingId = null;
            property.TemporaryFestivalTurnsRemaining = 0;
            property.PowerCutTurnsRemaining = 0;
            Array.Clear(property.InvestedByTier, 0, property.InvestedByTier.Length);
        }

        private void EnsureNoObligation()
        {
            if (State.PendingObligation.HasValue)
                throw new InvalidOperationException("The pending obligation must be settled first.");
        }

        private void EnsurePhase(TurnPhase phase)
        {
            if (State.TurnPhase != phase)
                throw new InvalidOperationException("The current turn is in an invalid phase.");
        }

        private void EnsureRunning()
        {
            if (State.MatchStatus != MatchStatus.Running)
                throw new InvalidOperationException("The match is not running.");
        }
    }
}
