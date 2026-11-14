using System;
using System.Collections.Generic;
using BtlGame.Core;

namespace BtlGame.Board
{
    public readonly struct MovementResult
    {
        public MovementResult(int startTileId, int destinationTileId, int steps, int startPassCount, bool landedOnJail)
        {
            StartTileId = startTileId;
            DestinationTileId = destinationTileId;
            Steps = steps;
            StartPassCount = startPassCount;
            LandedOnJail = landedOnJail;
        }

        public int StartTileId { get; }
        public int DestinationTileId { get; }
        public int Steps { get; }
        public int StartPassCount { get; }
        public bool LandedOnJail { get; }
    }

    public static class BoardCatalog
    {
        private static readonly BoardTile[] Tiles =
        {
            new BoardTile(0, TileType.Start, "Xuất phát", null, null, 0, 0),
            new BoardTile(1, TileType.City, "Hà Nội", "Vietnam", "TP1", 100000, 0),
            new BoardTile(2, TileType.City, "Đà Nẵng", "Vietnam", "TP2", 120000, 0),
            new BoardTile(3, TileType.City, "TP. Hồ Chí Minh", "Vietnam", "TP3", 140000, 0),
            new BoardTile(4, TileType.Beach, "Bãi biển", null, null, 0, 100000),
            new BoardTile(5, TileType.City, "Kyoto", "Japan", "TP4", 160000, 0),
            new BoardTile(6, TileType.City, "Osaka", "Japan", "TP5", 180000, 0),
            new BoardTile(7, TileType.City, "Tokyo", "Japan", "TP6", 200000, 0),
            new BoardTile(8, TileType.Jail, "Nhà tù", null, null, 0, 0),
            new BoardTile(9, TileType.City, "Incheon", "Korea", "TP7", 230000, 0),
            new BoardTile(10, TileType.City, "Busan", "Korea", "TP8", 260000, 0),
            new BoardTile(11, TileType.City, "Seoul", "Korea", "TP9", 290000, 0),
            new BoardTile(12, TileType.EventCard, "Thẻ sự kiện", null, null, 0, 0),
            new BoardTile(13, TileType.City, "Lyon", "France", "TP10", 330000, 0),
            new BoardTile(14, TileType.Beach, "Bãi biển", null, null, 0, 130000),
            new BoardTile(15, TileType.City, "Paris", "France", "TP11", 370000, 0),
            new BoardTile(16, TileType.Festival, "Tổ chức sự kiện", null, null, 0, 0),
            new BoardTile(17, TileType.City, "Milan", "Italy", "TP12", 410000, 0),
            new BoardTile(18, TileType.Beach, "Bãi biển", null, null, 0, 160000),
            new BoardTile(19, TileType.City, "Rome", "Italy", "TP13", 450000, 0),
            new BoardTile(20, TileType.EventCard, "Thẻ sự kiện", null, null, 0, 0),
            new BoardTile(21, TileType.City, "Chicago", "USA", "TP14", 500000, 0),
            new BoardTile(22, TileType.City, "Los Angeles", "USA", "TP15", 550000, 0),
            new BoardTile(23, TileType.City, "New York", "USA", "TP16", 600000, 0),
            new BoardTile(24, TileType.Teleport, "Dịch chuyển", null, null, 0, 0),
            new BoardTile(25, TileType.Beach, "Bãi biển", null, null, 0, 190000),
            new BoardTile(26, TileType.City, "Manchester", "UK", "TP18", 650000, 0),
            new BoardTile(27, TileType.City, "London", "UK", "TP19", 700000, 0),
            new BoardTile(28, TileType.EventCard, "Thẻ sự kiện", null, null, 0, 0),
            new BoardTile(29, TileType.City, "Melbourne", "Australia", "TP20", 750000, 0),
            new BoardTile(30, TileType.Tax, "Thuế", null, null, 0, 0),
            new BoardTile(31, TileType.City, "Sydney", "Australia", "TP21", 800000, 0)
        };

        public static IReadOnlyList<BoardTile> AllTiles => Tiles;

        public static BoardTile GetTile(int tileId)
        {
            if (tileId < 0 || tileId >= Tiles.Length)
                throw new ArgumentOutOfRangeException(nameof(tileId));
            return Tiles[tileId];
        }

        public static MovementResult Move(int startTileId, int steps)
        {
            if (startTileId < 0 || startTileId >= GameConstants.BoardSize)
                throw new ArgumentOutOfRangeException(nameof(startTileId));
            if (steps < 0)
                throw new ArgumentOutOfRangeException(nameof(steps));

            int destination = (startTileId + steps) % GameConstants.BoardSize;
            int passCount = (startTileId + steps) / GameConstants.BoardSize;
            return new MovementResult(startTileId, destination, steps, passCount, destination == 8);
        }
    }
}
