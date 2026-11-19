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

    /// <summary>Display-only mapping. Gameplay IDs and rules remain owned by Core.</summary>
    public static class BoardVisualCatalog
    {
        private static readonly BoardTileVisualDefinition[] Tiles =
        {
            D(0, BoardTileAppearance.Start, "Xuất phát", "", "", "", "#E6B85C"),
            C(1, "Hà Nội", "Việt Nam", "TP1", "100.000", "#D95D4F"),
            C(2, "Đà Nẵng", "Việt Nam", "TP2", "120.000", "#D95D4F"),
            C(3, "TP. Hồ Chí Minh", "Việt Nam", "TP3", "140.000", "#D95D4F"),
            D(4, BoardTileAppearance.Beach, "Bãi biển", "", "", "100.000", "#57AEB7"),
            C(5, "Kyoto", "Nhật Bản", "TP4", "160.000", "#D27D91"),
            C(6, "Osaka", "Nhật Bản", "TP5", "180.000", "#D27D91"),
            C(7, "Tokyo", "Nhật Bản", "TP6", "200.000", "#D27D91"),
            D(8, BoardTileAppearance.Jail, "Nhà tù", "", "", "", "#BD7168"),
            C(9, "Incheon", "Hàn Quốc", "TP7", "230.000", "#6289C2"),
            C(10, "Busan", "Hàn Quốc", "TP8", "260.000", "#6289C2"),
            C(11, "Seoul", "Hàn Quốc", "TP9", "290.000", "#6289C2"),
            D(12, BoardTileAppearance.Event, "Thẻ sự kiện", "", "", "", "#9675B8"),
            C(13, "Lyon", "Pháp", "TP10", "330.000", "#6B9A73"),
            D(14, BoardTileAppearance.Beach, "Bãi biển", "", "", "130.000", "#57AEB7"),
            C(15, "Paris", "Pháp", "TP11", "370.000", "#6B9A73"),
            D(16, BoardTileAppearance.Festival, "Tổ chức sự kiện", "", "", "", "#DB9851"),
            C(17, "Milan", "Ý", "TP12", "410.000", "#A875B1"),
            D(18, BoardTileAppearance.Beach, "Bãi biển", "", "", "160.000", "#57AEB7"),
            C(19, "Rome", "Ý", "TP13", "450.000", "#A875B1"),
            D(20, BoardTileAppearance.Event, "Thẻ sự kiện", "", "", "", "#9675B8"),
            C(21, "Chicago", "Mỹ", "TP14", "500.000", "#4B9AA6"),
            C(22, "Los Angeles", "Mỹ", "TP15", "550.000", "#4B9AA6"),
            C(23, "New York", "Mỹ", "TP16", "600.000", "#4B9AA6"),
            D(24, BoardTileAppearance.Teleport, "Dịch chuyển", "", "", "", "#5DA8B5"),
            D(25, BoardTileAppearance.Beach, "Bãi biển", "", "", "190.000", "#57AEB7"),
            C(26, "Manchester", "Anh", "TP18", "650.000", "#DB9E4D"),
            C(27, "London", "Anh", "TP19", "700.000", "#DB9E4D"),
            D(28, BoardTileAppearance.Event, "Thẻ sự kiện", "", "", "", "#9675B8"),
            C(29, "Melbourne", "Úc", "TP20", "750.000", "#4C9D80"),
            D(30, BoardTileAppearance.Tax, "Thuế", "", "", "20%", "#87928F"),
            C(31, "Sydney", "Úc", "TP21", "800.000", "#4C9D80")
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
