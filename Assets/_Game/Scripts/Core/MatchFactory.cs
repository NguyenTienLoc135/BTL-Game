using System;
using System.Collections.Generic;
using BtlGame.Board;

namespace BtlGame.Core
{
    public sealed class PlayerSetup
    {
        public int Id { get; }
        public string Name { get; }
        public RoleId Role { get; }
        public ControllerKind Controller { get; }
        public PlayerSetup(int id, string name, RoleId role, ControllerKind controller = ControllerKind.Human)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Cần tên người chơi.", nameof(name));
            if (role == RoleId.None || !Enum.IsDefined(typeof(RoleId), role)) throw new ArgumentOutOfRangeException(nameof(role));
            if (!Enum.IsDefined(typeof(ControllerKind), controller)) throw new ArgumentOutOfRangeException(nameof(controller));
            Id = id; Name = name; Role = role; Controller = controller;
        }
    }

    /// <summary>Chính sách Setup phải được người dùng duyệt; không ngầm chọn số người/trùng nghề/người đầu.</summary>
    public sealed class SetupDecisions
    {
        public static SetupDecisions Approved => new SetupDecisions(2, true, true);
        public int MinimumPlayers { get; }
        public bool AllowDuplicateRoles { get; }
        public bool RandomFirstPlayer { get; }
        public SetupDecisions(int minimumPlayers, bool allowDuplicateRoles, bool randomFirstPlayer)
        {
            if (minimumPlayers < 1 || minimumPlayers > 4) throw new ArgumentOutOfRangeException(nameof(minimumPlayers));
            MinimumPlayers = minimumPlayers; AllowDuplicateRoles = allowDuplicateRoles; RandomFirstPlayer = randomFirstPlayer;
        }
    }

    public static class MatchFactory
    {
        public static MatchCoordinator Create(IReadOnlyList<PlayerSetup> roster, MatchDuration duration,
            SetupDecisions setupDecisions = null, GameplayDecisions gameplayDecisions = null,
            int? firstPlayerId = null, Random random = null, IDiceSource dice = null, ICardActionProvider cards = null)
        {
            setupDecisions = setupDecisions ?? SetupDecisions.Approved;
            if (roster == null) throw new ArgumentNullException(nameof(roster));
            if (roster.Count < setupDecisions.MinimumPlayers || roster.Count > 4)
                throw new ArgumentException("Số người không đúng chính sách Setup đã chọn.", nameof(roster));
            if (!Enum.IsDefined(typeof(MatchDuration), duration)) throw new ArgumentOutOfRangeException(nameof(duration));
            var ids = new HashSet<int>();
            var roles = new HashSet<RoleId>();
            var players = new List<PlayerState>();
            for (int i = 0; i < roster.Count; i++)
            {
                PlayerSetup entry = roster[i];
                if (entry == null || !ids.Add(entry.Id)) throw new ArgumentException("PlayerId phải duy nhất.", nameof(roster));
                if (!roles.Add(entry.Role) && !setupDecisions.AllowDuplicateRoles)
                    throw new ArgumentException("Không được chọn trùng nghề theo chính sách Setup này.", nameof(roster));
                players.Add(new PlayerState(entry.Id, i, entry.Name, entry.Role, entry.Controller));
            }
            if (!setupDecisions.RandomFirstPlayer && (!firstPlayerId.HasValue || !ids.Contains(firstPlayerId.Value)))
                throw new ArgumentException("Cần chỉ rõ người đầu tiên trong danh sách.", nameof(firstPlayerId));
            if (setupDecisions.RandomFirstPlayer && firstPlayerId.HasValue)
                throw new ArgumentException("Không chỉ định người đầu khi chính sách đang chọn ngẫu nhiên.", nameof(firstPlayerId));
            random = random ?? new Random();
            int first = setupDecisions.RandomFirstPlayer ? roster[random.Next(roster.Count)].Id : firstPlayerId.Value;
            var cityIds = new List<int>();
            foreach (TileDefinition tile in BoardCatalog.Tiles) if (tile.Kind == TileKind.City) cityIds.Add(tile.Id);
            // Lấy mẫu không hoàn lại; không phụ thuộc layout Transform hoặc data của view.
            var festivals = new List<int>();
            for (int i = 0; i < 3; i++)
            {
                int selected = random.Next(cityIds.Count);
                festivals.Add(cityIds[selected]);
                cityIds.RemoveAt(selected);
            }
            return new MatchCoordinator(new GameState(players, first, festivals, duration,
                gameplayDecisions ?? GameplayDecisions.Approved), dice, cards);
        }
    }
}
