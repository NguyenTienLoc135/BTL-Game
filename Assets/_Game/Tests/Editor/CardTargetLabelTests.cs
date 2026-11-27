using System.Linq;
using BtlGame.Characters;
using BtlGame.Core;
using BTLGame.Cards;
using NUnit.Framework;

namespace BtlGame.Tests
{
    /// <summary>Thẻ chọn mục tiêu: nhãn nút phải ghi rõ hành động, thành phố và chủ sở hữu.</summary>
    public sealed class CardTargetLabelTests
    {
        [Test]
        public void DestroyCardOption_NamesActionCityOwnerAndLevel()
        {
            var roster = new[]
            {
                new PlayerSetup(0, "An", (RoleId)1, ControllerKind.Human),
                new PlayerSetup(1, "Bình", (RoleId)2, ControllerKind.Human)
            };
            var provider = new RandomEventCardProvider(5);
            var core = MatchFactory.Create(roster, MatchDuration.Unlimited, random: new System.Random(3),
                dice: new BtlGame.Board.RandomDiceSource(new System.Random(4)), cards: provider);
            int actor = core.State.CurrentPlayerId;
            var target = core.State.GetProperty(9);
            target.OwnerId = 1 - actor;
            target.Level = PropertyLevel.House2;
            core.State.PendingCardId = 9;

            var option = provider.QueryOptions(core.QuerySnapshot(), actor).Single();

            string ownerName = core.QuerySnapshot().Players.Single(p => p.Id == 1 - actor).DisplayName;
            Assert.AreEqual("Phá tài sản ở Incheon (của " + ownerName + ", Nhà 2)", option.Label);
        }
    }
}
