using System;
using System.Collections.Generic;
using BtlGame.Board;
using BtlGame.Core;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class GlobalFestivalTests
    {
        [Test]
        public void RepeatedVisitsToTheSameOwnedCityStackOnBaseRentAtFiftyPercentPerPayment()
        {
            var core = NewCore();
            Own(core, 1, 1);
            for (int visit = 1; visit <= 3; visit++)
            {
                Visit(core, 1, 13);
                Submit(core, 1, GameActionKind.OrganizeFestival, 1);
                Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(30000 + 15000 * visit));
                Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(2000000 - 200000 * visit));
                Assert.That(Festivals(core).Count, Is.EqualTo(1));
            }
        }

        [Test]
        public void SelectingAnotherOwnCityClearsAllOldStacksAndStartsAtFiftyPercent()
        {
            var core = NewCore();
            Own(core, 1, 1);
            Own(core, 1, 2);
            Organize(core, 1, 1);
            Organize(core, 1, 1);
            Organize(core, 1, 2);
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(30000));
            Assert.That(core.QuoteCityRent(2).FinalRent, Is.EqualTo(54000));
            Assert.That(Festivals(core).Count, Is.EqualTo(1));
            Assert.That(Festivals(core)[0].TargetTileId, Is.EqualTo(2));
        }

        [Test]
        public void AnotherOwnersFestivalReplacesThePreviousGlobalCityRatherThanAddingAnotherOne()
        {
            var core = NewCore();
            Own(core, 1, 1);
            Own(core, 2, 2);
            Organize(core, 1, 1);
            Organize(core, 1, 1);
            Organize(core, 2, 2);
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(30000));
            Assert.That(core.QuoteCityRent(2).FinalRent, Is.EqualTo(54000));
            Assert.That(Festivals(core).Count, Is.EqualTo(1));
            Assert.That(Festivals(core)[0].SourcePlayerId, Is.EqualTo(2));
        }

        [Test]
        public void OrganizedFestivalPersistsAcrossCompletedPersonalTurns()
        {
            var core = NewCore();
            Own(core, 1, 1);
            Organize(core, 1, 1);
            // Fixture tiến qua nhiều mốc domain; không giả lập thời hạn bằng animation.
            for (int boundary = 0; boundary < 8; boundary++)
            {
                core.State.Phase = TurnPhase.EndTurn;
                core.State.ClockPaused = false;
                core.CompletePersonalTurn();
            }
            Assert.That(core.State.GetPlayer(1).CompletedPersonalTurns, Is.EqualTo(4));
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(45000));
            Assert.That(Festivals(core).Count, Is.EqualTo(1));
            Assert.That(Festivals(core)[0].ExpiresAfterSourceTurnOrdinal, Is.Null);
            Assert.That(Festivals(core)[0].ExpiresBeforeSourceTurnOrdinal, Is.Null);
        }

        [Test]
        public void FixedFestivalsRemainIndependentWhenTheOrganizedFestivalMoves()
        {
            var core = NewCore(fixedFestivals: new[] { 1, 5, 6 });
            Own(core, 1, 1);
            Own(core, 1, 2);
            Organize(core, 1, 1);
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(60000));
            Organize(core, 1, 2);
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(45000));
            Assert.That(core.QuoteCityRent(2).FinalRent, Is.EqualTo(54000));
            CollectionAssert.AreEqual(new[] { 1, 5, 6 }, core.State.FixedFestivalCityIds);
        }

        [Test]
        public void SellingTheOrganizedCityRemovesItsEntireFestivalStack()
        {
            var core = NewCore();
            Own(core, 1, 1);
            Organize(core, 1, 1);
            Organize(core, 1, 1);
            Submit(core, 1, GameActionKind.SellProperty, 1);
            Assert.That(core.State.GetProperty(1).OwnerId, Is.Null);
            Assert.That(Festivals(core).Count, Is.Zero);
        }

        [Test]
        public void FestivalCardUsesTheSameGlobalStackAsThePaidBoardService()
        {
            var core = NewCore(new FestivalProvider());
            Own(core, 1, 1);
            Organize(core, 1, 1);
            Visit(core, 1, 9);
            Submit(core, 1, GameActionKind.CardChoice, 1);
            Assert.That(core.State.GetPlayer(1).Cash, Is.EqualTo(1600000));
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(60000));
            Assert.That(Festivals(core).Count, Is.EqualTo(1));
        }

        [Test]
        public void InsufficientFundsCannotCreateOrReplaceAnOrganizedFestival()
        {
            var core = NewCore();
            Own(core, 1, 1);
            Own(core, 2, 2);
            Organize(core, 1, 1);
            core.State.GetPlayer(2).Cash = 199999;
            Visit(core, 2, 13);
            var forged = new GameCommand(GameActionKind.OrganizeFestival, 2, core.State.Revision,
                core.State.DecisionId, targetTileId: 2);
            Assert.That(core.SubmitAction(forged).Status, Is.EqualTo(ActionStatus.Rejected));
            Assert.That(core.State.GetPlayer(2).Cash, Is.EqualTo(199999));
            Assert.That(core.QuoteCityRent(1).FinalRent, Is.EqualTo(45000));
            Assert.That(core.QuoteCityRent(2).FinalRent, Is.EqualTo(36000));
            Assert.That(Festivals(core).Count, Is.EqualTo(1));
        }

        private static MatchCoordinator NewCore(ICardActionProvider provider = null, int[] fixedFestivals = null)
        {
            var players = new[] { new PlayerState(1, 0, "A") { Cash = 2000000 }, new PlayerState(2, 1, "B") { Cash = 2000000 } };
            return new MatchCoordinator(new GameState(players, 1, fixedFestivals ?? new[] { 5, 6, 7 },
                decisions: GameplayDecisions.Approved), new FixedDice(), provider);
        }

        private static void Organize(MatchCoordinator core, int playerId, int target)
        {
            Visit(core, playerId, 13);
            Submit(core, playerId, GameActionKind.OrganizeFestival, target);
        }

        private static void Visit(MatchCoordinator core, int playerId, int from)
        {
            Assert.That(core.State.PendingObligations.Count, Is.Zero);
            core.State.CurrentPlayerId = playerId;
            core.State.GetPlayer(playerId).Position = from;
            core.State.Phase = TurnPhase.AwaitingRoll;
            core.State.ClockPaused = false;
            core.State.DecisionAllowsSkills = false;
            Submit(core, playerId, GameActionKind.RollDice);
            Submit(core, playerId, GameActionKind.ConfirmRoll);
            Assert.That(core.CompletePresentation(core.State.CurrentActionId).Status, Is.Not.EqualTo(ActionStatus.Rejected));
        }

        private static List<TurnEffectState> Festivals(MatchCoordinator core)
        {
            var result = new List<TurnEffectState>();
            foreach (TurnEffectState effect in core.State.ActiveEffects)
                if (effect.Kind == EffectKind.TemporaryFestival) result.Add(effect);
            return result;
        }

        private static void Submit(MatchCoordinator core, int actor, GameActionKind kind, int? target = null)
        {
            foreach (LegalAction action in core.QueryLegalActions(actor))
                if (action.Kind == kind && (!target.HasValue || action.TargetTileId == target))
                {
                    ActionResult result = core.SubmitAction(action.ToCommand());
                    Assert.That(result.Status, Is.Not.EqualTo(ActionStatus.Rejected), result.Message);
                    return;
                }
            Assert.Fail("Thiếu lựa chọn " + kind + " cho " + actor);
        }

        private static void Own(MatchCoordinator core, int owner, int tile)
        {
            PropertyState property = core.State.GetProperty(tile);
            property.OwnerId = owner;
            property.Level = PropertyLevel.House3;
            property.MutableInvestedByLevel[0] = 40000;
            for (int i = 1; i < 4; i++) property.MutableInvestedByLevel[i] = 20000;
        }

        private sealed class FixedDice : IDiceSource
        {
            public DiceRoll Roll() => new DiceRoll(1, 2);
        }

        private sealed class FestivalProvider : ICardActionProvider
        {
            public int? PeekNextCard(GameSnapshot snapshot) => 12;
            public IReadOnlyList<CardActionOption> QueryOptions(GameSnapshot snapshot, int actorId) =>
                Array.AsReadOnly(new[] { new CardActionOption(7, "Lễ hội trên Hà Nội", targetTileId: 1) });
            public CardEffectPlan Plan(GameSnapshot snapshot, int actorId, int choiceId) =>
                new CardEffectPlan(12, CardEffectKind.Festival, targetTileId: 1, amount: 200000);
        }
    }
}
