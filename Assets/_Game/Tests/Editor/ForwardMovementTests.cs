using System;
using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Characters;
using BtlGame.Core;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class ForwardMovementTests
    {
        [Test]
        public void PaidTeleportCanTravelThirtyOneStepsAndRewardsCrossingStartExactlyOnce()
        {
            var core = NewCore(21);
            ReachDestination(core);
            ActionResult teleport = Submit(core, GameActionKind.Teleport, 23);
            Assert.That(teleport.Movement.Steps, Is.EqualTo(31));
            Assert.That(teleport.Movement.Source, Is.EqualTo(MovementSource.Teleport));
            Assert.That(teleport.Movement.Path.Count, Is.EqualTo(31));
            Assert.That(teleport.Movement.StartCrossings, Is.EqualTo(1));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(400000));
            long actionId = core.State.CurrentActionId;
            core.CompletePresentation(actionId);
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(23));
            Assert.That(core.CompletePresentation(actionId).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(400000));
        }

        [TestCase(7, 8)]
        [TestCase(11, 24)]
        public void MovementCardsFromTwentyEightFollowTheForwardPathAndAwardStart(int cardId, int target)
        {
            var core = NewCore(25, new MovementProvider(cardId));
            ReachDestination(core);
            ActionResult moved = Submit(core, GameActionKind.CardChoice);
            Assert.That(moved.Movement.StartCrossings, Is.EqualTo(1));
            Assert.That(moved.Movement.Source, Is.EqualTo(MovementSource.Card));
            Assert.That(moved.Movement.Steps, Is.EqualTo((target - 28 + 32) % 32));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(500000));
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(target));
            core.CompletePresentation(core.State.CurrentActionId);
            Assert.That(core.State.GetPlayer(1).IsJailed, Is.EqualTo(cardId == 7));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(500000));
        }

        [Test]
        public void ACardReturningToTeleportAllowsAnotherPaidTeleportInTheSameChain()
        {
            var core = NewCore(21, new MovementProvider(11));
            ReachDestination(core);
            Submit(core, GameActionKind.Teleport, 12);
            core.CompletePresentation(core.State.CurrentActionId);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(400000));
            Assert.That(core.State.PendingCardId, Is.EqualTo(11));
            Submit(core, GameActionKind.CardChoice);
            core.CompletePresentation(core.State.CurrentActionId);
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(24));
            Submit(core, GameActionKind.Teleport, 25);
            core.CompletePresentation(core.State.CurrentActionId);
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(25));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.CardDrawOrdinal, Is.EqualTo(1));
        }

        [Test]
        public void TeleportCannotTargetItsOwnServiceTile()
        {
            var core = NewCore(21);
            ReachDestination(core);
            foreach (LegalAction action in core.QueryLegalActions(1))
                if (action.Kind == GameActionKind.Teleport) Assert.That(action.TargetTileId, Is.Not.EqualTo(24));
            var forged = new GameCommand(GameActionKind.Teleport, 1, core.State.Revision, core.State.DecisionId, 24);
            Assert.That(core.SubmitAction(forged).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300000));
            Assert.That(core.State.GetPlayer(1).Position, Is.EqualTo(24));
        }

        [Test]
        public void StrategistUltimateStartsANewMovementRootBeforeEnteringTeleport()
        {
            var core = NewCore(21, role: RoleId.Strategist);
            core.State.TeleportUsedInCurrentChain = true;
            ActionResult ultimate = Submit(core, GameActionKind.UseUltimate, 24);
            Assert.That(ultimate.Movement.Steps, Is.EqualTo(3));
            Assert.That(core.State.TeleportUsedInCurrentChain, Is.False);
            Assert.That(core.State.PendingDoubleRoll, Is.False);
            core.CompletePresentation(core.State.CurrentActionId);
            Submit(core, GameActionKind.Teleport, 25);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(200000));
            Assert.That(core.State.GetPlayer(1).UltimateUsed, Is.True);
        }

        private static MatchCoordinator NewCore(int position, ICardActionProvider provider = null, RoleId role = RoleId.None)
        {
            var players = new[] { new PlayerState(1, 0, "A", role) { Position = position }, new PlayerState(2, 1, "B") };
            return new MatchCoordinator(new GameState(players, 1, new[] { 5, 6, 7 },
                decisions: GameplayDecisions.Approved), new FixedDice(), provider);
        }

        private static void ReachDestination(MatchCoordinator core)
        {
            Submit(core, GameActionKind.RollDice);
            Submit(core, GameActionKind.ConfirmRoll);
            Assert.That(core.CompletePresentation(core.State.CurrentActionId).Status, Is.Not.EqualTo(ActionStatus.Rejected));
        }

        private static ActionResult Submit(MatchCoordinator core, GameActionKind kind, int? tile = null)
        {
            foreach (LegalAction action in core.QueryLegalActions(1))
                if (action.Kind == kind && (!tile.HasValue || action.TargetTileId == tile))
                {
                    ActionResult result = core.SubmitAction(action.ToCommand());
                    Assert.That(result.Status, Is.Not.EqualTo(ActionStatus.Rejected), result.Message);
                    return result;
                }
            Assert.Fail("Không có lựa chọn " + kind + " tới " + tile);
            return null;
        }

        private sealed class FixedDice : IDiceSource
        {
            public DiceRoll Roll() => new DiceRoll(1, 2);
        }

        private sealed class MovementProvider : ICardActionProvider
        {
            private readonly int _card;
            internal MovementProvider(int card) => _card = card;
            public int? PeekNextCard(GameSnapshot snapshot) => _card;
            public IReadOnlyList<CardActionOption> QueryOptions(GameSnapshot snapshot, int actorId) =>
                Array.AsReadOnly(new[] { new CardActionOption(7, "Di chuyển theo thẻ fixture") });
            public CardEffectPlan Plan(GameSnapshot snapshot, int actorId, int choiceId) => new CardEffectPlan(_card,
                _card == 7 ? CardEffectKind.SendJail : CardEffectKind.GoTeleport);
        }
    }
}
