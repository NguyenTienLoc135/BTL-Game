using System;
using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Core;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class GiftBatchTests
    {
        [Test]
        public void EverySolventOpponentTransfersImmediatelyWhenTheCardIsResolved()
        {
            var core = NewCore(120000, 100000, 300000);
            DrawAndResolve(core);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(600000));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(20000));
            Assert.That(core.State.GetPlayer(3).Cash, Is.Zero);
            Assert.That(core.State.GetPlayer(4).Cash, Is.EqualTo(200000));
            Assert.That(core.State.PendingObligations.Count, Is.Zero);
            Assert.That(core.State.CardDrawOrdinal, Is.EqualTo(1));
            Assert.That(core.State.GetPlayer(3).Status, Is.EqualTo(PlayerStatus.Active));
        }

        [Test]
        public void MixedBatchCreditsSolventPlayersAndCreatesOneDebtForEachShortPlayer()
        {
            var core = NewCore(100000, 50000, 80000);
            DrawAndResolve(core);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(400000));
            Assert.That(core.State.GetPlayer(2).Cash, Is.Zero);
            Assert.That(core.State.GetPlayer(3).Cash, Is.EqualTo(50000));
            Assert.That(core.State.GetPlayer(4).Cash, Is.EqualTo(80000));
            Assert.That(core.State.PendingObligations.Count, Is.EqualTo(2));
            var ids = new HashSet<long>();
            var payers = new HashSet<int>();
            foreach (MandatoryObligation obligation in core.State.PendingObligations)
            {
                Assert.That(ids.Add(obligation.Id), Is.True);
                Assert.That(payers.Add(obligation.PayerId), Is.True);
                Assert.That(obligation.AssessedAmount, Is.EqualTo(100000));
                Assert.That(obligation.PayableAmount, Is.EqualTo(100000));
                Assert.That(obligation.RecipientId, Is.EqualTo(1));
            }
            CollectionAssert.AreEquivalent(new[] { 3, 4 }, payers);
        }

        [Test]
        public void CardRecipientCannotAdvanceTheTurnWhileAnyOpponentStillOwesMoney()
        {
            var core = NewCore(100000, 50000, 80000);
            DrawAndResolve(core);
            Assert.That(core.QueryLegalActions(1).Count, Is.Zero);
            foreach (GameActionKind kind in new[] { GameActionKind.SkipLanding, GameActionKind.EndTurn, GameActionKind.RollDice })
            {
                var command = new GameCommand(kind, 1, core.State.Revision, core.State.DecisionId);
                Assert.That(core.SubmitAction(command).Status, Is.EqualTo(ActionStatus.Rejected));
            }
            Assert.That(core.State.CurrentPlayerId, Is.EqualTo(1));
            Assert.That(core.State.CardDrawOrdinal, Is.EqualTo(1));
        }

        [Test]
        public void EachDebtorCanOnlyLiquidateTheirOwnPropertyForTheirOwnObligation()
        {
            var core = NewCore(100000, 50000, 80000);
            OwnCity(core, 3, 1, 100000);
            OwnCity(core, 4, 2, 120000);
            DrawAndResolve(core);
            Assert.That(HasAction(core, 3, GameActionKind.LiquidateProperty, 1), Is.True);
            Assert.That(HasAction(core, 3, GameActionKind.LiquidateProperty, 2), Is.False);
            Assert.That(core.QuerySnapshot(3).PendingObligation.PayerId, Is.EqualTo(3));
            Assert.That(core.QuerySnapshot(4).PendingObligation.PayerId, Is.EqualTo(4));
            var forged = new GameCommand(GameActionKind.LiquidateProperty, 3, core.State.Revision, core.State.DecisionId, 2);
            Assert.That(core.SubmitAction(forged).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetProperty(2).OwnerId, Is.EqualTo(4));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(400000));
        }

        [Test]
        public void LiquidationPaysOnlyTheSelectedDebtorAndDoesNotTouchTheOtherDebt()
        {
            var core = NewCore(100000, 50000, 80000);
            OwnCity(core, 3, 1, 100000);
            DrawAndResolve(core);
            Submit(core, 3, GameActionKind.LiquidateProperty, 1);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(500000));
            Assert.That(core.State.GetPlayer(3).Cash, Is.Zero);
            Assert.That(core.State.GetProperty(1).OwnerId, Is.Null);
            Assert.That(core.State.PendingObligations.Count, Is.EqualTo(1));
            Assert.That(core.QuerySnapshot(4).PendingObligation.PayerId, Is.EqualTo(4));
            Assert.That(core.QuerySnapshot(4).PendingObligation.PayableAmount, Is.EqualTo(100000));
            Assert.That(core.State.GetPlayer(4).Cash, Is.EqualTo(80000));
        }

        [Test]
        public void BankruptDebtorTransfersTheirRemainingCashWithoutCancellingOtherObligations()
        {
            var core = NewCore(100000, 50000, 80000);
            DrawAndResolve(core);
            Submit(core, 3, GameActionKind.DeclareBankruptcy);
            Assert.That(core.State.GetPlayer(3).Cash, Is.Zero);
            Assert.That(core.State.GetPlayer(3).Status, Is.EqualTo(PlayerStatus.Bankrupt));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(450000));
            Assert.That(core.State.PendingObligations.Count, Is.EqualTo(1));
            Assert.That(core.QuerySnapshot(4).PendingObligation.PayableAmount, Is.EqualTo(100000));
            Assert.That(core.State.Status, Is.EqualTo(MatchStatus.Running));
        }

        [Test]
        public void ReplayingTheBatchChoiceDoesNotTransferAgainOrCreateDuplicateDebts()
        {
            var core = NewCore(100000, 50000, 80000);
            Submit(core, 1, GameActionKind.RollDice);
            Submit(core, 1, GameActionKind.ConfirmRoll);
            var card = new GameCommand(GameActionKind.CardChoice, 1, core.State.Revision, core.State.DecisionId, choiceId: 7);
            core.CompletePresentation(core.State.CurrentActionId);
            long revision = core.State.Revision;
            Assert.That(core.SubmitAction(card).Status, Is.EqualTo(ActionStatus.Rejected));
            var refreshedForgery = new GameCommand(GameActionKind.CardChoice, 1, revision, core.State.DecisionId, choiceId: 7);
            Assert.That(core.SubmitAction(refreshedForgery).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.Revision, Is.EqualTo(revision));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(400000));
            Assert.That(core.State.PendingObligations.Count, Is.EqualTo(2));
            Assert.That(core.State.CardDrawOrdinal, Is.EqualTo(1));
        }

        [Test]
        public void APreviouslyReadDebtorSnapshotDoesNotChangeWhenAnotherDebtIsSettled()
        {
            var core = NewCore(100000, 50000, 80000);
            OwnCity(core, 3, 1, 100000);
            DrawAndResolve(core);
            GameSnapshot before = core.QuerySnapshot(3);
            long id = before.PendingObligation.Id;
            Submit(core, 3, GameActionKind.LiquidateProperty, 1);
            Assert.That(before.PendingObligation.Id, Is.EqualTo(id));
            Assert.That(before.PendingObligation.PayableAmount, Is.EqualTo(100000));
            foreach (PlayerSnapshot player in before.Players)
                if (player.Id == 1) Assert.That(player.Cash, Is.EqualTo(400000));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(500000));
            Assert.That(core.QuerySnapshot(4).PendingObligation.PayerId, Is.EqualTo(4));
        }

        [Test]
        public void ExpiredClockStillAllowsEveryDebtorToCompleteTheirMandatoryBatchPayment()
        {
            var core = NewCore(100000, 50000, 80000, MatchDuration.Minutes30);
            OwnCity(core, 3, 1, 100000);
            Submit(core, 1, GameActionKind.RollDice);
            Submit(core, 1, GameActionKind.ConfirmRoll);
            core.AdvanceClock(TimeSpan.FromMinutes(30));
            Assert.That(core.State.TimeExpired, Is.True);
            core.CompletePresentation(core.State.CurrentActionId);
            Submit(core, 3, GameActionKind.LiquidateProperty, 1);
            Submit(core, 4, GameActionKind.DeclareBankruptcy);
            Assert.That(core.State.PendingObligations.Count, Is.Zero);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(580000));
            Assert.That(core.State.GetPlayer(4).Status, Is.EqualTo(PlayerStatus.Bankrupt));
            Assert.That(core.State.CurrentPlayerId, Is.EqualTo(1));
            Assert.That(core.State.Status, Is.EqualTo(MatchStatus.Finished));
            CollectionAssert.AreEqual(new[] { 1 }, core.State.WinnerPlayerIds);
        }

        [Test]
        public void CompletionOfAllDebtorsUnblocksTheCardRecipientWithoutCreatingAnotherPersonalTurn()
        {
            var core = NewCore(100000, 50000, 80000);
            DrawAndResolve(core);
            Submit(core, 3, GameActionKind.DeclareBankruptcy);
            Submit(core, 4, GameActionKind.DeclareBankruptcy);
            Assert.That(core.State.PendingObligations.Count, Is.Zero);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(530000));
            Assert.That(core.State.GetPlayer(1).CompletedPersonalTurns, Is.Zero);
            Assert.That(core.State.GetPlayer(2).CompletedPersonalTurns, Is.Zero);
            Assert.That(HasAction(core, 1, GameActionKind.EndTurn), Is.True);
            Assert.That(core.State.CardDrawOrdinal, Is.EqualTo(1));
        }

        private static MatchCoordinator NewCore(long bCash, long cCash, long dCash,
            MatchDuration duration = MatchDuration.Unlimited)
        {
            var players = new[]
            {
                new PlayerState(1, 0, "A") { Position = 9 },
                new PlayerState(2, 1, "B") { Cash = bCash },
                new PlayerState(3, 2, "C") { Cash = cCash },
                new PlayerState(4, 3, "D") { Cash = dCash }
            };
            return new MatchCoordinator(new GameState(players, 1, new[] { 5, 6, 7 }, duration),
                new FixedDice(), new BatchProvider());
        }

        private static void DrawAndResolve(MatchCoordinator core)
        {
            ReachEvent(core);
        }

        private static void ReachEvent(MatchCoordinator core)
        {
            Submit(core, 1, GameActionKind.RollDice);
            Submit(core, 1, GameActionKind.ConfirmRoll);
            Assert.That(core.CompletePresentation(core.State.CurrentActionId).Status, Is.Not.EqualTo(ActionStatus.Rejected));
        }

        private static LegalAction Find(MatchCoordinator core, int actorId, GameActionKind kind, int? tile = null)
        {
            foreach (LegalAction action in core.QueryLegalActions(actorId))
                if (action.Kind == kind && (!tile.HasValue || action.TargetTileId == tile)) return action;
            Assert.Fail("Không có hành động " + kind + " cho actor " + actorId);
            return null;
        }

        private static bool HasAction(MatchCoordinator core, int actorId, GameActionKind kind, int? tile = null)
        {
            foreach (LegalAction action in core.QueryLegalActions(actorId))
                if (action.Kind == kind && (!tile.HasValue || action.TargetTileId == tile)) return true;
            return false;
        }

        private static void Submit(MatchCoordinator core, int actorId, GameActionKind kind, int? tile = null)
        {
            ActionResult result = core.SubmitAction(Find(core, actorId, kind, tile).ToCommand());
            Assert.That(result.Status, Is.Not.EqualTo(ActionStatus.Rejected), result.Message);
        }

        private static void OwnCity(MatchCoordinator core, int ownerId, int tileId, long investment)
        {
            PropertyState property = core.State.GetProperty(tileId);
            property.OwnerId = ownerId;
            property.Level = PropertyLevel.House3;
            property.MutableInvestedByLevel[0] = investment * 2 / 5;
            for (int i = 1; i < 4; i++) property.MutableInvestedByLevel[i] = investment / 5;
        }

        private sealed class FixedDice : IDiceSource
        {
            public DiceRoll Roll() => new DiceRoll(1, 2);
        }

        private sealed class BatchProvider : ICardActionProvider
        {
            public int? PeekNextCard(GameSnapshot snapshot) => snapshot.CardDrawOrdinal == 0 ? 10 : (int?)null;
            public IReadOnlyList<CardActionOption> QueryOptions(GameSnapshot snapshot, int actorId) =>
                Array.AsReadOnly(new[] { new CardActionOption(7, "Nhận quà từ toàn bộ đối thủ") });
            public CardEffectPlan Plan(GameSnapshot snapshot, int actorId, int choiceId) =>
                new CardEffectPlan(10, CardEffectKind.Gift, amount: 100000);
        }
    }
}
