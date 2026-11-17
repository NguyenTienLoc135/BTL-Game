using System;
using BtlGame.AI;
using BtlGame.Board;
using BtlGame.Core;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class AiIntegrationTests
    {
        [Test]
        public void ControllerRequiresAnExistingPlayerAndCoordinator()
        {
            Assert.Throws<ArgumentNullException>(() => new AiController(null, 1));
            Assert.Throws<ArgumentException>(() => new AiController(NewCore(), 99));
        }

        [Test]
        public void OrdinaryAiTurnWaitsForTheActualPresentationCallback()
        {
            var core = NewCore();
            var ai = new AiController(core, 1);
            Assert.That(ai.Step().Status, Is.EqualTo(ActionStatus.Accepted));
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.AwaitingRollConfirmation));
            ActionResult movement = ai.Step();
            Assert.That(movement.Status, Is.EqualTo(ActionStatus.Pending));
            Assert.That(movement.Movement.ToTileId, Is.EqualTo(3));
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.Moving));
            long revision = core.State.Revision;
            Assert.That(ai.Step(), Is.Null);
            Assert.That(core.State.Revision, Is.EqualTo(revision));

            Assert.That(core.CompletePresentation(core.State.CurrentActionId).Status,
                Is.EqualTo(ActionStatus.Accepted));
            Assert.That(ai.Step().Status, Is.EqualTo(ActionStatus.Accepted)); // Mua bằng giá Core cấp.
            Assert.That(core.State.GetProperty(3).OwnerId, Is.EqualTo(1));
            ai.Step(); // Hoàn tất ô.
            ai.Step(); // Kết thúc lượt riêng.
            Assert.That(core.State.CurrentPlayerId, Is.EqualTo(2));
            Assert.That(core.State.GetPlayer(1).CompletedPersonalTurns, Is.EqualTo(1));
        }

        [Test]
        public void AiDoesNothingDuringAnotherPlayersTurn()
        {
            var core = NewCore();
            long revision = core.State.Revision;
            Assert.That(new AiController(core, 2).Step(), Is.Null);
            Assert.That(core.State.Revision, Is.EqualTo(revision));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(300_000));
        }

        [Test]
        public void ForgedAndStaleCommandsAreRejectedForHumanAndAiThroughTheSameGateway()
        {
            var human = NewCore();
            var machine = NewCore();
            foreach (MatchCoordinator core in new[] { human, machine })
            {
                GameSnapshot snapshot = core.QuerySnapshot();
                var forged = new GameCommand(GameActionKind.ForceBuyCity, 1,
                    snapshot.Revision, snapshot.DecisionId, targetTileId: 31);
                Assert.That(core.SubmitAction(forged).Status, Is.EqualTo(ActionStatus.Rejected));
                Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300_000));
                Assert.That(core.State.GetProperty(31).OwnerId, Is.Null);
            }
            LegalAction roll = Find(human, 1, GameActionKind.RollDice);
            human.SubmitAction(roll.ToCommand());
            Assert.That(human.SubmitAction(roll.ToCommand()).Status, Is.EqualTo(ActionStatus.Rejected));

            var policy = new HeuristicAi();
            GameCommand original = policy.Choose(machine.QuerySnapshot(), 1, machine.QueryLegalActions(1));
            Assert.That(machine.SubmitAction(original).Status, Is.EqualTo(ActionStatus.Accepted));
            Assert.That(machine.SubmitAction(original).Status, Is.EqualTo(ActionStatus.Rejected));
        }

        [Test]
        public void ZeroObligationSettlesWithoutConsumingHeldRentCardOrLawyerCooldown()
        {
            var core = NewCore(RoleId.Lawyer);
            core.State.GetPlayer(1).AddHeldCard(4);
            core.CreateObligation(ObligationKind.OtherMandatory, 1, 2, 0);
            ActionResult result = new AiController(core, 1).Step();
            Assert.That(result.Status, Is.EqualTo(ActionStatus.Accepted));
            Assert.That(core.State.PendingObligation, Is.Null);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(300_000));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(300_000));
            Assert.That(core.State.GetPlayer(1).HasHeldCard(4), Is.True);
            Assert.That(core.State.GetPlayer(1).ActiveReadyAfterCompletedTurns, Is.Zero);
        }

        [Test]
        public void BankerAiUltimateUsesCoreAndCannotBeAppliedTwice()
        {
            var core = NewCore(RoleId.Banker);
            var ai = new AiController(core, 1);
            Assert.That(ai.Step().Status, Is.EqualTo(ActionStatus.Accepted));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(500_000));
            Assert.That(core.State.GetPlayer(1).UltimateUsed, Is.True);
            ai.Step();
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(500_000));
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.AwaitingRollConfirmation));
        }

        [Test]
        public void InsolventAiIsRemovedAndTheSurvivingPlayersCanContinue()
        {
            var core = NewCore();
            core.State.GetPlayer(1).Cash = 10_000;
            core.CreateObligation(ObligationKind.Gift, 1, 2, 60_000);
            var ai = new AiController(core, 1);
            Assert.That(ai.Step().Status, Is.EqualTo(ActionStatus.Accepted));
            Assert.That(core.State.GetPlayer(1).Status, Is.EqualTo(PlayerStatus.Bankrupt));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(310_000));
            Assert.That(core.State.Status, Is.EqualTo(MatchStatus.Running));
            Assert.That(core.State.CurrentPlayerId, Is.EqualTo(2));
            Assert.That(ai.Step(), Is.Null);
            Assert.That(new AiController(core, 2).Step().Status, Is.EqualTo(ActionStatus.Accepted));
            Assert.That(core.State.Phase, Is.EqualTo(TurnPhase.AwaitingRollConfirmation));
        }

        [Test]
        public void AiCanSettleAnOutsideTurnObligationWithoutTakingOverTheTurn()
        {
            var core = NewCore();
            core.CreateObligation(ObligationKind.Gift, 2, 1, 100_000);
            Assert.That(new AiController(core, 2).Step().Status, Is.EqualTo(ActionStatus.Accepted));
            Assert.That(core.State.CurrentPlayerId, Is.EqualTo(1));
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(400_000));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(200_000));
            Assert.That(core.State.GetPlayer(2).CompletedPersonalTurns, Is.Zero);
        }

        private static MatchCoordinator NewCore(RoleId role = RoleId.None)
        {
            var state = new GameState(new[]
            {
                new PlayerState(1, 0, "AI", role), new PlayerState(2, 1, "B"), new PlayerState(3, 2, "C")
            }, 1, new[] { 26, 27, 29 });
            return new MatchCoordinator(state, new FixedDice());
        }

        private static LegalAction Find(MatchCoordinator core, int actor, GameActionKind kind)
        {
            foreach (LegalAction action in core.QueryLegalActions(actor))
                if (action.Kind == kind) return action;
            throw new InvalidOperationException("Thiếu legal action trong fixture: " + kind);
        }

        private sealed class FixedDice : IDiceSource
        {
            public DiceRoll Roll() => new DiceRoll(1, 2);
        }
    }
}
