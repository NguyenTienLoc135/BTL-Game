using System;
using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Characters;
using BtlGame.Core;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class CardGatewayTests
    {
        [Test]
        public void BankRewardIsGrantedOnceAndAReplayedChoiceCannotGrantMore()
        {
            var core = CreateCore(new FakeProvider(new[] { 1 }, (s, c) =>
                new CardEffectPlan(1, CardEffectKind.BankCredit, amount: 300000)));
            ReachEventTile(core);
            GameCommand command = Find(core, 1, GameActionKind.CardChoice).ToCommand();
            Assert.That(core.SubmitDecision(command).Status, Is.EqualTo(ActionStatus.Accepted));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(600000));
            Assert.That(core.State.CardDrawOrdinal, Is.EqualTo(1));
            Assert.That(core.SubmitDecision(command).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(600000));
            Assert.That(core.State.CardDrawOrdinal, Is.EqualTo(1));
        }

        [Test]
        public void TaxCardUsesLawyerPassiveThroughTheSameMandatoryPaymentGateway()
        {
            var core = CreateCore(new FakeProvider(new[] { 6 }, (s, c) =>
                new CardEffectPlan(6, CardEffectKind.Tax)), RoleId.Lawyer);
            ReachEventTile(core);
            Submit(core, 1, GameActionKind.CardChoice);
            Assert.That(core.State.PendingObligation.Kind, Is.EqualTo(ObligationKind.TaxCard));
            Assert.That(core.State.PendingObligation.PayableAmount, Is.EqualTo(45000));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Submit(core, 1, GameActionKind.SettleObligation);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(255000));
            Assert.That(core.State.PendingObligation, Is.Null);
        }

        [Test]
        public void ValidPowerOutageConsumesProtectionWithoutCreatingTheEffect()
        {
            var core = CreateCore(new FakeProvider(new[] { 13 }, (s, c) =>
                new CardEffectPlan(13, CardEffectKind.PowerOutage, targetTileId: 1),
                s => new[] { new CardActionOption(7, "Cúp điện TP1", targetTileId: 1) }));
            OwnCity(core, 2, 1);
            core.State.GetPlayer(2).AddHeldCard(5);
            ReachEventTile(core);
            Submit(core, 1, GameActionKind.CardChoice);
            Assert.That(core.State.GetPlayer(2).HasHeldCard(5), Is.False);
            Assert.That(core.State.ActiveEffects.Count, Is.Zero);
            Assert.That(core.State.GetProperty(1).OwnerId, Is.EqualTo(2));
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(30000));
        }

        [Test]
        public void GiftCardCreatesAnObligationBeforeAnyTransfer()
        {
            var core = CreateCore(new FakeProvider(new[] { 14 }, (s, c) =>
                new CardEffectPlan(14, CardEffectKind.Gift, targetPlayerId: 2, amount: 200000),
                s => new[] { new CardActionOption(7, "Tặng quà cho B", targetPlayerId: 2) }));
            ReachEventTile(core);
            Submit(core, 1, GameActionKind.CardChoice);
            Assert.That(core.State.PendingObligation.PayerId, Is.EqualTo(1));
            Assert.That(core.State.PendingObligation.RecipientId, Is.EqualTo(2));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(300000));
            Submit(core, 1, GameActionKind.SettleObligation);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(100000));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(500000));
        }

        [Test]
        public void GiftsFromAllSolventOpponentsAreTransferredImmediatelyWhenTheCardIsDrawn()
        {
            var provider = new FakeProvider(new[] { 10 }, (s, choice) =>
                new CardEffectPlan(10, CardEffectKind.Gift, amount: 100000));
            var core = CreateCore(provider, playerCount: 3);
            ReachEventTile(core);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(500000));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(200000));
            Assert.That(core.State.GetPlayer(3).Cash, Is.EqualTo(200000));
            Assert.That(core.State.PendingObligations.Count, Is.Zero);
            var duplicate = new GameCommand(GameActionKind.CardChoice, 1, core.State.Revision,
                core.State.DecisionId, choiceId: 7);
            Assert.That(core.SubmitAction(duplicate).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(500000));
            Assert.That(core.State.PendingCardId, Is.Null);
            Assert.That(core.State.CardDrawOrdinal, Is.EqualTo(1));
        }

        [Test]
        public void OnlyStrategistCanPreviewAndOnlyActualLandingAdvancesTheDrawOrdinal()
        {
            var core = CreateCore(new FakeProvider(new[] { 1, 3 }, (s, c) =>
                new CardEffectPlan(1, CardEffectKind.BankCredit, amount: 300000)), RoleId.Strategist);
            Assert.That(core.QueryNextCard(1), Is.EqualTo(1));
            Assert.That(core.QueryNextCard(1), Is.EqualTo(1));
            Assert.That(core.QueryNextCard(2), Is.Null);
            Assert.That(core.State.CardDrawOrdinal, Is.Zero);
            ReachEventTile(core);
            Assert.That(core.State.PendingCardId, Is.EqualTo(1));
            Assert.That(core.State.CardDrawOrdinal, Is.EqualTo(1));
            Assert.That(core.QueryNextCard(1), Is.EqualTo(3));
            for (int i = 0; i < 3; i++) core.QueryLegalActions(1);
            Assert.That(core.State.PendingCardId, Is.EqualTo(1));
            Assert.That(core.State.CardDrawOrdinal, Is.EqualTo(1));
        }

        [Test]
        public void AnEventWithoutTheCardModuleCannotBeSkippedSilently()
        {
            var core = CreateCore(null);
            ReachEventTile(core);
            Assert.That(core.State.PendingRuleDecisionId, Is.EqualTo("C_CARD_MODULE/Q02"));
            Assert.That(core.QueryLegalActions(1).Count, Is.Zero);
            var command = new GameCommand(GameActionKind.SkipLanding, 1, core.State.Revision, core.State.DecisionId);
            Assert.That(core.SubmitAction(command).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void InvalidCardPlansAreFilteredAndForgedChoicesDoNotMutateTheState(int invalidKind)
        {
            var provider = new FakeProvider(new[] { 1 }, (s, c) => invalidKind == 0
                ? new CardEffectPlan(1, CardEffectKind.BankCredit, amount: 999999)
                : invalidKind == 1 ? new CardEffectPlan(1, CardEffectKind.Tax)
                : new CardEffectPlan(14, CardEffectKind.Gift, targetPlayerId: 2, amount: 200000));
            var core = CreateCore(provider);
            ReachEventTile(core);
            long revision = core.State.Revision;
            Assert.That(core.QueryLegalActions(1).Count, Is.Zero);
            var command = new GameCommand(GameActionKind.CardChoice, 1, revision, core.State.DecisionId, choiceId: 7);
            Assert.That(core.SubmitAction(command).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.Revision, Is.EqualTo(revision));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(300000));
            Assert.That(core.State.PendingObligation, Is.Null);
        }

        [Test]
        public void ExpiredClockStillAllowsCardTeleportAndItsDestinationPurchase()
        {
            // Q10=false là chính sách riêng cho fixture này, không tự duyệt luật sản phẩm.
            var core = CreateCore(new FakeProvider(new[] { 11 }, (s, c) =>
                new CardEffectPlan(11, CardEffectKind.GoTeleport)),
                decisions: new GameplayDecisions(cardMovementAwardsStartBonus: false), duration: MatchDuration.Minutes30);
            Submit(core, 1, GameActionKind.RollDice);
            Submit(core, 1, GameActionKind.ConfirmRoll);
            core.AdvanceClock(TimeSpan.FromMinutes(30));
            Assert.That(core.State.TimeExpired, Is.True);
            core.CompletePresentation(core.State.CurrentActionId);
            Submit(core, 1, GameActionKind.CardChoice);
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(24));
            core.CompletePresentation(core.State.CurrentActionId);
            Submit(core, 1, GameActionKind.Teleport, 25);
            core.CompletePresentation(core.State.CurrentActionId);
            Submit(core, 1, GameActionKind.BuyBeach, 25);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(10000));
            Assert.That(core.State.GetProperty(25).OwnerId, Is.EqualTo(1));
            Assert.That(core.State.GetProperty(25).BeachInvestment, Is.EqualTo(190000));
            Assert.That(core.State.Status, Is.EqualTo(MatchStatus.Finished));
            CollectionAssert.AreEqual(new[] { 2 }, core.State.WinnerPlayerIds);
        }

        private static MatchCoordinator CreateCore(ICardActionProvider provider, RoleId role = RoleId.None,
            int playerCount = 2, GameplayDecisions decisions = null, MatchDuration duration = MatchDuration.Unlimited)
        {
            var players = new List<PlayerState> { new PlayerState(1, 0, "A", role) { Position = 9 }, new PlayerState(2, 1, "B") };
            if (playerCount == 3) players.Add(new PlayerState(3, 2, "C"));
            var state = new GameState(players, 1, new[] { 5, 6, 7 }, duration, decisions);
            return new MatchCoordinator(state, new FixedDice(), provider);
        }

        private static void ReachEventTile(MatchCoordinator core)
        {
            Submit(core, 1, GameActionKind.RollDice);
            Submit(core, 1, GameActionKind.ConfirmRoll);
            ActionResult completed = core.CompletePresentation(core.State.CurrentActionId);
            Assert.That(completed.Status, Is.Not.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(12));
        }

        private static LegalAction Find(MatchCoordinator core, int actorId, GameActionKind kind, int? target = null)
        {
            foreach (LegalAction action in core.QueryLegalActions(actorId))
                if (action.Kind == kind && (!target.HasValue || action.TargetTileId == target)) return action;
            Assert.Fail("Không có lựa chọn hợp lệ: " + kind + " cho actor " + actorId);
            return null;
        }

        private static void Submit(MatchCoordinator core, int actorId, GameActionKind kind, int? target = null)
        {
            ActionResult result = core.SubmitAction(Find(core, actorId, kind, target).ToCommand());
            Assert.That(result.Status, Is.Not.EqualTo(ActionStatus.Rejected), result.Message);
        }

        private static void OwnCity(MatchCoordinator core, int ownerId, int tileId)
        {
            PropertyState property = core.State.GetProperty(tileId);
            property.OwnerId = ownerId;
            property.Level = PropertyLevel.House3;
            property.MutableInvestedByLevel[0] = 40000;
            for (int i = 1; i < 4; i++) property.MutableInvestedByLevel[i] = 20000;
        }

        private sealed class FixedDice : IDiceSource
        {
            public DiceRoll Roll() => new DiceRoll(1, 2);
        }

        private sealed class FakeProvider : ICardActionProvider
        {
            private readonly int[] _sequence;
            private readonly Func<GameSnapshot, int, CardEffectPlan> _plan;
            private readonly Func<GameSnapshot, IReadOnlyList<CardActionOption>> _options;

            internal FakeProvider(int[] sequence, Func<GameSnapshot, int, CardEffectPlan> plan,
                Func<GameSnapshot, IReadOnlyList<CardActionOption>> options = null)
            {
                _sequence = (int[])sequence.Clone();
                _plan = plan;
                _options = options;
            }

            public int? PeekNextCard(GameSnapshot snapshot) => snapshot.CardDrawOrdinal < _sequence.Length
                ? _sequence[(int)snapshot.CardDrawOrdinal] : (int?)null;

            public IReadOnlyList<CardActionOption> QueryOptions(GameSnapshot snapshot, int actorId) =>
                _options != null ? _options(snapshot) : Array.AsReadOnly(new[] { new CardActionOption(7, "Lựa chọn fixture") });

            public CardEffectPlan Plan(GameSnapshot snapshot, int actorId, int choiceId) => _plan(snapshot, choiceId);
        }
    }
}
