using BTLGame.Presentation;
using NUnit.Framework;

namespace BtlGame.Tests
{
    /// <summary>Luân phiên nhạc nền: hai bản phải thay phiên, không bao giờ lặp một bản liên tiếp.</summary>
    public sealed class MusicPlayerTests
    {
        [Test]
        public void TwoTracks_AlternateAndNeverRepeatBackToBack()
        {
            int last = -1;
            var seen = new bool[2];
            for (int bag = 0; bag < 200; bag++)
            {
                foreach (int index in MusicPlayer.BuildBag(2, last))
                {
                    Assert.AreNotEqual(last, index, "Hai bản liên tiếp phải khác nhau.");
                    seen[index] = true;
                    last = index;
                }
            }

            Assert.That(seen[0] && seen[1], Is.True, "Cả hai bản đều phải được phát.");
        }

        [Test]
        public void ManyTracks_BagContainsEachOnceAndDoesNotStartWithLast()
        {
            for (int trial = 0; trial < 100; trial++)
            {
                var bag = MusicPlayer.BuildBag(4, 2);
                Assert.AreEqual(4, bag.Length);
                Assert.AreNotEqual(2, bag[0]);
                CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, bag);
            }
        }

        [Test]
        public void SingleTrack_BagIsJustThatTrack()
        {
            CollectionAssert.AreEqual(new[] { 0 }, MusicPlayer.BuildBag(1, 0));
        }
    }
}
