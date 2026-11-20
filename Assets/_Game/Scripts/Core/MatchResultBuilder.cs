using System.Collections.Generic;
using System.Linq;
using BtlGame.Properties;
using BTLGame.Board;
using BTLGame.UI;

namespace BtlGame.Core
{
    /// <summary>
    /// Dựng MatchResultSummary từ GameSnapshot (chỉ đọc). Thứ tự xếp hạng bám cách Core chọn người thắng hết giờ:
    /// người thắng trước; rồi người còn chơi theo tiền mặt giảm dần, hòa thì theo tổng giá niêm yết bất động sản;
    /// người đã phá sản xếp cuối. Không tính luật mới, không ghi trạng thái.
    /// </summary>
    public static class MatchResultBuilder
    {
        private static readonly string[] LevelNames = { "Cọc", "Nhà 1", "Nhà 2", "Nhà 3", "Khách sạn" };

        public static MatchResultSummary Build(GameSnapshot snapshot)
        {
            var winners = new HashSet<int>(snapshot.WinnerPlayerIds);
            var infos = snapshot.Players.Select(p => Collect(snapshot, p)).ToList();

            var ordered = infos
                .OrderByDescending(i => winners.Contains(i.Player.Id))
                .ThenBy(i => i.Player.Status == PlayerStatus.Bankrupt ? 1 : 0)
                .ThenByDescending(i => i.Player.Cash)
                .ThenByDescending(i => i.Value)
                .ThenBy(i => i.Player.SeatIndex)
                .ToList();

            var players = new List<MatchResultPlayer>();
            for (int index = 0; index < ordered.Count; index++)
            {
                var info = ordered[index];
                bool isWinner = winners.Contains(info.Player.Id);
                // Xếp hạng kiểu "đồng hạng 1": tất cả người thắng cùng hạng 1, người kế tiếp hạng (số người thắng + 1).
                int rank = isWinner ? 1 : index + 1;
                players.Add(new MatchResultPlayer(info.Player.Id, info.Player.SeatIndex, info.Player.DisplayName,
                    BtlGame.Characters.RoleCatalog.Get(info.Player.Role).DisplayName, rank, info.Player.Cash, info.Value,
                    info.Cities, info.Beaches, info.Monopolies, info.Properties, isWinner, info.Player.Status == PlayerStatus.Bankrupt));
            }

            return new MatchResultSummary(ReasonLabel(snapshot.Result), snapshot.CompletedRounds, players);
        }


        private sealed class Info
        {
            public PlayerSnapshot Player;
            public long Value;
            public int Cities;
            public int Beaches;
            public List<string> Monopolies = new List<string>();
            public List<MatchResultProperty> Properties = new List<MatchResultProperty>();
        }

        private static Info Collect(GameSnapshot snapshot, PlayerSnapshot player)
        {
            var info = new Info { Player = player };
            var owned = new HashSet<int>();
            foreach (PropertySnapshot property in snapshot.Properties)
            {
                if (property.OwnerId != player.Id) continue;
                TileDefinition tile = BoardCatalog.Get(property.TileId);
                long value;
                string level;
                if (tile.Kind == TileKind.Beach)
                {
                    value = tile.BeachPrice;
                    level = "Bãi biển";
                    info.Beaches++;
                }
                else
                {
                    value = property.Level.HasValue ? EconomyRules.ListedPrice(property.TileId, property.Level.Value) : 0;
                    level = property.Level.HasValue ? LevelNames[(int)property.Level.Value - 1] : "—";
                    info.Cities++;
                }

                owned.Add(property.TileId);
                info.Value += value;
                info.Properties.Add(new MatchResultProperty(property.TileId, BoardVisualCatalog.Get(property.TileId).DisplayName, level, value));
            }

            // Độc quyền: sở hữu trọn mọi thành phố của một quốc gia.
            foreach (var group in BoardCatalog.Tiles.Where(t => t.Kind == TileKind.City && t.Country.HasValue).GroupBy(t => t.Country.Value))
            {
                if (group.All(t => owned.Contains(t.Id)))
                    info.Monopolies.Add(BoardVisualCatalog.Get(group.First().Id).CountryName);
            }

            return info;
        }

        public static string ReasonLabel(WinReason? reason)
        {
            switch (reason)
            {
                case WinReason.ThreeCountries: return "Sở hữu trọn 3 quốc gia";
                case WinReason.FourBeaches: return "Sở hữu cả 4 bãi biển";
                case WinReason.LastSolventPlayer: return "Các đối thủ đã phá sản";
                case WinReason.TimeCash: return "Hết giờ — nhiều tiền mặt nhất";
                case WinReason.TimePropertyValue: return "Hết giờ — bằng tiền mặt, hơn về giá trị bất động sản";
                case WinReason.TimeShared: return "Hết giờ — đồng hạng";
                default: return "Kết thúc trận";
            }
        }
    }
}
