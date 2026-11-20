using System;
using System.Collections.Generic;

namespace BTLGame.UI
{
    /// <summary>Một bất động sản người chơi đang nắm khi kết thúc trận (chỉ để hiển thị).</summary>
    public sealed class MatchResultProperty
    {
        public int TileId { get; }
        public string Name { get; }
        public string LevelLabel { get; }
        public long Value { get; }

        public MatchResultProperty(int tileId, string name, string levelLabel, long value)
        {
            TileId = tileId;
            Name = name ?? string.Empty;
            LevelLabel = levelLabel ?? string.Empty;
            Value = value;
        }
    }

    /// <summary>Một dòng của bảng xếp hạng cuối trận.</summary>
    public sealed class MatchResultPlayer
    {
        public int Id { get; }
        public int SeatIndex { get; }
        public string Name { get; }
        public string RoleName { get; }
        public int Rank { get; }
        public long Cash { get; }
        /// <summary>Tổng giá niêm yết hiện tại của bất động sản đang sở hữu (đúng cách Core dùng khi xếp hạng hết giờ).</summary>
        public long PropertyValue { get; }
        public long TotalAssets => Cash + PropertyValue;
        public int Cities { get; }
        public int Beaches { get; }
        public IReadOnlyList<string> MonopolyCountries { get; }
        public IReadOnlyList<MatchResultProperty> Properties { get; }
        public bool IsWinner { get; }
        public bool IsBankrupt { get; }

        public MatchResultPlayer(int id, int seatIndex, string name, string roleName, int rank, long cash, long propertyValue,
            int cities, int beaches, IReadOnlyList<string> monopolyCountries, IReadOnlyList<MatchResultProperty> properties,
            bool isWinner, bool isBankrupt)
        {
            Id = id;
            SeatIndex = seatIndex;
            Name = name ?? string.Empty;
            RoleName = roleName ?? string.Empty;
            Rank = rank;
            Cash = cash;
            PropertyValue = propertyValue;
            Cities = cities;
            Beaches = beaches;
            MonopolyCountries = monopolyCountries ?? new string[0];
            Properties = properties ?? new MatchResultProperty[0];
            IsWinner = isWinner;
            IsBankrupt = isBankrupt;
        }
    }

    /// <summary>Kết quả cuối trận cho giao diện: lý do thắng, số vòng và người chơi đã xếp hạng (người thắng trước, người phá sản cuối).</summary>
    public sealed class MatchResultSummary
    {
        public string ReasonLabel { get; }
        public long Rounds { get; }
        public IReadOnlyList<MatchResultPlayer> Players { get; }

        public MatchResultSummary(string reasonLabel, long rounds, IReadOnlyList<MatchResultPlayer> players)
        {
            ReasonLabel = reasonLabel ?? string.Empty;
            Rounds = rounds;
            Players = players ?? new MatchResultPlayer[0];
        }

        public IEnumerable<MatchResultPlayer> Winners
        {
            get
            {
                foreach (var player in Players)
                {
                    if (player.IsWinner) yield return player;
                }
            }
        }
    }

    /// <summary>Giao diện có màn Chiến thắng + Bảng xếp hạng. Host dùng nếu giao diện hiện tại cài interface này.</summary>
    public interface IMatchResultUi
    {
        event Action RestartRequested;
        event Action MenuRequested;
        void ShowMatchResult(MatchResultSummary summary);
    }
}
