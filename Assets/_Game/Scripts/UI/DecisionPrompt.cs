using System;
using System.Collections.Generic;

namespace BTLGame.UI
{
    public enum DecisionPromptKind
    {
        Property,
        Rent,
        Tax,
        Liquidation,
        Jail,
        EventCard,
        EventCardTargetSelection,
        TargetSelection,
        Result,
        General
    }

    public readonly struct DecisionOption
    {
        public string Id { get; }
        public string Label { get; }
        public bool IsLegal { get; }
        public string DisabledReason { get; }

        public DecisionOption(string id, string label, bool isLegal = true, string disabledReason = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Label = label ?? string.Empty;
            IsLegal = isLegal;
            DisabledReason = disabledReason ?? string.Empty;
        }
    }

    public sealed class DecisionPrompt
    {
        public long DecisionId { get; }
        public int ActorId { get; }
        public long ExpectedRevision { get; }
        public long ActionId { get; }
        public DecisionPromptKind Kind { get; }
        public string ContextId { get; }
        public string Title { get; }
        public string Message { get; }
        public IReadOnlyList<DecisionOption> Options { get; }

        public DecisionPrompt(long decisionId, int actorId, long expectedRevision,
            DecisionPromptKind kind, string title, string message, IReadOnlyList<DecisionOption> options,
            long actionId = 0, string contextId = null)
        {
            if (decisionId <= 0) throw new ArgumentOutOfRangeException(nameof(decisionId));
            if (actorId < 0) throw new ArgumentOutOfRangeException(nameof(actorId));
            if (expectedRevision < 0) throw new ArgumentOutOfRangeException(nameof(expectedRevision));
            DecisionId = decisionId;
            ActorId = actorId;
            ExpectedRevision = expectedRevision;
            ActionId = actionId;
            Kind = kind;
            ContextId = contextId ?? string.Empty;
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }
    }

    public readonly struct DecisionSubmission
    {
        public long DecisionId { get; }
        public int ActorId { get; }
        public long ExpectedRevision { get; }
        public long ActionId { get; }
        public DecisionPromptKind Kind { get; }
        public string ContextId { get; }
        public string OptionId { get; }

        public DecisionSubmission(long decisionId, int actorId, long expectedRevision,
            long actionId, DecisionPromptKind kind, string contextId, string optionId)
        {
            DecisionId = decisionId;
            ActorId = actorId;
            ExpectedRevision = expectedRevision;
            ActionId = actionId;
            Kind = kind;
            ContextId = contextId ?? string.Empty;
            OptionId = optionId ?? string.Empty;
        }
    }
}
