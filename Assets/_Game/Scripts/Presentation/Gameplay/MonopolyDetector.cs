using System.Collections.Generic;
using BtlGame.Core;
using BTLGame.UI;

namespace BTLGame.Presentation
{
    /// <summary>Một lần "độc quyền một quốc gia": ai sở hữu trọn bộ thành phố của nước nào (2 hoặc 3 thành phố).</summary>
    public readonly struct MonopolyEvent
    {
        public int OwnerId { get; }
        public CountryId Country { get; }
        public IReadOnlyList<int> TileIds { get; }

        public MonopolyEvent(int ownerId, CountryId country, IReadOnlyList<int> tileIds)
        {
            OwnerId = ownerId;
            Country = country;
            TileIds = tileIds;
        }
    }

    /// <summary>
    /// Phát hiện (chỉ để HIỂN THỊ hiệu ứng) khi một người vừa sở hữu trọn các thành phố của một quốc gia, theo đúng định nghĩa ở
    /// GAME_RULES §3/§4.2: sở hữu TẤT CẢ thành phố của nước đó (2 hoặc 3). Nhóm quốc gia lấy từ BoardCatalog của Core; tiền thuê +20%
    /// do Core tính, lớp này không tính luật. Plain C#, không phụ thuộc Unity nên kiểm thử được.
    /// </summary>
    public static class MonopolyDetector
    {
        private static readonly Dictionary<CountryId, List<int>> CitiesByCountry = BuildGroups();

        private static Dictionary<CountryId, List<int>> BuildGroups()
        {
            var groups = new Dictionary<CountryId, List<int>>();
            for (int id = 0; id < 32; id++)
            {
                var tile = BoardCatalog.Get(id);
                if (tile.Kind != TileKind.City || !tile.Country.HasValue)
                {
                    continue;
                }

                if (!groups.TryGetValue(tile.Country.Value, out var list))
                {
                    list = new List<int>();
                    groups.Add(tile.Country.Value, list);
                }

                list.Add(id);
            }

            return groups;
        }

        public static IReadOnlyList<int> CitiesOf(CountryId country) => CitiesByCountry[country];

        /// <summary>Quốc gia nào đang bị MỘT người sở hữu trọn bộ → id người đó.</summary>
        public static Dictionary<CountryId, int> Current(IReadOnlyList<HudProperty> properties)
        {
            var owner = new Dictionary<int, int>();
            if (properties != null)
            {
                foreach (var p in properties) owner[p.TileId] = p.OwnerId;
            }

            var result = new Dictionary<CountryId, int>();
            foreach (var pair in CitiesByCountry)
            {
                int? first = null;
                bool all = true;
                foreach (int tile in pair.Value)
                {
                    if (!owner.TryGetValue(tile, out int o) || (first.HasValue && first.Value != o))
                    {
                        all = false;
                        break;
                    }

                    first = o;
                }

                if (all && first.HasValue)
                {
                    result[pair.Key] = first.Value;
                }
            }

            return result;
        }

        /// <summary>Những độc quyền MỚI xuất hiện giữa hai lần chụp (người nắm giữ đổi cũng tính là mới).</summary>
        public static List<MonopolyEvent> NewlyCompleted(IReadOnlyDictionary<CountryId, int> before, IReadOnlyDictionary<CountryId, int> after)
        {
            var events = new List<MonopolyEvent>();
            foreach (var pair in after)
            {
                if (before != null && before.TryGetValue(pair.Key, out int previousOwner) && previousOwner == pair.Value)
                {
                    continue;
                }

                events.Add(new MonopolyEvent(pair.Value, pair.Key, CitiesByCountry[pair.Key]));
            }

            return events;
        }
    }
}
