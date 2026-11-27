using System;
using System.Collections.Generic;

namespace BTLGame.Board
{
    public readonly struct BoardPlayerVisualState
    {
        public int PlayerId { get; }
        public int SeatIndex { get; }
        public int TileId { get; }
        public bool IsVisible { get; }

        public BoardPlayerVisualState(int playerId, int seatIndex, int tileId, bool isVisible = true)
        {
            PlayerId = playerId;
            SeatIndex = seatIndex;
            TileId = tileId;
            IsVisible = isVisible;
        }
    }

    public readonly struct BoardPropertyVisualState
    {
        public int TileId { get; }
        public int? OwnerId { get; }
        public PropertyVisualLevel? Level { get; }

        public BoardPropertyVisualState(int tileId, int? ownerId, PropertyVisualLevel? level)
        {
            TileId = tileId;
            OwnerId = ownerId;
            Level = level;
        }
    }

    /// <summary>Read-only presentation payload; it is not a second source of game state.</summary>
    public sealed class BoardPresentationSnapshot
    {
        public IReadOnlyList<BoardPlayerVisualState> Players { get; }
        public IReadOnlyList<BoardPropertyVisualState> Properties { get; }

        public BoardPresentationSnapshot(IReadOnlyList<BoardPlayerVisualState> players,
            IReadOnlyList<BoardPropertyVisualState> properties)
        {
            Players = players ?? throw new ArgumentNullException(nameof(players));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }
    }

    public sealed class MovementPresentation
    {
        public int PlayerId { get; }
        public long ActionId { get; }
        public long LandingId { get; }
        public IReadOnlyList<int> Path { get; }

        /// <summary>true: bay thẳng một vòng cung tới ô cuối (dịch chuyển) thay vì nhảy từng ô.</summary>
        public bool Fly { get; }

        public MovementPresentation(int playerId, long actionId, long landingId, IReadOnlyList<int> path, bool fly = false)
        {
            PlayerId = playerId;
            ActionId = actionId;
            LandingId = landingId;
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Fly = fly;
        }
    }
}
