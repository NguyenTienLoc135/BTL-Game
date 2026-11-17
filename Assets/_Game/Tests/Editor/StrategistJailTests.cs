using System;
using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Core;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class StrategistJailTests
    {
        [Test]
        public void RerolledNonDoubleReplacesOriginalDoubleAndKeepsPlayerInJail()
        {
            var dice = new SequenceDice(new DiceRoll(2, 2), new DiceRoll(1, 4));
            var core = NewJailedCore(1, dice);
            Submit(core, GameActionKind.JailRoll);
            Assert.That(core.State.GetPlayer(1).IsJailed, Is.True, "Chưa xét đôi trước bước xác nhận.");
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(8));
            Submit(core, GameActionKind.RerollDice);
            ActionResult result = Submit(core, GameActionKind.ConfirmRoll);
            Assert.That(result.Movement, Is.Null);
            Assert.That(result.Snapshot.LastDiceRoll.Value.First, Is.EqualTo(1));
            Assert.That(result.Snapshot.LastDiceRoll.Value.Second, Is.EqualTo(4));
            Assert.That(result.Snapshot.LastDicePlayerId, Is.EqualTo(1));
            Assert.That(core.State.GetPlayer(1).IsJailed, Is.True);
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(8));
            Assert.That(core.State.GetPlayer(1).JailTurnsStarted, Is.EqualTo(1));
            Assert.That(core.State.CurrentPlayerId, Is.EqualTo(2));
            Assert.That(core.QueryLegalActions(1).Count, Is.Zero);
            Assert.That(core.State.PendingDoubleRoll, Is.False);
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.EqualTo(6));
            Assert.That(dice.Calls, Is.EqualTo(2));
        }

        [Test]
        public void RerolledDoubleReplacesOriginalNonDoubleAndMovesUsingOnlyTheNewDice()
        {
            var core = NewJailedCore(2, new SequenceDice(new DiceRoll(1, 4), new DiceRoll(1, 1)));
            Submit(core, GameActionKind.JailRoll);
            Submit(core, GameActionKind.RerollDice);
            ActionResult result = Submit(core, GameActionKind.ConfirmRoll);
            Assert.That(core.State.GetPlayer(1).IsJailed, Is.False);
            Assert.That(core.State.GetPlayer(1).JailTurnsStarted, Is.Zero);
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(10));
            Assert.That(result.Movement.Steps, Is.EqualTo(2));
            CollectionAssert.AreEqual(new[] { 9, 10 }, result.Movement.Path);
            Assert.That(core.State.PendingDoubleRoll, Is.True);
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.Moving));
            core.CompletePresentation(core.State.CurrentActionId);
            Submit(core, GameActionKind.SkipLanding);
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.AwaitingRoll));
            Assert.That(Find(core, GameActionKind.RollDice), Is.Not.Null);
            Assert.That(core.State.GetPlayer(1).CompletedPersonalTurns, Is.Zero);
        }

        [Test]
        public void FailedRerollOnThirdJailTurnCreatesForcedFeeAndReleaseWithoutMovement()
        {
            var core = NewJailedCore(3, new SequenceDice(new DiceRoll(2, 2), new DiceRoll(2, 3)),
                new GameplayDecisions(lawyerWaivesForcedJailFee: false));
            Submit(core, GameActionKind.JailRoll);
            Submit(core, GameActionKind.RerollDice);
            ActionResult result = Submit(core, GameActionKind.ConfirmRoll);
            Assert.That(result.Movement, Is.Null);
            Assert.That(core.State.PendingObligation.Kind, Is.EqualTo(ObligationKind.ForcedJailReleaseFee));
            Assert.That(core.State.PendingObligation.PayableAmount, Is.EqualTo(200000));
            Assert.That(core.State.GetPlayer(1).IsJailed, Is.True);
            Submit(core, GameActionKind.SettleObligation);
            Assert.That(core.State.GetPlayer(1).IsJailed, Is.False);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(100000));
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(8));
            Assert.That(core.State.CurrentPlayerId, Is.EqualTo(2));
            Assert.That(core.QueryLegalActions(1).Count, Is.Zero);
            Assert.That(Find(core, GameActionKind.RollDice), Is.Null);
        }

        [Test]
        public void RerollIsOfferedOnlyOnceAndNeitherJailPayNorCardCanReplaceConfirmedTrial()
        {
            var dice = new SequenceDice(new DiceRoll(1, 4), new DiceRoll(1, 1));
            var core = NewJailedCore(1, dice);
            core.State.GetPlayer(1).AddHeldCard(8);
            Submit(core, GameActionKind.JailRoll);
            Assert.That(Find(core, GameActionKind.JailPay), Is.Null);
            Assert.That(Find(core, GameActionKind.JailCard), Is.Null);
            Submit(core, GameActionKind.RerollDice);
            Assert.That(Find(core, GameActionKind.RerollDice), Is.Null);
            Assert.That(core.SubmitAction(new GameCommand(GameActionKind.RerollDice, 1,
                core.State.Revision, core.State.DecisionId)).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(dice.Calls, Is.EqualTo(2));
            Submit(core, GameActionKind.ConfirmRoll);
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(10));
            Assert.That(core.State.GetPlayer(1).HasHeldCard(8), Is.True);
        }

        [Test]
        public void CooldownStillRequiresConfirmingTheOriginalJailDiceWithoutAnotherRoll()
        {
            var dice = new SequenceDice(new DiceRoll(1, 1));
            var core = NewJailedCore(2, dice);
            core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns = 9;
            Submit(core, GameActionKind.JailRoll);
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.AwaitingRollConfirmation));
            Assert.That(Find(core, GameActionKind.RerollDice), Is.Null);
            Submit(core, GameActionKind.ConfirmRoll);
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(10));
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.EqualTo(9));
            Assert.That(dice.Calls, Is.EqualTo(1));
        }

        [Test]
        public void ThirdTurnRerolledDoubleAvoidsForcedFeeAndRetainsTheExtraRoll()
        {
            var core = NewJailedCore(3, new SequenceDice(new DiceRoll(1, 4), new DiceRoll(1, 1)));
            Submit(core, GameActionKind.JailRoll);
            Submit(core, GameActionKind.RerollDice);
            Submit(core, GameActionKind.ConfirmRoll);
            Assert.That(core.State.PendingObligation, Is.Null);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.GetPlayer(1).IsJailed, Is.False);
            Assert.That(core.State.PendingDoubleRoll, Is.True);
        }

        private static MatchCoordinator NewJailedCore(int jailTurn, IDiceSource dice, GameplayDecisions decisions = null)
        {
            var actor = new PlayerState(1, 0, "Nhà Chiến Lược", RoleId.Strategist)
            { Position = 8, IsJailed = true, JailTurnsStarted = jailTurn };
            var state = new GameState(new[] { actor, new PlayerState(2, 1, "B") }, 1,
                new[] { 27, 29, 31 }, decisions: decisions);
            state.Phase = TurnPhase.AwaitingJailChoice;
            return new MatchCoordinator(state, dice);
        }

        private static LegalAction Find(MatchCoordinator core, GameActionKind kind)
        {
            foreach (LegalAction action in core.QueryLegalActions(1)) if (action.Kind == kind) return action;
            return null;
        }

        private static ActionResult Submit(MatchCoordinator core, GameActionKind kind)
        {
            LegalAction action = Find(core, kind);
            Assert.That(action, Is.Not.Null, "Không có lựa chọn: " + kind);
            ActionResult result = core.SubmitAction(action.ToCommand());
            Assert.That(result.Status, Is.Not.EqualTo(ActionStatus.Rejected), result.Message);
            return result;
        }

        private sealed class SequenceDice : IDiceSource
        {
            private readonly Queue<DiceRoll> _rolls;
            public int Calls { get; private set; }
            public SequenceDice(params DiceRoll[] rolls) => _rolls = new Queue<DiceRoll>(rolls);
            public DiceRoll Roll() { Calls++; return _rolls.Dequeue(); }
        }
    }
}
