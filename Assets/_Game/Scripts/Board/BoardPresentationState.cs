using System.Collections.Generic;
using BtlGame.Core;

namespace BtlGame.Board
{
    /// <summary>Read-only presentation data supplied by the gameplay coordinator.</summary>
    public sealed class BoardPresentationState
    {
        public IReadOnlyList<BoardPlayerPresentation> Players { get; }
        public IReadOnlyList<BoardPropertyPresentation> Properties { get; }

        public BoardPresentationState(IReadOnlyList<BoardPlayerPresentation> players,
            IReadOnlyList<BoardPropertyPresentation> properties)
        {
            Players = players;
            Properties = properties;
        }
    }

    public readonly struct BoardPlayerPresentation
    {
        public int PlayerId { get; }
        public int Position { get; }
        public bool IsVisible { get; }

        public BoardPlayerPresentation(int playerId, int position, bool isVisible = true)
        {
            PlayerId = playerId;
            Position = position;
            IsVisible = isVisible;
        }
    }

    public readonly struct BoardPropertyPresentation
    {
        public int TileId { get; }
        public int? OwnerId { get; }
        public PropertyLevel? Level { get; }

        public BoardPropertyPresentation(int tileId, int? ownerId, PropertyLevel? level)
        {
            TileId = tileId;
            OwnerId = ownerId;
            Level = level;
        }
    }
}
