using System;
using System.Collections.Generic;

namespace BtlGame.Core
{
    public enum TileKind { Start, City, Beach, Jail, EventCard, Festival, Teleport, Tax }
    public enum CountryId { Vietnam, Japan, Korea, France, Italy, Usa, UnitedKingdom, Australia }
    public enum PropertyLevel { Stake = 1, House1, House2, House3, Hotel }
    public enum PlayerStatus { Active, Bankrupt }
    public enum MatchStatus { Running, Finished }
    public enum TurnPhase { AwaitingRoll, ResolvingLanding, EndTurn, AwaitingJailChoice, AwaitingObligation, AwaitingRuleDecision, Finished }
    public enum MatchDuration { Minutes30, Minutes45, Minutes60, Unlimited }
    public enum ObligationKind { Rent, TaxTile, TaxCard, Gift, BankerDebt, ForcedJailReleaseFee, OtherMandatory }
    public enum ObligationStatus { AwaitingPayment, Settled, Defaulted, Cancelled }
    public enum EffectKind { RentDouble, RentBonus, TemporaryFestival, PowerOutage, PurchaseDiscount, PaymentImmunity, CancelRent }
    public enum WinReason { ThreeCountries, FourBeaches, LastSolventPlayer, TimeCash, TimePropertyValue, TimeShared }

    public sealed class TileDefinition
    {
        public int Id { get; }
        public TileKind Kind { get; }
        public string Name { get; }
        public CountryId? Country { get; }
        public long ReferencePrice { get; }
        public long BeachPrice { get; }

        public TileDefinition(int id, TileKind kind, string name, CountryId? country = null,
            long referencePrice = 0, long beachPrice = 0)
        {
            Id = id;
            Kind = kind;
            Name = name;
            Country = country;
            ReferencePrice = referencePrice;
            BeachPrice = beachPrice;
        }
    }

    public static class BoardCatalog
    {
        private static readonly TileDefinition[] Definitions =
        {
            new TileDefinition(0, TileKind.Start, "Xuất phát"),
            new TileDefinition(1, TileKind.City, "Hà Nội", CountryId.Vietnam, 100_000),
            new TileDefinition(2, TileKind.City, "Đà Nẵng", CountryId.Vietnam, 120_000),
            new TileDefinition(3, TileKind.City, "TP. Hồ Chí Minh", CountryId.Vietnam, 140_000),
            new TileDefinition(4, TileKind.Beach, "Bãi biển", beachPrice: 100_000),
            new TileDefinition(5, TileKind.City, "Kyoto", CountryId.Japan, 160_000),
            new TileDefinition(6, TileKind.City, "Osaka", CountryId.Japan, 180_000),
            new TileDefinition(7, TileKind.City, "Tokyo", CountryId.Japan, 200_000),
            new TileDefinition(8, TileKind.Jail, "Nhà tù"),
            new TileDefinition(9, TileKind.City, "Incheon", CountryId.Korea, 230_000),
            new TileDefinition(10, TileKind.City, "Busan", CountryId.Korea, 260_000),
            new TileDefinition(11, TileKind.City, "Seoul", CountryId.Korea, 290_000),
            new TileDefinition(12, TileKind.EventCard, "Thẻ sự kiện"),
            new TileDefinition(13, TileKind.City, "Lyon", CountryId.France, 330_000),
            new TileDefinition(14, TileKind.Beach, "Bãi biển", beachPrice: 130_000),
            new TileDefinition(15, TileKind.City, "Paris", CountryId.France, 370_000),
            new TileDefinition(16, TileKind.Festival, "Tổ chức sự kiện"),
            new TileDefinition(17, TileKind.City, "Milan", CountryId.Italy, 410_000),
            new TileDefinition(18, TileKind.Beach, "Bãi biển", beachPrice: 160_000),
            new TileDefinition(19, TileKind.City, "Rome", CountryId.Italy, 450_000),
            new TileDefinition(20, TileKind.EventCard, "Thẻ sự kiện"),
            new TileDefinition(21, TileKind.City, "Chicago", CountryId.Usa, 500_000),
            new TileDefinition(22, TileKind.City, "Los Angeles", CountryId.Usa, 550_000),
            new TileDefinition(23, TileKind.City, "New York", CountryId.Usa, 600_000),
            new TileDefinition(24, TileKind.Teleport, "Dịch chuyển"),
            new TileDefinition(25, TileKind.Beach, "Bãi biển", beachPrice: 190_000),
            new TileDefinition(26, TileKind.City, "Manchester", CountryId.UnitedKingdom, 650_000),
            new TileDefinition(27, TileKind.City, "London", CountryId.UnitedKingdom, 700_000),
            new TileDefinition(28, TileKind.EventCard, "Thẻ sự kiện"),
            new TileDefinition(29, TileKind.City, "Melbourne", CountryId.Australia, 750_000),
            new TileDefinition(30, TileKind.Tax, "Thuế"),
            new TileDefinition(31, TileKind.City, "Sydney", CountryId.Australia, 800_000)
        };

        private static readonly IReadOnlyList<TileDefinition> ReadOnlyDefinitions = Array.AsReadOnly(Definitions);
        public static IReadOnlyList<TileDefinition> Tiles => ReadOnlyDefinitions;
        public static TileDefinition Get(int tileId)
        {
            if (tileId < 0 || tileId >= Definitions.Length)
                throw new ArgumentOutOfRangeException(nameof(tileId));
            return Definitions[tileId];
        }
    }

    public sealed class PlayerState
    {
        private readonly HashSet<int> _heldCards = new HashSet<int>();
        public int Id { get; }
        public int SeatIndex { get; }
        public string DisplayName { get; }
        public long Cash { get; internal set; } = 300_000;
        public int Position { get; internal set; }
        public PlayerStatus Status { get; internal set; } = PlayerStatus.Active;
        public bool IsJailed { get; internal set; }
        public int JailTurnsStarted { get; internal set; }
        public long CompletedPersonalTurns { get; internal set; }
        public long ActiveReadyAfterCompletedTurns { get; internal set; }
        public bool UltimateUsed { get; internal set; }
        public long JailFeeDue { get; internal set; }
        public IReadOnlyCollection<int> HeldCards => new List<int>(_heldCards).AsReadOnly();
        public bool HasBankerPassive { get; }
        public long? BankerDebtDueAtTurnOrdinal { get; internal set; }
        public long BankerDebtAmount { get; internal set; }

        public PlayerState(int id, int seatIndex, string displayName, bool hasBankerPassive = false)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (seatIndex < 0 || seatIndex > 3) throw new ArgumentOutOfRangeException(nameof(seatIndex));
            Id = id;
            SeatIndex = seatIndex;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            HasBankerPassive = hasBankerPassive;
        }

        internal bool HasHeldCard(int cardId) => _heldCards.Contains(cardId);
        internal bool AddHeldCard(int cardId) => _heldCards.Add(cardId);
        internal bool ConsumeHeldCard(int cardId) => _heldCards.Remove(cardId);
        public bool IsActiveReady => CompletedPersonalTurns >= ActiveReadyAfterCompletedTurns;
    }

    public sealed class PropertyState
    {
        private readonly long[] _investedByLevel = new long[5];
        private readonly IReadOnlyList<long> _readOnlyInvestment;
        public int TileId { get; }
        public int? OwnerId { get; internal set; }
        public PropertyLevel? Level { get; internal set; }
        public IReadOnlyList<long> InvestedByLevel => _readOnlyInvestment;
        internal long[] MutableInvestedByLevel => _investedByLevel;
        public long BeachInvestment { get; internal set; }
        public long? House3ReachedLandingId { get; internal set; }
        public long? NormalUpgradeUsedOnLandingId { get; internal set; }

        public PropertyState(int tileId)
        {
            TileId = tileId;
            _readOnlyInvestment = Array.AsReadOnly(_investedByLevel);
        }
        public long TotalInvestment
        {
            get
            {
                long total = BeachInvestment;
                foreach (long amount in _investedByLevel) total = checked(total + amount);
                return total;
            }
        }
    }

    public sealed class MandatoryObligation
    {
        public long Id { get; }
        public ObligationKind Kind { get; }
        public int PayerId { get; }
        public int? RecipientId { get; }
        public long AssessedAmount { get; }
        public long PayableAmount { get; internal set; }
        public bool ActiveMitigationUsed { get; internal set; }
        public ObligationStatus Status { get; internal set; }
        public int? PropertyTileId { get; }

        internal MandatoryObligation(long id, ObligationKind kind, int payerId, int? recipientId,
            long assessedAmount, int? propertyTileId)
        {
            Id = id;
            Kind = kind;
            PayerId = payerId;
            RecipientId = recipientId;
            AssessedAmount = assessedAmount;
            PayableAmount = assessedAmount;
            PropertyTileId = propertyTileId;
            Status = ObligationStatus.AwaitingPayment;
        }
    }

    public sealed class PropertyAttackResult
    {
        public bool ProtectionConsumed { get; }
        public long InvestmentRemovedOrRefunded { get; }
        internal PropertyAttackResult(bool protectionConsumed, long amount)
        {
            ProtectionConsumed = protectionConsumed;
            InvestmentRemovedOrRefunded = amount;
        }
    }

    public sealed class TurnEffectState
    {
        public long Id { get; }
        public EffectKind Kind { get; }
        public int SourcePlayerId { get; }
        public int? TargetPlayerId { get; }
        public int? TargetTileId { get; }
        public int ModifierBasisPoints { get; }
        public long? ExpiresAfterSourceTurnOrdinal { get; }
        public long? ExpiresBeforeSourceTurnOrdinal { get; }

        internal TurnEffectState(long id, EffectKind kind, int sourcePlayerId, int? targetPlayerId,
            int? targetTileId, int modifierBasisPoints, long? expiresAfterSourceTurnOrdinal,
            long? expiresBeforeSourceTurnOrdinal)
        {
            Id = id;
            Kind = kind;
            SourcePlayerId = sourcePlayerId;
            TargetPlayerId = targetPlayerId;
            TargetTileId = targetTileId;
            ModifierBasisPoints = modifierBasisPoints;
            ExpiresAfterSourceTurnOrdinal = expiresAfterSourceTurnOrdinal;
            ExpiresBeforeSourceTurnOrdinal = expiresBeforeSourceTurnOrdinal;
        }
    }

    public sealed class GameState
    {
        private readonly List<PlayerState> _players;
        private readonly PropertyState[] _properties = new PropertyState[32];
        private readonly List<MandatoryObligation> _obligations = new List<MandatoryObligation>();
        private readonly List<TurnEffectState> _effects = new List<TurnEffectState>();
        private readonly HashSet<int> _completedTurnsThisRound = new HashSet<int>();
        public IReadOnlyList<PlayerState> Players => _players.AsReadOnly();
        public int InitialPlayerCount => _players.Count;
        public IReadOnlyList<PropertyState> Properties
        {
            get
            {
                var result = new List<PropertyState>();
                foreach (PropertyState property in _properties)
                    if (property != null) result.Add(property);
                return result.AsReadOnly();
            }
        }
        public MatchStatus Status { get; internal set; } = MatchStatus.Running;
        public int CurrentPlayerId { get; internal set; }
        public long Revision { get; internal set; }
        public WinReason? Result { get; internal set; }
        public TurnPhase Phase { get; internal set; } = TurnPhase.AwaitingRoll;
        public long CurrentLandingId { get; internal set; }
        public long CurrentActionId { get; internal set; }
        public bool PendingDoubleRoll { get; internal set; }
        public bool TimeExpired { get; internal set; }
        public bool ClockPaused { get; internal set; }
        public long ClockRemainingTicks { get; internal set; }
        public MatchDuration Duration { get; }
        public IReadOnlyList<int> FixedFestivalCityIds { get; }
        public IReadOnlyList<MandatoryObligation> Obligations => _obligations.AsReadOnly();
        public IReadOnlyList<TurnEffectState> ActiveEffects => _effects.AsReadOnly();
        public MandatoryObligation PendingObligation { get; internal set; }
        public TurnPhase PhaseBeforeObligation { get; internal set; }
        public bool ClockPausedBeforeObligation { get; internal set; }
        public bool CurrentLandingDecisionUsed { get; internal set; }
        public int? RentSettledAtCurrentLandingTileId { get; internal set; }
        public bool RoundCompletedAtLastTurnBoundary { get; internal set; }
        public string PendingRuleDecisionId { get; internal set; }

        public GameState(IReadOnlyList<PlayerState> players, int firstPlayerId, IReadOnlyList<int> festivalCityIds,
            MatchDuration duration = MatchDuration.Unlimited)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));
            if (festivalCityIds == null) throw new ArgumentNullException(nameof(festivalCityIds));
            if (players.Count < 1 || players.Count > 4) throw new ArgumentException("Trận hỗ trợ tối đa 4 người chơi.", nameof(players));
            if (festivalCityIds.Count != 3) throw new ArgumentException("Cần đúng 3 thành phố lễ hội.", nameof(festivalCityIds));
            var ids = new HashSet<int>();
            var seats = new HashSet<int>();
            foreach (PlayerState player in players)
                if (player == null || !ids.Add(player.Id) || !seats.Add(player.SeatIndex))
                    throw new ArgumentException("PlayerId và seatIndex phải duy nhất.", nameof(players));
            if (!ids.Contains(firstPlayerId)) throw new ArgumentException("Người bắt đầu không có trong trận.", nameof(firstPlayerId));
            var festivals = new HashSet<int>();
            foreach (int tileId in festivalCityIds)
                if (BoardCatalog.Get(tileId).Kind != TileKind.City || !festivals.Add(tileId))
                    throw new ArgumentException("Ba lễ hội phải là thành phố khác nhau.", nameof(festivalCityIds));

            _players = new List<PlayerState>(players);
            for (int id = 0; id < _properties.Length; id++)
                if (BoardCatalog.Get(id).Kind == TileKind.City || BoardCatalog.Get(id).Kind == TileKind.Beach)
                    _properties[id] = new PropertyState(id);
            CurrentPlayerId = firstPlayerId;
            Duration = duration;
            if (duration == MatchDuration.Unlimited) ClockRemainingTicks = long.MaxValue;
            else if (duration == MatchDuration.Minutes30) ClockRemainingTicks = TimeSpan.FromMinutes(30).Ticks;
            else if (duration == MatchDuration.Minutes45) ClockRemainingTicks = TimeSpan.FromMinutes(45).Ticks;
            else if (duration == MatchDuration.Minutes60) ClockRemainingTicks = TimeSpan.FromMinutes(60).Ticks;
            else throw new ArgumentOutOfRangeException(nameof(duration));
            FixedFestivalCityIds = new List<int>(festivalCityIds).AsReadOnly();
        }

        public PlayerState GetPlayer(int playerId) => _players.Find(p => p.Id == playerId)
            ?? throw new ArgumentException("Không tìm thấy người chơi.", nameof(playerId));
        public PropertyState GetProperty(int tileId)
        {
            BoardCatalog.Get(tileId);
            return _properties[tileId] ?? throw new ArgumentException("Ô không phải bất động sản.", nameof(tileId));
        }

        public GameSnapshot CreateSnapshot() => new GameSnapshot(this);

        internal void AddObligation(MandatoryObligation obligation) => _obligations.Add(obligation);
        internal void AddEffect(TurnEffectState effect) => _effects.Add(effect);
        internal void RemoveEffect(TurnEffectState effect) => _effects.Remove(effect);
        internal bool MarkTurnCompletedThisRound(int playerId) => _completedTurnsThisRound.Add(playerId);
        internal void RemoveFromRound(int playerId) => _completedTurnsThisRound.Remove(playerId);
        internal bool IsRoundComplete()
        {
            foreach (PlayerState player in _players)
                if (player.Status == PlayerStatus.Active && !_completedTurnsThisRound.Contains(player.Id)) return false;
            return true;
        }
        internal void ResetRound() => _completedTurnsThisRound.Clear();
    }
}
