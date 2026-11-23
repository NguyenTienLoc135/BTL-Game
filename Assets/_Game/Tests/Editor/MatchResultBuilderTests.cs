using System.Collections.Generic;
using System.Linq;
using BtlGame.Characters;
using BtlGame.Core;
using BTLGame.Cards;
using NUnit.Framework;

namespace BtlGame.Tests
{
    /// <summary>Bảng xếp hạng cuối trận: thứ tự, đồng hạng, người phá sản xếp cuối, độc quyền và tổng tài sản.</summary>
    public sealed class MatchResultBuilderTests
    {
        private static MatchCoordinator NewMatch(int players = 3)
        {
            var roster = new List<PlayerSetup>();
            for (int i = 0; i < players; i++)
                roster.Add(new PlayerSetup(i, "P" + i, (RoleId)(i + 1), ControllerKind.Human));
            return MatchFactory.Create(roster, MatchDuration.Unlimited, random: new System.Random(5),
                dice: new BtlGame.Board.RandomDiceSource(new System.Random(6)), cards: new RandomEventCardProvider(7));
        }

        [Test]
        public void NoWinnerYet_RanksByCashThenSeat_AllPlayersListed()
        {
            var summary = MatchResultBuilder.Build(NewMatch(3).QuerySnapshot());
            Assert.AreEqual(3, summary.Players.Count);
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2 }, summary.Players.Select(p => p.Id).ToArray());
            Assert.That(summary.Players.All(p => p.Cash == 300000 && p.TotalAssets == 300000 && !p.IsBankrupt), Is.True);
            Assert.AreEqual(new[] { 1, 2, 3 }, summary.Players.Select(p => p.Rank).ToArray(), "Không có người thắng: hạng tuần tự.");
        }

        [Test]
        public void ReasonLabels_CoverEveryWinReason()
        {
            foreach (WinReason reason in System.Enum.GetValues(typeof(WinReason)))
                Assert.That(string.IsNullOrEmpty(MatchResultBuilder.ReasonLabel(reason)), Is.False, reason.ToString());
        }
    }
}
