using System.Collections.Generic;

namespace BTLGame.UI
{
    /// <summary>Immutable values for presentation; this type never owns gameplay state.</summary>
    public sealed class GameHUDSnapshot
    {
        public int ActorId { get; }
        public long Revision { get; }
        public string PlayerName { get; }
        public long Cash { get; }
        public string TurnLabel { get; }
        public string DiceLabel { get; }
        public string ClockLabel { get; }
        public IReadOnlyList<string> PropertyLines { get; }
        public IReadOnlyList<string> StatusLines { get; }
        public bool CanRoll { get; }
        public bool CanUseActiveSkill { get; }
        public bool CanSell { get; }

        // Dữ liệu có cấu trúc cho HUD mới (danh sách người chơi, đồng hồ, vòng, xúc xắc). Host điền qua WithDetails;
        // HUD cũ (TMP) bỏ qua. Tất cả chỉ là bản chụp để hiển thị, không phải nguồn trạng thái.
        public IReadOnlyList<HudPlayerInfo> Players { get; private set; } = new HudPlayerInfo[0];
        public int CurrentPlayerId { get; private set; } = -1;
        public long Round { get; private set; } = 1;

        /// <summary>Số giây còn lại; null = vô hạn.</summary>
        public long? ClockSeconds { get; private set; }
        public int? Die1 { get; private set; }
        public int? Die2 { get; private set; }
        public bool MatchFinished { get; private set; }

        public GameHUDSnapshot WithDetails(IReadOnlyList<HudPlayerInfo> players, int currentPlayerId, long round,
            long? clockSeconds, int? die1, int? die2, bool matchFinished)
        {
            Players = players ?? new HudPlayerInfo[0];
            CurrentPlayerId = currentPlayerId;
            Round = round;
            ClockSeconds = clockSeconds;
            Die1 = die1;
            Die2 = die2;
            MatchFinished = matchFinished;
            return this;
        }

        public GameHUDSnapshot(int actorId, long revision, string playerName, long cash,
            string turnLabel, string diceLabel, string clockLabel,
            IReadOnlyList<string> propertyLines, IReadOnlyList<string> statusLines,
            bool canRoll, bool canUseActiveSkill, bool canSell)
        {
            ActorId = actorId;
            Revision = revision;
            PlayerName = playerName ?? string.Empty;
            Cash = cash;
            TurnLabel = turnLabel ?? string.Empty;
            DiceLabel = diceLabel ?? string.Empty;
            ClockLabel = clockLabel ?? string.Empty;
            PropertyLines = propertyLines ?? new string[0];
            StatusLines = statusLines ?? new string[0];
            CanRoll = canRoll;
            CanUseActiveSkill = canUseActiveSkill;
            CanSell = canSell;
        }
    }

    /// <summary>Một dòng người chơi trên HUD: ghế, tên, tiền mặt, nghề, phá sản hay chưa.</summary>
    public readonly struct HudPlayerInfo
    {
        public int Id { get; }
        public int Seat { get; }
        public string Name { get; }
        public long Cash { get; }
        public string RoleName { get; }
        public bool Bankrupt { get; }

        public HudPlayerInfo(int id, int seat, string name, long cash, string roleName, bool bankrupt)
        {
            Id = id;
            Seat = seat;
            Name = name ?? string.Empty;
            Cash = cash;
            RoleName = roleName ?? string.Empty;
            Bankrupt = bankrupt;
        }
    }

    public enum GameplayUiActionKind { Roll, UseActiveSkill, OpenSellSelection }

    public readonly struct GameplayUiAction
    {
        public GameplayUiActionKind Kind { get; }
        public int ActorId { get; }
        public long ExpectedRevision { get; }
        public string TargetId { get; }

        public GameplayUiAction(GameplayUiActionKind kind, int actorId, long expectedRevision, string targetId = null)
        {
            Kind = kind;
            ActorId = actorId;
            ExpectedRevision = expectedRevision;
            TargetId = targetId ?? string.Empty;
        }
    }
}
