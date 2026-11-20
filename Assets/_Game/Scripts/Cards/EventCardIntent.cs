using System;

namespace BTLGame.Cards
{
    /// <summary>Request payload only. Core validates and commits every gameplay effect.</summary>
    public readonly struct EventCardIntent
    {
        public EventCardId CardId { get; }
        public int ActorId { get; }
        public long ActionId { get; }
        public long ExpectedRevision { get; }
        public string SelectionId { get; }

        public EventCardIntent(EventCardId cardId, int actorId, long actionId,
            long expectedRevision, string selectionId)
        {
            CardId = cardId;
            ActorId = actorId;
            ActionId = actionId;
            ExpectedRevision = expectedRevision;
            SelectionId = selectionId ?? string.Empty;
        }
    }
}
