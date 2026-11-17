using System;
using System.Collections.Generic;
using BtlGame.Core;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class CardIntegrationTests
    {
        [Test]
        public void CardProviderCanPlanFromTheSameReadOnlySnapshotWithoutDrawingAgain()
        {
            var state = new GameState(new[] { new PlayerState(1, 0, "A"), new PlayerState(2, 1, "B") },
                1, new[] { 5, 6, 7 });
            GameSnapshot snapshot = state.CreateSnapshot();
            ICardActionProvider provider = new FixedGiftProvider();

            // Fixture chỉ cấp một thẻ xác định, không tuyên bố thuật toán deck sản phẩm.
            int? preview = provider.PeekNextCard(snapshot);
            IReadOnlyList<CardActionOption> options = provider.QueryOptions(snapshot, 1);
            CardEffectPlan plan = provider.Plan(snapshot, 1, options[0].ChoiceId);

            Assert.That(provider.PeekNextCard(snapshot), Is.EqualTo(preview));
            Assert.That(plan.SourceCardId, Is.EqualTo(preview));
            Assert.That(plan.TargetPlayerId, Is.EqualTo(options[0].TargetPlayerId));
            Assert.That(state.Revision, Is.EqualTo(snapshot.Revision));
            Assert.That(state.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(state.GetPlayer(2).Cash, Is.EqualTo(300000));
        }

        [TestCase(-1)]
        [TestCase(32)]
        public void OutOfBoardTargetCannotEnterEitherCardContract(int tileId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardActionOption(1, "Mục tiêu", tileId));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardEffectPlan(2, CardEffectKind.Earthquake, tileId));
        }

        [TestCase(0)]
        [TestCase(15)]
        public void UnknownSourceCardIsRejectedBeforeCoreExecutesAnIntent(int sourceCardId)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardEffectPlan(sourceCardId, CardEffectKind.None));
        }

        [Test]
        public void MalformedAmountsAndTargetsCannotBePresentedAsPaymentIntents()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CardEffectPlan(14, CardEffectKind.Gift, targetPlayerId: 2, amount: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CardEffectPlan(14, CardEffectKind.Gift, targetPlayerId: -1, amount: 200000));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardEffectPlan(14, (CardEffectKind)99));
        }

        private sealed class FixedGiftProvider : ICardActionProvider
        {
            public int? PeekNextCard(GameSnapshot snapshot) => 14;

            public IReadOnlyList<CardActionOption> QueryOptions(GameSnapshot snapshot, int actorId) =>
                Array.AsReadOnly(new[] { new CardActionOption(7, "Tặng quà cho B", targetPlayerId: 2) });

            public CardEffectPlan Plan(GameSnapshot snapshot, int actorId, int choiceId)
            {
                if (choiceId != 7) throw new ArgumentOutOfRangeException(nameof(choiceId));
                return new CardEffectPlan(14, CardEffectKind.Gift, targetPlayerId: 2, amount: 200000);
            }
        }
    }
}
