using System;
using System.Collections.Generic;
using BtlGame.Core;

namespace BtlGame.Board
{
    public static class BoardCatalog
    {
        private static readonly BoardTile[] Tiles =
        {
            new BoardTile(0, TileType.Start, "Xuất phát"),
            new BoardTile(1, TileType.City, "Hà Nội", "Vietnam", "TP1", 100000),
            new BoardTile(2, TileType.City, "Đà Nẵng", "Vietnam", "TP2", 120000),
            new BoardTile(3, TileType.City, "TP. Hồ Chí Minh", "Vietnam", "TP3", 140000),
            new BoardTile(4, TileType.Beach, "Bãi biển", null, null, 0, 100000),
            new BoardTile(5, TileType.City, "Kyoto", "Japan", "TP4", 160000),
            new BoardTile(6, TileType.City, "Osaka", "Japan", "TP5", 180000),
            new BoardTile(7, TileType.City, "Tokyo", "Japan", "TP6", 200000),
            new BoardTile(8, TileType.Jail, "Nhà tù"),
            new BoardTile(9, TileType.City, "Incheon", "Korea", "TP7", 230000),
            new BoardTile(10, TileType.City, "Busan", "Korea", "TP8", 260000),
            new BoardTile(11, TileType.City, "Seoul", "Korea", "TP9", 290000),
            new BoardTile(12, TileType.EventCard, "Thẻ sự kiện"),
            new BoardTile(13, TileType.City, "Lyon", "France", "TP10", 330000),
            new BoardTile(14, TileType.Beach, "Bãi biển", null, null, 0, 130000),
            new BoardTile(15, TileType.City, "Paris", "France", "TP11", 370000),
            new BoardTile(16, TileType.Festival, "Tổ chức sự kiện"),
            new BoardTile(17, TileType.City, "Milan", "Italy", "TP12", 410000),
            new BoardTile(18, TileType.Beach, "Bãi biển", null, null, 0, 160000),
            new BoardTile(19, TileType.City, "Rome", "Italy", "TP13", 450000),
            new BoardTile(20, TileType.EventCard, "Thẻ sự kiện"),
            new BoardTile(21, TileType.City, "Chicago", "USA", "TP14", 500000),
            new BoardTile(22, TileType.City, "Los Angeles", "USA", "TP15", 550000),
            new BoardTile(23, TileType.City, "New York", "USA", "TP16", 600000),
            new BoardTile(24, TileType.Teleport, "Dịch chuyển"),
            new BoardTile(25, TileType.Beach, "Bãi biển", null, null, 0, 190000),
            new BoardTile(26, TileType.City, "Manchester", "UK", "TP18", 650000),
            new BoardTile(27, TileType.City, "London", "UK", "TP19", 700000),
            new BoardTile(28, TileType.EventCard, "Thẻ sự kiện"),
            new BoardTile(29, TileType.City, "Melbourne", "Australia", "TP20", 750000),
            new BoardTile(30, TileType.Tax, "Thuế"),
            new BoardTile(31, TileType.City, "Sydney", "Australia", "TP21", 800000)
        };

        public static int TileCount { get { return Tiles.Length; } }

        public static BoardTile GetTile(int tileId)
        {
            if (tileId < 0 || tileId >= Tiles.Length)
            {
                throw new ArgumentOutOfRangeException("tileId");
            }

            return Tiles[tileId];
        }

        public static IEnumerable<BoardTile> GetTiles()
        {
            return Tiles;
        }

        public static int CountOwnedCitiesInCountry(IList<PropertyState> properties, string countryId, IList<BoardTile> catalog)
        {
            int count = 0;
            for (int i = 0; i < properties.Count; i++)
            {
                PropertyState property = properties[i];
                BoardTile tile = catalog[property.TileId];
                if (tile.Type == TileType.City && tile.CountryId == countryId && property.OwnerId.HasValue)
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountCitiesInCountry(string countryId)
        {
            int count = 0;
            for (int i = 0; i < Tiles.Length; i++)
            {
                if (Tiles[i].Type == TileType.City && Tiles[i].CountryId == countryId)
                {
                    count++;
                }
            }

            return count;
        }
    }

    public static class BoardRules
    {
        public static MovementResult CalculateMovement(int startTileId, DiceResult dice)
        {
            if (startTileId < 0 || startTileId >= BoardCatalog.TileCount)
            {
                throw new ArgumentOutOfRangeException("startTileId");
            }

            int rawDestination = startTileId + dice.Total;
            int destination = rawDestination % BoardCatalog.TileCount;
            int passedStart = rawDestination / BoardCatalog.TileCount;
            return new MovementResult(startTileId, destination, passedStart, dice);
        }
    }
}
