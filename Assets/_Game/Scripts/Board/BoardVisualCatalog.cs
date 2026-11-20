using System.Collections.Generic;
using UnityEngine;

namespace BTLGame.Board
{
    public enum BoardTileAppearance { Start, City, Beach, Jail, Event, Festival, Teleport, Tax }
    public enum PropertyVisualLevel { Stake = 1, House1, House2, House3, Hotel }

    public sealed class BoardTileVisualDefinition
    {
        public int TileId { get; }
        public BoardTileAppearance Appearance { get; }
        public string DisplayName { get; }
        public string CountryName { get; }
        public string DisplayCode { get; }
        public string PriceLabel { get; }
        public Color Color { get; }

        public BoardTileVisualDefinition(int tileId, BoardTileAppearance appearance, string displayName,
            string countryName, string displayCode, string priceLabel, Color color)
        {
            TileId = tileId;
            Appearance = appearance;
            DisplayName = displayName;
            CountryName = countryName;
            DisplayCode = displayCode;
            PriceLabel = priceLabel;
            Color = color;
        }
    }

    /// <summary>Display-only mapping. Gameplay IDs and rules remain owned by Core. Màu = bảng pastel dùng chung cho bàn cờ (mỗi quốc gia một màu, ô chức năng dịu).</summary>
    public static class BoardVisualCatalog
    {
        private static readonly BoardTileVisualDefinition[] Tiles =
        {
            D(0, BoardTileAppearance.Start, "Xuất phát", "", "", "", "#99D6A3"),
            C(1, "Hà Nội", "Việt Nam", "TP1", "100.000", "#94D6B8"),
            C(2, "Đà Nẵng", "Việt Nam", "TP2", "120.000", "#94D6B8"),
            C(3, "TP. Hồ Chí Minh", "Việt Nam", "TP3", "140.000", "#94D6B8"),
            D(4, BoardTileAppearance.Beach, "Bãi biển", "", "", "100.000", "#6BBDEB"),
            C(5, "Kyoto", "Nhật Bản", "TP4", "160.000", "#F0C2D4"),
            C(6, "Osaka", "Nhật Bản", "TP5", "180.000", "#F0C2D4"),
            C(7, "Tokyo", "Nhật Bản", "TP6", "200.000", "#F0C2D4"),
            D(8, BoardTileAppearance.Jail, "Nhà tù", "", "", "", "#B3BACC"),
            C(9, "Incheon", "Hàn Quốc", "TP7", "230.000", "#9ECCF2"),
            C(10, "Busan", "Hàn Quốc", "TP8", "260.000", "#9ECCF2"),
            C(11, "Seoul", "Hàn Quốc", "TP9", "290.000", "#9ECCF2"),
            D(12, BoardTileAppearance.Event, "Thẻ sự kiện", "", "", "", "#EDE6FA"),
            C(13, "Lyon", "Pháp", "TP10", "330.000", "#B3B8F2"),
            D(14, BoardTileAppearance.Beach, "Bãi biển", "", "", "130.000", "#6BBDEB"),
            C(15, "Paris", "Pháp", "TP11", "370.000", "#B3B8F2"),
            D(16, BoardTileAppearance.Festival, "Tổ chức sự kiện", "", "", "", "#F2D99E"),
            C(17, "Milan", "Ý", "TP12", "410.000", "#C4DE99"),
            D(18, BoardTileAppearance.Beach, "Bãi biển", "", "", "160.000", "#6BBDEB"),
            C(19, "Rome", "Ý", "TP13", "450.000", "#C4DE99"),
            D(20, BoardTileAppearance.Event, "Thẻ sự kiện", "", "", "", "#EDE6FA"),
            C(21, "Chicago", "Mỹ", "TP14", "500.000", "#F7D1AD"),
            C(22, "Los Angeles", "Mỹ", "TP15", "550.000", "#F7D1AD"),
            C(23, "New York", "Mỹ", "TP16", "600.000", "#F7D1AD"),
            D(24, BoardTileAppearance.Teleport, "Dịch chuyển", "", "", "", "#B3B8F0"),
            D(25, BoardTileAppearance.Beach, "Bãi biển", "", "", "190.000", "#6BBDEB"),
            C(26, "Manchester", "Anh", "TP18", "650.000", "#85CCD1"),
            C(27, "London", "Anh", "TP19", "700.000", "#85CCD1"),
            D(28, BoardTileAppearance.Event, "Thẻ sự kiện", "", "", "", "#EDE6FA"),
            C(29, "Melbourne", "Úc", "TP20", "750.000", "#D6BDE6"),
            D(30, BoardTileAppearance.Tax, "Thuế", "", "", "20%", "#F7E3D6"),
            C(31, "Sydney", "Úc", "TP21", "800.000", "#D6BDE6")
        };

        private static readonly IReadOnlyList<BoardTileVisualDefinition> ReadOnlyTiles = System.Array.AsReadOnly(Tiles);
        public static IReadOnlyList<BoardTileVisualDefinition> All => ReadOnlyTiles;

        public static BoardTileVisualDefinition Get(int tileId)
        {
            if (tileId < 0 || tileId >= Tiles.Length) throw new System.ArgumentOutOfRangeException(nameof(tileId));
            return Tiles[tileId];
        }

        private static BoardTileVisualDefinition C(int id, string name, string country, string code, string price, string color)
            => D(id, BoardTileAppearance.City, name, country, code, price, color);

        private static BoardTileVisualDefinition D(int id, BoardTileAppearance appearance, string name,
            string country, string code, string price, string color)
        {
            ColorUtility.TryParseHtmlString(color, out Color parsed);
            return new BoardTileVisualDefinition(id, appearance, name, country, code, price, parsed);
        }
    }
}
