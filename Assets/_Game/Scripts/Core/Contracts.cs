using System;
using System.Collections.Generic;

namespace BtlGame.Core
{
    public enum MatchStatus
    {
        Setup,
        Running,
        Finished
    }

    public enum TurnPhase
    {
        AwaitingRoll,
        Moving,
        ResolvingLanding,
        AwaitingDecision,
        AwaitingObligation,
        Finished
    }

    public enum TileType
    {
        Start,
        City,
        Beach,
        Jail,
        EventCard,
        Festival,
        Teleport,
        Tax
    }

    public enum PropertyLevel
    {
        Unowned = -1,
        Stake = 0,
        House1 = 1,
        House2 = 2,
        House3 = 3,
        Hotel = 4
    }

    public enum PlayerStatus
    {
        Active,
        Bankrupt
    }

    public enum ControllerKind
    {
        Human,
        Ai
    }

    public enum TimeLimit
    {
        Unlimited,
        Minutes30 = 30,
        Minutes45 = 45,
        Minutes60 = 60
    }

    public enum WinReason
    {
        None,
        ThreeCountries,
        FourBeaches,
        LastActivePlayer,
        TimeLimit
    }

    public readonly struct BoardTile
    {
        public BoardTile(int id, TileType type, string displayName, string countryId, string cityCode, long house3Price, long beachPrice)
        {
            Id = id;
            Type = type;
            DisplayName = displayName;
            CountryId = countryId;
            CityCode = cityCode;
            House3Price = house3Price;
            BeachPrice = beachPrice;
        }

        public int Id { get; }
        public TileType Type { get; }
        public string DisplayName { get; }
        public string CountryId { get; }
        public string CityCode { get; }
        public long House3Price { get; }
        public long BeachPrice { get; }
        public bool IsProperty => Type == TileType.City || Type == TileType.Beach;
    }

    public sealed class PlayerState
    {
        public PlayerState(string id, int seatIndex, string displayName, ControllerKind controller)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Player id is required.", nameof(id));

            PlayerId = id;
            SeatIndex = seatIndex;
            DisplayName = displayName ?? string.Empty;
            Controller = controller;
            Cash = GameConstants.StartingCash;
            PositionTileId = 0;
            Status = PlayerStatus.Active;
        }

        public string PlayerId { get; }
        public int SeatIndex { get; }
        public string DisplayName { get; }
        public ControllerKind Controller { get; }
        public long Cash { get; internal set; }
        public int PositionTileId { get; internal set; }
        public PlayerStatus Status { get; internal set; }
        public bool IsJailed { get; internal set; }
        public int JailTurnsStarted { get; internal set; }
        public int CompletedPersonalTurns { get; internal set; }
    }

    public sealed class PropertyState
    {
        public PropertyState(int tileId, TileType type)
        {
            TileId = tileId;
            Type = type;
            Level = type == TileType.City ? PropertyLevel.Unowned : PropertyLevel.Unowned;
            InvestedByTier = new long[GameConstants.InvestmentTierCount];
        }

        public int TileId { get; }
        public TileType Type { get; }
        public string OwnerId { get; internal set; }
        public PropertyLevel Level { get; internal set; }
        public long[] InvestedByTier { get; }
        public long BeachInvestment { get; internal set; }
        public string NormalUpgradeUsedOnLandingId { get; internal set; }

        public bool IsOwned => OwnerId != null;
    }

    public sealed class GameState
    {
        internal GameState(IReadOnlyList<BoardTile> board, IReadOnlyList<PlayerState> players)
        {
            Board = board;
            Players = players;
            PropertiesByTileId = new Dictionary<int, PropertyState>();
            foreach (BoardTile tile in board)
            {
                if (tile.IsProperty)
                    PropertiesByTileId.Add(tile.Id, new PropertyState(tile.Id, tile.Type));
            }

            MatchStatus = MatchStatus.Running;
            TurnPhase = TurnPhase.AwaitingRoll;
            CurrentPlayerIndex = 0;
        }

        public IReadOnlyList<BoardTile> Board { get; }
        public IReadOnlyList<PlayerState> Players { get; }
        public Dictionary<int, PropertyState> PropertiesByTileId { get; }
        public MatchStatus MatchStatus { get; internal set; }
        public TurnPhase TurnPhase { get; internal set; }
        public int CurrentPlayerIndex { get; internal set; }
        public int PersonalTurnOrdinal { get; internal set; }
        public int RollOrdinalWithinTurn { get; internal set; }
        public long RemainingSeconds { get; internal set; }
        public bool TimeExpired { get; internal set; }
        public WinReason WinReason { get; internal set; }
        public IReadOnlyList<string> Winners { get; internal set; }

        public PlayerState CurrentPlayer => Players[CurrentPlayerIndex];
    }

    public static class GameConstants
    {
        public const long StartingCash = 300000;
        public const long StartPassMoney = 100000;
        public const long JailFee = 200000;
        public const int BoardSize = 32;
        public const int InvestmentTierCount = 5;
        public const int MaximumJailTurns = 3;
    }
}
