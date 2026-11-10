using System;
using System.Collections.Generic;

namespace BtlGame.Core
{
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

    public enum CityLevel
    {
        Stake,
        House1,
        House2,
        House3,
        Hotel
    }

    public enum PlayerStatus
    {
        Active,
        Bankrupt
    }

    public enum MatchStatus
    {
        Setup,
        Running,
        Finished
    }

    public enum WinReason
    {
        None,
        ThreeCountries,
        FourBeaches,
        LastActivePlayer,
        TimeLimit
    }

    public enum ControllerKind
    {
        Human,
        Ai
    }

    public enum TimeLimit
    {
        Unlimited,
        ThirtyMinutes,
        FortyFiveMinutes,
        SixtyMinutes
    }

    public sealed class BoardTile
    {
        public int TileId { get; private set; }
        public TileType Type { get; private set; }
        public string DisplayName { get; private set; }
        public string CountryId { get; private set; }
        public string CityDisplayCode { get; private set; }
        public long ReferenceHouse3Price { get; private set; }
        public long BeachPurchasePrice { get; private set; }

        public bool IsProperty
        {
            get { return Type == TileType.City || Type == TileType.Beach; }
        }

        public BoardTile(
            int tileId,
            TileType type,
            string displayName,
            string countryId = null,
            string cityDisplayCode = null,
            long referenceHouse3Price = 0,
            long beachPurchasePrice = 0)
        {
            TileId = tileId;
            Type = type;
            DisplayName = displayName;
            CountryId = countryId;
            CityDisplayCode = cityDisplayCode;
            ReferenceHouse3Price = referenceHouse3Price;
            BeachPurchasePrice = beachPurchasePrice;
        }
    }

    public sealed class PropertyState
    {
        public int TileId { get; private set; }
        public int? OwnerId { get; internal set; }
        public CityLevel? CityLevel { get; internal set; }
        public long[] InvestedByTier { get; internal set; }
        public long BeachActualInvestment { get; internal set; }
        public int? AcquiredOnLandingId { get; internal set; }
        public int? House3ReachedOnLandingId { get; internal set; }
        public int? NormalUpgradeUsedOnLandingId { get; internal set; }

        public PropertyState(int tileId)
        {
            TileId = tileId;
            InvestedByTier = new long[5];
        }

        public long TotalInvestment
        {
            get
            {
                long total = BeachActualInvestment;
                for (int i = 0; i < InvestedByTier.Length; i++)
                {
                    total += InvestedByTier[i];
                }

                return total;
            }
        }

        internal void Reset()
        {
            OwnerId = null;
            CityLevel = null;
            BeachActualInvestment = 0;
            for (int i = 0; i < InvestedByTier.Length; i++)
            {
                InvestedByTier[i] = 0;
            }

            AcquiredOnLandingId = null;
            House3ReachedOnLandingId = null;
            NormalUpgradeUsedOnLandingId = null;
        }
    }

    public sealed class PlayerState
    {
        public int PlayerId { get; private set; }
        public int SeatIndex { get; private set; }
        public string DisplayName { get; private set; }
        public ControllerKind Controller { get; private set; }
        public string RoleId { get; private set; }
        public long Cash { get; internal set; }
        public int PositionTileId { get; internal set; }
        public PlayerStatus Status { get; internal set; }
        public bool IsJailed { get; internal set; }
        public int JailTurnsStarted { get; internal set; }
        public int CompletedPersonalTurnCount { get; internal set; }

        public PlayerState(int playerId, int seatIndex, string displayName, ControllerKind controller, string roleId)
        {
            PlayerId = playerId;
            SeatIndex = seatIndex;
            DisplayName = displayName;
            Controller = controller;
            RoleId = roleId;
            Cash = 300000;
            PositionTileId = 0;
            Status = PlayerStatus.Active;
        }
    }

    public sealed class MatchSetup
    {
        public IList<PlayerSetup> Players { get; private set; }
        public TimeLimit TimeLimit { get; private set; }

        public MatchSetup(IList<PlayerSetup> players, TimeLimit timeLimit)
        {
            if (players == null || players.Count < 2 || players.Count > 4)
            {
                throw new ArgumentException("A match requires 2 to 4 players.");
            }

            Players = players;
            TimeLimit = timeLimit;
        }
    }

    public sealed class PlayerSetup
    {
        public string DisplayName { get; private set; }
        public ControllerKind Controller { get; private set; }
        public string RoleId { get; private set; }

        public PlayerSetup(string displayName, ControllerKind controller, string roleId)
        {
            DisplayName = displayName;
            Controller = controller;
            RoleId = roleId;
        }
    }

    public struct DiceResult
    {
        public int First { get; private set; }
        public int Second { get; private set; }
        public int Total { get { return First + Second; } }
        public bool IsDouble { get { return First == Second; } }

        public DiceResult(int first, int second)
        {
            if (first < 1 || first > 6 || second < 1 || second > 6)
            {
                throw new ArgumentOutOfRangeException("Dice faces must be between 1 and 6.");
            }

            First = first;
            Second = second;
        }
    }

    public sealed class MovementResult
    {
        public int StartTileId { get; private set; }
        public int DestinationTileId { get; private set; }
        public int PassedStartCount { get; private set; }
        public DiceResult Dice { get; private set; }

        public MovementResult(int startTileId, int destinationTileId, int passedStartCount, DiceResult dice)
        {
            StartTileId = startTileId;
            DestinationTileId = destinationTileId;
            PassedStartCount = passedStartCount;
            Dice = dice;
        }
    }

    public sealed class RentQuote
    {
        public long BaseRent { get; private set; }
        public int ModifierPercent { get; private set; }
        public bool Doubled { get; private set; }
        public long FinalRent { get; private set; }

        public RentQuote(long baseRent, int modifierPercent, bool doubled, long finalRent)
        {
            BaseRent = baseRent;
            ModifierPercent = modifierPercent;
            Doubled = doubled;
            FinalRent = finalRent;
        }
    }

    public sealed class WinResult
    {
        public bool HasWinner { get; private set; }
        public IList<int> WinnerIds { get; private set; }
        public WinReason Reason { get; private set; }

        public WinResult(bool hasWinner, IList<int> winnerIds, WinReason reason)
        {
            HasWinner = hasWinner;
            WinnerIds = winnerIds;
            Reason = reason;
        }
    }
}
