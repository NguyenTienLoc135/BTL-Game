using BtlGame.Core;
using BtlGame.Properties;
using NUnit.Framework;

namespace BtlGame.Tests
{
    /// <summary>Bảng giá niêm yết 20 thành phố × 5 cấp và 4 bãi biển phải đúng bảng trong tài liệu (đơn vị 1.000).</summary>
    public sealed class PriceTableTests
    {
        // ô, Cọc, Nhà 1, Nhà 2, Nhà 3, Khách sạn
        private static readonly int[][] Cities =
        {
            new[] { 1, 40, 60, 80, 100, 120 }, new[] { 2, 48, 72, 96, 120, 144 }, new[] { 3, 56, 84, 112, 140, 168 },
            new[] { 5, 64, 96, 128, 160, 192 }, new[] { 6, 72, 108, 144, 180, 216 }, new[] { 7, 80, 120, 160, 200, 240 },
            new[] { 9, 92, 138, 184, 230, 276 }, new[] { 10, 104, 156, 208, 260, 312 }, new[] { 11, 116, 174, 232, 290, 348 },
            new[] { 13, 132, 198, 264, 330, 396 }, new[] { 15, 148, 222, 296, 370, 444 }, new[] { 17, 164, 246, 328, 410, 492 },
            new[] { 19, 180, 270, 360, 450, 540 }, new[] { 21, 200, 300, 400, 500, 600 }, new[] { 22, 220, 330, 440, 550, 660 },
            new[] { 23, 240, 360, 480, 600, 720 }, new[] { 26, 260, 390, 520, 650, 780 }, new[] { 27, 280, 420, 560, 700, 840 },
            new[] { 29, 300, 450, 600, 750, 900 }, new[] { 31, 320, 480, 640, 800, 960 }
        };

        [Test]
        public void ListedCityPrices_MatchDocumentTable()
        {
            foreach (var row in Cities)
                for (int level = 1; level <= 5; level++)
                    Assert.AreEqual(row[level] * 1000L, EconomyRules.ListedPrice(row[0], (PropertyLevel)level),
                        "Ô " + row[0] + " cấp " + (PropertyLevel)level);
        }

        [Test]
        public void BeachPrices_MatchDocument()
        {
            Assert.AreEqual(100_000, BoardCatalog.Get(4).BeachPrice);
            Assert.AreEqual(130_000, BoardCatalog.Get(14).BeachPrice);
            Assert.AreEqual(160_000, BoardCatalog.Get(18).BeachPrice);
            Assert.AreEqual(190_000, BoardCatalog.Get(25).BeachPrice);
        }
    }
}
