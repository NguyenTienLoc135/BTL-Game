using BtlGame.Core;
using BTLGame.Presentation;
using BTLGame.UI;
using NUnit.Framework;

namespace BtlGame.Tests
{
    /// <summary>Phát hiện độc quyền một quốc gia (màn chúc mừng MONOPOLY): sở hữu trọn mọi thành phố của nước.</summary>
    public sealed class MonopolyDetectorTests
    {
        private static HudProperty[] Own(int owner, params int[] tiles)
        {
            var list = new HudProperty[tiles.Length];
            for (int i = 0; i < tiles.Length; i++) list[i] = new HudProperty(tiles[i], owner);
            return list;
        }

        [Test]
        public void ThreeCityCountry_OwnedByOne_IsMonopoly()
        {
            var now = MonopolyDetector.Current(Own(1, 1, 2, 3));
            Assert.AreEqual(1, now[CountryId.Vietnam]);
            Assert.AreEqual(3, MonopolyDetector.CitiesOf(CountryId.Vietnam).Count);
        }

        [Test]
        public void PartialOrSplitOwnership_IsNotMonopoly()
        {
            Assert.IsFalse(MonopolyDetector.Current(Own(1, 1, 2)).ContainsKey(CountryId.Vietnam));
            var split = new[] { new HudProperty(1, 0), new HudProperty(2, 1), new HudProperty(3, 0) };
            Assert.IsFalse(MonopolyDetector.Current(split).ContainsKey(CountryId.Vietnam));
        }

        [Test]
        public void TwoCityCountry_FranceNeedsBothCities()
        {
            Assert.IsFalse(MonopolyDetector.Current(Own(2, 13)).ContainsKey(CountryId.France));
            Assert.AreEqual(2, MonopolyDetector.Current(Own(2, 13, 15))[CountryId.France]);
        }

        [Test]
        public void NewlyCompleted_FiresOnceAndAgainOnOwnerChange()
        {
            var empty = MonopolyDetector.Current(Own(1, 1, 2));
            var done = MonopolyDetector.Current(Own(1, 1, 2, 3));
            Assert.AreEqual(1, MonopolyDetector.NewlyCompleted(empty, done).Count);
            Assert.AreEqual(0, MonopolyDetector.NewlyCompleted(done, done).Count);
            var taken = MonopolyDetector.Current(Own(3, 1, 2, 3));
            var events = MonopolyDetector.NewlyCompleted(done, taken);
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(3, events[0].OwnerId);
        }
    }
}
