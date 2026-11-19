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
