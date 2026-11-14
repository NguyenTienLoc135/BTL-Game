using System;
using System.Collections.Generic;
using BtlGame.Board;

namespace BtlGame.Core
{
    public sealed class GameState
    {
        private readonly Dictionary<int, PlayerState> playersById = new Dictionary<int, PlayerState>();
        private readonly Dictionary<int, PropertyState> propertiesByTileId = new Dictionary<int, PropertyState>();

        public MatchStatus Status { get; internal set; }
        public int Revision { get; internal set; }
        public int CurrentPlayerId { get; internal set; }
        public int NextLandingId { get; internal set; }
        public IList<int> FixedFestivalCityIds { get; private set; }
        public WinResult Result { get; internal set; }

        public GameState(MatchSetup setup, IList<int> fixedFestivalCityIds)
        {
            if (setup == null)
            {
                throw new ArgumentNullException("setup");
            }

            if (fixedFestivalCityIds == null || fixedFestivalCityIds.Count != 3)
            {
                throw new ArgumentException("Exactly three festival cities are required.");
            }

            Status = MatchStatus.Running;
            FixedFestivalCityIds = new List<int>(fixedFestivalCityIds);
            for (int i = 0; i < setup.Players.Count; i++)
            {
                PlayerSetup player = setup.Players[i];
                playersById.Add(i, new PlayerState(i, i, player.DisplayName, player.Controller, player.RoleId));
            }

            for (int tileId = 0; tileId < BoardCatalog.TileCount; tileId++)
            {
                if (BoardCatalog.GetTile(tileId).IsProperty)
                {
                    propertiesByTileId.Add(tileId, new PropertyState(tileId));
                }
            }

            CurrentPlayerId = 0;
        }

        public PlayerState GetPlayer(int playerId)
        {
            PlayerState player;
            if (!playersById.TryGetValue(playerId, out player))
            {
                throw new KeyNotFoundException("Unknown player id.");
            }

            return player;
        }

        public PropertyState GetProperty(int tileId)
        {
            PropertyState property;
            if (!propertiesByTileId.TryGetValue(tileId, out property))
            {
                throw new KeyNotFoundException("Tile is not a property.");
            }

            return property;
        }

        public IEnumerable<PlayerState> GetPlayers()
        {
            return playersById.Values;
        }

        public IEnumerable<PropertyState> GetProperties()
        {
            return propertiesByTileId.Values;
        }

        internal void Commit()
        {
            Revision++;
        }
    }
}
