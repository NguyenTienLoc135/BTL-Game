using BTLGame.Presentation;
using NUnit.Framework;

namespace BtlGame.Tests
{
    /// <summary>Nhận biết lần tung xúc xắc mới (màn KẾT QUẢ TUNG XÚC XẮC): xúc xắc cũ giữ nguyên qua lượt không được chiếu lại.</summary>
    public sealed class DiceRollTrackerTests
    {
        [Test]
        public void FirstObservation_IsBaseline_NotShown()
        {
            var tracker = new DiceRollTracker();
            Assert.IsFalse(tracker.Observe(3, 4, 1), "Xúc xắc có sẵn lúc bắt đầu chỉ là mốc.");
        }

        [Test]
        public void NewRoll_IsShownOnce_AndStaleDiceAcrossTurnsAreIgnored()
        {
            var tracker = new DiceRollTracker();
            Assert.IsFalse(tracker.Observe(null, null, null));
            Assert.IsTrue(tracker.Observe(3, 4, 0), "Lần tung đầu tiên phải chiếu.");
            Assert.IsFalse(tracker.Observe(3, 4, 0), "Cập nhật lại cùng kết quả không chiếu lại.");
            Assert.IsFalse(tracker.Observe(3, 4, 0), "Sang lượt người khác nhưng xúc xắc cũ còn đó (cùng người tung) không chiếu.");
            Assert.IsTrue(tracker.Observe(6, 2, 1), "Người kế tiếp tung xúc xắc mới.");
        }

        [Test]
        public void SameValuesByDifferentPlayers_AreDistinctRolls()
        {
            var tracker = new DiceRollTracker();
            tracker.Observe(null, null, null);
            Assert.IsTrue(tracker.Observe(5, 5, 0));
            Assert.IsTrue(tracker.Observe(5, 5, 1));
        }
    }
}
